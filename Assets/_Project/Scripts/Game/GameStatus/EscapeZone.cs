using System.Collections.Generic;
using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    public List<PlayerActor> escapePlayers = new List<PlayerActor>();
    private bool isTriggered = false;
    [SerializeField] private GameObject diary;
    public void Exit()
    {
        P_DungeonEscapeReq pkt = new P_DungeonEscapeReq();

        Client.TCP.SendPacket2(E_PACKET.DUNGEON_ESCAPE_REQ, pkt);
        Debug.Log("[System] 탈출 요청 패킷 전송 완료");
    }
    private void Update()
    {
        if (!isTriggered && escapePlayers.Count == 2)
        {
            isTriggered = true;
            diary.SetActive(true);
            DungeonUiManager.Instance.ShowResult();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();

        if (actor != null)
        {
            if (!escapePlayers.Contains(actor))
            {
                escapePlayers.Add(actor);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();

        if (actor != null)
        {
            escapePlayers.Remove(actor);
        }
    }
}