
using UnityEngine.Serialization;

namespace TowerDefense
{
    using UnityEngine;
    using System;

    public class Projectile : MonoBehaviour
    {
        
        public float rotationAngleOffset;
        public Boolean doRotation = true;
        public Boolean lookAtTarget = true;
        public Boolean hideTilCollision;
        [Tooltip("Anzahl der möglichen Bounces")]public int canFindNewTarget;

        public float speed;

        private int damage;
        private (GameObject, int) target;
        private int targetIndex;

        private Boolean projectileIsDead;
        private SpriteAnim anim;

        public void Init((GameObject, int) target, int damage)
        {
            this.target = target;
            this.damage = damage;
            
            this.targetIndex = target.Item2;
        }

        private void Start()
        {
            anim = GetComponent<SpriteAnim>();
            if (hideTilCollision)
            {
                //Deaktivere alle subobjekte
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
        private void UpdateLookDirection(Vector3 targetPos)
        {
            if (targetPos.x < transform.position.x)
                transform.rotation = new Quaternion(0, 180, 0, 1);
            else
                transform.rotation = new Quaternion(0, 0, 0, 1);
        }

        void Update()
        {
            if (projectileIsDead) return;

            if (target.Item1 != null)
            {
                if (lookAtTarget) UpdateLookDirection(target.Item1.transform.position);

                // Wenn das Projektil das Ziel erreicht hat
                if (Vector3.Distance(transform.position, target.Item1.transform.position) < 0.3f)
                {
                    OnProjectileHitTarget();
                }
                else
                {
                    MoveProjectileTowardsTarget();
                }
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
                        else
                        {
                            OnProjectileHitTarget(); // Keine Gegner mehr
                        }
                    }
                    catch
                    {
                        OnProjectileHitTarget();
                    }
                }
                else
                {
                    OnProjectileHitTarget();
                }
            }
        }

        private void MoveProjectileTowardsTarget()
        {
            Vector3 direction = (target.Item1.transform.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;

            if (doRotation) // Drehung in Flugrichtung
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle + rotationAngleOffset, Vector3.forward);
            }
        }

        private void OnProjectileHitTarget()
        {
            if (hideTilCollision)
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(true);
                }
            }

            if (target.Item1 != null)
            {
                target.Item1.GetComponent<Enemy>().TakeDamage(damage);
            }

            // Bounce-Zähler runterzählen
            if (canFindNewTarget > 0)
            {
                canFindNewTarget--;
                // Versuche in Update(), ein neues Ziel zu finden
                target = (null, -1); // damit Update() in den "find new target"-Zweig geht
                return;
            }

            // Keine Bounces mehr → Projektil stirbt
            anim.TriggerDeadAnimation(true);
            projectileIsDead = true;
        }

    }
}
    

