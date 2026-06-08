using UnityEngine;

public class VillageTokenAuthor : MonoBehaviour, IPacketReceiver
{
    void Start()
    {
        // 1. 새 게임 서버 소켓에 리시버 등록
        if (Client.TCP != null) Client.TCP.AddPacketReceiver(this);

        // 2. 씬이 켜지자마자 게임 서버로 내 토큰을 제출하여 인증 시도!
        P_GameAuthReq authReq = new P_GameAuthReq();
        authReq.AuthToken = LocalPlayerInfo.AuthToken;

        Client.TCP.SendPacket2(E_PACKET.GAME_AUTH_REQUEST, authReq);
        Debug.Log("[Town] 게임 서버에 인증 토큰 제출 완료!");
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        if (packet.pbase.packet_id == (ushort)E_PACKET.GAME_AUTH_RESPONSE)
        {
            var res = UnsafeCode.ByteArrayToStructure<P_GameAuthRes>(packet.data);
            if (res.Result == 0) // 인증 성공
            {
                Debug.Log("<color=green>[Town] 서버 인증 성공! 캐릭터를 스폰합니다.</color>");
                // TODO: 내 캐릭터 스폰 & 마을 UI 띄우기
            }
            else
            {
                Debug.LogError("[Town] 토큰 인증 실패! 로비로 쫓겨납니다.");
                // TODO: 잘못된 접속이므로 로비 씬으로 돌려보내기
            }
        }
    }

    void OnDestroy()
    {
        if (Client.TCP != null) Client.TCP.RemovePacketReceiver(this);
    }
}