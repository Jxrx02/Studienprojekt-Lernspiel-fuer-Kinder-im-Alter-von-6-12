using System;
using TowerDefense.GridMovement;
using UnityEngine;
namespace TowerDefense
{
    public class Wall : Tower
    {
        [Header("Wall Settings")]
        [SerializeField] private int spikeDamage = 0;
        [SerializeField] private float spikeCooldown = 1f;
        [SerializeField] private bool hasSpikes = false;

        private float spikeTimer;

        void Start()
        {

            // Wall blockiert immer den Weg
            blocksPath = true;
            placementType = PlacementType.Wall;
            
            currentHealth = statHealthPoints;
        }



        private void Update()
        {
            HandleSpikeDamage();
        }

        // ───────────────── SPIKE LOGIC ─────────────────

        private void HandleSpikeDamage()
        {
            if (!hasSpikes || spikeDamage <= 0) return;

            spikeTimer -= Time.deltaTime;

            if (spikeTimer > 0f) return;

            spikeTimer = spikeCooldown;

            DamageEnemiesOnWall();
        }

        private void DamageEnemiesOnWall()
        {
            // einfache Implementierung: alle Collider im Bereich
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f);

            foreach (var hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(spikeDamage);
                }
            }
        }

        // ───────────────── OVERRIDES ─────────────────

        public override void Attack((GameObject, int) target)
        {
            // bewusst leer: Wall darf niemals angreifen
        }

        protected override void DestroyTower()
        {
            GridManager.Instance.RemoveWall(this.transform.position);
            base.DestroyTower();
        }
    }
}