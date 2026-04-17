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


    public int currentSectorIndex = 0;

    [SerializeField]
    private GameObject resultUI;
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
        resultUI.gameObject.SetActive(false);
    }
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape))
        {
            if (!resultUI.gameObject.activeSelf)
            {
                resultUI.gameObject.SetActive(true);

            }
            else
            {
                resultUI.gameObject.SetActive(false);
            }
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

    public void MoveToNextSector()
    {
        if (currentSectorIndex + 1 >= sectors.Length)
        {
            Debug.Log("[Dungeon] 마지막 구역입니다.");
            UiManager.instance.StopCount();
            resultUI.gameObject.SetActive(true);
            return;
        }

        currentSectorIndex++;

        Vector3 spawnPos = GetSpawnPosition(currentSectorIndex);

        Debug.Log($"[Dungeon] 다음 구역 이동: {currentSectorIndex}");

        ActorManager.Instance.UpdateAllSpawnPoints(currentSectorIndex);
        ActorManager.Instance.MoveAllPlayersToSector(currentSectorIndex);
    }
}