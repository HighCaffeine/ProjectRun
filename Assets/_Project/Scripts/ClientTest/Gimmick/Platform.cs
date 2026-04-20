using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : BaseGimmick
{
    [Header("이동 설정")]
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private List<PlayerActor> players = new List<PlayerActor>();

    private Vector3 lastPos;
    private P_GimmickInteractReq pkt;
    private float sendTimer = 0f; // 패킷 전송 타이머
    private Vector3 targetSyncPos;


    public void SetStartPos(GameObject start) { startPos = start.transform; }
    public void SetEndPos(GameObject end) { endPos = end.transform; }

    private Vector3 lastSentPos;
    private const float SEND_THRESHOLD = 0.01f;

    private void Start()
    {
        lastPos = transform.position;
        targetSyncPos = transform.position;

        if (GameManager.Instance.isHost)
        {
            pkt = new P_GimmickInteractReq
            {
                activeUUID = LocalPlayerInfo.ID,
                gimmickID = gimmickUID,
                gimmickKey = (byte)eGimmickKey.MovePlatform,
                state = (byte)eGimmickState.Sync,
                targetPos = new P_PacketVector3(),
                param = 0.0f
            };
            StartCoroutine(MoveLoop());
        }
    }

    private void Update()
    {
        if (GameManager.Instance.isHost)
        {
            if (Client.IS_SERVER_PLAY)
            {
                sendTimer += Time.deltaTime;
                Vector3 currentPos = transform.position;

                if (Vector3.SqrMagnitude(currentPos - lastSentPos) > SEND_THRESHOLD * SEND_THRESHOLD)
                {
                    pkt.targetPos.Set(currentPos);
                    Client.UDP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, pkt);
                    lastSentPos = currentPos;
                }
                sendTimer = 0f;
            }
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetSyncPos, Time.deltaTime * moveSpeed * 3f);
        }
    }

    private void LateUpdate()
    {
        Vector3 delta = transform.position - lastPos;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            PlayerActor p = players[i];
            
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (p == null || p.gameObject.activeInHierarchy == false || dist > 15f)
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
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator MoveLoop()
    {
        while (true)
        {
            yield return MoveTo(endPos.position);
            yield return MoveTo(startPos.position);
        }
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == (byte)eGimmickState.Sync && !GameManager.Instance.isHost)
        {
            targetSyncPos = ntf.targetPos.ToVector3();
        }
    }
}