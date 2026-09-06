using TowerDefense;
using TowerDefense.GridMovement;
using UnityEngine;

namespace TowerDefense
{
    public class WallSegment : MonoBehaviour
    {
        private Wall wallGroup;
        private Vector3Int cell;

        public void Initialize(
            Wall wallGroup,
            Vector3Int cell)
        {
            this.wallGroup = wallGroup;
            this.cell = cell;
        }

        private void OnDestroy()
        {
            if (wallGroup == null)
                return;

            wallGroup.RemoveWallSegment(cell);
        }
        

    }
}