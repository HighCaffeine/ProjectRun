using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : BaseGimmick
{
    [Header("이동 설정")]
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waitTimeAtSide = 0.5f;
    [SerializeField] private List<PlayerActor> players = new List<PlayerActor>();

    private Vector3 lastPos;
    private Vector3 currentTargetPos;


    public void SetStartPos(GameObject start) { startPos = start.transform; }
    public void SetEndPos(GameObject end) { endPos = end.transform; }

    private void Start()
    {
        lastPos = transform.position;

        if (GameManager.Instance.isHost)
        {
            currentTargetPos = endPos.position;
            StartCoroutine(HostMoveLoop());
        }
    }

    private IEnumerator HostMoveLoop()
    {
        while (true)
        {
            //출발 전 목표위치 전송
            P_GimmickInteractReq req = new P_GimmickInteractReq
            {
                activeUUID = LocalPlayerInfo.ID,
                gimmickID = gimmickUID,
                gimmickKey = (byte)eGimmickKey.MovePlatform,
                state = (byte)eGimmickState.Sync,
                targetPos = new P_PacketVector3 { x = currentTargetPos.x, y = currentTargetPos.y, z = currentTargetPos.z },
                param = moveSpeed
            };
            Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);

            // 본인 플랫폼 이동
            yield return StartCoroutine(MoveTo(currentTargetPos));

            // 목적지 설정, 대기
            currentTargetPos = (currentTargetPos == endPos.position) ? startPos.position : endPos.position;
            yield return new WaitForSeconds(waitTimeAtSide);
        }
    }

    private void LateUpdate()
    {
        Vector3 delta = transform.position - lastPos;

        if (delta == Vector3.zero) return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            PlayerActor p = players[i];

            if (p == null || !p.gameObject.activeInHierarchy || Vector3.Distance(transform.position, p.transform.position) > 15f)
            {
                players.RemoveAt(i);
                continue;
            }

            if (p.IsLocal)
            {
                p.SetPlatformDelta(delta);
            }
        }
        lastPos = transform.position;
    }

    public void AddPlayer(PlayerActor actor)
    {
        if (actor == null) return;
        if (!players.Contains(actor)) players.Add(actor);
    }

    public void RemovePlayer(PlayerActor actor)
    {
        if (actor == null) return;
        if (players.Contains(actor))
        {
            actor.SetPlatformDelta(Vector3.zero);
            players.Remove(actor);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null && actor.IsLocal && !players.Contains(actor)) players.Add(actor);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null && actor.IsLocal && players.Contains(actor)) players.Remove(actor);
    }

    IEnumerator MoveTo(Vector3 target)
    {
        float syncTimer = 0f;
        const float SYNC_INTERVAL = 0.5f; // 0.5초마다 현재 상태 재전송

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

            if (GameManager.Instance.isHost)
            {
                syncTimer += Time.deltaTime;
                if (syncTimer >= SYNC_INTERVAL)
                {
                    syncTimer = 0f;
                    SendSyncPacket(target); // 현재 목표 위치 재전송
                }
            }

            yield return null;
        }

        transform.position = target;
    }

    private void SendSyncPacket(Vector3 target)
    {
        P_GimmickInteractReq req = new P_GimmickInteractReq
        {
            activeUUID = LocalPlayerInfo.ID,
            gimmickID = gimmickUID,
            gimmickKey = (byte)eGimmickKey.MovePlatform,
            state = (byte)eGimmickState.Sync,
            targetPos = new P_PacketVector3 { x = target.x, y = target.y, z = target.z },
            param = moveSpeed
        };
        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == (byte)eGimmickState.Sync && !GameManager.Instance.isHost)
        {
            Vector3 targetSyncPos = ntf.targetPos.ToVector3();
            // float syncSpeed = ntf.param; // 필요시 속도 동기화

            StopAllCoroutines();
            StartCoroutine(MoveTo(targetSyncPos)); // 호스트가 지시한 목표로 이동 시작
        }
    }
}