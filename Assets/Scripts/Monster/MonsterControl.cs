﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterControl : MonoBehaviour
{
    private Monster monster;

    public enum MonsterState
    {
        Idle, // 대기 상태. Wait00

        Chasing, // 플레이어 추적 상태. OverlapSphere 으로 Trigger 발동되면 Chasing 으로 변화하되, 탐색 범위는 몬스터의 앞 쪽을 기준으로 해서 반구를 그려야 됨.

        Roaming, // 랜덤으로 운동. 이동 상태는 랜덤으로 정해지되, 자연스러운 움직임을 위해, 방향이 꺽이지 않고

        Attacking, // 공격 상태

        DashAttacking,

        Airbone, // 공중에 떠 있는 상태

        Stun, // 데미지를 받으면 일정 확률로 스턴 상태에 돌입

        Damaged, // 데미지를 받고 있는 상태

        Death, // 죽어 있는 상태
    }

    public Transform monsterTr;
    public Transform playerTr;
    public NavMeshAgent nvAgent;
    public MonsterState AIState;

    public Transform patrolArea;

    private static WaitForSeconds CheckingTime;
    private static WaitForSeconds IdleTime;
    private static WaitForSeconds RoamingTime;

    private bool IsGrounded;
    private bool IsAttacking;
    private bool IsDashAttack;
    private bool IsDied;
    private bool IsStuned;
    private bool IsDamaged;

    private Vector3 movingDirection;

    // 바닥 감지 거리
    private float GroundCheckDistance;
    // 공격 거리
    public float attackDistance; 
    // 탐지 거리
    public float detectionDistance;
    // 대쉬 어택 거리
    public float dashAttackDistance;

    private Animator animator;


    private void Start()
    {
        monster = GetComponent<MonsterAdapter>().monster;
        monsterTr = GetComponent<Transform>();
        animator = GetComponent<Animator>();
        playerTr = GameObject.FindWithTag("Player").GetComponent<Transform>();
        nvAgent = GetComponent<NavMeshAgent>();
        AIState = MonsterState.Idle;
        CheckingTime = new WaitForSeconds(0.2f);
        IdleTime = new WaitForSeconds(2.4f);
        RoamingTime = new WaitForSeconds(6.0f);

        attackDistance = 2.0f;
        dashAttackDistance = 7.0f;
        detectionDistance = 14.5f;

        nvAgent.speed = 3;

        StartCoroutine(this.CheckMonsterAI());
        StartCoroutine(this.MonsterAction());
    }

    // Fixed update is called in sync with physics
    private void FixedUpdate()
    {
        CheckGroundStatus();
    }

    IEnumerator CheckMonsterAI()
    {
        while (IsDied == false)
        {
            yield return CheckingTime;

            #region Status Decision 1
            float DistanceFromPlayer = Vector3.Distance(playerTr.position, monsterTr.position);

            // 플레이어가 탐지 거리 내로 들어오면 추적 시작
            if (DistanceFromPlayer < detectionDistance && DistanceFromPlayer > attackDistance)
            {
                AIState = MonsterState.Chasing;
            }

            // 플레이어가 애매한 거리에 있으면 대쉬 공격으로 거리를 좁히며 공격
            else if (DistanceFromPlayer < dashAttackDistance && DistanceFromPlayer > attackDistance)
            {
                AIState = MonsterState.DashAttacking;
            }

            // 플레이어가 공격 거리 내로 들어오면 공격 시작
            else if (DistanceFromPlayer < attackDistance)
            {
                AIState = MonsterState.Attacking;
            }

            // 아무 상태도 아니라면, Idle 상태로 대기하다, RomingTime 만큼 돌아다니는 것을 반복
            else
            {
                AIState = MonsterState.Idle;
                yield return IdleTime;
                AIState = MonsterState.Roaming;
                yield return RoamingTime;
            }
            #endregion

            #region Status Decision 2
            // 아래의 조건이 켜져 있다면 아래의 상태 변화를 우선시한다.

            // 데미지를 받고 있는 상태
            if (IsDamaged == true)
            {
                // 지상에서 공격 받음
                if (IsGrounded == true)
                {
                    AIState = MonsterState.Damaged;
                }
                // 공중에서 공격 받음
                else
                {
                    AIState = MonsterState.Airbone;
                    IsStuned = false;
                }

                if (monster.currentHP < 0)
                {
                    AIState = MonsterState.Death;
                    IsDied = true;
                }

            }

            // 스턴 상태
            if (IsStuned == true && IsGrounded == true)
            {
                AIState = MonsterState.Stun;
            }

            #endregion


        }

    }

    IEnumerator MonsterAction()
    {
        while (IsDied == false)
        {
            switch (AIState)
            {
                case MonsterState.Airbone:
                    {
                        animator.SetBool("IsAttacking", false);
                        animator.SetBool("IsAirDamaged", true);
                        animator.SetBool("IsDashAttacking", false);
                        animator.SetBool("IsGrounded", false);
                        animator.SetBool("IsMoving", false);
                        animator.SetBool("IsStunned", false);
                        break;
                    }
                case MonsterState.Attacking:
                    {
                        nvAgent.Stop();
                        animator.SetBool("IsAttacking", true);
                        animator.SetBool("IsAirDamaged", true);
                        animator.SetBool("IsDashAttacking", false);
                        animator.SetBool("IsGrounded", true);
                        animator.SetBool("IsMoving", false);
                        animator.SetBool("IsStunned", false);
                        break;
                    }
                case MonsterState.Chasing:
                    {
                        nvAgent.ResetPath();
                        nvAgent.SetDestination(playerTr.position);

                        animator.SetBool("IsAttacking", false);
                        animator.SetBool("IsAirDamaged", true);
                        animator.SetBool("IsDashAttacking", false);
                        animator.SetBool("IsGrounded", true);
                        animator.SetBool("IsMoving", true);
                        animator.SetBool("IsStunned", false);
                        break;
                    }
                case MonsterState.Damaged:
                    {
                        animator.SetBool("IsAttacking", false);
                        animator.SetBool("IsDashAttacking", false);
                        animator.SetBool("IsDamaged", true);
                        animator.SetBool("IsMoving", false);
                        break;
                    }

                case MonsterState.DashAttacking:
                    {
                        animator.SetBool("IsAttacking", false);
                        animator.SetBool("IsAirDamaged", false);
                        animator.SetBool("IsDashAttacking", true);
                        animator.SetBool("IsGrounded", true);
                        animator.SetBool("IsMoving", true);
                        animator.SetBool("IsStunned", false);
                        break;
                    }
                case MonsterState.Death:
                    {
                        animator.SetBool("IsDied", true);
                        break;
                    }

                case MonsterState.Idle:
                    {
                        animator.SetBool("IsAttacking", false);
                        animator.SetBool("IsAirDamaged", false);
                        animator.SetBool("IsDashAttacking", false);
                        animator.SetBool("IsGrounded", true);
                        animator.SetBool("IsMoving", false);
                        animator.SetBool("IsStunned", false);
                        break;
                    }

                case MonsterState.Roaming:
                    {
                        movingDirection = RandomDecideRoamingDirection();
                        nvAgent.ResetPath();
                        nvAgent.SetDestination(RandomDecideRoamingDirection());

                        animator.SetBool("IsAttacking", false);
                        animator.SetBool("IsAirDamaged", false);
                        animator.SetBool("IsDashAttacking", false);
                        animator.SetBool("IsGrounded", true);
                        animator.SetBool("IsMoving", true);
                        animator.SetBool("IsStunned", false);
                        break;
                    }
                case MonsterState.Stun:
                    {
                        Debug.Log("실행 갱신");
                        nvAgent.Stop();

                        animator.SetBool("IsAttacking", false);
                        animator.SetBool("IsAirDamaged", false);
                        animator.SetBool("IsDashAttacking", false);
                        animator.SetBool("IsGrounded", true);
                        animator.SetBool("IsMoving", false);
                        animator.SetBool("IsStunned", true);
                        break;
                    }
            }

            yield return null;
        }
    }

    // 몬스터가 Roaming할 방향을 난수 생성으로 결정
    private Vector3 RandomDecideRoamingDirection()
    {
        return new Vector3(100,100,100);

    }


    private void CheckGroundStatus()
    {
        RaycastHit hitInfo;

        if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, GroundCheckDistance))
        {
            IsGrounded = true;
        }
        else
        {
            IsGrounded = false;
        }
    }

}

