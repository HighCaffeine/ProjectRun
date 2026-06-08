using UnityEngine;

public class TEST_GimmickResetTrigger : MonoBehaviour
{
    public int targetSector = 6;

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.isHost)
        {
            SectorGimmickManager[] managers = FindObjectsByType<SectorGimmickManager>(FindObjectsSortMode.None);
            foreach (var mgr in managers)
            {
                if (mgr.sectorIndex == targetSector)
                {
                    mgr.RequestSectorReset();
                    break;
                }
            }
            Debug.Log($"<color=red>[ActorManager] 파티 전멸!</color> 방장이 섹터 {targetSector} 기믹 초기화를 요청합니다.");
        }
    }
}
