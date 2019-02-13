﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 아래 스크립트의 작성은 Stardard Asset의 ThirdPersonControl와
// http://www.yes24.com/24/goods/27894042 도서를 참고함

/// <summary>
/// 
/// 
/// </summary>

public class PlayerControl : MonoBehaviour, IInteractAble
{
    #region Variables
    private Player player;
    private Status status;

    public float JumpPower;
    public float MoveSpeed;

    private Vector3 CamForward;
    private Vector3 MoveVector;
    private Vector3 GroundNormal;
    private Vector3 CapsuleCenter;

    private const float MOVING_TURN_SPEED = 360;
    private const float STATIONARY_TURN_SPEED = 180;

    private float TurnAmount;
    private float ForwardAmount;
    private float CapsuleHeight;

    private bool IsJump;
    private bool IsOnGrounded;
    private bool IsKickAttacking;
    private bool IsPunchAttacking;
    private bool IsAirAttacking;
    private bool IsDashAttack;
    private bool IsFalling;

    private Animator Animator;
    private AudioManager voiceAudioManager;
    private AudioManager moveAudioManager;
    private Transform playerTr;
    private Transform cam;
    private new Rigidbody rigidbody;

    // 스태미나 소모, 회복량의 Default 값 (후에 아이템 사용등으로 변경 가능하게 하기 위해 const로 놓지 않음)
    public float staminaRecoverMultiplier = 15f;
    public float staminaUseMultiplier = 10f;

    // 일정 시간 이상 키보드 Input이 들어오지 않으면 랜덤으로 3개의 Waiting motion 중 하나를 재생
    private const float waitingTimeForWaitingMotion = 15.0f;
    private float waitingTimeForWaitingMotionTimer;

    // 레이 캐스팅을 통해 측정된 거리가 GroundCheckDistance 보다 작다면 지면에 있는 것으로 처리
    private float GroundCheckDistance;
    private float DistanceFromGround;

    // AttackArea의 컬라이더들을 Attack 애니메이션에 따라 활성화, 비활성화 함
    private AttackArea LeftHand;
    private AttackArea RightHand;
    private AttackArea LeftFoot;
    private AttackArea RightFoot;

    private CharacterController controller;
    private const float gravityValue = 15f;
    // 캐릭터 컴포넌트를 움직이기 위한 속도 변수
    public Vector3 currentVelocity;

    // 공격 애니메이션 String을 담는 변수
    public string AnimationNameString;

    // TerrainSlope가 CharacterController의 Slope Limit보다 높을 때, 즉 허가되지 않은 높이의 지형에 점프했을 땐,
    // Rigidbody로 움직이게 하고, 미끄러져 Slope가 Limit보다 작아질 때 까지 입력을 받지 않는다.
    public float TerrainSlope;
    public bool IsSliding;
    private const float SlideTime = 1.0f;

    private int TerrainLayer;

    #endregion

    #region Sound Processing by Animation State

    private enum Voice
    {
        attack,
        attack2,
        jump,
        yay,
        hehehe,
        hahaha,
        ouch,
        damage,
        hmm,
        wow
    }

    private enum MoveSound
    {
        walk
    }

    private void HandleVoiceSoundByAnimation(Voice voice)
    {
        int voiceInt = (int)voice;
        float volume = 1f;

        switch (voice)
        {
            case Voice.attack:
                break;
            case Voice.attack2:
                break;
            case Voice.jump:
                break;
            case Voice.yay:
                break;
            case Voice.hehehe:
                break;
            case Voice.hahaha:
                break;
            case Voice.ouch:
                break;
            case Voice.damage:
                break;
            case Voice.hmm:
                break;
            case Voice.wow:
                break;
            default:
                Debug.Assert(false, "unexpected value");
                break;
        }

        voiceAudioManager.Play(voiceInt, volume);
    }

    private void HandleMoveSoundByAnimation(MoveSound moveSound)
    {
        int moveSoundInt = (int)moveSound;
        float volume = 1f;

        switch (moveSound)
        {
            case MoveSound.walk:
                volume = 35;
                break;
            default:
                Debug.Assert(false, "unexpected value");
                break;
        }

        moveAudioManager.Play(moveSoundInt, volume);
    }

    #endregion

    #region Initialize
    void Start()
    {
        player = GetComponent<Player>();
        status = GetComponent<Status>();
        Animator = GetComponent<Animator>();
        playerTr = GetComponent<Transform>();
        controller = GetComponent<CharacterController>();
        rigidbody = GetComponent<Rigidbody>();

        TerrainLayer = LayerMask.NameToLayer("Terrain");

        cam = GameObject.FindGameObjectWithTag("MainCamera").gameObject.GetComponent<Transform>();
        voiceAudioManager = transform.Find("Sound").Find("Voice").gameObject.GetComponent<AudioManager>();
        moveAudioManager = transform.Find("Sound").Find("Move").gameObject.GetComponent<AudioManager>();

        CapsuleHeight = controller.height;
        CapsuleCenter = controller.center;

        waitingTimeForWaitingMotionTimer = 0;

        AttackArea[] area = gameObject.GetComponentsInChildren<AttackArea>();

        for (int i = 0; i < area.Length; i++)
        {
            switch (area[i].name)
            {
                case "Character1_RightFoot":
                    RightFoot = area[i];
                    break;
                case "Character1_LeftFoot":
                    LeftFoot = area[i];
                    break;
                case "Character1_LeftHand":
                    LeftHand = area[i];
                    break;
                case "Character1_RightHand":
                    RightHand = area[i];
                    break;
                default:
                    Debug.Assert(false, "PlayerControl.cs Error - Check UnAssigned AttackArea");
                    break;
            }
        }
    }

    #endregion

    #region Handle Move Event

    private void Update()
    {
        CheckTerrainStatus();

        if (IsSliding == true)
        {
            return;
        }

        #region DEBUGGING
        Debug.Log("DistanceFromGround : " + (DistanceFromGround));

        //Debug.Log("rigidbody.velocity.y <= 0.001f: " + (rigidbody.velocity.y <= 0.001f));

        //Debug.Log("rigidbody.velocity.y: " + rigidbody.velocity.y);

        //Debug.Log("TerrainSlope < controller.slopeLimit: " + (TerrainSlope < controller.slopeLimit));
        #endregion

        // 랜딩, 땅에서 걸어 다닐 때
        if (DistanceFromGround <= GroundCheckDistance && rigidbody.velocity.y <= 0.001f && (TerrainSlope < controller.slopeLimit))
        {
            // 점프 중엔 DistanceFromGround <= GroundCheckDistance 라도 IsOnGrounded를 false로 체크해줘야 한다.
            // 따라서, currentVelocity.y가 음의 방향일 때만 IsOnGrounded를 true로 체크함 (0.001f으로 놓은 이유는 0보다 미세하게 큰 값이 나올 수 있기 때문)
            IsOnGrounded = true;
            IsFalling = false;
            rigidbody.velocity = Vector3.zero;
            rigidbody.useGravity = false;
            controller.enabled = true;
            IsSliding = false;
        }
        // 미끄러짐 처리. 
        else if (DistanceFromGround <= GroundCheckDistance && (TerrainSlope > controller.slopeLimit))
        {
            IsOnGrounded = false;
            rigidbody.useGravity = true;
            controller.enabled = false;
            IsSliding = true;
            // 미끄러진 다음 일정 시간 동안 update, fixedupdate에서 입력을 받지 않고, TerrainSlope가 controller.slopeLimit와 충분한 간격을 두도록 강제함
            Invoke("SlideOff", SlideTime);
        }
        // 공중에 있을 때
        else
        {
            IsOnGrounded = false;
            rigidbody.useGravity = true;
            controller.enabled = false;
            IsSliding = false;
            // 공중에 떠 있다면 Update는 그대로 return
            return;
        }

        waitingTimeForWaitingMotionTimer += Time.deltaTime;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (IsJump == false)
        {
            IsJump = Input.GetButtonDown("Jump");
        }

        HandleAttackEvent();

        if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded") == true)
        {
            if (Input.GetButtonDown("KickAttack"))
            {
                IsKickAttacking = true;
                BreakRestTime();
            }

            else if (Input.GetButtonDown("PunchAttack"))
            {
                IsPunchAttacking = true;
                BreakRestTime();
            }
        }

        HandleDeathEvent();

        if ((h != 0 | v != 0))
        {
            BreakRestTime();
        }

        // 기본동작은 걷기
        CamForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        MoveVector = (v * CamForward + h * cam.right) / 2;

        // 지상에서 왼쪽 쉬프트 버튼을 누르면 달리기를 하며 속도가 두 배가 되지만, 스태미나를 소모한다.
        // 스태미나 부족 상태에서 달리려하면 속도가 느려지고, 계속되면 Relax 애니메이션에 들어가, 스태미너를 회복할 때 까지 움직일 수 없게 됨
        if (Input.GetKey(KeyCode.LeftShift) &&
            Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded") &&
            status.stamina > 30 &&
            (h != 0 | v != 0))
        {
            status.stamina -= staminaUseMultiplier * Time.deltaTime;
            MoveVector *= 2;
        }
        else if (Input.GetKey(KeyCode.LeftShift) &&
            Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded") &&
            status.stamina < 30 &&
            (h != 0 | v != 0))
        {
            status.stamina -= staminaUseMultiplier * Time.deltaTime;
            MoveVector *= 0.5f;
        }
        else
        {
            if (status.stamina + staminaRecoverMultiplier * Time.deltaTime < player.StaminaMax)
            {
                status.stamina += staminaRecoverMultiplier * Time.deltaTime;
            }
            else if (status.stamina < player.StaminaMax)
            {
                status.stamina = player.StaminaMax;
            }
        }

        // Attack 중이라면 움직일 수 없음
        if (Animator.GetInteger("AttackState") == 0 && IsOnGrounded == true &&
           (Animator.GetCurrentAnimatorStateInfo(0).IsTag("Ground") |
            Animator.GetCurrentAnimatorStateInfo(0).IsName("Dash Attack")))
        {
            if (IsOnGrounded == true && player.playerStatus.stamina > 15)
            {
                HandleGroundedMovement(MoveVector, IsJump);
            }
            else if (IsOnGrounded == true && player.playerStatus.stamina <= 15)
            {
                Animator.Play("Refresh");
                BreakRestTime();
            }
        }

        if (waitingTimeForWaitingMotionTimer > waitingTimeForWaitingMotion)
        {
            RandomDecideRestType();
            waitingTimeForWaitingMotionTimer = 0;
        }

        UpdateAnimator();

        IsKickAttacking = false;
        IsPunchAttacking = false;
        IsAirAttacking = false;
        IsDashAttack = false;
        IsJump = false;
    }

    // 공중에서의 움직임 처리는 FixedUpdate로, 지상에서의 움직임 처리는 Update로 해야 끊기지 않는다.
    // 하지만, TerrainSlope < controller.slopeLimit으로, 미끄러져야 할 땐 FixedUpdate로 미끄러지게 했음
    private void FixedUpdate()
    {
        if (IsOnGrounded == true && IsSliding == false)
        {
            return;
        }

        // IsOnGrounded에, 아래로 향하는 속도까지 붙어 있을 때 IsFalling이 true라고 한다. 
        if (rigidbody.velocity.y < -1)
        {
            IsFalling = true;
        }

        BreakRestTime();

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        HandleDeathEvent();

        HandleAttackEvent();

        // 기본동작은 걷기
        CamForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        MoveVector = (v * CamForward + h * cam.right) / 2;

        if (status.stamina + staminaRecoverMultiplier * Time.deltaTime < player.StaminaMax)
        {
            status.stamina += staminaRecoverMultiplier * Time.deltaTime;
        }
        else if (status.stamina < player.StaminaMax)
        {
            status.stamina = player.StaminaMax;
        }

        // 공중에서도 왼쪽 쉬프트 버튼을 누르면 살짝 빨라지게 처리함.
        // 스태미너는 소모 되지 않음 (FixedUpdate에서 Time.deltaTime으로 처리해도 정상적으로 감소하지 않음)
        if (Input.GetKey(KeyCode.LeftShift))
        {
            MoveVector *= 1.25f;
        }

        // Attack 중이라면 움직일 수 없음
        if (Animator.GetInteger("AttackState") == 0 &&
           (Animator.GetCurrentAnimatorStateInfo(0).IsTag("Airborne Movable")))
        {
            HandleAirborneMovement(MoveVector);
        }

        UpdateAnimator();

        IsKickAttacking = false;
        IsPunchAttacking = false;
        IsAirAttacking = false;
        IsDashAttack = false;
        IsJump = false;

    }


    private void UpdateAnimator()
    {
        Animator.SetFloat("Forward", ForwardAmount, 0.1f, Time.smoothDeltaTime);
        Animator.SetFloat("Turn", TurnAmount, 0.1f, Time.smoothDeltaTime);
        Animator.SetFloat("Height", DistanceFromGround);
        Animator.SetBool("OnGround", IsOnGrounded);
        Animator.SetBool("IsAirAttack", IsAirAttacking);
        Animator.SetBool("IsJump", IsJump);
        Animator.SetBool("IsFalling", IsFalling);

        // 지상에서 대쉬상태에서 공격버튼이 눌러지면 대쉬어택
        if (IsKickAttacking == true &&
            IsOnGrounded &&
            Input.GetKey(KeyCode.LeftShift))
        {
            IsDashAttack = true;
        }

        // 지상에서 움직이지 않는 상태에서 공격버튼이 눌러지면 AttackState를 1, 4로 활성화.
        if (IsOnGrounded &&
            IsDashAttack == false)
        {
            if (IsKickAttacking == true)
            {
                currentVelocity = Vector3.zero;
                Animator.SetInteger("AttackState", 1);
            }
            else if (IsPunchAttacking == true)
            {
                currentVelocity = Vector3.zero;
                Animator.SetInteger("AttackState", 4);
            }
        }

        Animator.SetBool("IsDashAttack", IsDashAttack);
    }

    private void HandleGroundedMovement(Vector3 moveVector, bool IsJump)
    {
        if (moveVector.magnitude > 1f) moveVector.Normalize();
        moveVector = transform.InverseTransformDirection(moveVector);
        moveVector = Vector3.ProjectOnPlane(moveVector, GroundNormal);
        TurnAmount = Mathf.Atan2(moveVector.x, moveVector.z);
        ForwardAmount = moveVector.z;
        ApplyExtraTurnRotation();

        Vector3 snapGround = Vector3.down;

        // 입력 값에 따라 캐릭터를 조정
        if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Landing") == false)
        {
            currentVelocity = Vector3.Lerp(currentVelocity, new Vector3(MoveSpeed * MoveVector.x, 0, MoveSpeed * MoveVector.z), Time.deltaTime * 5f);

            // 지상에서의 점프 처리 
            if (IsJump == true &&
                Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
            {
                currentVelocity = Vector3.zero;
                controller.enabled = false;
                rigidbody.useGravity = true;
                rigidbody.velocity = new Vector3(currentVelocity.x, JumpPower, currentVelocity.z);
                return;
            }
            controller.Move(currentVelocity * Time.deltaTime + snapGround);
        }
    }

    private void HandleAirborneMovement(Vector3 moveVector)
    {
        if (moveVector.magnitude > 1f) moveVector.Normalize();
        moveVector = transform.InverseTransformDirection(moveVector);
        moveVector = Vector3.ProjectOnPlane(moveVector, GroundNormal);
        TurnAmount = Mathf.Atan2(moveVector.x, moveVector.z);
        ForwardAmount = moveVector.z;

        ApplyExtraTurnRotation();

        if (Input.GetButtonDown("KickAttack"))
        {
            IsAirAttacking = true;
        }

        // 추가적인 중력 부여 해, 높은 곳에서 낙하해도 발이 Terrain에 묻히지 않게 하기 위해, Mass를 일정 값 이상으로 잡아야 하는데 이 때 움직임이 느려지는 것을 방지하려 했음
        rigidbody.AddForce(rigidbody.mass * Physics.gravity);

        if (TerrainSlope < controller.slopeLimit)
        {
            rigidbody.velocity =
               new Vector3(0.65f * MoveSpeed * MoveVector.x,
               rigidbody.velocity.y,
               0.65f * MoveSpeed * MoveVector.z);
        }
    }

    private void ApplyExtraTurnRotation()
    {
        float turnSpeed = Mathf.Lerp(STATIONARY_TURN_SPEED, MOVING_TURN_SPEED, ForwardAmount);
        transform.Rotate(0, TurnAmount * turnSpeed * Time.smoothDeltaTime, 0);
    }

    private void SlideOff()
    {
        // Character Controller가 활성화 될 때 이전의 ForwardAmount, TurnAmount 값이 들어가 있으면 그 쪽 방향으로 위치가 갱신되어 순간이동하는 것 처럼 보이게 된다.
        // 이걸 방지 하기 위해 0을 대입해줘야 함.
        ForwardAmount = 0;
        TurnAmount = 0;
        currentVelocity = Vector3.zero;
        IsSliding = false;
    }

    private void CheckTerrainStatus()
    {
        // 레이 캐스팅 방식을 이용해 지면까지 남은 거리를 계산
        RaycastHit hitInfo;

        // 아래의 Ray는 Terrain Layer만을 감지한다.
        // https://docs.unity3d.com/kr/530/Manual/Layers.html 참고

        if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, 1 << TerrainLayer))
        {
            GroundNormal = hitInfo.normal;

            // Terrain 버텍스의 GroundNormal과 Vector3.up (y축)이 이루는 각이 구하는 'Terrain의 경사도' 이다.
            TerrainSlope = Vector3.Angle(GroundNormal, Vector3.up);
        }
        else
        {
            GroundNormal = Vector3.up;
        }

        DistanceFromGround = hitInfo.distance;

        // 지상에서 GroundCheckDistance 가 DistanceFromGround보다 작으면 버그가 생김.
        // GroundCheckDistance를 0.5 정도로 맞추면 버그가 없어지지만, 점프할 때 공중에 착지하는 버그가 있어 아래처럼 씀 

        GroundCheckDistance = (TerrainSlope / 100f) + 0.1f;
    }

    #endregion

    #region Handle Attack Event

    public bool HandleAttackEvent()
    {
        if (Animator.GetCurrentAnimatorStateInfo(0).IsTag("DamageAttack") |
            Animator.GetCurrentAnimatorStateInfo(0).IsTag("DownAttack"))
        {
            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Kick Attack1"))
            {
                LeftFoot.OnAttack();
                LeftHand.OffAttack();
                RightFoot.OffAttack();
                RightHand.OffAttack();

                AnimationNameString = "Kick Attack1";
                return true;
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Kick Attack2"))
            {
                LeftFoot.OffAttack();
                LeftHand.OffAttack();
                RightFoot.OnAttack();
                RightHand.OffAttack();

                AnimationNameString = "Kick Attack2";
                return true;
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Kick Attack3"))
            {
                LeftFoot.OnAttack();
                LeftHand.OffAttack();
                RightFoot.OffAttack();
                RightHand.OffAttack();

                AnimationNameString = "Kick Attack3";
                return true;
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Punch Attack1"))
            {
                LeftFoot.OffAttack();
                LeftHand.OnAttack();
                RightFoot.OffAttack();
                RightHand.OffAttack();

                AnimationNameString = "Punch Attack1";
                return true;
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Punch Attack2"))
            {
                LeftFoot.OffAttack();
                LeftHand.OffAttack();
                RightFoot.OffAttack();
                RightHand.OnAttack();

                AnimationNameString = "Punch Attack2";
                return true;
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Punch Attack3"))
            {
                LeftFoot.OffAttack();
                LeftHand.OffAttack();
                RightFoot.OffAttack();
                RightHand.OnAttack();

                AnimationNameString = "Punch Attack3";
                return true;
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Punch Attack4"))
            {
                LeftFoot.OffAttack();
                LeftHand.OffAttack();
                RightFoot.OffAttack();
                RightHand.OnAttack();

                AnimationNameString = "Punch Attack4";
                return true;
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Dash Attack"))
            {
                LeftFoot.OffAttack();
                LeftHand.OffAttack();
                RightFoot.OnAttack();
                RightHand.OffAttack();

                AnimationNameString = "Dash Attack";
                return true;
            }

            if (Animator.GetCurrentAnimatorStateInfo(0).IsName("Air Attack"))
            {
                return true;
            }

        }

        else
        {
            LeftFoot.OffAttack();
            LeftHand.OffAttack();
            RightFoot.OffAttack();
            RightHand.OffAttack();

            AnimationNameString = "";
        }

        return false;
    }

    public void HandleAttackParticle(ref Damage damage)
    {
        // Animator의 실행 중인 애니메이션 이름을 구하는 함수가 없어, 직접 AnimationNameString을 선언해 사용했음
        PlayerSkill skill = PlayerSkillManager.GetSkill(AnimationNameString);

        damage.skillCoefficient = skill.AttackValueCoefficient;
        damage.EmittingParticleID = skill.EmittingParticleID;
    }

    #endregion

    #region Handle Attacked Event

    public void Damaged(Damage damage)
    {
        BreakRestTime();

        if (damage.attacker.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            Animator.SetBool("IsDamaged", true);
            LookAtAndInitAngle(damage.attacker.transform);
            currentVelocity = Vector3.zero;
        }

        else if (damage.attacker.GetCurrentAnimatorStateInfo(0).IsName("Dash Attack"))
        {
            Animator.Play("Down");
            LookAtAndInitAngle(damage.attacker.transform);
            currentVelocity = Vector3.zero;
        }
    }

    #endregion

    #region Handle Other Event

    private void HandleDeathEvent()
    {
        if (status.currentHP <= 0)
        {
            // 플레이어 사망에 관한 이벤트 처리는 여기서.
            // 지금은 원활한 디버깅을 위해 주석 처리

            // Animator.Play("Death");
        }

    }

    private void RandomDecideRestType()
    {
        Animator.SetInteger("RestType", UnityEngine.Random.Range(1, 4));
    }

    private void BreakRestTime()
    {
        Animator.SetInteger("RestType", 0);
        waitingTimeForWaitingMotionTimer = 0;
    }

    #endregion

    private void LookAtAndInitAngle(Transform target)
    {
        transform.LookAt(target);
        Vector3 swap = new Vector3(0, transform.localEulerAngles.y, 0);
        transform.localEulerAngles = swap;
    }
    private void LookAtAndInitAngle(Vector3 target)
    {
        transform.LookAt(target);
        Vector3 swap = new Vector3(0, transform.localEulerAngles.y, 0);
        transform.localEulerAngles = swap;
    }

}


