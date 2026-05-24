using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GimmickAutoFixer : Editor
{
    private static eGimmickKey GetGimmickKey(BaseGimmick gimmick)
    {
        if (gimmick is Bridge) return eGimmickKey.Bridge;
        if (gimmick is ReMovePlatform) return eGimmickKey.FallingPlatform;
        if (gimmick is Platform) return eGimmickKey.MovePlatform;
        if (gimmick is SeesawTrigger) return eGimmickKey.SeeSaw;
        if (gimmick is MovableGimmick) return eGimmickKey.MovableObject;
        if (gimmick is BreakableWall) return eGimmickKey.BreakableWall;
        if (gimmick is BreakableObj) return eGimmickKey.BreakableObj;
        if (gimmick is Bomb) return eGimmickKey.Bomb;
        if (gimmick is MonsterSpawnArea) return eGimmickKey.MonsterSpawnArea;

        return eGimmickKey.BreakableWall;
    }

    private static eGimmickType GetBaseGimmickType(BaseGimmick gimmick)
    {
        if (gimmick is MovableGimmick) return eGimmickType.Movable;
        if (gimmick is BreakableWall || gimmick is BreakableObj) return eGimmickType.Breakable;
        if (gimmick is MonsterSpawnArea) return eGimmickType.NONE;

        return eGimmickType.NONE;
    }

    private static string GetGimmickTag(BaseGimmick gimmick)
    {
        switch (gimmick)
        {
            case BreakableWall _:
            case BreakableObj _:
                return "Breakable";
            case MonsterSpawnArea _:
                return "Gimmick";
            default:
                return "Gimmick";
        }
    }

    [MenuItem("Tools/GimmickAutoFixer")]
    public static void FixAndLinkAllGimmicks()
    {
        BaseGimmick[] allGimmicks = FindObjectsByType<BaseGimmick>(FindObjectsSortMode.None);

        // 오브젝트 인스턴스 → 신규 ID 매핑
        Dictionary<BaseGimmick, int> gimmickToNewID = new Dictionary<BaseGimmick, int>();

        int fixedCount = 0;
        int currentId = 1000;

        // 모든 기믹에 새 ID 할당 + 매핑 저장
        foreach (var gimmick in allGimmicks)
        {
            int newUID = currentId++;

            // 매핑 저장 (이름이나 기존 ID가 아닌 오브젝트 자체를 키로 사용)
            gimmickToNewID[gimmick] = newUID;

            gimmick.gimmickUID = newUID;
            gimmick.gimmickType = GetBaseGimmickType(gimmick);
            EditorUtility.SetDirty(gimmick);

            // GimmickInfo 세팅
            GimmickInfo info = gimmick.GetComponent<GimmickInfo>();
            if (info != null)
            {
                info.gimmick_id = newUID;
                info.gimmick_type = GetGimmickKey(gimmick);
                EditorUtility.SetDirty(info);
            }

            // 태그 본체에 지정
            gimmick.gameObject.tag = GetGimmickTag(gimmick);
            gimmick.gameObject.layer = LayerMask.NameToLayer("Actionable");
            EditorUtility.SetDirty(gimmick.gameObject);

            // 부모 오브젝트 이름 강제 변경
            if (gimmick.transform.parent != null)
            {
                string scriptName = gimmick.GetType().Name;
                gimmick.transform.parent.name = $"{scriptName}_{newUID}";
                EditorUtility.SetDirty(gimmick.transform.parent.gameObject);
            }

            fixedCount++;
        }

        // 일반 기믹 트리거 연결
        foreach (var gimmick in allGimmicks)
        {
            if (gimmick is MonsterSpawnArea) continue;

            GimmickTrigger trigger = gimmick.GetComponent<GimmickTrigger>();
            if (trigger == null) trigger = gimmick.GetComponentInChildren<GimmickTrigger>();

            if (trigger != null)
            {
                TargetGimmickInfo tInfo = new TargetGimmickInfo();
                tInfo.gimmickID = gimmick.gimmickUID;
                tInfo.gimmickKey = GetGimmickKey(gimmick);

                trigger.targetGimmicks.Clear();
                trigger.targetGimmicks.Add(tInfo);
                EditorUtility.SetDirty(trigger);
            }
        }

        // MonsterSpawnArea 전용 트리거 처리
        FixMonsterSpawnTriggers(allGimmicks, gimmickToNewID);

        Debug.Log($"<color=cyan>[완료]</color> 총 {fixedCount}개의 기믹 세팅 및 트리거 자동 연결");
    }

    // MonsterSpawnArea 전용 트리거 처리 함수
    private static void FixMonsterSpawnTriggers(BaseGimmick[] allGimmicks, Dictionary<BaseGimmick, int> gimmickToNewID)
    {
        // MonsterSpawnArea만 필터링
        List<MonsterSpawnArea> spawnAreas = new List<MonsterSpawnArea>();
        foreach (var gimmick in allGimmicks)
        {
            if (gimmick is MonsterSpawnArea area)
            {
                spawnAreas.Add(area);
            }
        }

        if (spawnAreas.Count == 0)
        {
            Debug.Log("<color=yellow>[MonsterSpawn]</color> MonsterSpawnArea가 없습니다.");
            return;
        }

        // 모든 GimmickTrigger 찾기
        GimmickTrigger[] allTriggers = FindObjectsByType<GimmickTrigger>(FindObjectsSortMode.None);

        int triggerFixCount = 0;

        foreach (var trigger in allTriggers)
        {
            // MonsterSpawnArea 타겟이 하나라도 있는지 확인
            bool hasMonsterSpawnTarget = false;
            foreach (var target in trigger.targetGimmicks)
            {
                if (target.gimmickKey == eGimmickKey.MonsterSpawnArea)
                {
                    hasMonsterSpawnTarget = true;
                    break;
                }
            }

            if (!hasMonsterSpawnTarget) continue;

            // 트리거가 속한 부모의 자식들 중 MonsterSpawnArea 찾기
            List<TargetGimmickInfo> newTargets = new List<TargetGimmickInfo>();

            Transform triggerParent = trigger.transform.parent;

            // 같은 부모 안에서 MonsterSpawnArea 찾기
            if (triggerParent != null)
            {
                foreach (var area in spawnAreas)
                {
                    // 같은 부모를 공유하거나, 트리거가 스폰구역의 형제인 경우
                    if (area.transform.parent == triggerParent || area.transform.IsChildOf(triggerParent))
                    {
                        TargetGimmickInfo newTarget = new TargetGimmickInfo();
                        newTarget.gimmickID = area.gimmickUID;
                        newTarget.gimmickKey = eGimmickKey.MonsterSpawnArea;
                        newTargets.Add(newTarget);
                    }
                }
            }

            // 트리거 업데이트
            if (newTargets.Count > 0)
            {
                trigger.targetGimmicks = newTargets;
                EditorUtility.SetDirty(trigger);
                triggerFixCount++;
                Debug.Log($"<color=green>[MonsterSpawn Trigger]</color> {trigger.name} - {newTargets.Count}개 에어리어 재연결 완료");
            }
        }

        if (triggerFixCount == 0)
        {
            Debug.LogWarning("<color=yellow>[MonsterSpawn]</color> 연결된 트리거가 없습니다. 트리거와 스폰 구역이 같은 부모 안에 있는지 확인하세요.");
        }
    }
}