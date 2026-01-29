using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TowerDefense
{
    [RequireComponent(typeof(Tower))]
    public class OnTowerClickListener: MonoBehaviour
    {
        public Vector2 size = new Vector2(1.3f, 1.3f); // Größe des Colliders
        public Vector2 offset = new Vector2(0, .5f); // Größe des Colliders

        private Vector2 colliderCenter;

        private Tower tower;

        private void OnDrawGizmosSelected()
        {
            // Collider in der Szene zeichnen
            Gizmos.color = Color.green;
            colliderCenter = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y);
            Gizmos.DrawWireCube(colliderCenter, size);
        }

        private void Awake()
        {
            tower = GetComponent<Tower>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
               
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    // Wenn der Mauszeiger über einem UI-Element ist, tue nichts
                    return;
                }
                
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                
                if (IsPointInsideCollider(mouseWorldPos))
                {
                    Debug.Log($"Objekt {gameObject.name} wurde angeklickt!");
                    TowerHeroManager.instance.SelectTower(tower); 
                }

            }
        }

        private bool IsPointInsideCollider(Vector3 point)
        {
            colliderCenter = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y);
            Vector2 halfSize = size / 2;

            // Überprüfen, ob der Punkt innerhalb der Collider-Grenzen liegt
            return point.x >= colliderCenter.x - halfSize.x &&
                   point.x <= colliderCenter.x + halfSize.x &&
                   point.y >= colliderCenter.y - halfSize.y &&
                   point.y <= colliderCenter.y + halfSize.y;
        }
        
        
    }
}