using System.Collections;
using System.Linq;
using TowerDefense;
using ScriptableObjects;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public WaveConfig waveConfig;
    public Transform spawnPoint;
    [HideInInspector]
    public int currentWaveIndex = 0;
    public GameObject enemyPrefab;

    public GameObject[] walkPath; // Path the enemy follows
    public Text txt_wave;

    void Start()
    {
        txt_wave.text = $"{currentWaveIndex+1}/{waveConfig.waves.Count}";

        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        for (currentWaveIndex = 0; currentWaveIndex < waveConfig.waves.Count; currentWaveIndex++)
        {
            WaveConfig.Wave currentWave = waveConfig.waves[currentWaveIndex];
            yield return StartCoroutine(SpawnBursts(currentWave));

            if (currentWaveIndex < waveConfig.waves.Count - 1)
            {
                yield return new WaitForSeconds(waveConfig.timeBetweenWaves);
            }
            txt_wave.text = $"{currentWaveIndex+1}/{waveConfig.waves.Count}";


        }

        Debug.Log("Alle Wellen sind gespawnt!");
        Actions.onWaveSpawnComplete.Invoke();
    }

    private IEnumerator SpawnBursts(WaveConfig.Wave wave)
    {
        foreach (var burst in wave.bursts)
        {
            yield return StartCoroutine(SpawnBurst(burst));
            yield return new WaitForSeconds(wave.timeBetweenBursts);
        }
    }

    private IEnumerator SpawnBurst(WaveConfig.Burst burst)
    {
        foreach (var burstConfig in burst.burstConfigs)
        {
            for (int i = 0; i < burstConfig.spawnCount; i++)
            {
                // Gegner instanziieren
                GameObject enemyInstance = Instantiate(
                    enemyPrefab,
                    spawnPoint.transform.position,  // Beispielhafte Spawn-Position
                    Quaternion.identity
                );
                
                Enemy enemy = enemyInstance.AddComponent<Enemy>();  
                enemy.walkPath = this.walkPath;
                enemy.enemyConfig = burstConfig.enemyConfig;

                TowerHeroManager.instance.RegisterEnemy(enemyInstance);
                
                // Wartezeit zwischen den Gegnern
                yield return new WaitForSeconds(burstConfig.spawnInterval);
            }
        }
    }



}