﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleChargedState : State
{
    private PlayerCombatController _controller;

    public IdleChargedState(PlayerCombatController controller)
    {
        _controller = controller;
    }
    
    public override void Enter()
    {
        Debug.Log("Idle Charged");

        _controller.animator.SetTrigger("StartChargeAttack");

        _controller.AreaAttackChargingSound();
    }

    public override void Interact()
    {
        ExitState();
    }

    public override void ExitState()
    {
        _controller.AreaAttackSound();

        _controller.SetState(new AreaAttackState(_controller, 1));
    }
}
