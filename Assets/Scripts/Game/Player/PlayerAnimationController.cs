﻿using System;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private IPlayerAppearance appearance;
    private Animator defaultAnimator;
    private Animator activeAnimator;
    private PlayerAnimationState animationState;
    private float idleTimer;

    public bool IsMoving;

    // Use this for initialization
    void Start()
    {
        if (appearance == null)
            appearance = (IPlayerAppearance)GetComponent<SyntyPlayerAppearance>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!EnsureAnimator())
        {
            return;
        }

        idleTimer += Time.deltaTime;

        if (animationState != PlayerAnimationState.Idle)
        {
            idleTimer = 0f;
        }

        if (idleTimer >= 5f)
        {
            TriggerBored();
            idleTimer = 0f;
        }
    }

    #region General

    public void ResetActiveAnimator()
    {
        activeAnimator = defaultAnimator;
    }

    public void SetActiveAnimator(Animator animator)
    {
        activeAnimator = animator;
        for (var i = 0; i < defaultAnimator.parameterCount; ++i)
        {
            var parameter = defaultAnimator.parameters[i];
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    activeAnimator.SetBool(parameter.name, defaultAnimator.GetBool(parameter.name));
                    break;
                case AnimatorControllerParameterType.Float:
                    activeAnimator.SetFloat(parameter.name, defaultAnimator.GetFloat(parameter.name));
                    break;
                case AnimatorControllerParameterType.Int:
                    activeAnimator.SetInteger(parameter.name, defaultAnimator.GetInteger(parameter.name));
                    break;
                case AnimatorControllerParameterType.Trigger:
                    activeAnimator.SetBool(parameter.name, defaultAnimator.GetBool(parameter.name));
                    break;
            }
        }
        //activeAnimator.playbackTime = defaultAnimator.playbackTime;
        ResetAnimationStates();
    }

    private void SetTrigger(string trigger)
    {
        activeAnimator.SetTrigger(trigger);
        if (activeAnimator != defaultAnimator)
            defaultAnimator.SetTrigger(trigger);
    }

    private void SetInteger(string key, int value)
    {
        activeAnimator.SetInteger(key, value);
        if (activeAnimator != defaultAnimator)
            defaultAnimator.SetInteger(key, value);
    }

    private void SetBool(string key, bool value)
    {
        activeAnimator.SetBool(key, value);
        if (activeAnimator != defaultAnimator)
            defaultAnimator.SetBool(key, value);
    }
    public void ForceCheer()
    {
        if (!EnsureAnimator()) return;
        SetTrigger("ForceCheer");
    }

    public void Death()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Dead;
        SetBool("Dead", true);
        SetTrigger("Died");
        StopMoving(true);
    }

    public void SetCaptainState(bool isCaptain)
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Captain;
        SetBool("Captain", isCaptain);
        StopMoving(true);
    }

    public void Revive()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Dead", false);
        StopMoving(true);
    }

    private void TriggerBored()
    {
        if (!EnsureAnimator()) return;
        if (animationState == PlayerAnimationState.Idle)
            SetTrigger("Bored");
    }
    public void Cheer()
    {
        if (!EnsureAnimator()) return;
        SetTrigger("Cheer");
    }
    internal void Sit()
    {
        if (!EnsureAnimator()) return;
        SetBool("Sitting", true);
    }
    internal void Swim()
    {
        if (!EnsureAnimator()) return;
        SetBool("Swimming", true);
    }
    internal void Meditate()
    {
        if (!EnsureAnimator()) return;
        SetBool("Meditating", true);
    }
    internal void ClearOnsenAnimations()
    {
        if (!EnsureAnimator()) return;
        SetBool("Sitting", false);
        SetBool("Swimming", false);
        SetBool("Meditating", false);
    }

    public void StartMoving(bool force = false)
    {
        if ((!force && IsMoving) || !EnsureAnimator()) return;
        SetBool("Moving", true);
        IsMoving = true;
    }
    public void StopMoving(bool force = false)
    {
        if ((!force && !IsMoving) || !EnsureAnimator()) return;
        SetBool("Moving", false);
        IsMoving = false;
    }
    #endregion

    #region Combat
    public void StartCombat(int weapon, bool hasShield)
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Attacking;
        ResetAnimationStates();
        SetTrigger("Start");
        SetInteger("Weapon", weapon);
        SetBool("Attacking", true);
        SetBool("Shield", hasShield);
    }

    public bool IsAttacking()
    {
        if (!EnsureAnimator()) return false;
        return animationState == PlayerAnimationState.Attacking ||
               activeAnimator.GetBool("Attacking");
    }

    public void Attack(int weapon, int action = 0, bool hasShield = false)
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Attacking;
        SetInteger("Weapon", weapon);
        SetInteger("Action", action);
        SetBool("Attacking", true);
        SetBool("Shield", hasShield);
        SetTrigger("Attack");
    }

    public void EndCombat()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Attacking", false);
    }
    #endregion

    #region Mining
    public void StartMining()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        activeAnimator.SetTrigger("Start");
        Mine();
    }
    public void Mine()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Mining;
        activeAnimator.SetBool("Mining", true);
    }
    public void EndMining()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        activeAnimator.SetBool("Mining", false);
    }
    #endregion

    #region Woodcutting
    public void StartWoodcutting()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Woodcutting;
        ResetAnimationStates();
        SetTrigger("Start");
        SetBool("Woodcutting", true);
    }

    public void Chop(int animation)
    {
        if (!EnsureAnimator()) return;
        SetBool("Woodcutting", true);
        SetInteger("Action", animation);
        SetTrigger("Chop");
    }

    public void EndWoodcutting()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Woodcutting", false);
    }
    #endregion

    #region Fishing
    public void StartFishing()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        SetTrigger("Start");
        Fish();
    }

    public void Fish()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Fishing;
        SetBool("Fishing", true);
    }

    public void EndFishing()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Fishing", false);
    }
    #endregion

    #region Crafting
    public void StartCrafting()
    {
        if (!EnsureAnimator()) return;
        ResetAnimationStates();
        SetTrigger("Start");
        Craft();
    }

    public void Craft()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Crafting;
        SetBool("Crafting", true);
    }

    public void EndCrafting()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Crafting", false);
    }
    #endregion

    #region Farming
    public void StartFarming()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Farming;
        ResetAnimationStates();
        SetTrigger("Start");
        SetBool("Farming", true);
    }

    public void EndFarming()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Farming", false);
    }
    #endregion

    #region Farming
    public void StartCooking()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Cooking;
        ResetAnimationStates();
        SetTrigger("Start");
        SetBool("Cooking", true);
    }

    public void EndCooking()
    {
        if (!EnsureAnimator()) return;
        animationState = PlayerAnimationState.Idle;
        SetBool("Cooking", false);
    }
    #endregion

    private bool EnsureAnimator()
    {
        if (appearance == null)
            appearance = (IPlayerAppearance)GetComponent<SyntyPlayerAppearance>();

        if (appearance is MonoBehaviour behaviour)
        {
            //  && !appearance.AppearanceUpdated()
            if (activeAnimator) return true;
            if (!behaviour || !behaviour.transform) return false;
            activeAnimator = behaviour.transform.GetComponentInChildren<Animator>();
            defaultAnimator = activeAnimator;
        }

        return activeAnimator;
    }

    public void ResetAnimationStates()
    {
        EndCombat();
        EndCooking();
        EndCrafting();
        EndFarming();
        EndFishing();
        EndMining();
        EndWoodcutting();
    }
}

internal enum PlayerAnimationState
{
    Idle,
    Dead,
    Attacking,
    Mining,
    Crafting,
    Woodcutting,
    Cooking,
    Fishing,
    Farming,
    Drinking,
    Eating,
    Captain
}
