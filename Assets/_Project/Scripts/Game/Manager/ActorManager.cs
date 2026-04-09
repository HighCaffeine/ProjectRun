using System.Collections.Generic;
using UnityEngine;

public class ActorManager : GenericSingleton<ActorManager>
{

    public Dictionary<string, Actor> actors = new Dictionary<string, Actor>();
    public Dictionary<string, string> spawnPoints = new Dictionary<string, string>();

    public string localID;

    public float spawnDelay;

    public void OnPlayerDead(string name)
    {
        bool isLocal = (name == localID);

        Actor actor = actors[name];

        string spawnInfo = spawnPoints[name];
        string[] split = spawnInfo.Split('_');


        int mapLevel = int.Parse(split[0]);
        int spawnIndex = int.Parse(split[1]);
        //  Vector3 spawnPos = MapManager.Instance.GetSpawnPoint(mapLevel, spawnIndex);
        SendPlayerDeadPacket(name);


        if (isLocal)
        {
            ShowDeadUI(spawnDelay);
        }


    }
    void UpdateSpwanPoint(string name, int index)
    {

        // int currentMapLevel = MapManager.Instance.CurrentLevel;


        // spawnPoints[name] = $"{currentMapLevel}_{index}";
    }


    void ShowDeadUI(float delay)
    {
        // UI 코드 추가
    }

    void SendPlayerDeadPacket(string id)
    {
        // 서버 패킷 전송 코드 추가
    }


}
