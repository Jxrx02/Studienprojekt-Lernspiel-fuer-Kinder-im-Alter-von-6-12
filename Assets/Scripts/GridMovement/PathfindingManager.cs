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

        // ─────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────
        public PathResult FindPath(Vector3 startWorld, Vector3 targetWorld)
        {
            GridNode startNode = GridManager.Instance.GetNode(startWorld);
            GridNode targetNode = GridManager.Instance.GetNode(targetWorld);

            if (startNode == null || targetNode == null)
                return null;

            // 1. Normaler Pfadversuch
            PathResult direct = FindPathInternal(startNode, targetNode);

            if (direct != null && direct.reachedTarget)
                return direct;

            // 2. Fallback: nächste Mauer suchen
            GameObject wall = FindClosestWall(startWorld);
            if (wall == null)
                return null;

            return FindBestPathAroundWall(startNode, wall);
        }

        // ─────────────────────────────────────────────
        // A* CORE
        // ─────────────────────────────────────────────
        private PathResult FindPathInternal(GridNode startNode, GridNode targetNode)
        {
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
                {
                    return new PathResult
                    {
                        path = RetracePath(startNode, targetNode),
                        targetNode = targetNode,
                        attackTarget = null,
                        reachedTarget = true
                    };
                }

                foreach (GridNode neighbour in currentNode.neighbours)
                {
                    if (!neighbour.walkable || closedList.Contains(neighbour))
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

        // ─────────────────────────────────────────────
        // WALL FALLBACK LOGIC
        // ─────────────────────────────────────────────
        private PathResult FindBestPathAroundWall(GridNode startNode, GameObject wall)
        {
            GridNode wallNode = GridManager.Instance.GetNode(wall.transform.position);

            if (wallNode == null)
                return null;

            List<GridNode> candidates = new();

            foreach (GridNode n in wallNode.neighbours)
            {
                if (n.walkable)
                    candidates.Add(n);
            }

            PathResult best = null;
            int bestCost = int.MaxValue;

            foreach (GridNode candidate in candidates)
            {
                PathResult result = FindPathInternal(startNode, candidate);

                if (result == null || result.path == null)
                    continue;

                int cost = result.path.Count;

                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = result;
                    best.attackTarget = wall;
                    best.reachedTarget = false;
                }
            }

            return best;
        }

        private GameObject FindClosestWall(Vector3 startWorld)
        {
            GameObject closest = null;
            float best = float.MaxValue;

            foreach (GameObject w in TowerHeroManager.instance.walls)
            {
                float d = (w.transform.position - startWorld).sqrMagnitude;

                if (d < best)
                {
                    best = d;
                    closest = w;
                }
            }

            return closest;
        }

        // ─────────────────────────────────────────────
        // PATH RECONSTRUCTION
        // ─────────────────────────────────────────────
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

        // ─────────────────────────────────────────────
        // DISTANCE
        // ─────────────────────────────────────────────
        private int GetDistance(GridNode a, GridNode b)
        {
            int dstX = Mathf.Abs(a.cell.x - b.cell.x);
            int dstY = Mathf.Abs(a.cell.y - b.cell.y);

            return (dstX > dstY)
                ? 14 * dstY + 10 * (dstX - dstY)
                : 14 * dstX + 10 * (dstY - dstX);
        }

        // ─────────────────────────────────────────────
        // DIAGONAL CHECK
        // ─────────────────────────────────────────────
        private bool CanMoveDiagonal(GridNode current, GridNode neighbour)
        {
            int dx = neighbour.cell.x - current.cell.x;
            int dy = neighbour.cell.y - current.cell.y;

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

    // ─────────────────────────────────────────────
    // RESULT TYPE
    // ─────────────────────────────────────────────
    public class PathResult
    {
        public List<Vector3> path;
        public GridNode targetNode;
        public GameObject attackTarget;
        public bool reachedTarget;
    }
}