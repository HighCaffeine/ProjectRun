using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : BaseGimmick
{
    [Header("이동 설정")]
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private float moveSpeed = 3f;

    [SerializeField]
    private List<PlayerActor> players = new List<PlayerActor>();
    private Vector3 lastPos;


    ///TEST
    private P_GimmickInteractReq pkt;
    

    ///TEST

    public void SetStartPos(GameObject start) { startPos = start.transform; }
    public void SetEndPos(GameObject end) { endPos = end.transform; }

    private void Start()
    {
        v.Set(Vector3.zero);
        pkt = new P_GimmickInteractReq
        {
            activeUUID = LocalPlayerInfo.ID,
            gimmickID = gimmickUID,
            gimmickKey = (byte)eGimmickKey.MovePlatform,
            state = (byte)eGimmickState.Sync,
            targetPos = v,
            param = 0.0f
        };

        lastPos = transform.position;
        StartCoroutine(MoveLoop()); // 시작하자마자 이동
    }

    private void LateUpdate()
    {
        if (!GameManager.Instance.isHost) return;

        Vector3 delta = transform.position - lastPos;

        foreach (var actor in players)
        {
            if (actor != null) actor.SetPlatformDelta(delta);
        }

        lastPos = transform.position;

        //테스트용
        v.Set(transform.position);
        pkt.targetPos = v;
        Client.UDP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, pkt);
    }

    public void AddPlayer(PlayerActor actor)
    {
        if (actor == null) return;

        if (!players.Contains(actor))
        {
            players.Add(actor);
        }
    }

    public void RemovePlayer(PlayerActor actor)
    {
        if (actor == null) return;

        if (players.Contains(actor))
        {
            players.Remove(actor);
        }
    }

    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    IEnumerator MoveLoop()
    {
        while (true)
        {
            yield return MoveTo(endPos.transform.position);
            yield return MoveTo(startPos.transform.position);
        }
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (GameManager.Instance.isHost) return;

        //이동
        MoveTo(ntf.targetPos.ToVector3());
    }


}