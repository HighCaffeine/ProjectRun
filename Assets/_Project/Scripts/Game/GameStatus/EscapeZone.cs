using System.Collections.Generic;
using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    public List<PlayerActor> escapePlayers = new List<PlayerActor>();
    [SerializeField] private GameObject diary;

    private bool _cutscenePlayed = false;
    public bool CutscenePlayed => _cutscenePlayed;
    public bool isFirstShowResult = false;

    private bool _escapeSent = false;
    private bool _resultShown = false;
    private bool isTriggered = false;

    private void Update()
    {
        if (_escapeSent) return;

        PlayerActor localActor = GetLocalPlayerActor();
        if (localActor == null || !escapePlayers.Contains(localActor)) return;

        _escapeSent = true;
        SendEscapePacket();
    }

    private void SendEscapePacket()
    {
        P_DungeonEscapeReq pkt = new P_DungeonEscapeReq();

        PlayerActor localActor = GetLocalPlayerActor();
        if (localActor == null)
        {
            Client.TCP.SendPacket2(E_PACKET.DUNGEON_ESCAPE_REQ, pkt);
            return;
        }

        if (localActor == ActorManager.Instance.p1)
        {
            pkt.p1Push = localActor.pushCount;
            pkt.p1Pull = localActor.pullCount;
            pkt.p1Fall = localActor.fallDeathCount;
        }
        else // p2
        {
            pkt.p2Push = localActor.pushCount;
            pkt.p2Pull = localActor.pullCount;
            pkt.p2Fall = localActor.fallDeathCount;
        }

        Client.TCP.SendPacket2(E_PACKET.DUNGEON_ESCAPE_REQ, pkt);
        Debug.Log($"[EscapeZone] 탈출 패킷 전송 - push:{localActor.pushCount} pull:{localActor.pullCount} fall:{localActor.fallDeathCount}");
    }

    private PlayerActor GetLocalPlayerActor()
    {
        if (Match.Instance == null) return null;
        if (!Match.Instance.Players.TryGetValue(LocalPlayerInfo.ID, out Player localPlayer)) return null;
        return localPlayer.GetComponent<PlayerActor>();
    }

    public void OnActiveResult()
    {
        if (!_resultShown)
        {
            _resultShown = true;
            isTriggered = true;
            DungeonUiManager.Instance.ShowResult();
        }
        else
        {
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
        if (actor != null && !escapePlayers.Contains(actor))
            escapePlayers.Add(actor);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerActor actor = other.GetComponent<PlayerActor>();
        if (actor != null)
            escapePlayers.Remove(actor);
    }
}