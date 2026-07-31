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
		[Header("Movement")]
        [SerializeField] private float moveSpeed = 4f;

        private Vector2 movementInput;

        private Tower currentTower;

        private bool interactionMode = false;
        private bool hasReachedTargetPosition;

        [Header("Weapons")]
        [SerializeField] private Projectile[] equippedProjectiles;
        [SerializeField] private int activeWeaponIndex;
		
        [SerializeField] public float maxMoveSpeed = 4f;
        [SerializeField] public float acceleration = 10f;

        
        [HideInInspector]public Vector2 targetPosition;

        private void Start()
        {
            TowerHeroManager.instance.RegisterTower(this.gameObject);
            targetPosition = transform.position;
        }

        private void Update()
        {
            HandleAnimation();
            HandleMovement();

            if (Input.GetKeyDown(KeyCode.E) && currentTower != null)
            {
                TowerHeroManager.instance.SelectTower(currentTower);

            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                interactionMode = !interactionMode;
                SetInteraction(true);
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                interactionMode = !interactionMode;

                SetInteraction(false);
            }
        }

        public new void EnterTowerRange(Tower tower)
        {
            if (currentTower != null)
            {
                currentTower.SetHighlighted(false);
                currentTower.SetInteraction(false);
                currentTower.SetIsSelected(false);
            }
            currentTower = tower;
            currentTower.SetHighlighted(true);
            currentTower.SetInteraction(true);

            Debug.Log(tower.towerName + " ist in Range");
        }

        public new void ExitTowerRange(Tower tower)
        {
            if (tower != currentTower)
                return;

            currentTower.SetHighlighted(false);
            currentTower.SetInteraction(false);
            currentTower.SetIsSelected(false);

            currentTower = null;
            SetInteraction(false);

            TowerHeroManager.instance.DeselectTower();
        }



        private void HandleAnimation()
        {
            bool moving = movementInput.sqrMagnitude > 0.01f;

            if (isAttacking)
            {
                spriteAnim.animState = AnimationState.Attack_Animation;
            }
            else if (moving)
            {
                spriteAnim.animState = AnimationState.Walk_Animation;
            }
            else
            {
                spriteAnim.animState = AnimationState.Idle_Animation;
            }
        }

        private void HandleMovement()
        {
            movementInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            movementInput = movementInput.normalized;

            transform.position += (Vector3)(movementInput * moveSpeed * Time.deltaTime);

            if (movementInput.x < 0)
                transform.rotation = new Quaternion(0,180,0,1);
            else if (movementInput.x > 0)
                transform.rotation = new Quaternion(0,0,0,1);
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
            Quaternion canvasRot = GetComponentInChildren<Canvas>().transform.rotation;
            bool faceRight = targetPos.x < transform.position.x;
            transform.rotation = faceRight
                ? new Quaternion(0, 180, 0, 1)
                : Quaternion.identity;
            GetComponentInChildren<Canvas>().transform.rotation = canvasRot;
        }

        

    }
    
    
}