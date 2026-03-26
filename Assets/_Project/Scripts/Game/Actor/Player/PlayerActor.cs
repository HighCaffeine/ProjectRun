using System;
using UnityEngine;

public class PlayerActor : Actor
{
    public float moveSpeed = 5.0f;
    public Transform playerPivot;       //카메라 기준 계산용 피벗

    //서버 전송용
    private uint inputSeq = 0;          //전송 순서
    public bool IsLocal = false;        // 로컬 플레이어 구분
    private bool wasMoving = false;     // 이동했는데 씹힌거 검사용
    private P_PacketVector3 curPos;     
    private P_PacketQuaternion curRot;  
    private float sendTimer = 0f;       // 전송 시간 계산
    private float sendInterval = 0.02f; // 초당 20회 전송 (서버 50Hz 처리에 최적화)

    [Header("Action State")] //물리 처리
    public bool isActionCasting = false;
    private float castTimer = 0.0f;
    private const float CAST_DURATION = 0.5f;

    private enum ActionType : byte { PUSH = 0, PULL = 1, }

    protected override void Start()
    {
        base.Start();
    }

    void Update()
    {
        dir.x = Input.GetAxis("Horizontal");
        dir.y = Input.GetAxis("Vertical");
    }

    protected override void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 isometricForward = new Vector3(1f, 0f, 1f).normalized;
        Vector3 isometricRight = new Vector3(1f, 0f, -1f).normalized;
        Vector3 dir = (isometricForward * v + isometricRight * h).normalized;

        transform.Translate(dir * moveSpeed * Time.deltaTime);
        curPos.Set(transform.position);
        curRot.Set(transform.rotation);
        
        bool isMoving = (h != 0 || v != 0);

        sendTimer += Time.deltaTime;

        if (isMoving)
        {
            if (sendTimer >= sendInterval)
            {
                inputSeq++;
                SendMovePacket(curPos, curRot, h, v);
                sendTimer = 0f;
            }
        }
        else if (wasMoving)
        {
            inputSeq++;
            SendMovePacket(curPos, curRot, 0, 0);
            sendTimer = 0f;
        }

        wasMoving = isMoving;
    }

    void SendMovePacket(P_PacketVector3 p, P_PacketQuaternion q, float h, float v)
    {
        P_PlayerMovement pkt = new P_PlayerMovement
        {
            userUUID = LocalPlayerInfo.ID,
            inputSeq = inputSeq,
            currentPos = p,
            currentRot = q,
            axisH = h,
            axisV = v
        };
        Client.UDP.SendPacket2(E_PACKET.PLAYER_MOVEMENT, pkt);
    }
}