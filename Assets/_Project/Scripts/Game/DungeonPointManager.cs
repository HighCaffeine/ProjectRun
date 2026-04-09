using System;
using UnityEngine;

// 구역(Sector) 단위로 데이터를 묶어서 관리하는 구조체
[Serializable]
public struct DungeonSector
{
    [Tooltip("해당 구역 진입 시 플레이어가 스폰될 위치")]
    public Transform spawnPoint;

    // public int requiredGimmickID; 
}

public class DungeonPointManager : MonoBehaviour
{
    public static DungeonPointManager Instance => instance;
    private static DungeonPointManager instance;
    [Header("던전 구역(Sector) 데이터")]
    [Tooltip("0번: 던전 시작 구역 / 1~N번: 다음 기믹 구역")]
    public DungeonSector[] sectors;

    public int mapID;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 특정 구역의 스폰 좌표를 반환하는 함수
    public Vector3 GetSpawnPosition(int sectorIndex)
    {
        if (sectors == null || sectorIndex < 0 || sectorIndex >= sectors.Length)
        {
            Debug.LogError($"[DungeonPointManager] {sectorIndex}번 구역 데이터를 찾을 수 없습니다!");
            return Vector3.zero;
        }
        return sectors[sectorIndex].spawnPoint.position;
    }
}