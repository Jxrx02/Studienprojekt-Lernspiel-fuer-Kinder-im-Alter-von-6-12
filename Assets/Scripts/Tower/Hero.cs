using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TowerDefense
{
    public class Hero : Tower
    {
        [SerializeField] public float maxMoveSpeed = 4f;
        [SerializeField] public float acceleration = 10f;
        private Vector2 velocity;
        private Boolean hasReachedTargetPosition = true;
        
        [HideInInspector]public Vector2 targetPosition;

        private void Start()
        {
            TowerHeroManager.instance.RegisterTower(this.gameObject);
            targetPosition = transform.position;
        }

        private void Update()
        {
            if (!hasReachedTargetPosition && !isAttacking)
            {
                spriteAnim.animState = AnimationState.Walk_Animation;
                spriteAnim.SetWalkSpeed(2);
                
                Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
                float distance = Vector2.Distance(transform.position, targetPosition);

                if (distance > 0.15f)
                {
                    // Beschleunige in Richtung des Ziels
                    velocity += direction * acceleration * Time.deltaTime;

                    // Begrenze die maximale Geschwindigkeit
                    velocity = Vector2.ClampMagnitude(velocity, maxMoveSpeed);

                    // Bewege das Objekt
                    transform.position += (Vector3)(velocity * Time.deltaTime);
                }
                else
                {
                    // Ziel erreicht – stoppen und Idle setzen
                    velocity = Vector2.zero;
                    spriteAnim.animState = AnimationState.Idle_Animation;
                    hasReachedTargetPosition = true;
                }
            }

        }

        public void OnHeroMoveButtonPressed()
        {
            OneClickInWorldListener.ListenOnce((Vector3 pos) =>
            {
                if (!isAttacking)
                {
                    targetPosition = pos;

                    if (Vector2.Distance(transform.position, targetPosition) > 0.15f)
                    {
                        spriteAnim.animState = AnimationState.Walk_Animation;
                        spriteAnim.SetWalkSpeed(2);
                        hasReachedTargetPosition = false;

                        if (targetPosition.x < transform.position.x)                 
                            transform.rotation = new Quaternion(0,180,0,1); // schaut nach links
                        else
                            transform.rotation = new Quaternion(0,0,0,1); // schaut nach links
                    }
                }
                else
                {
                    //Dialog to show "Unable to move during fights"
                    DialogCanvas.instance.TriggerDialog("I HAVE TO FOCUS FIRST. Otherwise I might miss :(", transform.position);

                }
                TowerHeroManager.instance.DeselectTower();
            });
        }
        
        public override void Attack((GameObject, int) _target)
        {
            if (isAttacking) return; // Verhindert mehrfachen Angriff gleichzeitig
            
            target = _target;
            if (this.target.Item1 == null) return;
            
            if (IsObjectInRange(target.Item1))
            {
                isAttacking = true;
                StartCoroutine(BaseAttackCoroutine(Shoot));

            }

        }
        
        
        protected override void UpdateLookDirection(Vector3 targetPos)
        {
            Quaternion originalCanvasRotation = GetComponentInChildren<Canvas>().transform.rotation;

            if (targetPos.x > transform.position.x)
                transform.rotation = new Quaternion(0, 180, 0, 1);
            else
                transform.rotation = new Quaternion(0, 0, 0, 1);
            
            GetComponentInChildren<Canvas>().transform.rotation = originalCanvasRotation;
        }

        



    }
    
    
}