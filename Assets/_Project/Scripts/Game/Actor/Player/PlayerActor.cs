using System;
using UnityEngine;

public class PlayerActor : Actor
{
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

    // 상태머신 값
    public float h;
    public float v;


    private enum ActionType : byte { PUSH = 0, PULL = 1, }

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
        if (!IsLocal) return;

        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");

        sm.Update(); // State 머신 실행
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
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    public CollisionFlags DoMovePhysics(Vector3 moveDir, float speed)
    {
        if (controller != null) return controller.Move(moveDir * speed * Time.deltaTime);
        return CollisionFlags.None;
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