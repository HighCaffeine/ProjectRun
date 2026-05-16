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

    private bool isMoving = false;

    private GimmickInfo gimmickInfo;
    private int activationType = 0; // 0: 상시 왕복, 1: 밟으면 1초 뒤 출발
    private float waitTime = 0f;

    public void SetStartPos(GameObject start) { startPos = start.transform; }
    public void SetEndPos(GameObject end) { endPos = end.transform; }

    private void Start()
    {
        lastPos = transform.position;

        ParseGimmickProperties();

        if (activationType == 0)
        {
            if (GameManager.Instance.isHost)
            {
                currentTargetPos = endPos.position;
                StartCoroutine(HostMoveLoop());
            }
        }
        else
        {
            isMoving = false;
            transform.position = startPos.position;
        }
    }

    private void ParseGimmickProperties()
    {
        if (gimmickInfo == null) return;

        foreach (var prop in gimmickInfo.properties)
        {
            if (prop.key == eGimmickPropKey.ActivationType)
            {
                activationType = (int)prop.value;
            }
            if (prop.key == eGimmickPropKey.WaitTime)
            {
                waitTime = prop.value;
            }
            if (prop.key == eGimmickPropKey.MoveSpeed)
            {
                moveSpeed = prop.value;
            }
        }
    }

    private IEnumerator HostMoveLoop()
    {
        while (true)
        {
            // 출발 전 목표위치 전송
            SendSyncPacket(currentTargetPos);

            // 본인 플랫폼 이동
            isMoving = true;
            yield return StartCoroutine(MoveTo(currentTargetPos));
            isMoving = false;

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
        if (actor != null && actor.IsLocal)
        {
            if (!players.Contains(actor)) players.Add(actor);

            if (activationType == 1 && !isMoving)
            {
                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = gimmickUID,
                    gimmickKey = (byte)eGimmickKey.MovePlatform,
                    state = 1, // 1: On_Activate
                    targetPos = new P_PacketVector3 { x = transform.position.x, y = transform.position.y, z = transform.position.z },
                    param = 0f
                };
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null && actor.IsLocal && players.Contains(actor)) players.Remove(actor);
    }

    IEnumerator MoveTo(Vector3 target)
    {
        float syncTimer = 0f;
        const float SYNC_INTERVAL = 0.5f;

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

            if (activationType == 0 && GameManager.Instance.isHost)
            {
                syncTimer += Time.deltaTime;
                if (syncTimer >= SYNC_INTERVAL)
                {
                    syncTimer = 0f;
                    SendSyncPacket(target);
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

    private IEnumerator MovableTriggeredRoutine(Vector3 destination)
    {
        isMoving = true;
        yield return StartCoroutine(MoveTo(destination));
        isMoving = false; // 목적지에 도착하면 Update 정지
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == (byte)eGimmickState.Restore)
        {
            StopAllCoroutines();
            isMoving = false;
            transform.position = startPos.position;
            lastPos = startPos.position;
            return;
        }

        // [ActivationType 0: 상시 왕복] 비호스트 클라이언트 위치 동기화
        if (activationType == 0 && ntf.state == (byte)eGimmickState.Sync && !GameManager.Instance.isHost)
        {
            Vector3 targetSyncPos = ntf.targetPos.ToVector3();
            StopAllCoroutines();
            StartCoroutine(MoveTo(targetSyncPos));
        }

        // [ActivationType 1: 밟으면 이동] 1초 타이머가 끝난 후 서버가 준 목적지로 이동 명령 (state == 2)
        if (activationType == 1 && ntf.state == 2)
        {
            Vector3 targetSyncPos = ntf.targetPos.ToVector3(); // 서버가 연산해서 준 endPos 좌표
            StopAllCoroutines();
            StartCoroutine(MovableTriggeredRoutine(targetSyncPos));
        }
    }
}