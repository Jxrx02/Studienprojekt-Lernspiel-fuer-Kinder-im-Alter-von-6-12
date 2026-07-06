using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TowerDefense
{
    public enum ProjectileMode
    {
        Default,        //single shot
        AOE,
        DoT,
        ChainLightning, 
        Knockback,
        Splitter,
        Pierce,         
        BlackHole,
        Sticky,         //Semtex
        Chain           //Slow
    }

    public class Projectile : MonoBehaviour
    {
        [Header("Basis")] 
        public int baseDmg=1;
        public float rotationAngleOffset;
        public bool doRotation = true;
        public bool lookAtTarget = true;
        public bool hideTilCollision;
        [Tooltip("Anzahl der möglichen Bounces")] public int canFindNewTarget;
        public float speed;

        [Header("Projektil-Modus")]
        [SerializeField]
        private ProjectileMode mode = ProjectileMode.Default;

        public ProjectileMode Mode => mode;
        
        [Header("AOE")]
        [HideInInspector] public float aoeRadius = 1.5f;
        [HideInInspector] public GameObject aoeVfxPrefab;

        [Header("DoT (Damage over Time)")]
        [HideInInspector] public float dotDuration = 3f;
        [HideInInspector] public float dotInterval = 0.5f;
        [HideInInspector] public int dotDamagePerTick = 5;
        
        [Header("Chain Lightning")]
        [HideInInspector] public int chainCount = 3;
        [HideInInspector][Range(0f, 1f)] public float chainDamageFalloff = 0.2f;
        [HideInInspector] public float chainRadius = 3f;

        [Header("Knockback")]
        [HideInInspector] public float knockbackDistance = 1.5f;
        [HideInInspector] public float knockbackDuration = 0.2f;

        [Header("Splitter")]
        [HideInInspector] public int splitterCount = 5;
        [HideInInspector] public GameObject splitterPrefab;
        [HideInInspector] public int splitterDamage = 10;

        [Header("Pierce")]
        [HideInInspector] public int pierceCount = 5;

        [Header("Black Hole")]
        [HideInInspector] public float blackHolePullDuration = 2f;
        [HideInInspector] public float blackHoleRadius = 3f;
        [HideInInspector] public GameObject blackHoleVfxPrefab;

        [Header("Sticky")]
        [HideInInspector] public float stickyDelay = 2f;
        [HideInInspector] public float stickyAoeRadius = 1.5f;

        [Header("Chain (Verbindung)")]
        [HideInInspector] public float chainLinkRadius = 4f;
        [HideInInspector] public float chainSlowAmount = 0.4f;
        [HideInInspector] public int chainLinkDamage = 5;

        // ── Private ──────────────────────────────────────────────
        private int damage;
        private (GameObject, int) target;
        private int targetIndex;
        private bool projectileIsDead;
        private SpriteAnim anim;
        private int remainingPierce;

        public void Init((GameObject, int) target, int damage)
        {
            this.target = target;
            this.damage = baseDmg+damage;
            this.targetIndex = target.Item2;
            remainingPierce = pierceCount;
        }
        

        private void Start()
        {
            anim = GetComponent<SpriteAnim>();
            if (hideTilCollision)
                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);
        }

        private void UpdateLookDirection(Vector3 targetPos)
        {
            transform.rotation = targetPos.x < transform.position.x
                ? new Quaternion(0, 180, 0, 1)
                : new Quaternion(0, 0, 0, 1);
        }

        void Update()
        {
            if (projectileIsDead) return;

            if (target.Item1 != null)
            {
                if (lookAtTarget) UpdateLookDirection(target.Item1.transform.position);

                if (Vector3.Distance(transform.position, target.Item1.transform.position) < 0.3f)
                    OnProjectileHitTarget();
                else
                    MoveProjectileTowardsTarget();
            }
            else
            {
                if (canFindNewTarget > 0)
                {
                    try
                    {
                        var enemies = TowerHeroManager.instance.enemies;
                        if (enemies.Count > 0)
                        {
                            int newIndex = Mathf.Clamp(targetIndex - 1, 0, enemies.Count - 1);
                            target = (enemies[newIndex], newIndex);
                            targetIndex = newIndex;
                        }
                        else OnProjectileHitTarget();
                    }
                    catch { OnProjectileHitTarget(); }
                }
                else OnProjectileHitTarget();
            }
        }

        private void MoveProjectileTowardsTarget()
        {
            Vector3 dir = (target.Item1.transform.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;

            if (doRotation)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle + rotationAngleOffset, Vector3.forward);
            }
        }

        private void OnProjectileHitTarget()
        {
            if (hideTilCollision)
                foreach (Transform child in transform)
                    child.gameObject.SetActive(true);

            Vector3 hitPos = target.Item1 != null
                ? target.Item1.transform.position
                : transform.position;

            switch (Mode)
            {
                case ProjectileMode.Default:
                    HitSingle();
                    break;
                case ProjectileMode.AOE:
                    ApplyAoeDamage(hitPos, aoeRadius, damage, aoeVfxPrefab);
                    break;
                case ProjectileMode.DoT:
                    HitSingle();

                    if (target.Item1 != null)
                    {
                        StartCoroutine(ApplyDoT(target.Item1));
                        return; // Projektil lebt weiter
                    }
                    break;

                case ProjectileMode.ChainLightning:
                    if (target.Item1 != null)
                    {
                        StartCoroutine(ApplyChainLightning(target.Item1, damage, chainCount));
                        return;
                    }
                    break;
                case ProjectileMode.Knockback:
                    HitSingle();
                    if (target.Item1 != null)
                        StartCoroutine(ApplyKnockback(target.Item1, hitPos));
                    break;
                case ProjectileMode.Splitter:
                    HitSingle();
                    SpawnSplitter(hitPos);
                    break;
                case ProjectileMode.Pierce:
                    ApplyPierce(hitPos);
                    if (remainingPierce > 0) return; // noch nicht sterben
                    break;

                case ProjectileMode.BlackHole:
                    StartCoroutine(ApplyBlackHole(hitPos));
                    break;
                case ProjectileMode.Sticky:
                    if (target.Item1 != null)
                        StartCoroutine(ApplySticky(target.Item1));
                    return; // Projektil stirbt erst nach Delay
                case ProjectileMode.Chain:
                    HitSingle();
                    if (target.Item1 != null)
                        ApplyChainLink(target.Item1, hitPos);
                    break;
            }

            // Bounce-Logik (nur Default)
            if (Mode == ProjectileMode.Default && canFindNewTarget > 0)
            {
                canFindNewTarget--;
                target = (null, -1);
                return;
            }

            anim.TriggerDeadAnimation(true);
            projectileIsDead = true;
        }

        // ── Effekt-Implementierungen ──────────────────────────────

        private void HitSingle()
        {
            if (target.Item1 != null)
                target.Item1.GetComponent<Enemy>()?.TakeDamage(damage);
        }

        private void ApplyAoeDamage(Vector3 center, float radius, int dmg, GameObject vfx)
        {
            if (vfx != null) Instantiate(vfx, center, Quaternion.identity);
            foreach (var go in TowerHeroManager.instance.enemies)
            {
                if (go == null) continue;
                if (Vector3.Distance(center, go.transform.position) <= radius)
                    go.GetComponent<Enemy>()?.TakeDamage(dmg);
            }
        }

        private IEnumerator ApplyDoT(GameObject enemy)
        {
            float elapsed = 0f;
            float nextTick = 0f;

            while (elapsed < dotDuration)
            {
                if (enemy == null)
                    break;

                // Projektil am Gegner halten
                transform.position = enemy.transform.position;

                if (elapsed >= nextTick)
                {
                    enemy.GetComponent<Enemy>()?.TakeDamage(dotDamagePerTick);
                    nextTick += dotInterval;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            anim.TriggerDeadAnimation(true);
            projectileIsDead = true;
        }


        private IEnumerator ApplyChainLightning(GameObject first, int dmg, int remaining)
        {
            HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
            GameObject current = first;
            int currentDamage = dmg;

            while (current != null && remaining > 0)
            {
                current.GetComponent<Enemy>()?.TakeDamage(currentDamage);

                GameObject nearest = null;
                float minDist = chainRadius;

                foreach (var go in TowerHeroManager.instance.enemies)
                {
                    if (go == null || go == current || hitEnemies.Contains(go))
                        continue;

                    float dist = Vector3.Distance(current.transform.position, go.transform.position);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = go;
                    }
                }

                if (nearest == null)
                    break;

                yield return StartCoroutine(ShowLightning(current.transform.position,
                    nearest.transform.position));

                hitEnemies.Add(current);
                current = nearest;
                currentDamage = Mathf.RoundToInt(currentDamage * (1f - chainDamageFalloff));
                remaining--;
            }

            anim.TriggerDeadAnimation(true);
            projectileIsDead = true;
        }
        
        private IEnumerator ShowLightning(Vector3 start, Vector3 end)
        {
            GameObject go = new GameObject("Lightning");

            LineRenderer lr = go.AddComponent<LineRenderer>();

            lr.positionCount = 8;
            lr.startWidth = 0.08f;
            lr.endWidth = 0.08f;

            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.white;
            lr.endColor = new Color(0.6f, 1f, 1f);

            Vector3 dir = end - start;
            Vector3 normal = Vector3.Cross(dir.normalized, Vector3.forward);

            for (int i = 0; i < lr.positionCount; i++)
            {
                float t = i / (float)(lr.positionCount - 1);

                Vector3 p = Vector3.Lerp(start, end, t);

                if (i != 0 && i != lr.positionCount - 1)
                    p += normal * UnityEngine.Random.Range(-0.18f, 0.18f);

                lr.SetPosition(i, p);
            }

            yield return new WaitForSeconds(0.06f);

            Destroy(go);
        }
        private IEnumerator ApplyKnockback(GameObject enemy, Vector3 hitPos)
        {
            Vector3 dir = (enemy.transform.position - hitPos).normalized;
            Vector3 destination = enemy.transform.position + dir * knockbackDistance;
            float elapsed = 0f;
            Vector3 start = enemy.transform.position;

            while (elapsed < knockbackDuration && enemy != null)
            {
                elapsed += Time.deltaTime;
                enemy.transform.position = Vector3.Lerp(start, destination, elapsed / knockbackDuration);
                yield return null;
            }
        }

        private void SpawnSplitter(Vector3 hitPos)
        {
            if (splitterPrefab == null) return;
            float angleStep = 360f / splitterCount;
            var enemies = TowerHeroManager.instance.enemies;

            for (int i = 0; i < splitterCount; i++)
            {
                if (enemies.Count == 0) break;
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                // Nächsten Gegner in dieser Richtung suchen
                GameObject splitterTarget = enemies[UnityEngine.Random.Range(0, enemies.Count)];
                var proj = Instantiate(splitterPrefab, hitPos, Quaternion.identity)
                    .GetComponent<Projectile>();
                if (proj != null)
                    proj.Init((splitterTarget, 0), splitterDamage);
            }
        }

        private void ApplyPierce(Vector3 hitPos)
        {
            if (target.Item1 != null)
            {
                target.Item1.GetComponent<Enemy>()?.TakeDamage(damage);
                remainingPierce--;
            }

            // Nächstes Ziel auf dem Weg suchen
            if (remainingPierce > 0)
            {
                GameObject next = null;
                float minDist = float.MaxValue;
                foreach (var go in TowerHeroManager.instance.enemies)
                {
                    if (go == null || go == target.Item1) continue;
                    float d = Vector3.Distance(transform.position, go.transform.position);
                    if (d < minDist) { minDist = d; next = go; }
                }
                target = next != null ? (next, 0) : (null, -1);
            }
        }
        

        private IEnumerator ApplyBlackHole(Vector3 center)
        {
            if (blackHoleVfxPrefab != null) Instantiate(blackHoleVfxPrefab, center, Quaternion.identity);

            float elapsed = 0f;
            while (elapsed < blackHolePullDuration)
            {
                elapsed += Time.deltaTime;
                foreach (var go in TowerHeroManager.instance.enemies)
                {
                    if (go == null) continue;
                    if (Vector3.Distance(center, go.transform.position) <= blackHoleRadius)
                        go.transform.position = Vector3.MoveTowards(
                            go.transform.position, center, 3f * Time.deltaTime);
                }
                yield return null;
            }
            // Explosion am Ende
            ApplyAoeDamage(center, blackHoleRadius, damage, null);
            anim.TriggerDeadAnimation(true);
            projectileIsDead = true;
        }

        private IEnumerator ApplySticky(GameObject enemy)
        {
            // Projektil bleibt am Gegner kleben
            float elapsed = 0f;
            while (elapsed < stickyDelay)
            {
                elapsed += Time.deltaTime;
                if (enemy == null) break;
                transform.position = enemy.transform.position;
                yield return null;
            }
            Vector3 explodePos = enemy != null ? enemy.transform.position : transform.position;
            ApplyAoeDamage(explodePos, stickyAoeRadius, damage, aoeVfxPrefab);
            anim.TriggerDeadAnimation(true);
            projectileIsDead = true;
        }

        private void ApplyChainLink(GameObject enemyA, Vector3 hitPos)
        {
            GameObject nearest = null;
            float minDist = chainLinkRadius;
            foreach (var go in TowerHeroManager.instance.enemies)
            {
                if (go == null || go == enemyA) continue;
                float d = Vector3.Distance(hitPos, go.transform.position);
                if (d < minDist) { minDist = d; nearest = go; }
            }
            if (nearest == null) return;

            // Beide verlangsamen & Schaden
            enemyA.GetComponent<Enemy>()?.ApplySlow(chainSlowAmount, 99f);
            nearest.GetComponent<Enemy>()?.ApplySlow(chainSlowAmount, 99f);
            StartCoroutine(ChainLinkDamageLoop(enemyA, nearest));
        }

        private IEnumerator ChainLinkDamageLoop(GameObject a, GameObject b)
        {
            while (a != null && b != null)
            {
                float dist = Vector3.Distance(a.transform.position, b.transform.position);
                if (dist > chainLinkRadius * 1.5f) // Kette reißt wenn zu weit
                {
                    a.GetComponent<Enemy>()?.ApplySlow(0f, 0f); // Slow aufheben
                    b.GetComponent<Enemy>()?.ApplySlow(0f, 0f);
                    yield break;
                }
                a.GetComponent<Enemy>()?.TakeDamage(chainLinkDamage);
                b.GetComponent<Enemy>()?.TakeDamage(chainLinkDamage);
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ── Gizmos ───────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            switch (Mode)
            {
                case ProjectileMode.AOE:
                    DrawGizmoSphere(aoeRadius, new Color(1f, 0.3f, 0f));
                    break;
                case ProjectileMode.ChainLightning:
                    DrawGizmoSphere(chainRadius, new Color(1f, 1f, 0f));
                    break;
                case ProjectileMode.BlackHole:
                    DrawGizmoSphere(blackHoleRadius, new Color(0.5f, 0f, 1f));
                    break;
                case ProjectileMode.Sticky:
                    DrawGizmoSphere(stickyAoeRadius, new Color(1f, 0f, 0.5f));
                    break;
                case ProjectileMode.Chain:
                    DrawGizmoSphere(chainLinkRadius, new Color(0f, 0.8f, 1f));
                    break;
            }
        }

        private void DrawGizmoSphere(float radius, Color color)
        {
            Gizmos.color = new Color(color.r, color.g, color.b, 0.2f);
            Gizmos.DrawSphere(transform.position, radius);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.9f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Projectile))]
    public class ProjectileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Alle Felder bis auf die HideInInspector-Felder normal zeichnen
            DrawPropertiesExcluding(serializedObject,
                "aoeRadius", "aoeVfxPrefab",
                "dotDuration", "dotInterval", "dotDamagePerTick",
                "slowDuration", "slowAmount",
                "chainCount", "chainDamageFalloff", "chainRadius",
                "knockbackDistance", "knockbackDuration",
                "splitterCount", "splitterPrefab", "splitterDamage",
                "pierceCount",
                "orbitProjectilePrefab", "orbitDuration", "orbitDamagePerSecond",
                "blackHolePullDuration", "blackHoleRadius", "blackHoleVfxPrefab",
                "stickyDelay", "stickyAoeRadius",
                "chainLinkRadius", "chainSlowAmount", "chainLinkDamage"
            );

            var p = (Projectile)target;

            EditorGUILayout.Space();

            switch (p.Mode)
            {
                case ProjectileMode.AOE:
                    EditorGUILayout.LabelField("AOE Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeRadius"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeVfxPrefab"));
                    break;

                case ProjectileMode.DoT:
                    EditorGUILayout.LabelField("DoT Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dotDuration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dotInterval"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dotDamagePerTick"));
                    break;

                case ProjectileMode.ChainLightning:
                    EditorGUILayout.LabelField("Chain Lightning Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chainCount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chainDamageFalloff"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chainRadius"));
                    break;

                case ProjectileMode.Knockback:
                    EditorGUILayout.LabelField("Knockback Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackDistance"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackDuration"));
                    break;

                case ProjectileMode.Splitter:
                    EditorGUILayout.LabelField("Splitter Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("splitterCount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("splitterPrefab"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("splitterDamage"));
                    break;

                case ProjectileMode.Pierce:
                    EditorGUILayout.LabelField("Pierce Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pierceCount"));
                    break;
                
                case ProjectileMode.BlackHole:
                    EditorGUILayout.LabelField("Black Hole Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("blackHolePullDuration"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("blackHoleRadius"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("blackHoleVfxPrefab"));
                    break;

                case ProjectileMode.Sticky:
                    EditorGUILayout.LabelField("Sticky Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("stickyDelay"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("stickyAoeRadius"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("aoeVfxPrefab"));
                    break;

                case ProjectileMode.Chain:
                    EditorGUILayout.LabelField("Chain (Verbindung) Parameter", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chainLinkRadius"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chainSlowAmount"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("chainLinkDamage"));
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}