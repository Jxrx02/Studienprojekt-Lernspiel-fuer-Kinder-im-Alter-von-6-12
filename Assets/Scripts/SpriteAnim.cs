using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AnimationState
{
    Idle_Animation, Attack_Animation, Walk_Animation, Dead_Animation
}

public class SpriteAnim : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField][Tooltip("Only For Shop Icons")] private Boolean loopThrough;
    
    private float attackSpeedMultiplier = 1f;
    private float walkSpeedMultiplier = 1f;
    
    [SerializeField]
    public float mTimePerFrame = .125f;

    private SpriteRenderer sr = null;
    private Image img = null;

    [SerializeField]
    public bool loop = true;

    public Sprite[] idle_sprites = null;

    public Sprite[] attack_sprites = null;

    public Sprite[] evolve_sprites = null;

    public Sprite[] dead_sprites = null;

    public Sprite[] walk_sprites = null;

    private float mElapsedTime = 0f;
    private int mCurrentFrame = 0;
    [HideInInspector]public Boolean destroyOnEndDeadAnimation;
    [SerializeField]public AnimationState animState { get; set; }
    
    [HideInInspector]
    public Action OnIdleAnimationComplete, OnAttackAnimationComplete, OnWalkAnimationComplete, OnDeadAnimationComplete;
    

    void Start()
    {
        // Versuche zuerst, den SpriteRenderer zu bekommen
        sr = GetComponent<SpriteRenderer>();
        img = GetComponent<Image>();
        animState = AnimationState.Idle_Animation;
        Play();
    }

    void Play()
    {
        enabled = true;
    }

    void Update()
    {
        float currentSpeedMultiplier = 1f; // Standardgeschwindigkeit

        if (animState == AnimationState.Attack_Animation)
        {
            currentSpeedMultiplier = attackSpeedMultiplier;
        }
        else if (animState == AnimationState.Walk_Animation)
        {
            currentSpeedMultiplier = walkSpeedMultiplier;
        }

        mElapsedTime += (speed * currentSpeedMultiplier) * Time.deltaTime;
        if (mElapsedTime >= mTimePerFrame)
        {
            mElapsedTime = 0;
            ++mCurrentFrame;
            SetSprite();
        }
    }
    public void SetAttackSpeed(float multiplier)
    {
        attackSpeedMultiplier = Mathf.Max(0.1f, multiplier); // Minimum, um Hänger zu vermeiden
    }
    public void SetWalkSpeed(float multiplier)
    {
        attackSpeedMultiplier = Mathf.Max(0.1f, multiplier); 
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
            // Je nach AnimationState das richtige Sprite setzen
            switch (animState)
            {
                case AnimationState.Idle_Animation:
                    if (mCurrentFrame >= 0 && mCurrentFrame < idle_sprites.Length)
                        SetSpriteRendererOrImage(idle_sprites[mCurrentFrame]);

                    if (mCurrentFrame >= idle_sprites.Length)
                    {
                        if (loop)
                            mCurrentFrame = 0;
                        else
                            enabled = false;
                        
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
                        OnAttackAnimationComplete?.Invoke(); // Event nur am Ende auslösen
                        AdvanceToNextAnimationIfLooping();
                    }
                    break;


                case AnimationState.Walk_Animation:
                    if (mCurrentFrame >= 0 && mCurrentFrame < walk_sprites.Length)
                        SetSpriteRendererOrImage(walk_sprites[mCurrentFrame]);

                    if (mCurrentFrame >= walk_sprites.Length)
                    {
                        if (loop)
                            mCurrentFrame = 0;
                        else
                            enabled = false;
                        
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
                        {
                            Destroy(gameObject);
                        }

                        AdvanceToNextAnimationIfLooping();
                    }
                    
                    break;
            }

        }
        catch { }
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
