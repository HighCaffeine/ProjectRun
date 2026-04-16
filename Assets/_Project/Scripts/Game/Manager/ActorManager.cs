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

    public void OnPlayerDead(string name)
    {
        bool isLocal = (name == localID);

        PlayerActor actor = actors[name];

        string spawnInfo = spawnPoints[name];
        string[] split = spawnInfo.Split('_');


        int mapLevel = int.Parse(split[0]);
        int spawnIndex = int.Parse(split[1]);
        //  Vector3 spawnPos = MapManager.Instance.GetSpawnPoint(mapID, spawnIndex);
        Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(spawnIndex);
        SendPlayerDeadPacket(name);

        actor.PlayerDead(spawnPos, spawnDelay);

        if (isLocal)
        {
            ShowDeadUI(spawnDelay);
        }


    }

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

    void SendPlayerDeadPacket(string id)
    {
        // ���� ��Ŷ ���� �ڵ� �߰�
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
