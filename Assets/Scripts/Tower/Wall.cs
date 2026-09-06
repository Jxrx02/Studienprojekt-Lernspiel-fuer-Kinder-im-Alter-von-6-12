using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TowerDefense.GridMovement;

namespace TowerDefense
{
    public class Wall : Tower
    {
        [Header("Wall Settings")] [SerializeField]
        private int buildCost = 25;

        [Header("Wall Visual")] [Tooltip("16 Sprites entsprechend der 4-Bit-Nachbarschaft.")] [SerializeField]
        private Sprite[] wallSprites = new Sprite[16];

        [Tooltip("Material für eine noch nicht gebaute Wall.")] [SerializeField]
        private Material unbuiltMaterial;

        [Tooltip("Material für eine gebaute Wall.")] [SerializeField]
        private Material builtMaterial;

        [Header("Wall Segment")] [SerializeField]
        private int sortingOrder = 100;

        [Header("Spike Settings")] [SerializeField]
        private int spikeDamage = 0;

        [SerializeField] private float spikeCooldown = 1f;

        [SerializeField] private bool hasSpikes = false;

        private float spikeTimer;

        // Alle Zellen, auf denen diese WallGroup liegt.
        private readonly HashSet<Vector3Int> wallCells = new();

        // SpriteRenderer der einzelnen visuellen Segmente.
        private readonly Dictionary<
            Vector3Int,
            SpriteRenderer
        > wallRenderers = new();

        private bool isBuilt = false;

        public bool IsBuilt => isBuilt;

        public int BuildCost => buildCost;

        public IReadOnlyCollection<Vector3Int> WallCells =>
            wallCells;

        // =========================================================
        // INIT
        // =========================================================

        protected override void Awake()
        {
            base.Awake();

            placementType = PlacementType.Wall;

            if (wallSprites == null ||
                wallSprites.Length != 16)
            {
                Debug.LogError(
                    $"WallGroup '{name}': " +
                    "Es müssen genau 16 Wall-Sprites zugewiesen werden!",
                    this
                );
            }

            if (unbuiltMaterial == null)
            {
                Debug.LogError(
                    $"WallGroup '{name}': " +
                    "Kein Unbuilt Material zugewiesen!",
                    this
                );
            }

            if (builtMaterial == null)
            {
                Debug.LogError(
                    $"WallGroup '{name}': " +
                    "Kein Built Material zugewiesen!",
                    this
                );
            }
        }

        /// <summary>
        /// Erstellt die visuellen Wall-Segmente aus den
        /// Zellen der WallTilemap.
        ///
        /// Die Grid-Zellen werden NICHT hier blockiert.
        /// Das übernimmt der GridManager.
        /// </summary>
        public void Initialize(
            List<Vector3Int> cells,
            Tilemap groundTilemap)
        {
            if (cells == null || cells.Count == 0)
            {
                Debug.LogWarning(
                    $"WallGroup '{name}': Keine Wall-Zellen übergeben.",
                    this
                );

                return;
            }

            wallCells.Clear();
            wallRenderers.Clear();

            foreach (Vector3Int cell in cells)
            {
                if (wallCells.Contains(cell))
                    continue;

                wallCells.Add(cell);

                Vector3 worldPosition =
                    groundTilemap.GetCellCenterWorld(cell);

                Vector3 localPosition =
                    worldPosition - transform.position;

                CreateWallSegment(
                    cell,
                    localPosition
                );
                

            }

            // =========================================================
            // WALL IST NOCH NICHT GEBAUT
            // =========================================================

            isBuilt = false;

            blocksPath = false;

            placementType = PlacementType.Wall;

            // Ungebautes Material
            SetMaterial(unbuiltMaterial);

            // Wall-Sprites anhand der WallTilemap-Nachbarn
            RefreshVisual();
        }

        private void CreateWallSegment(
            Vector3Int cell,
            Vector3 localPosition)
        {
            GameObject segment =
                new GameObject(
                    $"WallSegment_{cell.x}_{cell.y}"
                );

            segment.transform.SetParent(transform);
            segment.transform.localPosition = localPosition;

            WallSegment wall = segment.AddComponent<WallSegment>();
            wall.Initialize(this, cell);

            SpriteRenderer renderer =
                segment.AddComponent<SpriteRenderer>();

            renderer.sortingOrder = sortingOrder;
            renderer.sortingLayerName = "Details";

            wallRenderers.Add(cell, renderer);

            BoxCollider2D col =
                segment.AddComponent<BoxCollider2D>();
            col.size = Vector2.one; 

            TowerHeroManager.instance.walls.Add(segment);
            //TowerHeroManager.instance.RegisterTower(wall);

        }
        public void RemoveWallSegment(Vector3Int cell)
        {
            if (!wallCells.Remove(cell))
                return;

            GridManager.Instance.RemoveWallGroup(
                new[] { cell }
            );

            wallRenderers.Remove(cell);
        }
        // =========================================================
        // BUILD
        // =========================================================

        /// <summary>
        /// Baut die komplette WallGroup.
        ///
        /// Die Grid-Zellen sind zu diesem Zeitpunkt bereits
        /// blockiert. Deshalb wird hier KEIN PlaceWall()
        /// mehr aufgerufen.
        /// </summary>
        public void Build()
        {
            if (isBuilt)
                return;


            if (LevelManager.instance == null)
            {
                Debug.LogError(
                    "WallGroup: LevelManager.instance ist null!",
                    this
                );

                return;
            }

            if (LevelManager.instance.cur_coins < buildCost)
            {
                Debug.Log(
                    $"Nicht genug Coins für WallGroup '{name}'. " +
                    $"Benötigt: {buildCost}"
                );

                return;
            }

            // =========================================================
            // PRÜFEN, OB ALLE ZELLEN BEBAUBAR SIND
            // =========================================================

            if (!GridManager.Instance.CanPlaceWallGroup(wallCells))
            {
                Debug.Log(
                    $"WallGroup '{name}' kann nicht gebaut werden."
                );

                return;
            }

            // =========================================================
            // BEZAHLEN
            // =========================================================

            LevelManager.instance.cur_coins -= buildCost;

            // =========================================================
            // GRID BLOCKIEREN
            // =========================================================

            GridManager.Instance.PlaceWallGroup(
                wallCells
            );

            // =========================================================
            // WALL ALS GEBAUT INITIALISIEREN
            // =========================================================

            
            InitializeBuiltWall();

            TowerUI.Instance.UpdateUI();

            Debug.Log(
                $"WallGroup '{name}' wurde gebaut."
            );
        }

        // =========================================================
        // INITIALIZE BUILT WALL
        // =========================================================

        /// <summary>
        /// Wechselt die WallGroup vom geplanten Zustand
        /// in den tatsächlich gebauten Zustand.
        ///
        /// WICHTIG:
        /// Das Grid wurde bereits beim Start durch
        /// GridManager.PlaceWall() blockiert.
        /// </summary>
        public void InitializeBuiltWall()
        {
            if (isBuilt)
                return;

            isBuilt = true;

            blocksPath = true;
            placementType = PlacementType.Wall;

            currentHealth = statHealthPoints;

            // Normales Wall-Material
            SetMaterial(
                builtMaterial
            );

            // Gebaute Wall-Sprites
            RefreshVisual();

            // Das Grid wurde bereits blockiert.
            // Wir müssen es hier nicht erneut ändern.
        }

        // =========================================================
        // MATERIAL
        // =========================================================

        private void SetMaterial(
            Material material)
        {
            if (material == null)
                return;

            foreach (
                SpriteRenderer renderer
                in wallRenderers.Values)
            {
                renderer.sharedMaterial =
                    material;
            }
        }

        public void SetUnbuiltVisual()
        {
            if (isBuilt)
                return;
            sr.enabled = false;
            SetMaterial(unbuiltMaterial);
        }
        // =========================================================
        // VISUAL
        // =========================================================

        public void RefreshVisual()
        {
            if (wallSprites == null ||
                wallSprites.Length != 16)
            {
                return;
            }

            foreach (
                KeyValuePair<
                    Vector3Int,
                    SpriteRenderer
                > pair
                in wallRenderers)
            {
                Vector3Int cell =
                    pair.Key;

                SpriteRenderer renderer =
                    pair.Value;

                int mask =
                    CalculateNeighbourMask(
                        cell
                    );

                Sprite sprite =
                    wallSprites[mask];

                if (sprite != null)
                {
                    renderer.sprite =
                        sprite;
                }
            }
        }

        // =========================================================
        // NEIGHBOURS
        // =========================================================

        /// <summary>
        /// Bit 0 = oben
        /// Bit 1 = rechts
        /// Bit 2 = unten
        /// Bit 3 = links
        /// </summary>
        private int CalculateNeighbourMask(
            Vector3Int cell)
        {
            int mask = 0;

            if (ContainsCell(
                    cell + Vector3Int.up))
            {
                mask |= 1;
            }

            if (ContainsCell(
                    cell + Vector3Int.right))
            {
                mask |= 2;
            }

            if (ContainsCell(
                    cell + Vector3Int.down))
            {
                mask |= 4;
            }

            if (ContainsCell(
                    cell + Vector3Int.left))
            {
                mask |= 8;
            }

            return mask;
        }

        private bool ContainsCell(
            Vector3Int cell)
        {
            return wallCells.Contains(cell);
        }

        // =========================================================
        // UPDATE
        // =========================================================

        private void Update()
        {
            if (!isBuilt)
                return;

            HandleSpikeDamage();
        }

        // =========================================================
        // SPIKES
        // =========================================================

        private void HandleSpikeDamage()
        {
            if (!hasSpikes ||
                spikeDamage <= 0)
            {
                return;
            }

            spikeTimer -=
                Time.deltaTime;

            if (spikeTimer > 0f)
                return;

            spikeTimer =
                spikeCooldown;

            DamageEnemiesOnWall();
        }

        private void DamageEnemiesOnWall()
        {
            foreach (Vector3Int cell in wallCells)
            {
                GridNode node =
                    GridManager.Instance.GetNode(
                        cell
                    );

                if (node == null)
                    continue;

                Collider2D[] hits =
                    Physics2D.OverlapCircleAll(
                        node.worldPosition,
                        0.5f
                    );

                foreach (Collider2D hit in hits)
                {
                    Enemy enemy =
                        hit.GetComponent<Enemy>();

                    if (enemy != null)
                    {
                        enemy.TakeDamage(
                            spikeDamage
                        );
                    }
                }
            }
        }

        // =========================================================
        // TOWER
        // =========================================================

        public override void Attack(
            (GameObject, int) target)
        {
            // Walls greifen nicht aktiv an.
        }

        // =========================================================
        // DESTROY
        // =========================================================

        protected override void DestroyTower()
        {
            if (wallCells.Count > 0)
            {
                GridManager.Instance.RemoveWallGroup(
                    wallCells
                );
            }

            isBuilt = false;

            base.DestroyTower();
        }
    }
}