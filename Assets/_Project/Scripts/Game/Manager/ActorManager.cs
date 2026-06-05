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

    private bool isDeadAllAlready = false;

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
        if (!actors.ContainsKey(name)) return;

        if (isDeadAllAlready) return;
        isDeadAllAlready = true;

        bool isLocal = (name == localID);

        int currentMap = DungeonPointManager.Instance.currentMapID;
        int currentSector = DungeonPointManager.Instance.currentSectorIndex;
        Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(currentMap, currentSector);

        foreach (var kvp in actors)
        {
            PlayerActor teammate = kvp.Value;
            if (!teammate.isDead)
            {
                teammate.PlayerDead(spawnPos, spawnDelay);
            }
        }

        ShowDeadUI(spawnDelay);

        if (isLocal)
        {
            SendPlayerDeadPacket(name, spawnPos);
        }

        if (GameManager.Instance.isHost)
        {
            SectorGimmickManager[] managers = FindObjectsByType<SectorGimmickManager>(FindObjectsSortMode.None);
            foreach (var mgr in managers)
            {
                if (mgr.sectorIndex == currentSector)
                {
                    mgr.RequestSectorReset();
                    break;
                }
            }
            Debug.Log($"<color=red>[ActorManager] 파티 전멸!</color> 방장이 섹터 {currentSector} 기믹 초기화를 요청합니다.");
        }

        // 부활 대기시간이 끝난 후 다시 죽을 수 있도록 플래그 해제
        Invoke(nameof(ResetDeadState), spawnDelay + 0.5f);
    }

    private void ResetDeadState()
    {
        isDeadAllAlready = false;
    }

    public void AddPlayer(PlayerActor actor)
    {
        string actorID = actor.gameObject.name;
        if (!actors.ContainsKey(actorID))
        {
            // 현재 던전 매니저의 맵/섹터 정보로 초기화
            int currentMap = DungeonPointManager.Instance.currentMapID;
            int currentSector = DungeonPointManager.Instance.currentSectorIndex;

            spawnPoints.Add(actorID, $"{currentMap}_{currentSector}");
            actors.Add(actorID, actor);
            actor.OnUpdatePoint += UpdateSpawnIndex;

            Debug.Log($"[ActorManager] {actorID} 초기 스폰 포인트: Map{currentMap}_Sector{currentSector}");
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
    public void UpdateAllSpawnPoints(int mapID, int newIndex)
    {
        List<string> keys = new List<string>(actors.Keys);
        foreach (var id in keys)
        {
            Debug.Log($"{mapID}_{newIndex}");
            spawnPoints[id] = $"{mapID}_{newIndex}";
        }
    }

    public void MoveAllPlayersToSector(int mapID, int index)
    {
        Vector3 pos = DungeonPointManager.Instance.GetSpawnPosition(mapID, index);

        foreach (var actor in actors.Values)
        {
            CharacterController cc = actor.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            actor.transform.position = pos;

            if (cc != null) cc.enabled = true;
        }
    }


}
