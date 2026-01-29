using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ScriptableObjects
{
    [System.Serializable]
    public class TowerUpgradeLevel
    {
        [Header("Basic Info")]
        public string upgradeName; 
        public string description; 
        
        [Header("Research")]
        public int researchCost;
        
        [Header("Base Stats Upgrade")]
        public int upgradeCost; // Kosten für das nächste Upgrade
        public int damageIncrease; // Schadenserhöhung
        public float rangeIncrease; // Reichweitenerhöhung
        public float attackSpeedIncrease; // Feuerratenerhöhung
        public float timeInBetweenShotsDecrease;// Feuerratenerhöhung

        [Header("Mine")] 
        public float coinsEarnedPerSecond;
        public float healthRegenPerSecond;

        [Header("Support")] 
        public float rangeMultiplier;
        public float dmgMultiplier;
        public float attackSpeedMultiplier;

        public float slowMultiplier;
        
        [Header("New Sprites")]
        public Sprite[] idle_sprites;
        public Sprite[] attack_sprites;
        public Sprite[] pathShopIcon;
        public GameObject projectile;
    }

    [System.Serializable]
    public class UpgradePath
    {
        public string towerName;
        public int researchCost;
        
        public TowerUpgradeLevel[] levels; // Upgrade-Stufen für diesen Pfad

    }
}