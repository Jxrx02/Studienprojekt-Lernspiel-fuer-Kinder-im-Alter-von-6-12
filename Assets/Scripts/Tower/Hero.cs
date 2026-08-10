using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TowerDefense
{
    // =====================================================================
    // TASTENBELEGUNG (alle Tasten sind im Inspector frei änderbar):
    //   Bewegung          : WASD / Pfeiltasten (Horizontal/Vertical Axis) - unverändert
    //   Sprint (halten)   : Left Shift
    //   Gehen (halten)    : Left Ctrl   (ohne Modifier = Run/Standardtempo)
    //   Springen          : Left Alt
    //   Dash              : Q
    //   Back Dash         : X
    //   Dodge             : C
    //   Leichter Angriff  : Maus links
    //   Schwerer Angriff  : F
    //   Blocken (halten)  : Maus rechts
    //   Bogen (halten)    : R  (Charge -> Charging -> Release beim Loslassen)
    //   Bogen Hoch(halten): T  (Charge -> Charging -> Release beim Loslassen)
    //   Turm auswählen    : E   - unverändert
    //   Interaktion       : Space - unverändert
    // =====================================================================
    public class Hero : Tower
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4f;    // "Walk"-Tempo (walkKey gehalten)
        [SerializeField] private float runSpeed = 5.5f;   // Standardtempo (kein Modifier)

        [SerializeField] public float maxMoveSpeed = 8f;  // "Sprint"-Tempo (sprintKey gehalten)
        [SerializeField] public float acceleration = 10f; // aktuell ungenutzt, für optionales Ease-In/Out reserviert

        [Header("Legs (unabhängiger Beine-Layer)")]
        [Tooltip("Zweite SpriteAnim-Komponente (z.B. auf einem Kind-GameObject 'Legs'). " +
                 "Spielt nur Idle/Walk/Run - unabhängig davon, was der Oberkörper gerade tut.")]
        [SerializeField] private SpriteAnim legsAnim;

        [Header("Action Keys")]
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode walkKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode jumpKey = KeyCode.LeftAlt;
        [SerializeField] private KeyCode dashKey = KeyCode.Q;
        [SerializeField] private KeyCode backDashKey = KeyCode.X;
        [SerializeField] private KeyCode dodgeKey = KeyCode.C;
        [SerializeField] private KeyCode lightAttackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode heavyAttackKey = KeyCode.F;
        [SerializeField] private KeyCode blockKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode bowChargeKey = KeyCode.R;
        [SerializeField] private KeyCode bowUpChargeKey = KeyCode.T;

        [Header("Action Tuning")]
        [SerializeField] private float dashSpeed = 12f;

        private Vector2 movementInput;
        private Vector2 lastVelocity;
        private FacingDirection facingDirection = FacingDirection.Down;
        private Vector2 dashDirection;

        private bool isDashing;
        private bool isBackDashing;
        private bool isDodging;
        private bool isBlocking;
        private bool isJumping;
        private bool isChargingBow;
        private bool isChargingBowUp;

        private Tower currentTower;

        private bool interactionMode = false;
        private bool hasReachedTargetPosition;

        [Header("Weapons")]
        [SerializeField] private Projectile[] equippedProjectiles;
        [SerializeField] private int activeWeaponIndex;

        [HideInInspector] public Vector2 targetPosition;

        public FacingDirection CurrentFacingDirection => facingDirection;

        private Rigidbody2D rb;

        private void Start()
        {
            TowerHeroManager.instance.RegisterTower(this.gameObject);
            targetPosition = transform.position;

            if (spriteAnim != null)
                spriteAnim.OnDirectionalAnimationComplete += HandleBodyAnimationComplete;

            rb = GetComponent<Rigidbody2D>();
        }

        private void OnDestroy()
        {
            if (spriteAnim != null)
                spriteAnim.OnDirectionalAnimationComplete -= HandleBodyAnimationComplete;
        }

        private void Update()
        {
            ReadMovementInput();
            HandleActionInput();
            HandleMovement();
            HandleAnimation();

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

        // ---------------------------------------------------------------
        // Input
        // ---------------------------------------------------------------

        private void ReadMovementInput()
        {
            movementInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            movementInput = movementInput.normalized;

        }

        private bool CanStartNewAction()
        {
            return !isAttacking && !isDashing && !isBackDashing && !isDodging && !isJumping && !isBlocking;
        }

        private bool CanBlock()
        {
            return !isAttacking && !isDashing && !isBackDashing && !isDodging && !isJumping;
        }

        private void HandleActionInput()
        {
            // Block: gehalten, sperrt alle anderen neuen Aktionen solange die Taste gedrückt ist
            if (Input.GetKey(blockKey) && CanBlock())
                isBlocking = true;
            else if (!Input.GetKey(blockKey))
                isBlocking = false;

            // Bogen-Loslassen muss auch während isAttacking (=Charging) erkannt werden,
            // deshalb NICHT hinter das CanStartNewAction()-Gate stellen.
            HandleBowRelease();

            if (isBlocking) return;
            if (!CanStartNewAction()) return;

            if (Input.GetKeyDown(dodgeKey)) { StartDodge(); return; }
            if (Input.GetKeyDown(dashKey)) { StartDash(); return; }
            if (Input.GetKeyDown(backDashKey)) { StartBackDash(); return; }
            if (Input.GetKeyDown(jumpKey)) { StartJump(); return; }
            if (Input.GetKeyDown(lightAttackKey)) { StartLightAttack(); return; }
            if (Input.GetKeyDown(heavyAttackKey)) { StartHeavyAttack(); return; }
            if (Input.GetKeyDown(bowChargeKey)) { StartBowCharge(); return; }
            if (Input.GetKeyDown(bowUpChargeKey)) { StartBowUpCharge(); return; }
        }

        private void HandleBowRelease()
        {
            if (isChargingBow && Input.GetKeyUp(bowChargeKey))
            {
                isChargingBow = false;
                spriteAnim.SetCharging(false);
            }
            if (isChargingBowUp && Input.GetKeyUp(bowUpChargeKey))
            {
                isChargingBowUp = false;
                spriteAnim.SetCharging(false);
            }
        }

        private void StartDodge()
        {
            isDodging = true;
            spriteAnim.SetState(AnimationState.Dodge);
        }

        private void StartDash()
        {
            isDashing = true;
            dashDirection = movementInput.sqrMagnitude > 0.01f ? movementInput : DirectionToVector(facingDirection);
            spriteAnim.SetState(AnimationState.Dash);
        }

        private void StartBackDash()
        {
            isBackDashing = true;
            dashDirection = -DirectionToVector(facingDirection);
            spriteAnim.SetState(AnimationState.BackDash);
        }

        private void StartJump()
        {
            isJumping = true;
            spriteAnim.SetState(AnimationState.JumpFall);
        }

        private void StartLightAttack()
        {
            isAttacking = true;
            spriteAnim.SetState(AnimationState.LightAttack);
        }

        private void StartHeavyAttack()
        {
            isAttacking = true;
            spriteAnim.SetState(AnimationState.HeavyAttack);
        }

        private void StartBowCharge()
        {
            isChargingBow = true;
            isAttacking = true;
            spriteAnim.SetState(AnimationState.BowRangedAttack);
            spriteAnim.SetCharging(true);
        }

        private void StartBowUpCharge()
        {
            isChargingBowUp = true;
            isAttacking = true;
            spriteAnim.SetState(AnimationState.BowUpAttack);
            spriteAnim.SetCharging(true);
        }

        /// Wird aufgerufen, sobald eine One-Shot-Körperanimation fertig abgespielt ist
        /// (Dash, Back Dash, Dodge, Jump, Light/Heavy/Bow-Attacke) und setzt die zugehörige Sperre zurück.
        private void HandleBodyAnimationComplete(AnimationState finished)
        {
            switch (finished)
            {
                case AnimationState.Dash: isDashing = false; break;
                case AnimationState.BackDash: isBackDashing = false; break;
                case AnimationState.Dodge: isDodging = false; break;
                case AnimationState.JumpFall: isJumping = false; break;
                case AnimationState.LightAttack:
                case AnimationState.HeavyAttack:
                case AnimationState.BowRangedAttack:
                case AnimationState.BowUpAttack:
                    isAttacking = false;
                    break;
            }
        }

        // ---------------------------------------------------------------
        // Bewegung & Blickrichtung
        // ---------------------------------------------------------------

        private void HandleMovement()
        {
            Vector2 velocity;

            if (isDashing || isBackDashing)
            {
                velocity = dashDirection * dashSpeed;
            }
            else if (isDodging)
            {
                Vector2 dodgeDir = movementInput.sqrMagnitude > 0.01f ? movementInput : DirectionToVector(facingDirection);
                velocity = dodgeDir * dashSpeed;
            }
            else if (isAttacking || isBlocking)
            {
                velocity = Vector2.zero; // während Angriff/Block steht der Hero fest
                Debug.Log($"Attack:{isAttacking} Block:{isBlocking}");
            }
            else
            {
                float currentSpeed = runSpeed;
                if (Input.GetKey(sprintKey)) currentSpeed = maxMoveSpeed;
                else if (Input.GetKey(walkKey)) currentSpeed = moveSpeed;

                velocity = movementInput * currentSpeed;
            }

            rb.MovePosition(rb.position + velocity * Time.deltaTime);
            lastVelocity = velocity;

            // Blickrichtung: bevorzugt aus dem Input, sonst aus der tatsächlichen Bewegung
            // (z.B. während Dash/Dodge, wo movementInput evtl. 0 ist).
            Vector2 directionSource = movementInput.sqrMagnitude > 0.01f ? movementInput : velocity;
            if (directionSource.sqrMagnitude > 0.01f)
                facingDirection = DirectionUtility.FromVector(directionSource);

            spriteAnim.SetDirection(facingDirection);
            if (legsAnim != null) legsAnim.SetDirection(facingDirection);
        }

        private static Vector2 DirectionToVector(FacingDirection dir)
        {
            switch (dir)
            {
                case FacingDirection.Up: return Vector2.up;
                case FacingDirection.UpRight: return new Vector2(1f, 1f).normalized;
                case FacingDirection.Right: return Vector2.right;
                case FacingDirection.DownRight: return new Vector2(1f, -1f).normalized;
                case FacingDirection.Down: return Vector2.down;
                case FacingDirection.DownLeft: return new Vector2(-1f, -1f).normalized;
                case FacingDirection.Left: return Vector2.left;
                case FacingDirection.UpLeft: return new Vector2(-1f, 1f).normalized;
                default: return Vector2.down;
            }
        }

        // ---------------------------------------------------------------
        // Animation
        // ---------------------------------------------------------------

        private void HandleAnimation()
        {
            bool isMoving = lastVelocity.sqrMagnitude > 0.0001f;

            // --- Oberkörper: Aktionen haben Vorrang vor der Fortbewegung ---
            if (isBlocking)
            {
                spriteAnim.SetState(AnimationState.Block);
            }
            else if (isDashing || isBackDashing || isDodging || isJumping)
            {
                // Zustand wurde bereits beim Start der jeweiligen Aktion gesetzt (StartDash() etc.)
            }
            else if (isAttacking)
            {
                // Falls isAttacking über den geerbten Tower-Auto-Angriff (Attack()-Override) gesetzt wurde,
                // ohne dass Light/Heavy/Bow-Attacke aktiv gestartet wurde, Standard-Angriffsanimation zeigen.
                bool alreadyPlayingAttack =
                    spriteAnim.animState == AnimationState.LightAttack ||
                    spriteAnim.animState == AnimationState.HeavyAttack ||
                    spriteAnim.animState == AnimationState.BowRangedAttack ||
                    spriteAnim.animState == AnimationState.BowUpAttack;

                if (!alreadyPlayingAttack)
                    spriteAnim.SetState(AnimationState.BowRangedAttack);
            }
            else if (isMoving)
            {
                if (Input.GetKey(sprintKey))
                    spriteAnim.SetState(AnimationState.Sprint);
                else if (Input.GetKey(walkKey))
                    spriteAnim.SetState(AnimationState.Walk);
                else
                    spriteAnim.SetState(AnimationState.Run);
            }
            else
            {
                spriteAnim.SetState(AnimationState.Idle);
            }

            // --- Beine: immer an die tatsächliche Fortbewegung gekoppelt, unabhängig vom Oberkörper ---
            if (legsAnim == null) return;

            if (!isMoving || isBlocking)
            {
                legsAnim.SetState(AnimationState.Idle);
            }
            else if (Input.GetKey(walkKey) && !isDashing && !isBackDashing && !isDodging)
            {
                legsAnim.SetState(AnimationState.Walk);
            }
            else
            {
                legsAnim.SetState(AnimationState.Run); // Run-Frames werden für Run, Sprint, Dash & Dodge wiederverwendet
            }
        }

        // ---------------------------------------------------------------
        // Klick-Bewegung (UI-Button) - unverändert nutzbar
        // ---------------------------------------------------------------

        public void OnHeroMoveButtonPressed()
        {
            OneClickInWorldListener.ListenOnce((Vector3 pos) =>
            {
                if (!isAttacking)
                {
                    targetPosition = pos;

                    if (Vector2.Distance(transform.position, targetPosition) > 0.15f)
                    {
                        spriteAnim.SetState(AnimationState.Walk);
                        spriteAnim.SetSpeedMultiplier(2f);
                        hasReachedTargetPosition = false;

                        Vector2 dir = (Vector2)(targetPosition - (Vector2)transform.position);
                        facingDirection = DirectionUtility.FromVector(dir);
                        spriteAnim.SetDirection(facingDirection);
                        if (legsAnim != null) legsAnim.SetDirection(facingDirection);
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
            // Verhindert mehrfachen Angriff gleichzeitig sowie eine Unterbrechung von Dash/Dodge/Jump
            // durch den geerbten Tower-Auto-Angriff.
            if (isAttacking || isDashing || isBackDashing || isDodging || isJumping) return;

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
            // Mit echten 8-Richtungs-Sprites wird nicht mehr per Transform-Rotation gespiegelt,
            // sondern das passende Richtungs-Sprite ausgewählt.
            Vector2 dir = (Vector2)(targetPos - transform.position);
            facingDirection = DirectionUtility.FromVector(dir);
            spriteAnim.SetDirection(facingDirection);
            if (legsAnim != null) legsAnim.SetDirection(facingDirection);
        }
    }
}