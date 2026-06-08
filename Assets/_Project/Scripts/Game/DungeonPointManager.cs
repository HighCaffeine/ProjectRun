using NUnit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

// 구역(Sector) 단위로 데이터를 묶어서 관리하는 구조체
[Serializable]
public struct DungeonSector
{
    [Tooltip("해당 구역 진입 시 플레이어가 스폰될 위치")]
    public Transform spawnPoint;

    // public int requiredGimmickID; 
}

[Serializable]
public struct MapData
{
    public int mapID;
    public DungeonSector[] sectors;
}

public class DungeonPointManager : GenericSingleton<DungeonPointManager>
{
    [Header("던전 구역(Map) 데이터")]
    [Tooltip("0번: 던전 시작 구역 / 1~N번: 다음 기믹 구역")]
    public List<MapData> mapDataList = new List<MapData>();

    public int currentMapID = 1;
    public int currentSectorIndex = 0;

    private new void Awake()
    {
        base.Awake();
    }

    // 특정 구역의 스폰 좌표를 반환하는 함수
    public Vector3 GetSpawnPosition(int mapID, int sectorIndex)
    {
        MapData mapData = mapDataList.Find(m => m.mapID == mapID);

        if (mapData.sectors == null || sectorIndex < 0 || sectorIndex >= mapData.sectors.Length)
        {
            Debug.LogError($"[DungeonPointManager] Map {mapID}의 {sectorIndex}번 구역을 찾을 수 없습니다!");
            return Vector3.zero;
        }
        return mapData.sectors[sectorIndex].spawnPoint.position;
    }

    public void MoveToNextSector()
    {
        MapData mapData = mapDataList.Find(m => m.mapID == currentMapID);

        if (currentSectorIndex + 1 >= mapData.sectors.Length)
        {
            Debug.Log("[Dungeon] 맵의 마지막 구역입니다.");
            return;
        }

        currentSectorIndex++;

        Vector3 spawnPos = GetSpawnPosition(currentMapID, currentSectorIndex);
        Debug.Log($"[Dungeon] 다음 구역 이동: Map {currentMapID} - Sector {currentSectorIndex}");
        SetCurrentMap(currentMapID);
        ActorManager.Instance.UpdateAllSpawnPoints(currentMapID, currentSectorIndex);
        ActorManager.Instance.MoveAllPlayersToSector(currentMapID, currentSectorIndex);
    }
    public void SetCurrentMap(int mapID)
    {
        Debug.Log(mapID);
        ProGressUi.Instance.StageUpdate(mapID);
        if (!GameManager.Instance.hasShownDungeonIntro)
        {
            GameManager.Instance.hasShownDungeonIntro = true;

            ProGressUi.Instance.OnProgressFinished = () =>
            {
                DialogueManager.Instance.StartDialogue("ProGressText");
            };
        }
        DungeonUiManager.Instance.ShowProgress();
    }
}