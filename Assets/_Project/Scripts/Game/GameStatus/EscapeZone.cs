using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();

        // 던전이 클리어된 상태에서만 작동하도록 제어
        if (actor != null && actor.IsLocal)
        {
            P_DungeonEscapeReq pkt = new P_DungeonEscapeReq();

            Client.TCP.SendPacket2(E_PACKET.DUNGEON_ESCAPE_REQ, pkt);
            Debug.Log("[System] 탈출 요청 패킷 전송 완료");
        }
    }
}