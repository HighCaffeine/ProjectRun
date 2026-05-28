using System;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public enum AniState { Idle, Move, Dead, Count };

public abstract class Actor : MonoBehaviour
{
    private const float ROT_INTERPOLATION_VALUE = 15f;

    public abstract Vector3 GetMovementDirection(); //이동방향 (플레이어 카메라 기준, 몬스터 타겟 위치)
    public abstract bool HasMoveIntent();           //이동 입력이 있는지 (h / v가 0이 아니거나, 몬스터는 이동 타겟을 받았을 때)
    public abstract bool CheckActionIntent();       //액션 입력 판단 (키입력 / 몬스터 -> 쿨타임)


    //sync
    public float sendTimer = 0f;
    public const float sendInterval = 0.1f;
    //

    public bool isDead = false;

    public StateMachine sm = new StateMachine();
    protected P_PacketVector3 dir;
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float moveSpeed = 5.0f;
    [SerializeField]
    protected Transform playerPivot;


    protected CharacterController controller;
    [SerializeField]
    public Animator animator;

    public virtual bool IsLocal => false;
    public virtual bool Is2p => false;
    // ���¸ӽ� ��
    public float h { protected set; get; }
    public float v { protected set; get; }
    protected Vector3 horizontalMove;
    protected float verticalVelocity;
    protected float gravity = -20f;
    protected float maxVerticalVelocity = -30f; // 최대 낙하 속도 제한

    [Header("Aiming Indicator")]
    public LineRenderer aimLine;

    [SerializeField]
    protected bool usegravity = true;

    public BaseGimmick currentMovableGround;
    #region �׼� ��Ÿ�� ����
    public float lastSkillUseTime = -999f;
    public const float SKILL_COOLDOWN = 1.0f;
    public float lastKnockbackTime = -999f;
    public const float KNOCKBACK_IMMUNE_TIME = 1.0f;
    #endregion

    public Vector3 GetForward()
    {
        Transform forwardPivot = playerPivot != null ? playerPivot : transform;
        return forwardPivot.forward;
    }

    public void SetControllerActive(bool isActive)
    {
        if (controller != null) controller.enabled = isActive;
    }

    public void SetVerticalVelocity(float velocity)
    {
        verticalVelocity = velocity;
    }

    public virtual void OnActionStateEnter(eState actionType) { }
    public virtual void OnActionStateExit(eState actionType) { }
    public virtual void OnKnockbackStateEnter(eState actionType, float duration) { }
    public virtual void OnKnockbackStateComplete() { }
    public virtual void OnKnockbackStateExit() { }

    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        sm.ChangeState(new IdleState(this));
    }


    public void SetAni(AniState aniState)
    {
        switch (aniState)
        {
            case AniState.Idle:
                break;
            case AniState.Move:
                break;
            case AniState.Dead:
                break;
        }
    }

    public virtual void ApplyMovement()
    {
        if (controller == null || !controller.enabled) return;

        if (usegravity)
        {


            if (controller.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -0.5f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            if (verticalVelocity < maxVerticalVelocity) verticalVelocity = maxVerticalVelocity;
        }
        else
        {
            verticalVelocity = 0f;
        }

        Vector3 moveDelta = (horizontalMove + Vector3.up * verticalVelocity) * Time.deltaTime;

        controller.Move(moveDelta);

        horizontalMove = Vector3.zero;
    }
    public void Move(Vector3 dir, float speed)
    {
        horizontalMove += dir * speed;
    }
    public virtual void LookAtDirection(Vector3 dir, bool isImmediate = false)
    {
        if (dir == Vector3.zero) return;

        Transform target = (playerPivot != null) ? playerPivot : transform;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        if (isImmediate)
        {
            target.rotation = targetRot;
        }
        else
        {
            target.rotation = Quaternion.Slerp(target.rotation, targetRot, Time.deltaTime * 15f);
        }
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
    }
    public virtual void DrawAimLine(Vector3 targetPos)
    {
        if (aimLine == null) return;
        aimLine.enabled = true;
        // 가슴 높이(y + 1f)에서부터 목표물까지 선을 그림
        aimLine.SetPosition(0, transform.position + Vector3.up * 1f);
        aimLine.SetPosition(1, targetPos + Vector3.up * 1f);
    }
    public virtual Vector3 GetActionDir() { return Vector3.zero; }

    public virtual void SendMovePacket(float h, float v) { }
    public virtual void SendStateChange(eState stateCode, Vector3 dir = default, float param = 0f, long targetUUID = 0, bool isPull = false, Vector3 casterPos = default) { }
}
