using UnityEngine;

public class NextZone : MonoBehaviour
{
    [Header("텔레포트 설정")]
    public int targetMapID = 1;
    public int targetMapStartIndex;

    private void OnTriggerEnter(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();

        if (actor != null && actor.IsLocal)
        {
            DungeonPointManager.Instance.currentMapID = targetMapID;
            DungeonPointManager.Instance.currentSectorIndex = targetMapStartIndex;

            DungeonPointManager.Instance.SetCurrentMap(targetMapID, targetMapStartIndex);

            Vector3 destPos = DungeonPointManager.Instance.GetSpawnPosition(targetMapID, targetMapStartIndex);

            Debug.Log(destPos);

            actor.OnUpdatePoint?.Invoke(actor.name, targetMapStartIndex);

            if (GameManager.Instance.currentMode == GameManager.PlayMode.Offline_Test)
            {
                return;
            }
            if (!Client.IS_SERVER_PLAY || Match.Instance.isDebugMode)
            {
                actor.transform.position = destPos;
                actor.GetComponent<Player>().SetPos(destPos);
                Debug.Log($"[Debug] 로컬 모드: {targetMapStartIndex}번 구역으로 강제 이동");
                return;
            }

            // mapID와 sectorIndex를 하나의 float으로 인코딩
            float encodedValue = (targetMapID * 100) + targetMapStartIndex;

            P_GimmickInteractReq req = new P_GimmickInteractReq
            {
                activeUUID = LocalPlayerInfo.ID,
                gimmickID = 999,
                gimmickKey = (byte)eGimmickKey.NextZone,
                state = 2,
                targetPos = new P_PacketVector3 { x = destPos.x, y = destPos.y, z = destPos.z },
                param = encodedValue, // 예: 1_2 -> 102
                timestamp = NetworkTimeManager.Instance.GetServerTime()
            };

            Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
            Debug.Log($"[System] Map{targetMapID}_Sector{targetMapStartIndex}번 구역으로 이동 요청 (encoded: {encodedValue})");
        }
    }
}