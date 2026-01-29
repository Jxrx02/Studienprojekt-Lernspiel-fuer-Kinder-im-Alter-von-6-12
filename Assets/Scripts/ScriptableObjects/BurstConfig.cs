using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{

    [CreateAssetMenu(fileName = "BurstConfig", menuName = "ScriptableObjects/Wave System/BurstConfig")]
    [System.Serializable]
    public class BurstConfig : ScriptableObject
    {
        [SerializeReference]
        public EnemyConfig enemyConfig; // Der Gegner-Typ
        [Tooltip("Zeit zwischen den Gegner-Spawns")]
        public float spawnInterval;
        [Tooltip("Anzahl der Gegner-Spawns")]
        public int spawnCount;
        
    }
}
