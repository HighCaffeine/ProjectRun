using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ReadyZone : MonoBehaviour
{
    public List<PlayerActor> readyPlayers = new List<PlayerActor>();
    [SerializeField]
    private DungeonPointManager pointManager;
    private bool isTriggered = false;

    private void Update()
    {
        if (!isTriggered && readyPlayers.Count == 2)
        {
            isTriggered = true;
            pointManager.MoveToNextSector();
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null && actor.IsLocal)
        {
            // 서버로 준비 패킷 전송
            P_PlayerReadyRequest req = new P_PlayerReadyRequest { isReady = true };
            Client.TCP.SendPacket2(E_PACKET.PLAYER_READY_REQUEST, req);
            Debug.Log("[System] 준비 영역 진입");
            if (!readyPlayers.Contains(actor))
            {
                readyPlayers.Add(actor);
            }
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
            readyPlayers.Remove(actor);
        }
    }
}