using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();

        // 던전이 클리어된 상태에서만 작동하도록 제어
        // and로 던전 클리어 상태추가
        if (actor != null && actor.IsLocal)
        {
            P_GameStartNtf pkt = new P_GameStartNtf
            {
              mapId = 0  
            };

            Client.TCP.SendPacket2(E_PACKET.DUNGEON_ESCAPE_REQ, pkt);
            Debug.Log("[System] 탈출 대기 중");
        }
    }
}