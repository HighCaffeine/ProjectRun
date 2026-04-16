using System.Collections.Generic;
using UnityEngine;

public class ActorManager : GenericSingleton<ActorManager>
{

    public Dictionary<string, PlayerActor> actors = new Dictionary<string, PlayerActor>();
    public Dictionary<string, string> spawnPoints = new Dictionary<string, string>();

    public string localID;

    public float spawnDelay = 5f;

    [SerializeField]
    private GameObject deadUIPrefab;


    public void AddPlayer(PlayerActor actor)
    {
        string actorID = actor.gameObject.name;
        if (!actors.ContainsKey(actorID))
        {
            spawnPoints.Add(actorID, "1_0");
            actors.Add(actorID, actor);

            actor.OnUpdatePoint += UpdateSpwanPoint;
        }
    }

    void UpdateSpwanPoint(string id, int index)
    {

        //int currentMapID = MapManager.Instance.currentMapID;
        int currentMapID = DungeonPointManager.Instance.mapID;


        spawnPoints[id] = $"{currentMapID}_{index}";
    }


    void ShowDeadUI(float delay)
    {
        deadUIPrefab.GetComponent<ReSpawnTimer>().respawnTime = delay;
        deadUIPrefab.SetActive(true);
    }

    public void OnPlayerDead(string name)
    {
        if (!actors.ContainsKey(name)) return;
        bool isLocal = (name == localID);

        PlayerActor actor = actors[name];
        string spawnInfo = spawnPoints[name];
        string[] split = spawnInfo.Split('_');

        int mapLevel = int.Parse(split[0]);
        int spawnIndex = int.Parse(split[1]);
        Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(spawnIndex);

        SendPlayerDeadPacket(name, spawnPos);

        actor.PlayerDead(spawnPos, spawnDelay);

        if (isLocal)
        {
            ShowDeadUI(spawnDelay);
        }
    }

    // ★ 구현됨: 로컬 유저일 때만 서버로 패킷 전송
    void SendPlayerDeadPacket(string id, Vector3 spawnPos)
    {
        if (id != localID) return; // 내 캐릭터가 죽은 것만 서버에 보고

        P_PlayerDeadReq req = new P_PlayerDeadReq();
        req.respawnPos = new P_PacketVector3 { x = spawnPos.x, y = spawnPos.y, z = spawnPos.z };

        Client.TCP.SendPacket2(E_PACKET.PLAYER_DEAD_REQ, req);
        Debug.Log($"[System] 사망 패킷 전송 완료 (부활 좌표: {spawnPos})");
    }

    public Actor GetActor(string name)
    {
        if (actors.TryGetValue(name, out PlayerActor actor))
        {
            return actor;
        }
        return null;
    }
}
