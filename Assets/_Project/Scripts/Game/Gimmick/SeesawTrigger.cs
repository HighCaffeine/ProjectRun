using UnityEngine;
using System.Collections.Generic;

public class SeesawTrigger : BaseGimmick
{
    public Rigidbody boardRb;
    public float maxForce = 100f;
    public float maxDistance = 1.5f;

    [SerializeField] private List<Transform> players = new List<Transform>();
    private float sendTimer = 0f;
    private Quaternion targetRot;

    void Start()
    {
        targetRot = boardRb.transform.rotation;
    }

    // 서버가 뿌려준 시소 각도 반영
    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == (byte)eGimmickState.Sync && !GameManager.Instance.isHost)
        {
            targetRot = Quaternion.Euler(ntf.targetPos.ToVector3());
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        PlayerActor actor = collision.transform.GetComponent<PlayerActor>();
        if (actor != null && actor.IsLocal) players.Add(collision.transform);
    }

    void OnCollisionExit(Collision collision)
    {
        PlayerActor actor = collision.transform.GetComponent<PlayerActor>();
        if (actor != null && actor.IsLocal) players.Remove(collision.transform);
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.isHost)
        {
            players.RemoveAll(p => p == null);
            foreach (var player in players)
            {
                Vector3 localPos = boardRb.transform.InverseTransformPoint(player.position);
                float normalized = Mathf.Clamp(localPos.x / maxDistance, -1f, 1f);
                boardRb.AddForceAtPosition(Vector3.down * Mathf.Abs(normalized * maxForce), player.position);
            }

            // 방장이 각도를 계산해서 0.1초마다 브로드캐스트
            sendTimer += Time.fixedDeltaTime;
            if (sendTimer > 0.1f)
            {
                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = this.gimmickUID,
                    gimmickKey = (byte)eGimmickKey.SeeSaw,
                    state = (byte)eGimmickState.Sync,
                    targetPos = new P_PacketVector3(),
                    param = 0f,
                    timestamp = NetworkTimeManager.Instance.GetServerTime()
                };
                req.targetPos.Set(boardRb.transform.eulerAngles);
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                sendTimer = 0f;
            }
        }
        else
        {
            // 일반 클라이언트는 물리 끄고 동기화
            boardRb.isKinematic = true;
            boardRb.transform.rotation = Quaternion.Slerp(boardRb.transform.rotation, targetRot, Time.fixedDeltaTime * 10f);
        }
    }
}