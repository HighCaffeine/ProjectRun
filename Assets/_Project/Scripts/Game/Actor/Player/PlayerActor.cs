using System;
using UnityEngine;
using System.Collections;

public class PlayerActor : Actor
{
    [SerializeField] private Transform playerPivot;

    public float moveSpeed = 5.0f;
    public LayerMask targetLayer;
    public CharacterController controller;

    //동기화
    public uint inputSeq = 0;
    public bool IsLocal = false;
    public float sendTimer = 0f;
    public const float sendInterval = 0.02f;
    private P_PacketVector3 curPos;
    private P_PacketQuaternion curRot;

    [Header("Visual Effects")]
    public TrailRenderer trailRenderer;
    public ParticleSystem travelSparkParticle;
    public ParticleSystem[] brakeParticle;

    [Header("Gravity Settings")]
    public float gravity = -20f;       // 기본 중력 값
    private float verticalVelocity;     // 현재 수직 속도
    private float maxVerticalVelocity = -30f; // 최대 낙하 속도 제한

    // 상태머신 값
    public float h { private set; get; }
    public float v { private set; get; }


    private enum ActionType : byte { PUSH = 0, PULL = 1, }

    public Vector3 GetForward() { return playerPivot.forward; }

    protected override void Start()
    {
        controller = GetComponent<CharacterController>();
        if (IsLocal)
        {
            sm.ChangeState(new IdleState(this));
        }
    }

    void Update()
    {
        sm.Update(); // State 머신 실행
        ApplyGravity();
        if (!IsLocal) return;

        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

    }

    //마우스 방향 보기
    public void LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, transform.position);

        if (ground.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Vector3 dir = hit - transform.position;
            dir.y = 0; // 수평 회전만

            if (dir.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    public void LookAtDirection(Vector3 dir)
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

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            // 중력 가속도 적용
            verticalVelocity += gravity * Time.deltaTime;

            // 최대 낙하 속도 제한
            if (verticalVelocity < maxVerticalVelocity) verticalVelocity = maxVerticalVelocity;
        }
    }

    public CollisionFlags Move(Vector3 horizontalDir, float speed)
    {
        if (controller == null) return CollisionFlags.None;

        Vector3 finalMove = (horizontalDir * speed) + (Vector3.up * verticalVelocity);

        return controller.Move(finalMove * Time.deltaTime);
    }

    public void SetVerticalVelocity(float velocity) => verticalVelocity = velocity;

    public IEnumerator HitStopRoutine(float duration = 0.05f)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }

    // 상태 머신이 호출, 확정 좌표 패킷 전송
    public void SendMovePacket(float axisH, float axisV)
    {
        curPos.Set(transform.position);
        curRot.Set(transform.rotation);

        P_PlayerMovement pkt = new P_PlayerMovement
        {
            userUUID = LocalPlayerInfo.ID,
            inputSeq = ++inputSeq,
            currentPos = curPos,
            currentRot = curRot,
            axisH = axisH,
            axisV = axisV
        };
        Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, pkt);
    }
}