using System.Collections;
using System.Collections.Generic;
using TowerDefense;
using ScriptableObjects;
using TowerDefense.Wavesystem;
using UnityEngine;
using UnityEngine.UI;

public class WaveManager : MonoBehaviour
{
    public WaveConfig waveConfig;
    public SpawnPoint[] spawnPoints;
    public Transform target;
    
    [HideInInspector] public int currentWaveIndex = 0;
    public GameObject enemyPrefab;

    public Text txt_wave;

    [Header("Spawn Overview UI")]
    [SerializeField] private GameObject spawnOverviewCanvasPrefab; // World-Space Canvas Prefab (SpawnOverviewUI)
    [SerializeField] private GameObject edgeIndicatorPrefab;       // UI-Prefab (EdgeIndicatorUI)
    [SerializeField] private RectTransform overlayCanvasRect;      // Screen Space - Overlay Canvas
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float edgeIndicatorPadding = 50f;
    [SerializeField] private Vector3 overviewCanvasOffset = new Vector3(0f, 2f, 0f);

    private readonly Dictionary<SpawnPoint, GameObject> _activeOverviewCanvases = new();
    private readonly Dictionary<SpawnPoint, EdgeIndicatorUI> _activeEdgeIndicators = new();

    private List<PlannedBurst> plannedBursts = new();
    
    private int _activeEnemyCount = 0;
    private bool _waveInProgress   = false;
    private bool _nextWaveReady     = false; // wartet auf StartNextWave()

    void Start()
    {
        Actions.onEnemyDeath        += OnEnemyRemoved;
        Actions.onEnemyReachedEnd   += OnEnemyRemoved;

        if (mainCamera == null)
            mainCamera = Camera.main;

        UpdateWaveText();
        StartCoroutine(RunWaves());
    }

    private void Update()
    {
        UpdateSpawnOverviewVisibility();
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

            PlanWave(waveConfig.waves[currentWaveIndex]);

            Dictionary<SpawnPoint, Dictionary<string, int>> overview = BuildWaveOverview();
            PrintWaveOverview(overview);
            ShowSpawnOverviewUI(overview);
            
            yield return new WaitUntil(() => _nextWaveReady);

            _nextWaveReady = false;

            Debug.Log($"Starting wave {currentWaveIndex + 1}");

            UpdateWaveText();

            // Die Vorschau wird nicht mehr gebraucht, sobald die Welle tatsächlich startet
            ClearSpawnOverviewUI();

            _waveInProgress = true;
            _activeEnemyCount = 0;
            

            yield return StartCoroutine(SpawnWave());

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
    private int spawnerIndex = 0;

    private void PlanWave(WaveConfig.Wave wave)
    {
        plannedBursts.Clear();

        foreach (var burst in wave.bursts)
        {
            SpawnPoint spawner = spawnPoints[spawnerIndex];

            foreach (var config in burst.burstConfigs)
            {
                plannedBursts.Add(new PlannedBurst
                {
                    burstConfig = config,
                    spawnPoint = spawner
                });
                
                // Nächster Burst → nächster Spawnpunkt
                spawnerIndex = (spawnerIndex + 1) % spawnPoints.Length;
            }


        }
    }
    private IEnumerator SpawnWave()
    {
        SpawnPoint lastSpawnPoint = null;

        foreach (var plannedBurst in plannedBursts)
        {
            if (lastSpawnPoint != null)
            {
                // Zeit zwischen Bursts
                yield return new WaitForSeconds(
                    waveConfig.waves[currentWaveIndex].timeBetweenBursts
                );
            }

            yield return StartCoroutine(SpawnBurst(plannedBurst));

            lastSpawnPoint = plannedBurst.spawnPoint;
        }
    }

    private IEnumerator SpawnBurst(PlannedBurst plannedBurst)
    {
        BurstConfig burstConfig = plannedBurst.burstConfig;
        SpawnPoint spawnPoint = plannedBurst.spawnPoint;

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
            enemy.SetLevelEnd(target);

            TowerHeroManager.instance.RegisterEnemy(enemyInstance);

            _activeEnemyCount++;

            yield return new WaitForSeconds(burstConfig.spawnInterval);
        }
    }

    // ── Gegner-Tracking ───────────────────────────────────────────

    private void OnEnemyRemoved(GameObject enemy)
    {
        _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
    }

    // ── UI: Wave-Text ────────────────────────────────────────────

    private void UpdateWaveText()
    {
        if (txt_wave != null)
            txt_wave.text = $"{currentWaveIndex + 1}/{waveConfig.waves.Count}";
    }

    // ── UI: Overview / Debug ────────────────────────────────────

    private Dictionary<SpawnPoint, Dictionary<string, int>> BuildWaveOverview()
    {
        Dictionary<SpawnPoint, Dictionary<string, int>> overview = new();

        foreach (var plannedBurst in plannedBursts)
        {
            SpawnPoint spawner = plannedBurst.spawnPoint;
            BurstConfig config = plannedBurst.burstConfig;

            string enemyName = config.enemyConfig.name;

            if (!overview.ContainsKey(spawner))
                overview[spawner] = new Dictionary<string, int>();

            if (!overview[spawner].ContainsKey(enemyName))
                overview[spawner][enemyName] = 0;

            overview[spawner][enemyName] += config.spawnCount;
        }

        return overview;
    }

    private void PrintWaveOverview(Dictionary<SpawnPoint, Dictionary<string, int>> overview)
    {
        Debug.Log($"--- Wave {currentWaveIndex + 1} ---");

        foreach (var entry in overview)
        {
            string text = $"{entry.Key.spawnerName}: ";

            bool first = true;

            foreach (var enemy in entry.Value)
            {
                if (!first)
                    text += ", ";

                text += $"{enemy.Value}x {enemy.Key}";
                first = false;
            }

            Debug.Log(text);
        }
    }

    // ── UI: Spawn-Vorschau am Spawnpoint + Edge-Indikator ────────

    private void ShowSpawnOverviewUI(Dictionary<SpawnPoint, Dictionary<string, int>> overview)
    {
        ClearSpawnOverviewUI();

        if (spawnOverviewCanvasPrefab == null || overlayCanvasRect == null)
            return;

        foreach (var entry in overview)
        {
            SpawnPoint spawner = entry.Key;

            // Weltraum-Canvas über dem Spawnpoint
            GameObject canvasInstance = Instantiate(
                spawnOverviewCanvasPrefab,
                spawner.transform.position + overviewCanvasOffset,
                Quaternion.identity
            );

            SpawnOverviewUI overviewUI = canvasInstance.GetComponent<SpawnOverviewUI>();
            overviewUI?.Setup(entry.Value);

            _activeOverviewCanvases[spawner] = canvasInstance;

            // Passenden Edge-Indikator vorbereiten (zunächst deaktiviert)
            if (edgeIndicatorPrefab != null)
            {
                GameObject indicatorInstance = Instantiate(edgeIndicatorPrefab, overlayCanvasRect);
                indicatorInstance.SetActive(false);

                // GetComponentInChildren statt GetComponent, falls das Script
                // nicht auf dem Root-Objekt des Prefabs sitzt
                EdgeIndicatorUI indicatorUI = indicatorInstance.GetComponentInChildren<EdgeIndicatorUI>();

                if (indicatorUI == null)
                {
                    Debug.LogWarning($"[WaveManager] edgeIndicatorPrefab hat kein EdgeIndicatorUI-Script — Indicator für {spawner.spawnerName} wird ignoriert.", indicatorInstance);
                    Destroy(indicatorInstance);
                    continue; // ggf. als lokale Funktion/foreach anpassen
                }

                _activeEdgeIndicators[spawner] = indicatorUI;
            }
        }
    }

    private void ClearSpawnOverviewUI()
    {
        foreach (var canvas in _activeOverviewCanvases.Values)
        {
            if (canvas != null)
                Destroy(canvas);
        }
        _activeOverviewCanvases.Clear();

        foreach (var indicator in _activeEdgeIndicators.Values)
        {
            if (indicator != null)
                Destroy(indicator.gameObject);
        }
        _activeEdgeIndicators.Clear();
    }


    /// Prüft für jeden aktiven Spawnpoint, ob die Overview-Canvas noch (teilweise)
    /// im Kamerabild ist. Erst wenn ALLE Ecken der Canvas außerhalb des Viewports
    /// liegen, gilt sie als vollständig verlassen und wird deaktiviert.
    private void UpdateSpawnOverviewVisibility()
    {
        if (mainCamera == null || _activeOverviewCanvases.Count == 0)
            return;

        foreach (var entry in _activeOverviewCanvases)
        {
            SpawnPoint spawner = entry.Key;
            GameObject canvasInstance = entry.Value;

            if (canvasInstance == null)
                continue;

            Vector3 worldPos = spawner.transform.position + overviewCanvasOffset;

            bool isVisible = IsCanvasAtLeastPartiallyVisible(canvasInstance, mainCamera);

            canvasInstance.SetActive(isVisible);

            if (_activeEdgeIndicators.TryGetValue(spawner, out EdgeIndicatorUI indicator) && indicator != null)
            {
                indicator.gameObject.SetActive(!isVisible);

                if (!isVisible)
                    indicator.UpdatePosition(mainCamera, worldPos, overlayCanvasRect, edgeIndicatorPadding);
            }
        }
    }

    /// Prüft anhand der vier Weltraum-Ecken der Canvas, ob mindestens eine Ecke
    /// im sichtbaren Kamerabereich liegt (Viewport 0..1 und vor der Kamera).
    private bool IsCanvasAtLeastPartiallyVisible(GameObject canvasInstance, Camera cam)
    {
        RectTransform canvasRect = canvasInstance.GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            // Fallback: falls kein RectTransform vorhanden, alten Punkt-Check nutzen
            Vector3 viewportPos = cam.WorldToViewportPoint(canvasInstance.transform.position);
            return viewportPos.z > 0f
                   && viewportPos.x is >= 0f and <= 1f
                   && viewportPos.y is >= 0f and <= 1f;
        }

        Vector3[] worldCorners = new Vector3[4];
        canvasRect.GetWorldCorners(worldCorners);

        foreach (Vector3 corner in worldCorners)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(corner);

            bool cornerVisible = viewportPos.z > 0f
                                  && viewportPos.x is >= 0f and <= 1f
                                  && viewportPos.y is >= 0f and <= 1f;

            if (cornerVisible)
                return true; // eine sichtbare Ecke reicht, um NICHT zu deaktivieren
        }

        return false; // alle vier Ecken außerhalb → vollständig verlassen
    }
}