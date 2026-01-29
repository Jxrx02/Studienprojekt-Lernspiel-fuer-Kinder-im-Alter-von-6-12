
using System;
using System.Collections;
using System.Collections.Generic;
using TowerDefense;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TowerShop : MonoBehaviour
{
    [FormerlySerializedAs("GameManager")] [Header("Tower UI")]
    public LevelManager levelManager; //TODO: Consider costs when buying

    private GameObject selectedTower;
    private bool isPlacingTower = false; // Flag, um zu prüfen, ob der Tower gerade platziert wird

    public void BuyTower(GameObject towerPrefab)
    {
        if (selectedTower != null) return;
        
        var tow = towerPrefab.GetComponent<Tower>();
        if (tow is Hero && LevelManager.instance.heroFielded) return;

        if (LevelManager.instance.DoPurchase(tow.towerInitPrice) == false)
        {
            selectedTower = null;
            isPlacingTower = false;
            return;
        }
            
        
        GameObject tower = Instantiate(towerPrefab);
        selectedTower = tower;
        isPlacingTower = true;
            
        tower.GetComponent<Collider2D>().enabled = false;
        
        //setzte isAttacking true, um Angriff zu meiden
        if (selectedTower.TryGetComponent(out Hero hero))
        {
            hero.isAttacking = true;
        }
    }
    
    private void PlaceTower()
    {
        if (selectedTower != null && isPlacingTower)
        {
            TowerHeroManager.instance.RegisterTower(selectedTower);

            selectedTower.GetComponent<Collider2D>().enabled = true;
            
            if (selectedTower.TryGetComponent(out Tower tower))
                tower.SetIsSelected(false);

            if (selectedTower.TryGetComponent(out Hero hero))
            {
                hero.SetIsSelected(false);
                hero.targetPosition = mousePosition;
                
                hero.isAttacking = false;
                
                levelManager.heroFielded = true;
            }
            
            selectedTower = null;
            isPlacingTower = false;
        }
    }

    private Vector3 mousePosition;
    private void Update()
    {
        if (isPlacingTower && selectedTower != null)
        {
            // Bewege den Tower zur Mausposition
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0; 
            selectedTower.transform.position = mousePosition;
            
            selectedTower.GetComponent<Tower>().SetIsSelected(true);

            // Prüfe, ob der Spieler den Tower platzieren möchte
            OneClickInWorldListener.ListenOnce((Vector3 pos) =>
            {
                PlaceTower();
            });
        }
    }
    
    

    
    
    
}
