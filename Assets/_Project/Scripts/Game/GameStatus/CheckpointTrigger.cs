using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [Header("저장할 구역 설정")]
    public int targetMapID;      // 현재 레벨 ID
    public int targetSectorIndex; // 갱신될 섹터 번호

    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        // 이미 활성화된 체크포인트면 무시
        if (isActivated) return;

        PlayerActor actor = other.GetComponent<PlayerActor>();

        if (actor != null && actor.IsLocal)
        {
            isActivated = true;

            // 매니저들의 현재 상태 정보 갱신
            DungeonPointManager.Instance.currentMapID = targetMapID;
            DungeonPointManager.Instance.currentSectorIndex = targetSectorIndex;

            // 모든 플레이어의 부활 지점을 이 구역으로 동기화
            ActorManager.Instance.UpdateAllSpawnPoints(targetMapID, targetSectorIndex);

            Debug.Log($"<color=yellow>[Checkpoint]</color> Map {targetMapID} - Sector {targetSectorIndex} 체크포인트 설정");

            // PlaySaveEffect();
        }
    }
}