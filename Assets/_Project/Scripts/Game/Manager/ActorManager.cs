using System.Collections.Generic;
using UnityEngine;

public class ActorManager : GenericSingleton<ActorManager>
{

    public Dictionary<string, PlayerActor> actors = new Dictionary<string, PlayerActor>();
    public Dictionary<string, string> spawnPoints = new Dictionary<string, string>();

    public string localID;

    public float spawnDelay = 1f;

    [SerializeField]
    private GameObject deadUIPrefab;

    public PlayerActor p1 = null;
    public PlayerActor p2 = null;

    public PlayerActor GetPlayer(bool is2p)
    {
        foreach (var actor in actors.Values)
        {
            PlayerActor pa = actor as PlayerActor;
            if (pa == null) continue;

            if (pa.is2p == is2p)
                return pa;
        }

        return null;
    }
    public void OnPlayerDead(string name)
    {
        bool isLocal = (name == localID);

        if (!actors.ContainsKey(name)) return;

        PlayerActor actor = actors[name];

        string spawnInfo = spawnPoints[name];
        string[] split = spawnInfo.Split('_');


        int mapLevel = int.Parse(split[0]);
        int spawnIndex = int.Parse(split[1]);
        //  Vector3 spawnPos = MapManager.Instance.GetSpawnPoint(mapID, spawnIndex);
        Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(spawnIndex);
        SendPlayerDeadPacket(name, spawnPos);

        actor.PlayerDead(spawnPos, spawnDelay);

        if (isLocal)
        {
            ShowDeadUI(spawnDelay);
        }


    }

    public void OnPlayerDeadTestSpawn(string name, float time = 0.0f)
    {
        bool isLocal = (name == localID);

        if (!actors.ContainsKey(name)) return;

        PlayerActor actor = actors[name];

        string spawnInfo = spawnPoints[name];
        string[] split = spawnInfo.Split('_');


        int mapLevel = int.Parse(split[0]);
        int spawnIndex = int.Parse(split[1]);
        //  Vector3 spawnPos = MapManager.Instance.GetSpawnPoint(mapID, spawnIndex);
        Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(spawnIndex);
        SendPlayerDeadPacket(name, spawnPos);

        actor.PlayerDead(spawnPos, time);
    }

    public void AddPlayer(PlayerActor actor)
    {
        string actorID = actor.gameObject.name;
        if (!actors.ContainsKey(actorID))
        {
            spawnPoints.Add(actorID, "1_0");
            actors.Add(actorID, actor);

            actor.OnUpdatePoint += UpdateSpawnIndex;
        }
    }

    public void UpdateSpawnIndex(string playerName, int newSectorIndex)
    {
        Debug.Log($"[UpdateSpawnIndex] {playerName}_{newSectorIndex}");

        if (spawnPoints.ContainsKey(playerName))
        {
            string current = spawnPoints[playerName];
            string mapLevel = current.Split('_')[0];
            spawnPoints[playerName] = $"{mapLevel}_{newSectorIndex}";
            Debug.Log($"[System] {playerName}의 스폰 포인트가 {newSectorIndex} 구역으로 갱신되었습니다.");
        }
    }


    void ShowDeadUI(float delay)
    {
        deadUIPrefab.GetComponent<ReSpawnTimer>().respawnTime = delay;
        deadUIPrefab.SetActive(true);
    }

    // public void OnPlayerDead(string name)
    // {
    //     if (!actors.ContainsKey(name)) return;
    //     bool isLocal = (name == localID);

    //     PlayerActor actor = actors[name];
    //     string spawnInfo = spawnPoints[name];
    //     string[] split = spawnInfo.Split('_');

    //     int mapLevel = int.Parse(split[0]);
    //     int spawnIndex = int.Parse(split[1]);
    //     Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(spawnIndex);

    //     SendPlayerDeadPacket(name, spawnPos);

    //     actor.PlayerDead(spawnPos, spawnDelay);

    //     if (isLocal)
    //     {
    //         ShowDeadUI(spawnDelay);
    //     }
    // }

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
    public void UpdateAllSpawnPoints(int newIndex)
    {
        int currentMapID = DungeonPointManager.Instance.mapID;

        List<string> keys = new List<string>(actors.Keys);

        foreach (var id in keys)
        {
            spawnPoints[id] = $"{currentMapID}_{newIndex}";
        }
    }
    public void MoveAllPlayersToSector(int index)
    {
        Vector3 pos = DungeonPointManager.Instance.GetSpawnPosition(index);

        foreach (var actor in actors.Values)
        {
            CharacterController cc = actor.GetComponent<CharacterController>();

            if (cc != null)
                cc.enabled = false;

            actor.transform.position = pos;

            if (cc != null)
                cc.enabled = true;
        }
    }
}
