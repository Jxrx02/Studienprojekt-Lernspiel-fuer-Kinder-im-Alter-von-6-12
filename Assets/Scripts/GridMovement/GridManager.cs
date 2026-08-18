using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense.GridMovement
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance;

        [Header("Tilemaps")]
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap obstacleTilemap;
        [SerializeField] public Tilemap WallTilemap;

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
            Vector3Int cell = groundTilemap.WorldToCell(worldPosition);

            return groundTilemap.GetCellCenterWorld(cell);
        }

        // ───────────────── WALL ─────────────────

        /// <summary>
        /// Prüft, ob auf dieser Position grundsätzlich eine Wall
        /// gebaut werden kann.
        ///
        /// Wird für das neue BuildPoint-System normalerweise
        /// nicht mehr benötigt, kann aber für Validierung bleiben.
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
        
        public void RefreshWallVisuals(Vector3Int centerCell)
        {
            if (WallTilemap == null)
                return;

            // Zuerst die RuleTiles neu berechnen.
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int cell =
                        centerCell + new Vector3Int(x, y, 0);

                    WallTilemap.RefreshTile(cell);
                }
            }

            // Danach die SpriteRenderer der betroffenen Walls aktualisieren.
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int cell =
                        centerCell + new Vector3Int(x, y, 0);

                    RefreshWallVisual(cell);
                }
            }
        }
        private void RefreshWallVisual(Vector3Int cell)
        {
            Vector3 worldPosition =
                WallTilemap.GetCellCenterWorld(cell);

            Collider2D[] colliders =
                Physics2D.OverlapPointAll(worldPosition);

            foreach (Collider2D collider in colliders)
            {
                Wall wall = collider.GetComponent<Wall>();

                if (wall != null && wall.IsBuilt)
                {
                    wall.RefreshVisual();
                }
            }
        }
    }
}