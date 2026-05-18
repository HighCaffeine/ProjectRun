using UnityEngine;

public class NextZone : MonoBehaviour
{
    [Header("텔레포트 설정")]
    public int targetSectorIndex;

    private void OnTriggerEnter(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();

        if (actor != null && actor.IsLocal)
        {
            Vector3 destPos = DungeonPointManager.Instance.GetSpawnPosition(targetSectorIndex);

            Debug.Log(destPos);

            actor.OnUpdatePoint?.Invoke(actor.name, targetSectorIndex);
            
            if (GameManager.Instance.currentMode == GameManager.PlayMode.Offline_Test)
            {
                return; 
            }
            if (!Client.IS_SERVER_PLAY || Match.Instance.isDebugMode)
            {
                actor.transform.position = destPos;
                actor.GetComponent<Player>().SetPos(destPos);
                Debug.Log($"[Debug] 로컬 모드: {targetSectorIndex}번 구역으로 강제 이동");
                return;
            }

            // 서버로 기믹 이동 패킷 전송
            P_GimmickInteractReq req = new P_GimmickInteractReq
            {
                activeUUID = LocalPlayerInfo.ID,
                gimmickID = 999, // 포탈 공통 ID
                gimmickKey = (byte)eGimmickKey.NextZone,
                state = 2,       // 2 = Next 텔레포트
                targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
                param = targetSectorIndex
            };

            Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            Debug.Log($"[System] {targetSectorIndex}번 구역으로 이동 요청");
        }
    }
}