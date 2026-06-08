using System.Collections.Generic;
using UnityEngine;

//트리거 하나가 area ID 전부 받아서 실행시킬거임
public class MonsterSpawnArea : BaseGimmick
{
    [System.Serializable]
    public struct SpawnData
    {
        public int assignMonsterID;      // 부여할 몬스터 고유 ID
        public GameObject monsterPrefab; // 스폰할 몬스터 프리팹
    }

    [Header("Monster Spawn Settings")]
    [Tooltip("이 구역이 작동할 때 동시에 스폰될 몬스터 목록")]
    public List<SpawnData> spawnList = new List<SpawnData>();

    private bool isTriggered = false;

    private List<GameObject> spawnedMonsters = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        // 로컬 플레이어가 구역에 진입했을 때만 서버로 트리거 작동 요청
        PlayerActor player = other.GetComponent<PlayerActor>();
        if (player != null && player.IsLocal)
        {
            P_GimmickInteractReq req = new P_GimmickInteractReq
            {
                activeUUID = LocalPlayerInfo.ID,
                gimmickID = this.gimmickUID,
                gimmickKey = (byte)this.gimmickType,
                state = 1, // 1 = 활성화 (스폰 작동)
                targetPos = new P_PacketVector3 { x = 0, y = 0, z = 0 },
                param = 0f,
                timestamp = NetworkTimeManager.Instance.GetServerTime()
            };

            Client.TCP.SendPacket2(E_PACKET.GIMMICK_INTERACT_REQ, req);
        }
    }

    public override void Execute(P_GimmickInteractNtf ntf)
    {
        if (ntf.state == 1 && !isTriggered)
        {
            isTriggered = true;
            SpawnAllMonsters();
        }
    }

    private void SpawnAllMonsters()
    {
        for (int i = 0; i < spawnList.Count; i++)
        {
            var data = spawnList[i];
            if (data.monsterPrefab != null)
            {
                GameObject obj = Instantiate(data.monsterPrefab, transform.position, transform.rotation);
                spawnedMonsters.Add(obj); // 추적용 리스트에 추가

                MonsterActor monster = obj.GetComponent<MonsterActor>();
                if (monster != null)
                {
                    int generatedMonsterID = (this.gimmickUID * 100) + i;
                    monster.InitMonster(generatedMonsterID, transform.position, transform.rotation);
                }
            }
        }
        //Debug.Log($"<color=green>[MonsterSpawnArea]</color> ({gimmickUID}) : {spawnList.Count}마리 몬스터 스폰");
    }
    public override void ResetGimmick()
    {
        isTriggered = false;

        // 스폰된 몬스터 오브젝트 제거
        foreach (var obj in spawnedMonsters)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedMonsters.Clear();

        //Debug.Log($"[MonsterSpawnArea] {gimmickUID} 구역 트리거 초기화 완료");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.crimson;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
    }
}