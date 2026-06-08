using System.Collections.Generic;
using UnityEngine;

public class ActorManager : GenericSingleton<ActorManager>
{
    private SectorGimmickManager[] _sectorManagers;


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

        bool isLocal = actors[name].IsLocal;

        int currentMap = DungeonPointManager.Instance.currentMapID;
        int currentSector = DungeonPointManager.Instance.currentSectorIndex;
        Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(currentMap, currentSector);

        foreach (var kvp in actors)
        {
            PlayerActor teammate = kvp.Value;
            if (teammate == null || teammate.gameObject == null) continue;
            if (!teammate.isDead)
            {
                teammate.PlayerDead(spawnPos, spawnDelay);
            }
        }

        if (isLocal)
        {
            SendPlayerDeadPacket(name, spawnPos);
        }

        Invoke(nameof(ResetDeadState), spawnDelay + 0.5f);
    }

    // public void RemovePlayer(string name)
    // {
    //     if (actors.TryGetValue(name, out PlayerActor targetActor))
    //     {
    //         actors.Remove(name);
    //         spawnPoints.Remove(name);
    //         if (p1 == targetActor) p1 = null;
    //         if (p2 == targetActor) p2 = null;
    //         ReassignRoles();
    //     }
    // }

    public void RemovePlayer(string name)
    {
        if (actors.TryGetValue(name, out PlayerActor targetActor))
        {
            actors.Remove(name);
            spawnPoints.Remove(name);

            if (p1 == targetActor) p1 = null;
            if (p2 == targetActor) p2 = null;

           // //Debug.Log($"[ActorManager] {name} 퇴장 처리 완료.");
        }
    }

    private void ReassignRoles()
    {
        // 먼저 모든 사람의 호스트 권한을 초기화
        foreach (var actor in actors.Values)
        {
            if (actor != null) actor.isHost = false;
        }

        // 딕셔너리에 남아있는 첫 번째 사람을 찾음
        PlayerActor newHost = null;
        foreach (var actor in actors.Values)
        {
            if (actor != null)
            {
                newHost = actor;
                break;
            }
        }

        // 새로운 호스트 지정 및 로컬 권한 동기화
        if (newHost != null)
        {
            newHost.isHost = true; // 내부 동적 호스트 갱신

            if (GameManager.Instance != null)
            {
                // 새로 방장이 된 사람이 내 화면의 로컬 캐릭터인지 체크해서 분기 권한 제어
                GameManager.Instance.isHost = newHost.IsLocal;
            }

           // //Debug.Log($"[ActorManager] 호스트 변경 완료 -> 새로운 호스트: {newHost.gameObject.name} (여캐여부 P1:{newHost == p1})");
        }
    }

    private void ResetDeadState()
    {
        isDeadAllAlready = false;
    }

    public void RequestGimmickReset()
    {
        int currentSector = DungeonPointManager.Instance.currentMapID;
        SectorGimmickManager[] managers = FindObjectsByType<SectorGimmickManager>(FindObjectsSortMode.None);
        foreach (var mgr in managers)
        {
            if (mgr.sectorIndex == currentSector)
            {
                mgr.RequestSectorReset();
                break;
            }
        }
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

           // //Debug.Log($"[ActorManager] {actorID} 초기 스폰 포인트: Map{currentMap}_Sector{currentSector}");
        }
    }

    public void UpdateSpawnIndex(string playerName, int newSectorIndex)
    {
////Debug.Log($"[UpdateSpawnIndex] {playerName}_{newSectorIndex}");

        if (spawnPoints.ContainsKey(playerName))
        {
            string current = spawnPoints[playerName];
            string mapLevel = current.Split('_')[0];
            spawnPoints[playerName] = $"{mapLevel}_{newSectorIndex}";
         //   //Debug.Log($"[System] {playerName}의 스폰 포인트가 {newSectorIndex} 구역으로 갱신되었습니다.");
        }
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
        if (!actors.ContainsKey(id) || !actors[id].IsLocal) return;

        P_PlayerDeadReq req = new P_PlayerDeadReq();
        req.respawnPos = new P_PacketVector3 { x = spawnPos.x, y = spawnPos.y, z = spawnPos.z };

        Client.TCP.SendPacket2(E_PACKET.PLAYER_DEAD_REQ, req);
      //  //Debug.Log($"[System] 사망 패킷 전송 완료 (부활 좌표: {spawnPos})");
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
          //  //Debug.Log($"{mapID}_{newIndex}");
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
