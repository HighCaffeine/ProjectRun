using UnityEngine;

public class ReadyZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null && actor.IsLocal)
        {
            // 서버로 준비 패킷 전송
            P_PlayerReadyRequest req = new P_PlayerReadyRequest { isReady = true };
            Client.TCP.SendPacket2(E_PACKET.PLAYER_READY_REQUEST, req);
            Debug.Log("[System] 준비 영역 진입");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null && actor.IsLocal)
        {
            // 서버로 준비 취소 패킷 전송
            P_PlayerReadyRequest req = new P_PlayerReadyRequest { isReady = false };
            Client.TCP.SendPacket2(E_PACKET.PLAYER_READY_REQUEST, req);
            Debug.Log("[System] 준비 영역 이탈");
        }
    }
}