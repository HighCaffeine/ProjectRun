using System;
using UnityEngine;

public enum AniState { Idle, Move, Dead, Count };

public class Actor : MonoBehaviour
{
    //sync
    public float sendTimer = 0f;
    public const float sendInterval = 0.02f;
    //

    public StateMachine sm = new StateMachine();
    protected P_PacketVector3 dir;
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    protected float moveSpeed = 5.0f;
    [SerializeField]
    protected Transform playerPivot;


    protected CharacterController controller;
    protected Vector3 horizontalMove;
    [SerializeField]
    public Animator animator;

    public virtual bool IsLocal => false;
    public virtual bool Is2p => false;
    // 상태머신 값
    public float h { protected set; get; }
    public float v { protected set; get; }

    #region 액션 쿨타임 관리
    public float lastSkillUseTime = -999f;
    public const float SKILL_COOLDOWN = 1.0f;
    public float lastKnockbackTime = -999f;
    public const float KNOCKBACK_IMMUNE_TIME = 1.0f;
    #endregion

    public Vector3 GetForward() { return playerPivot.forward; }

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
    public void Move(Vector3 dir, float speed)
    {
        horizontalMove += dir * speed;
    }
    public virtual void LookAtDirection(Vector3 dir)
    {
        if (playerPivot != null)
        {
            playerPivot.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    public virtual Vector3 GetActionDir() { return Vector3.zero; }

    public virtual void SendMovePacket(float h, float v) { }
    public virtual void SendStateChange(eState newState, Vector3 dir = default, float power = 0f, long targetUUID = 0) { }

   
}