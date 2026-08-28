using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TowerDefense;

namespace TowerDefense.GridMovement
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance;

        [Header("Tilemaps")]
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap obstacleTilemap;

        // Neu: Tilemap, die alle möglichen Wall-Platzierungen enthält (RuleTile / Tile)
        [SerializeField] private Tilemap wallTilemap;

        // Neu: Prefab, das beim Bau instanziert wird (muss Wall-Komponente enthalten)
        [Header("Wall Prefab")]
        [SerializeField] private GameObject wallPrefab;

        private Dictionary<Vector3Int, GridNode> nodes = new();

        private void Awake()
        {
            Instance = this;

            BuildGrid();
            CacheNeighbours();
        }

        // ───────────────── SNAP ─────────────────

        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            Vector3Int cell =
                groundTilemap.WorldToCell(worldPosition);

            return groundTilemap.GetCellCenterWorld(cell);
        }

        // ───────────────── WALL TILEMAP HELPERS ─────────────────

        // Prüft, ob an dieser Zelle eine Wall-Tile (potentieller Platz) liegt
        public bool IsWallTileAtCell(Vector3Int cell)
        {
            return wallTilemap != null && wallTilemap.HasTile(cell);
        }

        // Liefert alle Wall-Tile-Zellen innerhalb eines Radius um eine Weltposition
        public List<Vector3Int> GetWallTilesInRadius(Vector3 worldPosition, float radius)
        {
            List<Vector3Int> result = new();

            float radiusSqr = radius * radius;

            foreach (var node in nodes.Values)
            {
                if (!IsWallTileAtCell(node.cell))
                    continue;

                if ((node.worldPosition - worldPosition).sqrMagnitude <= radiusSqr)
                    result.Add(node.cell);
            }

            return result;
        }

        // Ermittelt die zusammenhängende Gruppe (4-Wege) startend bei startCell
        public List<Vector3Int> GetConnectedWallTileGroup(Vector3Int startCell)
        {
            List<Vector3Int> result = new();

            if (!IsWallTileAtCell(startCell))
                return result;

            Queue<Vector3Int> q = new();
            HashSet<Vector3Int> seen = new();

            q.Enqueue(startCell);
            seen.Add(startCell);

            Vector3Int[] dirs = new[]
            {
                Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left
            };

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                result.Add(cur);

                foreach (var d in dirs)
                {
                    var n = cur + d;
                    if (seen.Contains(n))
                        continue;

                    if (IsWallTileAtCell(n))
                    {
                        seen.Add(n);
                        q.Enqueue(n);
                    }
                }
            }

            return result;
        }

        // Prüft, ob an der Zelle bereits ein gebautes Wall-GameObject sitzt
        public bool IsBuiltWallAtCell(Vector3Int cell)
        {
            GridNode node = GetNode(cell);
            if (node == null)
                return false;

            Collider2D[] colliders = Physics2D.OverlapPointAll(node.worldPosition);

            foreach (var c in colliders)
            {
                Wall wall = c.GetComponent<Wall>();
                if (wall != null && wall.IsBuilt)
                    return true;
            }

            return false;
        }

        // Gibt die Anzahl gebauter Walls in einer Gruppe zurück
        public int CountBuiltWallsInGroup(List<Vector3Int> group)
        {
            int count = 0;
            foreach (var c in group)
                if (IsBuiltWallAtCell(c)) count++;
            return count;
        }

        // Baue Wall-GameObjects an allen angegebenen Zellen (instanziert wallPrefab),
        // returns instantiated Wall components (nur für weitergehende Logik)
        public List<Wall> BuildWallsAtCells(List<Vector3Int> cells)
        {
            List<Wall> built = new();

            if (wallPrefab == null)
            {
                Debug.LogError("GridManager: wallPrefab ist nicht gesetzt.");
                return built;
            }

            foreach (var cell in cells)
            {
                GridNode node = GetNode(cell);
                if (node == null)
                    continue;

                // Überspringe, falls bereits gebaut
                if (IsBuiltWallAtCell(cell))
                    continue;

                Vector3 pos = node.worldPosition;

                GameObject go = Instantiate(wallPrefab, pos, Quaternion.identity);
                Wall w = go.GetComponent<Wall>();
                if (w != null)
                {
                    // Stelle sicher, dass die Wall initialisiert wird
                    w.InitializeBuiltWall();
                    built.Add(w);
                }
                else
                {
                    Debug.LogWarning("Instanziertes Prefab hat keine Wall-Komponente.");
                }

                // Markiere Knoten als unbegehbar
                node.walkable = false;
            }

            NotifyGridChanged();

            return built;
        }

        // Option: Gesamtkosten für Gruppe (einfach: pro Tile * preis)
        public int GetGroupBuildCost(List<Vector3Int> group, int costPerTile)
        {
            return group.Count * costPerTile;
        }

        // ───────────────── WALL (legacy) ─────────────────
        // Bestehende PlaceWall/RemoveWall-Funktionen bleiben erhalten für Kompatibilität.

        /// <summary>
        /// Prüft, ob auf dieser Position grundsätzlich eine Wall
        /// gebaut werden kann.
        /// </summary>
        public bool CanPlaceWall(Vector3 worldPosition)
        {
            Vector3Int cell =
                groundTilemap.WorldToCell(worldPosition);

            if (!nodes.TryGetValue(cell, out GridNode node))
                return false;

            // Wall darf nur auf begehbarem Boden gebaut werden.
            return node.walkable;
        }

        /// <summary>
        /// Markiert das Feld als durch eine Wall blockiert.
        /// </summary>
        public bool PlaceWall(Vector3 worldPosition)
        {
            Vector3Int cell =
                groundTilemap.WorldToCell(worldPosition);

            if (!nodes.TryGetValue(cell, out GridNode node))
                return false;

            // Bereits blockiert
            if (!node.walkable)
                return false;

            node.walkable = false;

            NotifyGridChanged();

            return true;
        }

        /// <summary>
        /// Entfernt die Blockierung durch eine Wall.
        /// </summary>
        public bool RemoveWall(Vector3 worldPosition)
        {
            Vector3Int cell =
                groundTilemap.WorldToCell(worldPosition);

            if (!nodes.TryGetValue(cell, out GridNode node))
                return false;

            node.walkable = true;

            NotifyGridChanged();

            return true;
        }

        // ───────────────── GRID ─────────────────

        private void BuildGrid()
        {
            nodes.Clear();

            BoundsInt bounds = groundTilemap.cellBounds;

            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!groundTilemap.HasTile(cell))
                    continue;

                GridNode node = new GridNode(
                    cell,
                    groundTilemap.GetCellCenterWorld(cell)
                );

                node.walkable = !obstacleTilemap.HasTile(cell);

                nodes.Add(cell, node);
            }
        }

        private void CacheNeighbours()
        {
            foreach (GridNode node in nodes.Values)
            {
                node.neighbours.Clear();

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        Vector3Int neighbourCell =
                            node.cell +
                            new Vector3Int(x, y, 0);

                        if (nodes.TryGetValue(
                                neighbourCell,
                                out GridNode neighbour))
                        {
                            node.neighbours.Add(neighbour);
                        }
                    }
                }
            }
        }

        // ───────────────── NODE ACCESS ─────────────────

        public GridNode GetNode(Vector3 worldPosition)
        {
            Vector3Int cell =
                groundTilemap.WorldToCell(worldPosition);

            nodes.TryGetValue(
                cell,
                out GridNode node
            );

            return node;
        }

        public GridNode GetNode(Vector3Int cell)
        {
            nodes.TryGetValue(
                cell,
                out GridNode node
            );

            return node;
        }

        public IEnumerable<GridNode> GetAllNodes()
        {
            return nodes.Values;
        }

        // ───────────────── GRID UPDATE ─────────────────

        public void NotifyGridChanged()
        {
            foreach (var node in nodes.Values)
            {
                node.neighbours.Clear();
            }

            CacheNeighbours();

            Actions.onGridChanged?.Invoke();
        }
    }
}
