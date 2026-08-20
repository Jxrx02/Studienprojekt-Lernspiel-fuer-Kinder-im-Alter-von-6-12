using TowerDefense.GridMovement;
using UnityEngine;

namespace TowerDefense
{
    public class Wall : Tower
    {
        [Header("Wall Settings")]
        [SerializeField] private int buildCost = 25;

        [Header("Wall Visual")]
        [Tooltip("16 Sprites entsprechend der 4-Bit-Nachbarschaft.")]
        [SerializeField] private Sprite[] wallSprites = new Sprite[16];
        private Vector3Int wallCellPosition;
        
        [Header("Spike Settings")]
        [SerializeField] private int spikeDamage = 0;
        [SerializeField] private float spikeCooldown = 1f;
        [SerializeField] private bool hasSpikes = false;

        private float spikeTimer;

        private bool isBuilt = true;

        public bool IsBuilt => isBuilt;
        public int BuildCost => buildCost;

        // ───────────────── INIT ─────────────────

        protected override void Awake()
        {
            base.Awake();

            if (wallSprites == null || wallSprites.Length != 16)
            {
                Debug.LogError(
                    $"Wall '{name}': Es müssen genau 16 Wall-Sprites vorhanden sein!",
                    this
                );
            }
        }

        private void Start()
        {
            blocksPath = false;
            placementType = PlacementType.Wall;

            if (!isBuilt)
                return;

        }

        public void InitializeBuiltWall()
        {
            isBuilt = true;

            blocksPath = true;
            placementType = PlacementType.Wall;

            currentHealth = statHealthPoints;

            GridNode node =
                GridManager.Instance.GetNode(transform.position);

            if (node == null)
            {
                Debug.LogError(
                    $"Wall '{name}': Position {transform.position} liegt nicht auf dem Grid!",
                    this
                );

                isBuilt = false;
                return;
            }

            wallCellPosition = node.cell;

            GridManager.Instance.PlaceWall(transform.position);

            RefreshVisualsAroundWall();
        }

        private void Update()
        {
            if (!isBuilt)
                return;

            HandleSpikeDamage();
        }

        // ───────────────── BUILD ─────────────────

        public void Build()
        {
            if (isBuilt)
                return;

            if (LevelManager.instance.cur_coins < buildCost)
                return;

            if (!GridManager.Instance.CanPlaceWall(transform.position))
                return;

            LevelManager.instance.cur_coins -= buildCost;

            InitializeBuiltWall();

            TowerUI.Instance.UpdateUI();
        }

        // ───────────────── WALL VISUAL ─────────────────

        /// <summary>
        /// Berechnet anhand der vier direkten Nachbarn,
        /// welches Sprite für diese Wall verwendet werden soll.
        /// </summary>
        public void RefreshVisual()
        {
            if (!isBuilt)
                return;

            if (wallSprites == null || wallSprites.Length != 16)
                return;

            int mask = CalculateNeighbourMask();

            Sprite sprite = wallSprites[mask];

            //Debug.Log("Sprite:" + mask + " |" + wallSprites[mask]);
            if (sprite != null)
                sr.sprite = sprite;
        }

        /// <summary>
        /// Berechnet die 4-Bit-Nachbarschaft:
        ///
        /// Bit 0 = oben
        /// Bit 1 = rechts
        /// Bit 2 = unten
        /// Bit 3 = links
        ///
        /// Dadurch entstehen 16 mögliche Kombinationen.
        /// </summary>
        private int CalculateNeighbourMask()
        {
            int mask = 0;

            if (IsWallAtCell(wallCellPosition + Vector3Int.up))
                mask |= 1;

            if (IsWallAtCell(wallCellPosition + Vector3Int.right))
                mask |= 2;

            if (IsWallAtCell(wallCellPosition + Vector3Int.down))
                mask |= 4;

            if (IsWallAtCell(wallCellPosition + Vector3Int.left))
                mask |= 8;

            return mask;
        }
        /// <summary>
        /// Prüft, ob sich auf einer bestimmten Grid-Zelle
        /// eine gebaute Wall befindet.
        /// </summary>
        private bool IsWallAtCell(Vector3Int cell)
        {
            GridNode node = GridManager.Instance.GetNode(cell);

            if (node == null)
                return false;

            Vector3 worldPosition = node.worldPosition;

            Collider2D[] colliders =
                Physics2D.OverlapPointAll(worldPosition);

            foreach (Collider2D collider in colliders)
            {
                Wall wall = collider.GetComponent<Wall>();

                if (wall != null && wall.IsBuilt)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Aktualisiert diese Wall und alle direkten Nachbarn.
        /// </summary>
        private void RefreshVisualsAroundWall()
        {
            RefreshVisual();

            RefreshWallAtCell(wallCellPosition + Vector3Int.up);
            RefreshWallAtCell(wallCellPosition + Vector3Int.right);
            RefreshWallAtCell(wallCellPosition + Vector3Int.down);
            RefreshWallAtCell(wallCellPosition + Vector3Int.left);
        }

        private void RefreshWallAtCell(Vector3Int cell)
        {
            GridNode node =
                GridManager.Instance.GetNode(cell);

            if (node == null)
                return;

            Collider2D[] colliders =
                Physics2D.OverlapPointAll(node.worldPosition);

            foreach (Collider2D collider in colliders)
            {
                Wall wall = collider.GetComponent<Wall>();

                if (wall != null && wall.IsBuilt)
                {
                    wall.RefreshVisual();
                }
            }
        }

        // ───────────────── UPGRADES ─────────────────

        /// <summary>
        /// Wird vom Upgrade-System aufgerufen.
        /// Der RuleTile-Parameter bleibt hier absichtlich nicht mehr nötig,
        /// da die Visualisierung jetzt über wallSprites erfolgt.
        /// </summary>
        public void RefreshWallUpgradeVisual()
        {
            RefreshVisualsAroundWall();
        }

        // ───────────────── SPIKES ─────────────────

        private void HandleSpikeDamage()
        {
            if (!hasSpikes || spikeDamage <= 0)
                return;

            spikeTimer -= Time.deltaTime;

            if (spikeTimer > 0f)
                return;

            spikeTimer = spikeCooldown;

            DamageEnemiesOnWall();
        }

        private void DamageEnemiesOnWall()
        {
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(
                    transform.position,
                    0.5f
                );

            foreach (var hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();

                if (enemy != null)
                    enemy.TakeDamage(spikeDamage);
            }
        }

        // ───────────────── TOWER ─────────────────

        public override void Attack((GameObject, int) target)
        {
            // Walls greifen nicht aktiv an.
        }

        // ───────────────── DESTROY ─────────────────

        protected override void DestroyTower()
        {
            if (!isBuilt)
            {
                base.DestroyTower();
                return;
            }

            GridNode node =
                GridManager.Instance.GetNode(transform.position);

            Vector3Int cell = node != null
                ? node.cell
                : Vector3Int.zero;

            GridManager.Instance.RemoveWall(transform.position);

            // Wall selbst deaktivieren, damit sie beim
            // Neuberechnen nicht mehr als Nachbar erkannt wird.
            isBuilt = false;

            // Nachbarn müssen jetzt ihre Verbindung verlieren.
            RefreshWallAtCell(cell + Vector3Int.up);
            RefreshWallAtCell(cell + Vector3Int.right);
            RefreshWallAtCell(cell + Vector3Int.down);
            RefreshWallAtCell(cell + Vector3Int.left);

            base.DestroyTower();
        }
    }
}