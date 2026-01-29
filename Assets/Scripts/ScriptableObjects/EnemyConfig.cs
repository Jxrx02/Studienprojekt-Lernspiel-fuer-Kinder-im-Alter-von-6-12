using System;
using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Enemy", menuName = "ScriptableObjects/Enemy")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Basic Info")]
        public string description; // Description of the enemy
        public Sprite[] walkAnim; // Walk animation sprites
        public Sprite[] deadAnim; // Walk animation sprites

        [Header("Core Stats")]
        public int health; // Health points
        public int tenacity; // Resistance to crowd control effects (percentage reduction)
        public int movementSpeed; // Movement speed (units per second)
        public int xpDrop;
        
        [Header("Special Attributes")]
        public int evasionChance; // Chance to dodge attacks (percentage)
    
        [Header("Damage Interaction")]
        public float fireResistance; // Modifier for fire-based attacks (percentage reduction)
        public float iceWeakness; // Modifier for ice-based attacks (percentage increase)
        public float poisonImmunity; // Boolean-like value (0 = none, 1 = immune)
    
        [Header("Abilities")]
        public bool canFly; // If the enemy can fly

        [Header("Economic Impact")]
        public int goldReward; // Gold rewarded when killed
        public int penaltyOnReachingEnd; // Player's penalty if the enemy reaches the end (e.g., health or score loss)
    
        [Header("Visual Effects")]
        public Color lightColor;
        public Boolean hasLight;
        public GameObject deathEffect; // Effect shown on death
        public GameObject hitEffect; // Effect shown when hit
        public GameObject evasionEffect; // Effect shown when hit

    }
}