using UnityEngine;
using System.Collections.Generic;

public class SeesawTrigger : BaseGimmick
{
    public Rigidbody boardRb;
    public float maxForce = 100f;
    public float maxDistance = 1.5f; // ���� �� ����

    [SerializeField]
    private List<Transform> players = new List<Transform>();

    private Quaternion targetRot;
    private float sendTimer = 0f;

    void Start()
    {
        targetRot = boardRb.transform.rotation;
    }

    //방장이 뿌려준 시소 각도 반영
    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == (byte)eGimmickState.Sync && !GameManager.Instance.isHost)
        {
            targetRot = Quaternion.Euler(ntf.targetPos.ToVector3());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerActor>()) players.Add(other.transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerActor>()) players.Remove(other.transform);
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.currentMode == GameManager.PlayMode.Offline_Test) return;

        // 방장인지 체크하고 물리 킴
        bool isHost = GameManager.Instance.isHost;
        boardRb.isKinematic = !isHost;

        if (!isHost) return; // 방장이 아니면 리턴

        // 방장 전용 물리 연산
        players.RemoveAll(p => p == null);
        foreach (var player in players)
        {
            Vector3 localPos = boardRb.transform.InverseTransformPoint(player.position);
            float normalized = Mathf.Clamp(localPos.x / maxDistance, -1f, 1f);
            float force = normalized * maxForce;
            boardRb.AddForceAtPosition(Vector3.down * Mathf.Abs(force), player.position);
        }
    }

    void Update()
    {
        if (GameManager.Instance.currentMode == GameManager.PlayMode.Offline_Test) return;

        if (GameManager.Instance.isHost)
        {
            //0.1초마다 시소 각도 전송
            sendTimer += Time.deltaTime;
            if (sendTimer >= 0.1f)
            {
                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = this.gimmickUID,
                    gimmickKey = (byte)eGimmickKey.SeeSaw,
                    state = (byte)eGimmickState.Sync,
                    targetPos = new P_PacketVector3 { x = boardRb.transform.eulerAngles.x, y = boardRb.transform.eulerAngles.y, z = boardRb.transform.eulerAngles.z },
                    param = 0f
                };
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
                sendTimer = 0f;
            }
        }
        else
        {
            // 일반 유저들은 서버(방장)가 보내준 각도로 부드럽게 기울어집니다.
            boardRb.transform.rotation = Quaternion.Slerp(boardRb.transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }



    // void OnTriggerEnter(Collider other)
    // {
    //     Debug.Log("Trigger Enter: " + other.name);
    //     if (other.GetComponent<PlayerActor>())
    //         players.Add(other.transform);
    // }

    // void OnTriggerExit(Collider other)
    // {
    //     if (other.GetComponent<PlayerActor>())
    //         players.Remove(other.transform);
    // }

    // void FixedUpdate()
    // {
    //     players.RemoveAll(p => p == null);

    //     foreach (var player in players)
    //     {
    //         Vector3 localPos = boardRb.transform.InverseTransformPoint(player.position);

    //         float normalized = Mathf.Clamp(localPos.x / maxDistance, -1f, 1f);

    //         float force = normalized * maxForce;

    //         /* float threshold = 0.2f; // �ּ� �� ������ ���� �Ӱ谪

    //          if (Mathf.Abs(normalized) < threshold)
    //              continue;*/

    //         boardRb.AddForceAtPosition(
    //             Vector3.down * Mathf.Abs(force),
    //             player.position
    //         );
    //     }
    // }
}