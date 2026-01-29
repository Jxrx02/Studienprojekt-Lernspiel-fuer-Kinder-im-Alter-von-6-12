namespace TowerDefense
{
    using UnityEngine;

    public class LevelSelectorCamera : MonoBehaviour
    {
        public float dragSpeed = 1.0f;
        private Vector3 dragOrigin;

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                dragOrigin = Input.mousePosition;
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 difference = Input.mousePosition - dragOrigin;
                float verticalMovement = difference.y * dragSpeed * Time.deltaTime;

                transform.position += new Vector3(0, verticalMovement, 0);

                dragOrigin = Input.mousePosition;
            }
        }
    }
}