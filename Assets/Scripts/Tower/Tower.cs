using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    public enum TargetType
    {
        First,
        Last,
        Strongest,
        Nearest
    }

    public abstract class Tower : MonoBehaviour
    {
        [SerializeField] public string towerName;
        [SerializeField] public string towerDesc;
        [SerializeField] public int towerInitPrice;
        [SerializeField] public float range;
        [SerializeField] public int damage;
        [Range(0.1f, 50f)][SerializeField] public float timeInBetweenShots;
        [SerializeField] public float attackSpeedMultiplier = 1f;

        [SerializeField] private SpriteRenderer rangeIndicator;
        [SerializeField] private Vector3 rangeIndicatorOffset;

        [SerializeField] public float statCoinsEarnedPerSecond, statHealthRegenPerSecond,
            statRangeMultiplier, statDmgMultiplier, statAttackSpeedMultiplier, statSlowMultiplier;

        [SerializeField] protected Transform spawnProjectileOffsetPoint;
        public UpgradePath[] upgradePaths = new UpgradePath[3];
        public int[] pathLevels = new int[5];

        [HideInInspector] public int level = 1;
        [HideInInspector] public int experience;
        [HideInInspector] public int requiredExperience;
        public LevelConfig levelConfig;
        [HideInInspector] public Boolean isAttacking;
        public GameObject projectilePrefab;

        [SerializeField] protected Boolean isSelected;

        protected internal SpriteAnim spriteAnim;
        protected SpriteRenderer sr;

        private Material notOutlinedMaterial;
        [SerializeField] private Material outlineMaterial;
        [SerializeField] public TargetType targetPreference = TargetType.Nearest;

        private StatDiffDisplay statDiffDisplay;

        protected (GameObject, int) target;
        private List<GameObject> _enemiesInRange = new List<GameObject>();

        public abstract void Attack((GameObject, int) _target);

        public void Awake()
        {
            spriteAnim = GetComponent<SpriteAnim>();
            spriteAnim.animState = AnimationState.Idle_Animation;
            sr = GetComponent<SpriteRenderer>();
            notOutlinedMaterial = sr.material;

            statDiffDisplay = GetComponent<StatDiffDisplay>();
            Actions.onEnemyDeath += this.IncreaseExp;
            DrawRangeIndicatior();
        }

        private void IncreaseExp(GameObject enemy)
        {
            if (this == null || !this.gameObject) return;
            if (!IsObjectInRange(enemy)) return;
            var value = enemy.GetComponent<Enemy>().enemyConfig.xpDrop;
            experience += value;

            if (experience >= requiredExperience)
            {
                while (experience >= requiredExperience)
                {
                    experience -= requiredExperience;
                    LevelUp();
                    statDiffDisplay.ShowDiff("Level", level - 1, level);
                }
            }
            TowerUI.Instance.UpdateUI();
        }

        private void LevelUp()
        {
            level++;
            CalculateRequiredExp();
        }

        private void CalculateRequiredExp()
        {
            requiredExperience = levelConfig.GetRequiredExp(level);
        }

        protected IEnumerator BaseAttackCoroutine(Action onShoot = null)
        {
            if (target.Item1 != null)
            {
                UpdateLookDirection(target.Item1.transform.position);

                spriteAnim.SetAttackSpeed(attackSpeedMultiplier);
                spriteAnim.animState = AnimationState.Attack_Animation;

                bool animationComplete = false;
                void AnimationFinished() => animationComplete = true;
                spriteAnim.OnAttackAnimationComplete += AnimationFinished;
                yield return new WaitUntil(() => animationComplete);
                spriteAnim.OnAttackAnimationComplete -= AnimationFinished;

                onShoot?.Invoke();

                yield return new WaitForSeconds(timeInBetweenShots);
                isAttacking = false;
            }
            else
            {
                isAttacking = false;
                spriteAnim.animState = AnimationState.Idle_Animation;
            }
        }

        protected virtual void UpdateLookDirection(Vector3 targetPos)
        {
            Quaternion canvasRot = GetComponentInChildren<Canvas>().transform.rotation;
            bool faceRight = targetPos.x > transform.position.x;
            transform.rotation = faceRight
                ? new Quaternion(0, 180, 0, 1)
                : Quaternion.identity;
            GetComponentInChildren<Canvas>().transform.rotation = canvasRot;
        }

        /// <summary>
        /// Spawnt ein Projektil und überträgt Schaden + optionale Projektil-Überschreibungen vom Turm.
        /// </summary>
        protected void Shoot()
        {
            if (target.Item1 == null || projectilePrefab == null) return;

            GameObject go = Instantiate(projectilePrefab,
                spawnProjectileOffsetPoint.position, Quaternion.identity);

            var proj = go.GetComponent<Projectile>();
            if (proj == null) return;

            // Schaden vom Turm berechnen (Multiplikatoren anwenden)
            int finalDamage = Mathf.RoundToInt(damage * Mathf.Max(1f, statDmgMultiplier));

            proj.Init(target, finalDamage);
            
        }

        public (GameObject, int) FindTargetInRange(List<GameObject> enemies)
        {
            _enemiesInRange.Clear();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && IsObjectInRange(enemies[i]))
                    _enemiesInRange.Add(enemies[i]);
            }
            if (_enemiesInRange.Count == 0) return (null, 0);
            return FindTargetByPreference(_enemiesInRange);
        }

        public Boolean IsObjectInRange(GameObject obj)
        {
            return Vector3.Distance(transform.position, obj.transform.position) <= range;
        }

        public (GameObject, int) FindTargetByPreference(List<GameObject> enemies)
        {
            switch (targetPreference)
            {
                case TargetType.First:     return (FindFirstEnemy(enemies), 0);
                case TargetType.Last:      return (FindLastEnemy(enemies), enemies.Count - 1);
                case TargetType.Strongest: return FindStrongestEnemy(enemies);
                case TargetType.Nearest:   return FindNearestEnemy(enemies);
                default:                   return (null, 0);
            }
        }

        private GameObject FindFirstEnemy(List<GameObject> enemies)
            => enemies.Count > 0 ? enemies[0] : null;

        private GameObject FindLastEnemy(List<GameObject> enemies)
            => enemies.Count > 0 ? enemies[enemies.Count - 1] : null;

        private (GameObject, int) FindStrongestEnemy(List<GameObject> enemies)
        {
            GameObject strongest = null;
            int bestPos = 0;
            float maxHealth = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                var ec = enemies[i].GetComponent<Enemy>();
                if (ec != null && ec.currentHealth > maxHealth)
                {
                    strongest = enemies[i];
                    maxHealth = ec.currentHealth;
                    bestPos = i;
                }
            }
            return (strongest, bestPos);
        }

        private (GameObject, int) FindNearestEnemy(List<GameObject> enemies)
        {
            GameObject nearest = null;
            float shortest = range;
            int bestPos = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                float d = Vector3.Distance(transform.position, enemies[i].transform.position);
                if (d < shortest)
                {
                    nearest = enemies[i];
                    shortest = d;
                    bestPos = i;
                }
            }
            return (nearest, bestPos);
        }

        public void SetIsSelected(Boolean _isSelected)
        {
            DrawRangeIndicatior();
            isSelected = _isSelected;

            if (isSelected)
            {
                sr.material = outlineMaterial;
                rangeIndicator.gameObject.SetActive(true);
            }
            else
            {
                sr.material = notOutlinedMaterial;
                rangeIndicator.gameObject.SetActive(false);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + rangeIndicatorOffset, range);
            GetComponent<CircleCollider2D>().radius = range;
            GetComponent<CircleCollider2D>().offset = rangeIndicatorOffset;
            DrawRangeIndicatior();
        }

        private void DrawRangeIndicatior()
        {
            if (rangeIndicator != null)
            {
                float spriteDiameter = rangeIndicator.sprite.bounds.size.x;
                float scaleFactor = (range * 2) / spriteDiameter;
                rangeIndicator.transform.position = transform.position + rangeIndicatorOffset;
                rangeIndicator.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
            }
        }

        public void UpgradePath(int pathIndex)
        {
            if (pathIndex < 0 || pathIndex >= upgradePaths.Length) return;

            UpgradePath path = upgradePaths[pathIndex];
            int currentLevel = pathLevels[pathIndex];

            if (currentLevel < path.levels.Length)
            {
                TowerUpgradeLevel lvl = path.levels[currentLevel];

                float oldDamage           = damage;
                float oldRange            = range;
                float oldFireRate         = 1f / timeInBetweenShots;
                float oldCoinsPerSecond   = statCoinsEarnedPerSecond;
                float oldRegen            = statHealthRegenPerSecond;
                float oldDmgMult          = statDmgMultiplier;
                float oldRangeMult        = statRangeMultiplier;
                float oldAtkSpeedMult     = statAttackSpeedMultiplier;
                float oldSlowMult         = statSlowMultiplier;

                towerName              = lvl.upgradeName;
                towerDesc              = lvl.description;
                damage                += lvl.damageIncrease;
                range                 += lvl.rangeIncrease;
                timeInBetweenShots     = (timeInBetweenShots - lvl.timeInBetweenShotsDecrease) > 0.1f
                                            ? (timeInBetweenShots - lvl.timeInBetweenShotsDecrease)
                                            : 0.1f;
                attackSpeedMultiplier += lvl.attackSpeedIncrease;

                statCoinsEarnedPerSecond += lvl.coinsEarnedPerSecond;
                statHealthRegenPerSecond += lvl.healthRegenPerSecond;
                statRangeMultiplier      += lvl.rangeMultiplier;
                statDmgMultiplier        += lvl.dmgMultiplier;
                statAttackSpeedMultiplier+= lvl.attackSpeedMultiplier;
                statSlowMultiplier       += lvl.slowMultiplier;

                pathLevels[pathIndex]++;

                spriteAnim.idle_sprites   = lvl.idle_sprites;
                spriteAnim.attack_sprites = lvl.attack_sprites;
                projectilePrefab          = lvl.projectile;

                if (statDiffDisplay != null)
                {
                    statDiffDisplay.ShowDiff("Damage",        oldDamage,         damage);
                    statDiffDisplay.ShowDiff("Range",         oldRange,          range);
                    statDiffDisplay.ShowDiff("Abklingzeit",   oldFireRate,       1f / timeInBetweenShots);
                    statDiffDisplay.ShowDiff("Gold/sec",      oldCoinsPerSecond, statCoinsEarnedPerSecond);
                    statDiffDisplay.ShowDiff("Regen/sec",     oldRegen,          statHealthRegenPerSecond);
                    statDiffDisplay.ShowDiff("Dmg Mult.",     oldDmgMult,        statDmgMultiplier);
                    statDiffDisplay.ShowDiff("Range Mult.",   oldRangeMult,      statRangeMultiplier);
                    statDiffDisplay.ShowDiff("AtkSpeed Mult.",oldAtkSpeedMult,   statAttackSpeedMultiplier);
                    statDiffDisplay.ShowDiff("Slow Mult.",    oldSlowMult,       statSlowMultiplier);
                }

                Debug.Log($"Pfad {path} auf Level {pathLevels[pathIndex]} verbessert!");
            }
            else
            {
                Debug.Log($"Pfad {path} ist bereits maximal verbessert!");
            }

            TowerUI.Instance.UpdateUI();
            DrawRangeIndicatior();
        }

        public void NextTargetType()
        {
            targetPreference = (TargetType)(((int)targetPreference + 1) %
                System.Enum.GetValues(typeof(TargetType)).Length);
            TowerUI.Instance.txtTargetPreference.text = targetPreference.ToString();
        }

        public void PreviousTargetType()
        {
            int count = System.Enum.GetValues(typeof(TargetType)).Length;
            targetPreference = (TargetType)(((int)targetPreference - 1 + count) % count);
            TowerUI.Instance.txtTargetPreference.text = targetPreference.ToString();
        }

        public void SellTower()
        {
            LevelManager.instance.cur_coins += CalculateSellPrice();
            if (this is Hero && LevelManager.instance.heroFielded)
                LevelManager.instance.heroFielded = false;

            Actions.onEnemyDeath -= this.IncreaseExp;
            TowerHeroManager.instance.UnRegisterTower(this.gameObject);
            TowerHeroManager.instance.DeselectTower();
            Destroy(this.gameObject);
        }

        public int CalculateSellPrice()
        {
            int coins = towerInitPrice;
            for (int i = 0; i < upgradePaths.Length; i++)
            {
                UpgradePath path = upgradePaths[i];
                int lvl = pathLevels[i];
                for (int j = 0; j < lvl; j++)
                    coins += path.levels[j].upgradeCost;
            }
            return (int)(coins * 0.8f);
        }
    }
}