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
        First,     // Erster Gegner in der Liste
        Last,      // Letzter Gegner in der Liste
        Strongest, // Gegner mit dem meisten Leben
        Nearest    // Nächstgelegener Gegner
    }

    
public abstract class Tower : MonoBehaviour
{
    [SerializeField] public string towerName;
    [SerializeField] public string towerDesc;
    [SerializeField] public int towerInitPrice;
    [SerializeField] public float range;
    [SerializeField] public int damage;
    [Range(0.1f,50f)][SerializeField] public float timeInBetweenShots;
    [SerializeField] public float attackSpeedMultiplier = 1f; // Standard: 1x Geschwindigkeit
    
    [SerializeField] private SpriteRenderer rangeIndicator;
    [SerializeField] private Vector3 rangeIndicatorOffset;

    [SerializeField] public float statCoinsEarnedPerSecond, statHealthRegenPerSecond, statRangeMultiplier,statDmgMultiplier,statAttackSpeedMultiplier,statSlowMultiplier;
    
    [SerializeField] protected Transform spawnProjectileOffsetPoint;
    public UpgradePath[] upgradePaths = new UpgradePath[3]; 
    public int[] pathLevels = new int[5]; 

    [HideInInspector] public int level = 1; 
    [HideInInspector] public int experience;
    [HideInInspector] public int requiredExperience;
    public LevelConfig levelConfig;
    [HideInInspector] public Boolean isAttacking;
    public GameObject projectilePrefab;
    
    [SerializeField] protected Boolean isSelected; //for TowerUI
    
    protected internal SpriteAnim spriteAnim;
    protected SpriteRenderer sr;
    
    private Material notOutlinedMaterial;
    [SerializeField]private Material outlineMaterial;
    [SerializeField] public TargetType targetPreference = TargetType.Nearest; // Standard: Nächstgelegener Gegner

    private StatDiffDisplay statDiffDisplay;

    protected (GameObject, int) target;

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
        if(!IsObjectInRange(enemy)) return;
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
        //TowerUI.Instance.UpdateUI(this);
    }
    
    protected IEnumerator BaseAttackCoroutine(Action onShoot = null)
    {
        if (target.Item1 != null)
        {
            // Optional: Blickrichtung setzen (falls notwendig, z. B. für Hero)
            UpdateLookDirection(target.Item1.transform.position);

            spriteAnim.SetAttackSpeed(attackSpeedMultiplier);
            spriteAnim.animState = AnimationState.Attack_Animation;

            bool animationComplete = false;
            void AnimationFinished() => animationComplete = true;
            spriteAnim.OnAttackAnimationComplete += AnimationFinished;
            yield return new WaitUntil(() => animationComplete);
            spriteAnim.OnAttackAnimationComplete -= AnimationFinished;

            // Shooting action
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

    }

    protected void Shoot()
    {
        if (target.Item1 == null) return;

        GameObject projectile = Instantiate(projectilePrefab, spawnProjectileOffsetPoint.position, Quaternion.identity);
        var projectileScript = projectile.GetComponent<Projectile>();

        projectileScript.Init(target, damage);
    }

    
    public (GameObject, int) FindTargetInRange(List<GameObject> enemies)
    {
        // Finde alle Gegner in Reichweite
        var enemiesInRange = enemies.Where(e => e != null && IsObjectInRange(e.gameObject)).ToList();

        if (enemiesInRange.Count == 0) return (null,0); // Kein Ziel in Reichweite

        // Wende die Zielpräferenz an
        return FindTargetByPreference(enemiesInRange);
    }

    public Boolean IsObjectInRange(GameObject obj)
    {
        float distance = Vector3.Distance(transform.position, obj.transform.position);
        return distance <= range;
    }
    
    public (GameObject, int) FindTargetByPreference(List<GameObject> enemies)
    {
        (GameObject, int) bestTarget = (null, 0);
        switch (targetPreference)
        {
            case TargetType.First:
                bestTarget = (FindFirstEnemy(enemies),0);
                break;
            case TargetType.Last:
                bestTarget = (FindLastEnemy(enemies), enemies.Count-1);
                break;
            case TargetType.Strongest:
                bestTarget = FindStrongestEnemy(enemies);
                break;
            case TargetType.Nearest:
                bestTarget = FindNearestEnemy(enemies);
                break;
        }

        return bestTarget;
    }

    private GameObject FindFirstEnemy(List<GameObject> enemies)
    {
        return enemies.Count > 0 ? enemies[0] : null;
    }

    private GameObject FindLastEnemy(List<GameObject> enemies)
    {
        return enemies.Count > 0 ? enemies[enemies.Count - 1] : null;
    }

    private (GameObject, int) FindStrongestEnemy(List<GameObject> enemies)
    {
        GameObject strongest = null;
        int bestTargetPosition = 0;
        float maxHealth = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            var enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null && enemyComponent.currentHealth > maxHealth)
            {
                strongest = enemy;
                maxHealth = enemyComponent.currentHealth;
                bestTargetPosition = i; // Speichere die aktuelle Position
            }
        }

        return (strongest, bestTargetPosition);
    }

    private (GameObject, int) FindNearestEnemy(List<GameObject> enemies)
    {
        GameObject nearest = null;
        float shortestDistance = range;
        int bestTargetPosition = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < shortestDistance)
            {
                nearest = enemy;
                shortestDistance = distance;
                bestTargetPosition = i; // Aktualisiere den Index des nächstgelegenen Gegners
            }
        }

        return (nearest, bestTargetPosition);
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
        // Skaliere das Sprite entsprechend der Range
        if (rangeIndicator != null)
        {
            // Hole die ursprüngliche Größe des Sprites (Durchmesser)
            float spriteDiameter = rangeIndicator.sprite.bounds.size.x;

            float scaleFactor = (range * 2) / spriteDiameter;

            // Setze die Skalierung des Sprites
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
            TowerUpgradeLevel level = path.levels[currentLevel];

            // Vorherige Werte merken
            float oldDamage = damage;
            float oldRange = range;
            float oldFireRate = 1f / timeInBetweenShots; // Shots per second
            float oldCoinsPerSecond = statCoinsEarnedPerSecond;
            float oldRegen = statHealthRegenPerSecond;
            float oldDmgMult = statDmgMultiplier;
            float oldRangeMult = statRangeMultiplier;
            float oldAtkSpeedMult = statAttackSpeedMultiplier;
            float oldSlowMult = statSlowMultiplier;
            
            // Upgrade-Werte anwenden
            towerName = level.upgradeName;
            towerDesc = level.description;
            damage += level.damageIncrease;
            range += level.rangeIncrease;
            timeInBetweenShots = (timeInBetweenShots - level.timeInBetweenShotsDecrease)>0.1? 
                (timeInBetweenShots - level.timeInBetweenShotsDecrease) : 0.1f; // Geringere Feuerrate = schnelleres Schießen
            attackSpeedMultiplier += level.attackSpeedIncrease;

            statCoinsEarnedPerSecond += level.coinsEarnedPerSecond;
            statHealthRegenPerSecond += level.healthRegenPerSecond;
            statRangeMultiplier += level.rangeMultiplier; 
            statDmgMultiplier+= level.dmgMultiplier;
            statAttackSpeedMultiplier += level.attackSpeedMultiplier;
            statSlowMultiplier += level.slowMultiplier;
            
            pathLevels[pathIndex]++;

            //neue sprites laden
            spriteAnim.idle_sprites = level.idle_sprites;
            spriteAnim.attack_sprites = level.attack_sprites;

            projectilePrefab = level.projectile;
            
            Debug.Log($"Pfad {path} auf Level {pathLevels[pathIndex]} verbessert!");
            

            if (statDiffDisplay != null)
            {
                statDiffDisplay.ShowDiff("Damage", oldDamage, damage);
                statDiffDisplay.ShowDiff("Range", oldRange, range);

                float newFireRate = 1f / timeInBetweenShots;
                statDiffDisplay.ShowDiff("Abklingzeit", oldFireRate, newFireRate); // Höher = schneller

                statDiffDisplay.ShowDiff("Gold/sec", oldCoinsPerSecond, statCoinsEarnedPerSecond);
                statDiffDisplay.ShowDiff("Regen/sec", oldRegen, statHealthRegenPerSecond);
                statDiffDisplay.ShowDiff("Dmg Mult.", oldDmgMult, statDmgMultiplier);
                statDiffDisplay.ShowDiff("Range Mult.", oldRangeMult, statRangeMultiplier);
                statDiffDisplay.ShowDiff("AtkSpeed Mult.", oldAtkSpeedMult, statAttackSpeedMultiplier);
                statDiffDisplay.ShowDiff("Slow Mult.", oldSlowMult, statSlowMultiplier);
            }
            
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
        targetPreference = (TargetType)(((int)targetPreference + 1) % System.Enum.GetValues(typeof(TargetType)).Length);
        TowerUI.Instance.txtTargetPreference.text = targetPreference.ToString();

    }

    public void PreviousTargetType()
    {
        int targetCount = System.Enum.GetValues(typeof(TargetType)).Length;
        targetPreference = (TargetType)(((int)targetPreference - 1 + targetCount) % targetCount);
        
        TowerUI.Instance.txtTargetPreference.text = targetPreference.ToString();
    }

    public void SellTower()
    {
        LevelManager.instance.cur_coins += CalculateSellPrice();
        if (this is Hero && LevelManager.instance.heroFielded) LevelManager.instance.heroFielded = false;

        //Unregister
        Actions.onEnemyDeath -= this.IncreaseExp;
        TowerHeroManager.instance.UnRegisterTower(this.gameObject);
        TowerHeroManager.instance.DeselectTower();
        Destroy(this.gameObject);
    }

    public int CalculateSellPrice()
    {
        var coins = towerInitPrice;

        for (int i = 0; i < upgradePaths.Length; i++)
        {
            UpgradePath path = upgradePaths[i];
            int level = pathLevels[i]; // wie viele Upgrades gekauft wurden

            for (int j = 0; j < level; j++)
            {
                coins += path.levels[j].upgradeCost;
            }
        }

        coins = (int)(coins * 0.8f); // Wertverlust

        return coins;
    }

}

}