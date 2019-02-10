﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAirAttack : StateMachineBehaviour
{

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("DamagedProcessed", false);
        animator.SetInteger("AttackState", 0);
    }

}
