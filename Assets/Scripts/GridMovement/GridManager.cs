using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense.GridMovement
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance;

        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private Tilemap obstacleTilemap;

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
        // ───────────────── WALL VALIDATION ─────────────────
        public bool CanPlaceWall(Vector3 worldPosition)
        {
            Vector3Int cell = groundTilemap.WorldToCell(worldPosition);

            if (!nodes.TryGetValue(cell, out GridNode node))
                return false;

            // Wall darf NUR auf Pfad (walkable = true) gebaut werden
            return node.walkable;
        }

        public void PlaceWall(Vector3 worldPosition)
        {
            Vector3Int cell = groundTilemap.WorldToCell(worldPosition);

            if (!nodes.TryGetValue(cell, out GridNode node))
                return;

            node.walkable = false;
            
            NotifyGridChanged();
        }

        public void RemoveWall(Vector3 worldPosition)
        {
            Vector3Int cell = groundTilemap.WorldToCell(worldPosition);

            if (!nodes.TryGetValue(cell, out GridNode node))
                return;

            node.walkable = true;

            NotifyGridChanged();
        }
        // ───────────────── EXISTING ─────────────────

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
                    groundTilemap.GetCellCenterWorld(cell));

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

                        Vector3Int neighbourCell = node.cell + new Vector3Int(x, y, 0);

                        if (nodes.TryGetValue(neighbourCell, out GridNode neighbour))
                            node.neighbours.Add(neighbour);
                    }
                }
            }
        }

        public GridNode GetNode(Vector3 worldPosition)
        {
            Vector3Int cell = groundTilemap.WorldToCell(worldPosition);
            nodes.TryGetValue(cell, out GridNode node);
            return node;
        }

        public GridNode GetNode(Vector3Int cell)
        {
            nodes.TryGetValue(cell, out GridNode node);
            return node;
        }
        

        public IEnumerable<GridNode> GetAllNodes()
        {
            return nodes.Values;
        }
        
        
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