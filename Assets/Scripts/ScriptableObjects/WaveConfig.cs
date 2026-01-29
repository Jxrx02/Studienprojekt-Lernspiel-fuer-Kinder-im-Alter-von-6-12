using UnityEngine.Serialization;

namespace ScriptableObjects
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "WaveConfig", menuName = "ScriptableObjects/Wave System/WaveConfig", order = 1)]
    public class WaveConfig : ScriptableObject
    {    
        
        [System.Serializable]
        public class Burst
        {
            [SerializeReference]
            public List<BurstConfig> burstConfigs;
            [Tooltip("Zeit zwischen einzelnen Gegnern im Burst")]
            public float spawnInterval; 
        }


        [System.Serializable]
        public class Wave
        {
            public List<Burst> bursts;
            [Tooltip("Zeit zwischen zwei Bursts")]
            public float timeBetweenBursts;
        }

        public List<Wave> waves;
        [Tooltip("Zeit zwischen zwei Wellen")]
        public float timeBetweenWaves;
    }
}
