using System.Collections;
using TowerDefense;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public WaveConfig waveConfig;
    public Transform spawnPoint;
    public Transform target;

    [HideInInspector] public int currentWaveIndex = 0;
    public GameObject enemyPrefab;

    public Text txt_wave;

    private int _activeEnemyCount = 0;
    private bool _waveInProgress   = false;
    private bool _nextWaveReady     = false; // wartet auf StartNextWave()

    void Start()
    {
        Actions.onEnemyDeath        += OnEnemyRemoved;
        Actions.onEnemyReachedEnd   += OnEnemyRemoved;

        UpdateWaveText();
        StartCoroutine(RunWaves());
    }

    // Wird von LevelManager aufgerufen sobald das Spiel per Klick gestartet wird
    public void OnGameStarted()
    {
        _nextWaveReady = true;
    }

    private void OnDestroy()
    {
        Actions.onEnemyDeath        -= OnEnemyRemoved;
        Actions.onEnemyReachedEnd   -= OnEnemyRemoved;
    }

    // ── Hauptschleife ─────────────────────────────────────────────

    private IEnumerator RunWaves()
    {
        for (currentWaveIndex = 0; currentWaveIndex < waveConfig.waves.Count; currentWaveIndex++)
        {
            Debug.Log("Waiting for wave start");

            yield return new WaitUntil(() => _nextWaveReady);

            _nextWaveReady = false;

            Debug.Log($"Starting wave {currentWaveIndex + 1}");

            UpdateWaveText();

            _waveInProgress = true;
            _activeEnemyCount = 0;

            yield return StartCoroutine(SpawnWave(waveConfig.waves[currentWaveIndex]));

            yield return new WaitUntil(() => _activeEnemyCount <= 0);

            _waveInProgress = false;

            if (currentWaveIndex >= waveConfig.waves.Count - 1)
                Actions.onWaveSpawnComplete?.Invoke();
            else
                Actions.onWaveCleared?.Invoke();
        }
    }

    // ── Wird vom LevelManager aufgerufen ─────────────────────────

    /// <summary>
    /// Startet die nächste Welle – entweder durch Frühstart-Button
    /// oder automatisch wenn das Frühstart-Fenster abläuft.
    /// </summary>
    public void AllowNextWave()
    {
        Debug.Log("AllowNextWave");

        if (!_waveInProgress)
            _nextWaveReady = true;
    }

    // ── Spawn-Logik (unverändert) ─────────────────────────────────

    private IEnumerator SpawnWave(WaveConfig.Wave wave)
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
                GameObject enemyInstance = Instantiate(
                    enemyPrefab,
                    spawnPoint.transform.position,
                    Quaternion.identity
                );

                Enemy enemy = enemyInstance.GetComponent<Enemy>() 
                           ?? enemyInstance.AddComponent<Enemy>();
                enemy.enemyConfig = burstConfig.enemyConfig;
                enemy.SetTarget(target);

                TowerHeroManager.instance.RegisterEnemy(enemyInstance);
                _activeEnemyCount++;

                yield return new WaitForSeconds(burstConfig.spawnInterval);
            }
        }
    }

    // ── Gegner-Tracking ───────────────────────────────────────────

    private void OnEnemyRemoved(GameObject enemy)
    {
        _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
    }

    // ── UI ────────────────────────────────────────────────────────

    private void UpdateWaveText()
    {
        if (txt_wave != null)
            txt_wave.text = $"{currentWaveIndex + 1}/{waveConfig.waves.Count}";
    }
}