using System.Collections.Generic;
using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    public List<PlayerActor> escapePlayers = new List<PlayerActor>();
    private bool isTriggered = false;
    [SerializeField] private GameObject diary;

    private bool _cutscenePlayed = false;
    public bool CutscenePlayed => _cutscenePlayed;

    public bool isFirstShowResult = false;

    public void Exit()
    {
        P_DungeonEscapeReq pkt = new P_DungeonEscapeReq();
        Client.TCP.SendPacket2(E_PACKET.DUNGEON_ESCAPE_REQ, pkt);
        Debug.Log("[System] 탈출 요청 패킷 전송 완료");
    }
    private void Update()
    {
        if (isTriggered) return;

        if (GameManager.Instance.isHost)
        {
            if (escapePlayers.Count < 2) return;

            isTriggered = true;

            Exit();
        }
        else
        {
            PlayerActor localActor = GetLocalPlayerActor();
            if (localActor != null && escapePlayers.Contains(localActor))
            {
                isTriggered = true;
                Exit();
            }
        }
    }

    private PlayerActor GetLocalPlayerActor()
    {
        if (Match.Instance == null) return null;
        if (!Match.Instance.Players.TryGetValue(LocalPlayerInfo.ID, out Player localPlayer)) return null;
        return localPlayer.GetComponent<PlayerActor>();
    }

    private bool _resultShown = false;

    public void OnActiveResult()
    {
        if (!_resultShown)
        {
            // 결과창 아직 안 봤으면 -> 결과창 표시
            _resultShown = true;
            isTriggered = true;
            DungeonUiManager.Instance.ShowResult();
        }
        else
        {
            // 결과창 이미 봤으면 -> 마을 이동
            GameManager.Instance.DungeonClear();
            GameManager.Instance.LoadVillage();
        }
    }

    public void GoToVillage()
    {
        SceneCutsceneController.Instance.PlayCutscene(ECutsceneType.DungeonEscape);
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