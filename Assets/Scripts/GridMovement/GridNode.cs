using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.GridMovement
{
    public class GridNode
    {
        public Vector3Int cell;
        public Vector3 worldPosition;

        public bool walkable = true;
        public int movementCost = 1;

        // Wird einmal beim Erstellen berechnet
        public List<GridNode> neighbours = new();

        // A*
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;
        public GridNode parent;

        public GridNode(Vector3Int cell, Vector3 worldPosition)
        {
            this.cell = cell;
            this.worldPosition = worldPosition;
        }
    }
}