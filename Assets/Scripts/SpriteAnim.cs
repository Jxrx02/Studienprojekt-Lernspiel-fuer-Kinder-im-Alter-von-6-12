using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------------
// Alte Animationszustände (Idle_Animation / Attack_Animation / Walk_Animation /
// Dead_Animation) bleiben unverändert erhalten -> z.B. Shop-Icons funktionieren
// exakt wie bisher, mit den flachen idle_sprites/attack_sprites/... Arrays.
//
// Neue Zustände (ohne "_Animation"-Suffix) sind die 8-Richtungs-Animationen
// des Hero-Assets. Sie benutzen die "directionalAnimations"-Liste weiter unten.
// -----------------------------------------------------------------------
public enum AnimationState
{
    // Legacy, nicht-direktional (z.B. Shop-Icon-Diashow)
    Idle_Animation, Attack_Animation, Walk_Animation, Dead_Animation,

    // Hero: 8-Richtungs-Animationen
    Idle, Block, Walk, Run, Sprint, JumpFall, Dash, BackDash, Dodge,
    LightAttack, HeavyAttack, BowRangedAttack, BowUpAttack
}

public enum FacingDirection
{
    Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
}

public static class DirectionUtility
{
    // Reihenfolge muss zum Winkel-Bucketing unten passen (0° = Right, 45° = UpRight, ...)
    private static readonly FacingDirection[] Order =
    {
        FacingDirection.Right, FacingDirection.UpRight, FacingDirection.Up, FacingDirection.UpLeft,
        FacingDirection.Left, FacingDirection.DownLeft, FacingDirection.Down, FacingDirection.DownRight
    };

    /// Wandelt einen 2D-Vektor (z.B. Bewegungs- oder Blickrichtung) in eine der 8 Himmelsrichtungen um.
    public static FacingDirection FromVector(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f)
            return FacingDirection.Down;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;

        int index = Mathf.RoundToInt(angle / 45f) % 8;
        return Order[index];
    }
}

/// Ein Satz von 8 Sprite-Sequenzen, eine pro Blickrichtung, für genau eine Animation
/// (z.B. "Dash" oder "Walk"). Im Inspector einfach die passenden Frames einziehen.
[Serializable]
public class DirectionalFrames
{
    public Sprite[] up;
    public Sprite[] upRight;
    public Sprite[] right;
    public Sprite[] downRight;
    public Sprite[] down;
    public Sprite[] downLeft;
    public Sprite[] left;
    public Sprite[] upLeft;

    public Sprite[] Get(FacingDirection dir)
    {
        switch (dir)
        {
            case FacingDirection.Up: return up;
            case FacingDirection.UpRight: return upRight;
            case FacingDirection.Right: return right;
            case FacingDirection.DownRight: return downRight;
            case FacingDirection.Down: return down;
            case FacingDirection.DownLeft: return downLeft;
            case FacingDirection.Left: return left;
            case FacingDirection.UpLeft: return upLeft;
            default: return down;
        }
    }
}

[Serializable]
public class StateAnimationEntry
{
    public AnimationState state;
    public DirectionalFrames frames;

    [Tooltip("An für loopende Animationen (Idle, Walk, Run, Sprint, Block). " +
             "Aus für One-Shot-Animationen (Angriffe, Dash, Dodge, Jump).")]
    public bool loop = true;

    [Header("Charge (optional, z.B. für Bow-Attacken)")]
    [Tooltip("Aktivieren für Animationen mit Charge/Charging/Release-Phasen (Bow Ranged Atk, Bow UP Atk).")]
    public bool isChargeable = false;
    [Tooltip("Frame-Index, ab dem die 'Charging'-Schleife beginnt (0-basiert).")]
    public int chargeLoopStartFrame = 0;
    [Tooltip("Letzter Frame-Index der 'Charging'-Schleife, bevor 'Release' folgt.")]
    public int chargeLoopEndFrame = 0;
}

public class SpriteAnim : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField][Tooltip("Only For Shop Icons")] private Boolean loopThrough;

    private float attackSpeedMultiplier = 1f;
    private float walkSpeedMultiplier = 1f;

    // Genereller Tempo-Multiplikator für alle neuen (Hero-)Richtungs-Zustände.
    private float directionalSpeedMultiplier = 1f;

    [SerializeField]
    public float mTimePerFrame = .125f;

    private SpriteRenderer sr = null;
    private Image img = null;

    [SerializeField]
    public bool loop = true;

    [Header("Legacy Sprites (Shop-Icons etc. - unverändert)")]
    public Sprite[] idle_sprites = null;
    public Sprite[] attack_sprites = null;
    public Sprite[] evolve_sprites = null;
    public Sprite[] dead_sprites = null;
    public Sprite[] walk_sprites = null;

    [Header("Hero: 8-Richtungs-Animationen")]
    [Tooltip("Ein Eintrag pro Zustand (Idle, Walk, Run, ... ). Für den Beine-Layer nur Idle/Walk/Run befüllen.")]
    public List<StateAnimationEntry> directionalAnimations = new List<StateAnimationEntry>();

    private Dictionary<AnimationState, StateAnimationEntry> _lookup;
    private FacingDirection currentDirection = FacingDirection.Down;
    private bool isCharging = false;

    private float mElapsedTime = 0f;
    private int mCurrentFrame = 0;
    [HideInInspector] public Boolean destroyOnEndDeadAnimation;
    [field: SerializeField] public AnimationState animState { get; set; }

    [HideInInspector]
    public Action OnIdleAnimationComplete, OnAttackAnimationComplete, OnWalkAnimationComplete, OnDeadAnimationComplete;

    /// Wird einmalig ausgelöst, wenn eine nicht-loopende Hero-Richtungsanimation fertig abgespielt ist
    /// (z.B. Dash, Dodge, Jump, Light/Heavy/Bow-Attacke).
    public Action<AnimationState> OnDirectionalAnimationComplete;

    void Awake()
    {
        BuildLookup();
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        img = GetComponent<Image>();
        animState = AnimationState.Idle_Animation;
        Play();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<AnimationState, StateAnimationEntry>();
        foreach (var entry in directionalAnimations)
        {
            if (entry != null && !_lookup.ContainsKey(entry.state))
                _lookup.Add(entry.state, entry);
        }
    }

    void Play()
    {
        enabled = true;
    }

    void Update()
    {
        float currentSpeedMultiplier;

        if (animState == AnimationState.Attack_Animation)
            currentSpeedMultiplier = attackSpeedMultiplier;
        else if (animState == AnimationState.Walk_Animation)
            currentSpeedMultiplier = walkSpeedMultiplier;
        else
            currentSpeedMultiplier = directionalSpeedMultiplier;

        mElapsedTime += (speed * currentSpeedMultiplier) * Time.deltaTime;
        if (mElapsedTime >= mTimePerFrame)
        {
            mElapsedTime = 0;
            ++mCurrentFrame;

            // Solange eine chargeable Animation (z.B. Bow) gehalten wird, in der "Charging"-Schleife bleiben.
            if (_lookup != null && isCharging &&
                _lookup.TryGetValue(animState, out var chargingEntry) &&
                chargingEntry.isChargeable && mCurrentFrame > chargingEntry.chargeLoopEndFrame)
            {
                mCurrentFrame = chargingEntry.chargeLoopStartFrame;
            }

            SetSprite();
        }
    }

    public void SetAttackSpeed(float multiplier)
    {
        attackSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void SetWalkSpeed(float multiplier)
    {
        // Fix: hat vorher fälschlich attackSpeedMultiplier gesetzt und hatte dadurch nie einen Effekt.
        walkSpeedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    /// Genereller Tempo-Multiplikator für die neuen Hero-Zustände (Walk/Run/Sprint/Attacken/...).
    /// Wird bei jedem Zustandswechsel (SetState) automatisch auf 1 zurückgesetzt.
    public void SetSpeedMultiplier(float multiplier)
    {
        directionalSpeedMultiplier = Mathf.Max(0.05f, multiplier);
    }

    /// Legt fest, welches der 8 Sprite-Arrays abgespielt wird. Der aktuelle Frame bleibt erhalten,
    /// damit ein Richtungswechsel mitten in der Animation nicht neu startet (z.B. beim Drehen im Laufen).
    public void SetDirection(FacingDirection dir)
    {
        currentDirection = dir;
    }

    public FacingDirection GetDirection() => currentDirection;

    /// Wechselt den aktuellen Hero-Animationszustand. Setzt den Frame-Zähler nur zurück, wenn sich der
    /// Zustand tatsächlich ändert - dadurch kann man das z.B. jeden Frame mit demselben Zustand (Walk)
    /// aufrufen, ohne dass die Animation ruckelt/neustartet.
    public void SetState(AnimationState newState, bool forceRestart = false)
    {
        if (animState == newState && !forceRestart) return;

        animState = newState;
        mCurrentFrame = 0;
        mElapsedTime = 0f;
        isCharging = false;
        directionalSpeedMultiplier = 1f;
        enabled = true;
    }

    /// Hält eine chargeable Richtungsanimation (z.B. Bow) in ihrer "Charging"-Schleife, solange true.
    /// Auf false setzen, damit die Animation bis zu den Release-Frames weiterläuft.
    public void SetCharging(bool charging)
    {
        isCharging = charging;
    }

    public void TriggerDeadAnimation(Boolean destroyOnEndDeadAnimation)
    {
        animState = AnimationState.Dead_Animation;
        mCurrentFrame = 0;
        mElapsedTime = 0f;
        this.destroyOnEndDeadAnimation = destroyOnEndDeadAnimation;
    }

    private void Pause()
    {
        enabled = false;
    }

    private void SetSprite()
    {
        try
        {
            switch (animState)
            {
                case AnimationState.Idle_Animation:
                    if (mCurrentFrame >= 0 && mCurrentFrame < idle_sprites.Length)
                        SetSpriteRendererOrImage(idle_sprites[mCurrentFrame]);

                    if (mCurrentFrame >= idle_sprites.Length)
                    {
                        if (loop) mCurrentFrame = 0;
                        else enabled = false;

                        OnIdleAnimationComplete?.Invoke();
                        AdvanceToNextAnimationIfLooping();
                    }
                    break;

                case AnimationState.Attack_Animation:
                    if (mCurrentFrame >= 0 && mCurrentFrame < attack_sprites.Length)
                        SetSpriteRendererOrImage(attack_sprites[mCurrentFrame]);

                    if (mCurrentFrame >= attack_sprites.Length)
                    {
                        animState = AnimationState.Idle_Animation;
                        OnAttackAnimationComplete?.Invoke();
                        AdvanceToNextAnimationIfLooping();
                    }
                    break;

                case AnimationState.Walk_Animation:
                    if (mCurrentFrame >= 0 && mCurrentFrame < walk_sprites.Length)
                        SetSpriteRendererOrImage(walk_sprites[mCurrentFrame]);

                    if (mCurrentFrame >= walk_sprites.Length)
                    {
                        if (loop) mCurrentFrame = 0;
                        else enabled = false;

                        OnWalkAnimationComplete?.Invoke();
                        AdvanceToNextAnimationIfLooping();
                    }
                    break;

                case AnimationState.Dead_Animation:
                    if (mCurrentFrame >= 0 && mCurrentFrame < dead_sprites.Length)
                        SetSpriteRendererOrImage(dead_sprites[mCurrentFrame]);

                    if (mCurrentFrame >= dead_sprites.Length)
                    {
                        OnDeadAnimationComplete?.Invoke();
                        if (destroyOnEndDeadAnimation)
                            Destroy(gameObject);

                        AdvanceToNextAnimationIfLooping();
                    }
                    break;

                default:
                    PlayDirectionalFrame();
                    break;
            }
        }
        catch { }
    }

    private void PlayDirectionalFrame()
    {
        if (_lookup == null) BuildLookup();

        if (!_lookup.TryGetValue(animState, out StateAnimationEntry entry) || entry.frames == null)
            return;

        Sprite[] frames = entry.frames.Get(currentDirection);
        if (frames == null || frames.Length == 0)
            return;

        if (mCurrentFrame >= 0 && mCurrentFrame < frames.Length)
            SetSpriteRendererOrImage(frames[mCurrentFrame]);

        if (mCurrentFrame >= frames.Length)
        {
            if (entry.loop)
            {
                mCurrentFrame = 0;
            }
            else
            {
                mCurrentFrame = frames.Length - 1; // letzten Frame halten
                enabled = false;
                OnDirectionalAnimationComplete?.Invoke(entry.state);
            }
        }
    }

    // Diese Methode setzt entweder das Sprite im SpriteRenderer oder im Image, je nachdem, welche Komponente vorhanden ist
    private void SetSpriteRendererOrImage(Sprite sprite)
    {
        if (sr != null)
        {
            sr.sprite = sprite;
        }
        else if (img != null)
        {
            img.sprite = sprite;
        }
    }

    //Logik für Shop Diashow
    private AnimationState[] loopSequence = new AnimationState[]
    {
        AnimationState.Attack_Animation,
        AnimationState.Walk_Animation,
        //AnimationState.Dead_Animation
    };

    private int currentLoopIndex = 0;

    private void AdvanceToNextAnimationIfLooping()
    {
        if (loopThrough)
        {
            currentLoopIndex = (currentLoopIndex + 1) % loopSequence.Length;
            this.animState = loopSequence[currentLoopIndex];
            mCurrentFrame = 0;
        }
    }
}