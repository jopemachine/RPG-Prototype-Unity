// ==============================+===============================================================
// @ Author : jopemachine
// ==============================+===============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityChanRPG
{
    public class PlayerDashAttack : StateMachineBehaviour
    {
        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetBool("DamagedProcessed", false);
            animator.SetInteger("AttackState", 0);
            Player.mInstance.state = PlayerSkillState.None;
        }
    }
}