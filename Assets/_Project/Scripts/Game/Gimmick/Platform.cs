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
    private bool isMoving = false;
    private bool hasTriggered = false; // 무한 왕복 전환 체크용

    private GimmickInfo gimmickInfo;
    private int activationType = 0;
    private float waitTime = 0f;

    public void SetStartPos(GameObject start) { startPos = start.transform; }
    public void SetEndPos(GameObject end) { endPos = end.transform; }

    private void Start()
    {
        lastPos = TargetTransform.position;
        gimmickInfo = GetComponent<GimmickInfo>();
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
            TargetTransform.position = startPos.position;
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
            if (p.IsLocal) p.SetPlatformDelta(delta);
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
                    param = 0f
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

            yield return new WaitForSeconds(waitTimeAtSide);

            currentTargetPos = (currentTargetPos == endPos.position) ? startPos.position : endPos.position;

            if (activationType == 1 && currentTargetPos == endPos.position)
            {
                break;
            }
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

        if (activationType == 1 && target == startPos.position)
        {
            hasTriggered = false;
        }
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
        //처음 밟았을 때 트리거
        if (activationType == 1 && ntf.state == 1 && !hasTriggered)
        {
            hasTriggered = true;
            if (GameManager.Instance.isHost)
            {
                currentTargetPos = endPos.position;
                StartCoroutine(HostMoveLoop());     // 호스트가 컨트롤
            }
        }

        //호스트가 동기화
        if (ntf.state == (byte)eGimmickState.Sync && !GameManager.Instance.isHost)
        {
            Vector3 targetSyncPos = ntf.targetPos.ToVector3();
            StopAllCoroutines();
            StartCoroutine(MoveTo(targetSyncPos));
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