using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense.GridMovement
{
    public class PathfindingManager : MonoBehaviour
    {
        public static PathfindingManager Instance;

        private void Awake()
        {
            Instance = this;
        }

        public List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
        {
            GridNode startNode = GridManager.Instance.GetNode(startWorld);
            GridNode targetNode = GridManager.Instance.GetNode(targetWorld);

            if (startNode == null || targetNode == null)
                return null;

            List<GridNode> openList = new();
            HashSet<GridNode> closedList = new();

            foreach (GridNode node in GridManager.Instance.GetAllNodes())
            {
                node.gCost = int.MaxValue;
                node.hCost = 0;
                node.parent = null;
            }

            startNode.gCost = 0;
            startNode.hCost = GetDistance(startNode, targetNode);

            openList.Add(startNode);

            while (openList.Count > 0)
            {
                GridNode currentNode = openList[0];

                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].fCost < currentNode.fCost ||
                        (openList[i].fCost == currentNode.fCost &&
                         openList[i].hCost < currentNode.hCost))
                    {
                        currentNode = openList[i];
                    }
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode);

                if (currentNode == targetNode)
                    return RetracePath(startNode, targetNode);

                foreach (GridNode neighbour in currentNode.neighbours)
                {
                    if (!neighbour.walkable)
                        continue;

                    if (closedList.Contains(neighbour))
                        continue;

                    if (!CanMoveDiagonal(currentNode, neighbour))
                        continue;

                    int movementCost =
                        currentNode.gCost +
                        GetDistance(currentNode, neighbour) +
                        neighbour.movementCost;

                    if (movementCost < neighbour.gCost || !openList.Contains(neighbour))
                    {
                        neighbour.gCost = movementCost;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if (!openList.Contains(neighbour))
                            openList.Add(neighbour);
                    }
                }
            }

            return null;
        }

        private List<Vector3> RetracePath(GridNode start, GridNode end)
        {
            List<Vector3> path = new();

            GridNode current = end;

            while (current != start)
            {
                path.Add(current.worldPosition);
                current = current.parent;
            }

            path.Add(start.worldPosition);

            path.Reverse();

            return path;
        }

        private int GetDistance(GridNode a, GridNode b)
        {
            int dstX = Mathf.Abs(a.cell.x - b.cell.x);
            int dstY = Mathf.Abs(a.cell.y - b.cell.y);

            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);

            return 14 * dstX + 10 * (dstY - dstX);
        }

        private bool CanMoveDiagonal(GridNode current, GridNode neighbour)
        {
            int dx = neighbour.cell.x - current.cell.x;
            int dy = neighbour.cell.y - current.cell.y;

            // Keine diagonale Bewegung → immer erlaubt
            if (Mathf.Abs(dx) != 1 || Mathf.Abs(dy) != 1)
                return true;

            GridNode horizontal = GridManager.Instance.GetNode(
                current.cell + new Vector3Int(dx, 0, 0));

            GridNode vertical = GridManager.Instance.GetNode(
                current.cell + new Vector3Int(0, dy, 0));

            if (horizontal == null || vertical == null)
                return false;

            return horizontal.walkable && vertical.walkable;
        }
    }
}