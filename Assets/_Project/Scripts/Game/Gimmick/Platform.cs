using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : BaseGimmick
{
    [Header("이동 설정")]
    public Transform startPos;
    public Transform endPos;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waitTimeAtSide = 0.5f;
    [SerializeField] private List<PlayerActor> players = new List<PlayerActor>();

    private Vector3 lastPos;
    private Vector3 currentTargetPos;
    private Vector3 expectPos;
    private Vector3 targetSyncPos;
    private bool isMoving = false;
    private bool hasTriggered = false; // 무한 왕복 전환 체크용

    private GimmickInfo gimmickInfo;
    private int activationType = 0;
    private float waitTime = 0f;

    private Vector3 startWorldPos;
    private Vector3 endWorldPos;

    public void SetStartPos(GameObject start) { startPos = start.transform; }
    public void SetEndPos(GameObject end) { endPos = end.transform; }

    private void Start()
    {
        lastPos = TargetTransform.position;
        gimmickInfo = GetComponent<GimmickInfo>();
        ParseGimmickProperties();

        startWorldPos = startPos.position;
        endWorldPos = endPos.position;

        if (activationType == 0)
        {
            if (GameManager.Instance.isHost)
            {
                currentTargetPos = endWorldPos;
                StartCoroutine(HostMoveLoop());
            }
        }
        else
        {
            isMoving = false;
            TargetTransform.position = startWorldPos;
        }
    }

    private void ParseGimmickProperties()
    {
        if (gimmickInfo == null) return;
        foreach (var prop in gimmickInfo.properties)
        {
            if (prop.key == eGimmickPropKey.ActivationType) activationType = (int)prop.value;
            if (prop.key == eGimmickPropKey.WaitTime) waitTime = prop.value;
            if (prop.key == eGimmickPropKey.MoveSpeed) moveSpeed = prop.value;
        }
    }

    private void LateUpdate()
    {
        Vector3 delta = TargetTransform.position - lastPos;
        if (delta == Vector3.zero) return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            PlayerActor p = players[i];

            if (p == null || !p.gameObject.activeInHierarchy || Vector3.Distance(TargetTransform.position, p.transform.position) > 15f)
            {
                players.RemoveAt(i);
                continue;
            }

            if (p.IsLocal)
            {
                p.SetPlatformDelta(delta);
            }
            else
            {
                p.transform.position += delta;

                Player playerComp = p.GetComponent<Player>();
                if (playerComp != null)
                {
                    playerComp.ServerPos += delta;
                }
            }
        }
        lastPos = TargetTransform.position;
    }

    public void AddPlayer(PlayerActor actor)
    {
        if (actor == null) return;
        if (actor != null && actor.IsLocal)
        {
            if (!players.Contains(actor)) players.Add(actor);

            // 트리거 방식이고 아직 한 번도 발동 안 했다면 서버에 알림
            if (activationType == 1 && !hasTriggered)
            {
                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = gimmickUID,
                    gimmickKey = (byte)eGimmickKey.MovePlatform,
                    state = 1, // On_Activate
                    targetPos = new P_PacketVector3 { x = TargetTransform.position.x, y = TargetTransform.position.y, z = TargetTransform.position.z },
                    param = 0f,
                    timestamp = NetworkTimeManager.Instance.GetServerTime()
                };
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            }
        }
    }

    public void RemovePlayer(PlayerActor actor)
    {
        if (actor != null && actor.IsLocal && players.Contains(actor)) players.Remove(actor);
    }


    private IEnumerator HostMoveLoop()
    {
        if (activationType == 1) yield return new WaitForSeconds(waitTime);

        while (true)
        {
            SendSyncPacket(currentTargetPos);
            isMoving = true;
            yield return StartCoroutine(MoveTo(currentTargetPos));
            isMoving = false;

            yield return new WaitForSeconds(waitTime);

            // startPos에 도달했을 때 리셋
            if (currentTargetPos == startWorldPos && activationType == 1)
            {
                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = gimmickUID,
                    gimmickKey = (byte)eGimmickKey.MovePlatform,
                    state = (byte)eGimmickState.Restore,
                    targetPos = new P_PacketVector3 { x = startWorldPos.x, y = startWorldPos.y, z = startWorldPos.z },
                    param = 0f,
                    timestamp = NetworkTimeManager.Instance.GetServerTime()
                };
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                yield break;
            }

            currentTargetPos = (currentTargetPos == endWorldPos) ? startWorldPos : endWorldPos;
        }
    }

    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(TargetTransform.position, target) > 0.01f)
        {
            TargetTransform.position = Vector3.MoveTowards(TargetTransform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        TargetTransform.position = target;
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
            param = moveSpeed,
            timestamp = NetworkTimeManager.Instance.GetServerTime()
        };
        Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        // 처음 밟았을 때 호스트가 제어권 획득
        if (activationType == 1 && ntf.state == 1 && !hasTriggered)
        {
            hasTriggered = true;
            if (GameManager.Instance.isHost)
            {
                currentTargetPos = endPos.position;
                StartCoroutine(HostMoveLoop());
            }
            return;
        }

        if (activationType == 1 && ntf.state == (byte)eGimmickState.Restore)
        {
            ResetGimmick();
            return;
        }

        // 리모트 클라이언트는 호스트가 보낸 Sync 패킷을 기반으로 위치 보간
        if (ntf.state == (byte)eGimmickState.Sync && !GameManager.Instance.isHost)
        {
            if (activationType == 1 && !hasTriggered) return;
            targetSyncPos = ntf.targetPos.ToVector3();

            long hostSendTime = ntf.timestamp;
            long myCurrentTime = NetworkTimeManager.Instance.GetServerTime();

            float latency = Mathf.Max(0f, (myCurrentTime - hostSendTime) / 1000f);

            expectPos = Vector3.MoveTowards(TargetTransform.position, targetSyncPos, moveSpeed * latency);

            StopAllCoroutines();
            StartCoroutine(SyncMoveRoutine());
        }
    }

    private IEnumerator SyncMoveRoutine()
    {
        // 디싱크로 인해 위치가 튀는 걸 방지하기 위해 예측 위치 보간
        while (Vector3.Distance(TargetTransform.position, expectPos) > 0.05f)
        {
            TargetTransform.position = Vector3.Lerp(TargetTransform.position, expectPos, Time.deltaTime * 15f);
            yield return null;
        }

        // 예측 위치에 도달하면 최종 목표 지점까지 정상 속도로 이동
        while (Vector3.Distance(TargetTransform.position, targetSyncPos) > 0.01f)
        {
            TargetTransform.position = Vector3.MoveTowards(TargetTransform.position, targetSyncPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public override void ResetGimmick()
    {
        // //Debug.Log($"[Platform] {gimmickUID}번 리셋 호출됨 이동 전 위치: {TargetTransform.position}");

        StopAllCoroutines();

        isMoving = false;
        hasTriggered = false;
        players.Clear();

        TargetTransform.position = startPos.position;
        lastPos = TargetTransform.position;

        Physics.SyncTransforms();

        //  //Debug.Log($"[Platform] {gimmickUID}번 시작 위치로 이동 완료 현재 위치: {TargetTransform.position}");

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider c in cols)
        {
            if (c.isTrigger)
            {
                c.enabled = false;
                c.enabled = true;
            }
        }

        if (activationType == 0 && GameManager.Instance.isHost)
        {
            currentTargetPos = endPos.position;
            StartCoroutine(HostMoveLoop());
        }
    }

    public void OnDrawGizmos()
    {
        if (startPos == null || endPos == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(startPos.position, endPos.position);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(endPos.position, TargetTransform.localScale);
    }
}