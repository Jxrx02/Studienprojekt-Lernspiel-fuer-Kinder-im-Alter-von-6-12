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
    public EnemyConfig enemyConfig; // Referenz auf die Konfiguration

    public int currentHealth;
    private bool isDead;

    
    //Path
    private int waypointIndex = 0; // Der aktuelle Zielpunkt auf dem Pfad
    public GameObject[] walkPath; // Der Pfad, dem der Gegner folgt
    private SpriteAnim _spriteAnim;

    private void Start()
    {
        ApplyConfig();

    }


    /// <summary>
    /// Wendet die Werte aus der EnemyConfig auf diesen Gegner an.
    /// </summary>
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
        this._spriteAnim.animState = AnimationState.Walk_Animation;
        
        // Grundwerte initialisieren
        currentHealth = enemyConfig.health;
        isDead = false;
        
        Actions.onEnemySpawn(this.gameObject);

    }

    /// <summary>
    /// Verarbeitet Schaden basierend auf den Resistenzen des Gegners.
    /// </summary>
    public void TakeDamage(float damage, string damageType = "physical")
    {
        if (isDead) return;

        float finalDamage = damage;

        // Resistenzen und Schwächen anwenden
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
            {
                Instantiate(enemyConfig.evasionEffect, transform.position, Quaternion.identity);
            }
            Debug.Log($"{enemyConfig.name} ist ausgewichen");
            return; // Schaden wird nicht angewendet
        }


        finalDamage = Mathf.Max(finalDamage, 0); // Schaden kann nicht negativ sein
        currentHealth -= Mathf.RoundToInt(finalDamage);

        // Treffer-Effekte abspielen
        if (enemyConfig.hitEffect != null)
        {
            Instantiate(enemyConfig.hitEffect, transform.position, Quaternion.identity);
        }

        if (currentHealth <= 0)
        {
            // Todes-Effekte
            if (enemyConfig.deathEffect != null)
            {
                Instantiate(enemyConfig.deathEffect, transform.position, Quaternion.identity);
            }
            // Belohnung
            Actions.onEnemyDeath?.Invoke(this.gameObject);
            
            _spriteAnim.TriggerDeadAnimation(true);
            isDead = true;
        }
    }
    
    void Update()
    {
        // Bewegungsgeschwindigkeit anwenden, falls nötig
        if (!isDead && enemyConfig.movementSpeed > 0 && walkPath != null)
        {
            MoveAlongPath();
        }
    }

    /// <summary>
    /// Bewegt den Gegner entlang seines vordefinierten Pfads.
    /// </summary>
    private void MoveAlongPath()
    {
        transform.position = Vector2.MoveTowards(transform.position, walkPath[waypointIndex].transform.position, enemyConfig.movementSpeed * Time.deltaTime);
        rotateToObject(walkPath[waypointIndex].transform.position);

        if (Vector2.Distance(transform.position, walkPath[waypointIndex].transform.position) < 0.1f)
        {
            if (waypointIndex < walkPath.Length - 1)
                waypointIndex++;
            else
            {
                //Hat ziel erreicht; Leben geht runter
                Debug.Log($"{enemyConfig.description} hat ziel erreicht und veursacht {enemyConfig.penaltyOnReachingEnd} Schaden.");
                
                Actions.onEnemyReachedEnd(this.gameObject);
                Destroy(this.gameObject);

            }
        }
    }


    
    public bool canRotateOnZAxis = false, faceToDir = true;

    void rotateToObject(Vector3 toObject)
    {
        if (faceToDir)
        {
            Vector3 dir = toObject - transform.position;
            if(dir.x > 0)
            {
                transform.rotation = new Quaternion(0,0,0,0);
            }
            else
                transform.rotation = new Quaternion(0, 180, 0, 0);
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

