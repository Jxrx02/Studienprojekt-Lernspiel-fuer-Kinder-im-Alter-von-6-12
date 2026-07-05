using System;
using System.Collections;
using Unity.VisualScripting;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace TowerDefense
{
    public class Enemy : MonoBehaviour
    {
        public EnemyConfig enemyConfig;

        public int currentHealth;
        private bool isDead;

        // Path
        private int waypointIndex = 0;
        public GameObject[] walkPath;
        private SpriteAnim _spriteAnim;

        // ── Status-Effekte ────────────────────────────────────────
        private float slowMultiplier = 1f;       // 1 = normal, 0.5 = halb so schnell
        private Coroutine slowCoroutine;

        private bool isKnockbacking = false;     // Wird von Projectile per Coroutine gesetzt

        private bool isBeingPulled = false;      // Black Hole zieht den Gegner
        public bool IsBeingPulled
        {
            get => isBeingPulled;
            set => isBeingPulled = value;
        }

        private void Start()
        {
            ApplyConfig();
        }

        private void ApplyConfig()
        {
            if (enemyConfig == null)
            {
                Debug.LogError("EnemyConfig ist nicht zugewiesen!", this);
                return;
            }

            Light2D light = gameObject.GetComponentInChildren<Light2D>();
            if (enemyConfig.hasLight)
            {
                light.color = enemyConfig.lightColor;
                light.gameObject.SetActive(true);
            }
            else
            {
                light.gameObject.SetActive(false);
            }

            _spriteAnim = GetComponent<SpriteAnim>();
            _spriteAnim.walk_sprites = enemyConfig.walkAnim;
            _spriteAnim.dead_sprites = enemyConfig.deadAnim;
            _spriteAnim.animState = AnimationState.Walk_Animation;

            currentHealth = enemyConfig.health;
            isDead = false;

            Actions.onEnemySpawn(this.gameObject);
        }

        // ── Schaden ───────────────────────────────────────────────

        /// <summary>
        /// Verarbeitet Schaden – jetzt mit int-Überladung für Rückwärtskompatibilität
        /// mit Projectile.cs (das int damage übergibt).
        /// </summary>
        public void TakeDamage(int damage, string damageType = "physical")
            => TakeDamage((float)damage, damageType);

        public void TakeDamage(float damage, string damageType = "physical")
        {
            if (isDead) return;

            float finalDamage = damage;

            if (damageType == "fire")
                finalDamage *= 1 - enemyConfig.fireResistance;
            else if (damageType == "ice")
                finalDamage *= 1 + enemyConfig.iceWeakness;
            else if (damageType == "poison" && enemyConfig.poisonImmunity > 0)
                finalDamage = 0;

            float evasionRoll = Random.Range(0f, 100f);
            if (evasionRoll < enemyConfig.evasionChance)
            {
                if (enemyConfig.evasionEffect != null)
                    Instantiate(enemyConfig.evasionEffect, transform.position, Quaternion.identity);
                Debug.Log($"{enemyConfig.name} ist ausgewichen");
                return;
            }

            finalDamage = Mathf.Max(finalDamage, 0);
            currentHealth -= Mathf.RoundToInt(finalDamage);

            if (enemyConfig.hitEffect != null)
                Instantiate(enemyConfig.hitEffect, transform.position, Quaternion.identity);

            if (currentHealth <= 0)
                Die();
        }

        private void Die()
        {
            if (enemyConfig.deathEffect != null)
                Instantiate(enemyConfig.deathEffect, transform.position, Quaternion.identity);

            Actions.onEnemyDeath?.Invoke(this.gameObject);
            _spriteAnim.TriggerDeadAnimation(true);
            isDead = true;
        }

        // ── Status-Effekte ────────────────────────────────────────

        /// <summary>
        /// Verlangsamt den Gegner. amount=0.5 → halbierte Geschwindigkeit.
        /// amount=0 und duration=0 hebt den Slow auf (wird von Chain genutzt).
        /// </summary>
        public void ApplySlow(float amount, float duration)
        {
            if (slowCoroutine != null)
                StopCoroutine(slowCoroutine);

            if (duration <= 0f)
            {
                slowMultiplier = 1f; // Slow direkt aufheben
                return;
            }

            slowMultiplier = 1f - Mathf.Clamp01(amount);
            slowCoroutine = StartCoroutine(SlowTimer(duration));
        }

        private IEnumerator SlowTimer(float duration)
        {
            yield return new WaitForSeconds(duration);
            slowMultiplier = 1f;
        }

        /// <summary>
        /// Bewegt den Gegner um einen Offset (wird von Knockback genutzt).
        /// Deaktiviert kurz die normale Pfadbewegung.
        /// </summary>
        public void ApplyPositionOffset(Vector3 offset, float duration)
        {
            if (!isDead)
                StartCoroutine(KnockbackMove(offset, duration));
        }

        private IEnumerator KnockbackMove(Vector3 offset, float duration)
        {
            isKnockbacking = true;
            Vector3 start = transform.position;
            Vector3 dest  = transform.position + offset;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, dest, elapsed / duration);
                yield return null;
            }
            isKnockbacking = false;
        }

        // ── Update / Bewegung ─────────────────────────────────────

        void Update()
        {
            // Black Hole oder Knockback übernehmen kurzzeitig die Kontrolle
            if (isBeingPulled || isKnockbacking) return;

            if (!isDead && enemyConfig.movementSpeed > 0 && walkPath != null)
                MoveAlongPath();
        }

        private void MoveAlongPath()
        {
            float effectiveSpeed = enemyConfig.movementSpeed * slowMultiplier;

            transform.position = Vector2.MoveTowards(
                transform.position,
                walkPath[waypointIndex].transform.position,
                effectiveSpeed * Time.deltaTime);

            rotateToObject(walkPath[waypointIndex].transform.position);

            if (Vector2.Distance(transform.position, walkPath[waypointIndex].transform.position) < 0.1f)
            {
                if (waypointIndex < walkPath.Length - 1)
                    waypointIndex++;
                else
                {
                    Debug.Log($"{enemyConfig.description} hat Ziel erreicht und verursacht {enemyConfig.penaltyOnReachingEnd} Schaden.");
                    Actions.onEnemyReachedEnd(this.gameObject);
                    Destroy(this.gameObject);
                }
            }
        }

        // ── Rotation ──────────────────────────────────────────────

        public bool canRotateOnZAxis = false, faceToDir = true;

        void rotateToObject(Vector3 toObject)
        {
            if (faceToDir)
            {
                Vector3 dir = toObject - transform.position;
                transform.rotation = dir.x > 0
                    ? new Quaternion(0, 0, 0, 0)
                    : new Quaternion(0, 180, 0, 0);
            }
            else if (canRotateOnZAxis)
            {
                Vector3 dir = toObject - transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }
}