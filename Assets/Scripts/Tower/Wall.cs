using TowerDefense.GridMovement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TowerDefense
{
    public class Wall : Tower
    {
        [Header("Wall Settings")]
        [SerializeField] private int buildCost = 25;

        [Header("Wall Visual")]
        [SerializeField] private RuleTile baseWallTile;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Spike Settings")]
        [SerializeField] private int spikeDamage = 0;
        [SerializeField] private float spikeCooldown = 1f;
        [SerializeField] private bool hasSpikes = false;

        private float spikeTimer;

        private Tilemap wallTilemap;
        private Vector3Int wallCellPosition;
        private RuleTile currentWallTile;

        private bool isBuilt = false;

        public bool IsBuilt => isBuilt;
        public int BuildCost => buildCost;

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            blocksPath = false;
            placementType = PlacementType.Wall;

            if (!isBuilt)
                return;

            InitializeBuiltWall();
        }

        public void InitializeBuiltWall()
        {
            isBuilt = true;

            blocksPath = true;
            placementType = PlacementType.Wall;

            currentHealth = statHealthPoints;

            wallTilemap = GridManager.Instance.WallTilemap;

            wallCellPosition =
                wallTilemap.WorldToCell(transform.position);

            currentWallTile = baseWallTile;

            UpdateWallTile();

            GridManager.Instance.PlaceWall(transform.position);
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

        // ───────────────── WALL TILE ─────────────────
        public void RefreshVisual()
        {
            if (wallTilemap == null || spriteRenderer == null)
                return;

            Sprite sprite = wallTilemap.GetSprite(wallCellPosition);

            if (sprite != null)
                spriteRenderer.sprite = sprite;
        }
        private void UpdateWallTile()
        {
            if (wallTilemap == null)
                return;

            wallTilemap.SetTile(
                wallCellPosition,
                currentWallTile
            );

            GridManager.Instance.RefreshWallVisuals(
                wallCellPosition
            );
        }
        
        public void ApplyWallTile(RuleTile newTile)
        {
            if (newTile == null)
                return;

            currentWallTile = newTile;

            UpdateWallTile();
        }

        private void RefreshNeighbourTiles()
        {
            if (wallTilemap == null)
                return;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int cell =
                        wallCellPosition +
                        new Vector3Int(x, y, 0);

                    wallTilemap.RefreshTile(cell);
                }
            }
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

        protected override void DestroyTower()
        {
            if (!isBuilt)
            {
                base.DestroyTower();
                return;
            }

            if (wallTilemap != null)
            {
                wallTilemap.SetTile(
                    wallCellPosition,
                    null
                );

                RefreshNeighbourTiles();
            }

            GridManager.Instance.RemoveWall(
                transform.position
            );

            base.DestroyTower();
        }
    }
}