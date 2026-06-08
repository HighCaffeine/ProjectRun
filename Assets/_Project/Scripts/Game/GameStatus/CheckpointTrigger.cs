using UnityEngine;

public class CheckpointTrigger : BaseGimmick
{
    [Header("저장할 구역 설정")]
    public int targetMapID;
    public int targetSpawnIndex;

    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated) return;

        PlayerActor actor = other.GetComponent<PlayerActor>();

        if (actor != null && actor.IsLocal)
        {
            isActivated = true;

            // 로컬 즉시 적용
            DungeonPointManager.Instance.currentMapID = targetMapID;
            DungeonPointManager.Instance.currentSectorIndex = targetSpawnIndex;
            ActorManager.Instance.UpdateAllSpawnPoints(targetMapID, targetSpawnIndex);

        

            // 서버 모드일 경우 패킷 전송
            if (Client.IS_SERVER_PLAY && GameManager.Instance.currentMode == GameManager.PlayMode.Server_Online)
            {
                // mapID와 sectorIndex를 하나의 float으로 인코딩
                // 예: mapID를 100의 자리로, sectorIndex를 1의 자리로
                float encodedValue = (targetMapID * 100) + targetSpawnIndex;

                P_GimmickInteractReq req = new P_GimmickInteractReq
                {
                    activeUUID = LocalPlayerInfo.ID,
                    gimmickID = this.gimmickUID,
                    gimmickKey = (byte)eGimmickKey.Checkpoint,
                    state = (byte)eGimmickState.On_Activate,
                    targetPos = new P_PacketVector3(),
                    param = encodedValue, // 예: 0_1 -> 1, 1_2 -> 102
                    timestamp = NetworkTimeManager.Instance.GetServerTime()
                };
                Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
             
            }
        }
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == (byte)eGimmickState.On_Activate)
        {
            isActivated = true;

            // float에서 mapID와 sectorIndex 디코딩
            int encodedValue = (int)ntf.param;
            int mapID = encodedValue / 100;
            int sectorIndex = encodedValue % 100;

            DungeonPointManager.Instance.currentMapID = mapID;
            DungeonPointManager.Instance.currentSectorIndex = sectorIndex;
            ActorManager.Instance.UpdateAllSpawnPoints(mapID, sectorIndex);

        
        }
    }
}