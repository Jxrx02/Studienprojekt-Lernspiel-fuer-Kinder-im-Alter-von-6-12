using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using TowerDefense.GridMovement;
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

        // ── Pathfinding ─────────────────────────────────────
        private List<Vector3> currentPath;
        private int pathIndex;
        private Transform target;

        // ── Visual / Animation ──────────────────────────────
        private SpriteAnim _spriteAnim;
        private SpriteRenderer _spriteRenderer;

        // ── Status Effects ───────────────────────────────────
        private float slowMultiplier = 1f;
        private Coroutine slowCoroutine;

        private bool isKnockbacking;
        private bool isBeingPulled;

        // ── AI Type Control ─────────────────────────────────
        private float repathTimer;
        private const float cleverRepathInterval = 1.5f;

        public bool IsBeingPulled
        {
            get => isBeingPulled;
            set => isBeingPulled = value;
        }

        // ───────────────── INIT ─────────────────────────────

        private void Start()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteAnim = GetComponent<SpriteAnim>();

            ApplyConfig();
            CalculatePath();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void ApplyConfig()
        {
            if (enemyConfig == null)
                return;

            // Light
            Light2D light = GetComponentInChildren<Light2D>();
            if (enemyConfig.hasLight)
            {
                light.color = enemyConfig.lightColor;
                light.gameObject.SetActive(true);
            }
            else
            {
                light.gameObject.SetActive(false);
            }

            // Animation
            if (_spriteAnim != null)
            {
                _spriteAnim.walk_sprites = enemyConfig.walkAnim;
                _spriteAnim.dead_sprites = enemyConfig.deadAnim;
                _spriteAnim.animState = AnimationState.Walk_Animation;
            }

            currentHealth = enemyConfig.health;
            isDead = false;

            Actions.onEnemySpawn?.Invoke(gameObject);
        }

        // ───────────────── PATH ─────────────────────────────

        private void CalculatePath()
        {
            if (target == null) return;

            currentPath = PathfindingManager.Instance.FindPath(
                transform.position,
                target.position
            );

            pathIndex = 0;
        }

        private void UpdatePathing()
        {
            if (enemyConfig == null) return;

            switch (enemyConfig.enemyType)
            {
                case EnemyType.Stubborn:
                    return;

                case EnemyType.Adaptive:
                    CalculatePath();
                    break;

                case EnemyType.Clever:
                    repathTimer += Time.deltaTime;
                    if (repathTimer >= cleverRepathInterval)
                    {
                        repathTimer = 0;
                        CalculatePath();
                    }
                    break;
            }
        }

        // ───────────────── UPDATE ───────────────────────────

        private void Update()
        {
            if (isDead || isKnockbacking || isBeingPulled)
                return;

            UpdatePathing();

            if (enemyConfig != null && enemyConfig.movementSpeed > 0)
                MoveAlongPath();
        }

        // ───────────────── MOVEMENT ─────────────────────────

        private void MoveAlongPath()
        {
            if (currentPath == null || pathIndex >= currentPath.Count)
                return;

            float speed = enemyConfig.movementSpeed * slowMultiplier;
            Vector3 targetPos = currentPath[pathIndex];

            transform.position = Vector2.MoveTowards(
                transform.position,
                targetPos,
                speed * Time.deltaTime
            );

            Rotate(targetPos);

            if (Vector2.Distance(transform.position, targetPos) < 0.1f)
            {
                pathIndex++;

                if (pathIndex >= currentPath.Count)
                    ReachEnd();
            }
        }

        private void ReachEnd()
        {
            Actions.onEnemyReachedEnd?.Invoke(gameObject);
            Destroy(gameObject);
        }

        // ───────────────── DAMAGE ───────────────────────────

        public void TakeDamage(int damage, string type = "physical")
        {
            if (isDead) return;

            float final = damage;

            if (type == "fire")
                final *= 1 - enemyConfig.fireResistance;
            else if (type == "ice")
                final *= 1 + enemyConfig.iceWeakness;
            else if (type == "poison" && enemyConfig.poisonImmunity > 0)
                final = 0;

            if (Random.Range(0f, 100f) < enemyConfig.evasionChance)
            {
                if (enemyConfig.evasionEffect != null)
                    Instantiate(enemyConfig.evasionEffect, transform.position, Quaternion.identity);
                return;
            }

            currentHealth -= Mathf.RoundToInt(final);

            if (enemyConfig.hitEffect != null)
                Instantiate(enemyConfig.hitEffect, transform.position, Quaternion.identity);

            if (currentHealth <= 0)
                Die();
        }

        private void Die()
        {
            if (isDead) return;

            isDead = true;

            if (enemyConfig.deathEffect != null)
                Instantiate(enemyConfig.deathEffect, transform.position, Quaternion.identity);

            Actions.onEnemyDeath?.Invoke(gameObject);

            if (_spriteAnim != null)
                _spriteAnim.TriggerDeadAnimation(true);
        }

        // ───────────────── STATUS EFFECTS ───────────────────

        public void ApplySlow(float amount, float duration)
        {
            if (slowCoroutine != null)
                StopCoroutine(slowCoroutine);

            if (duration <= 0)
            {
                slowMultiplier = 1f;
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

        public void ApplyPositionOffset(Vector3 offset, float duration)
        {
            if (!isDead)
                StartCoroutine(Knockback(offset, duration));
        }

        private IEnumerator Knockback(Vector3 offset, float duration)
        {
            isKnockbacking = true;

            Vector3 start = transform.position;
            Vector3 end = start + offset;

            float t = 0;

            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, t / duration);
                yield return null;
            }

            isKnockbacking = false;
        }

        // ───────────────── ROTATION ─────────────────────────

        private void Rotate(Vector3 targetPos)
        {
            Vector3 dir = targetPos - transform.position;

            transform.rotation = dir.x > 0
                ? Quaternion.identity
                : Quaternion.Euler(0, 180, 0);
        }

        private void OnDestroy()
        {
            Actions.onGridChanged -= OnGridChanged;
        }

        public void OnGridChanged()
        {
            if (enemyConfig != null &&
                enemyConfig.enemyType == EnemyType.Adaptive)
            {
                CalculatePath();
            }
        }
    }
}