using TowerDefense;
using UnityEngine;


public class TowerInteractionArea : MonoBehaviour
 {
     [SerializeField] private Tower tower;
     
     private void OnTriggerEnter2D(Collider2D other)
     {
         var othertower = other.GetComponent<Tower>();
 
         if (othertower == null)
             return;
 
         tower.EnterTowerRange(othertower);
     }
 
     private void OnTriggerExit2D(Collider2D other)
     {
         var othertower = other.GetComponent<Tower>();
 
         if (othertower == null)
             return;
 
         tower.ExitTowerRange(othertower);
     }
 }
