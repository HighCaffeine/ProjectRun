using UnityEngine;
using UnityEditor;

public class GimmickAutoFixer : Editor
{
    // 기믹 타입을 판별하는 헬퍼 함수
    private static eGimmickKey GetGimmickKey(BaseGimmick gimmick)
    {
        if (gimmick is Bridge) return eGimmickKey.Bridge;
        if (gimmick is ReMovePlatform) return eGimmickKey.FallingPlatform;
        if (gimmick is Platform) return eGimmickKey.MovePlatform;
        if (gimmick is SeesawTrigger) return eGimmickKey.SeeSaw;
        if (gimmick is MovableGimmick) return eGimmickKey.MovableObject;

        if (gimmick is BreakableWall) return eGimmickKey.BreakableWall; // 넉백 벽
        if (gimmick is BreakableObj) return eGimmickKey.BreakableObj;   // 평타 항아리
        return eGimmickKey.BreakableWall;
    }

    private static eGimmickType GetBaseGimmickType(BaseGimmick gimmick)
    {
        if (gimmick is MovableGimmick) return eGimmickType.Movable;
        if (gimmick is BreakableWall || gimmick is BreakableObj) return eGimmickType.Breakable;
        return eGimmickType.NONE;
    }


    [MenuItem("Tools/GimmickAutoFixer")]
    public static void FixAndLinkAllGimmicks()
    {
        BaseGimmick[] allGimmicks = FindObjectsByType<BaseGimmick>(FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (var gimmick in allGimmicks)
        {
            int uid = gimmick.gimmickUID;
            if (uid == 0)
            {
                uid = EditorPrefs.GetInt("GlobalGimmickUID_Counter", 1000);
                EditorPrefs.SetInt("GlobalGimmickUID_Counter", uid + 1);
                gimmick.gimmickUID = uid;
                EditorUtility.SetDirty(gimmick);
            }

            gimmick.gimmickType = GetBaseGimmickType(gimmick);
            EditorUtility.SetDirty(gimmick);

            GimmickInfo info = gimmick.GetComponent<GimmickInfo>();
            if (info != null)
            {
                info.gimmick_id = uid;
                info.gimmick_type = GetGimmickKey(gimmick);
                EditorUtility.SetDirty(info);
            }

            GimmickTrigger trigger = gimmick.GetComponent<GimmickTrigger>();
            if (trigger == null) trigger = gimmick.GetComponentInChildren<GimmickTrigger>();

            if (trigger != null)
            {
                TargetGimmickInfo tInfo = new TargetGimmickInfo();
                tInfo.gimmickID = uid;

                tInfo.gimmickKey = GetGimmickKey(gimmick);

                trigger.targetGimmicks.Clear();
                trigger.targetGimmicks.Add(tInfo);
                EditorUtility.SetDirty(trigger);
            }

            Transform parentT = gimmick.transform.parent;
            if (parentT != null && parentT.name.Contains(uid.ToString()))
            {
                parentT.gameObject.tag = "Gimmick";
                EditorUtility.SetDirty(parentT.gameObject);
            }
            else
            {
                gimmick.gameObject.tag = "Gimmick";
            }

            fixedCount++;
        }

        Debug.Log($"<color=cyan>[완료]</color> 총 {fixedCount}개의 기믹 세팅 및 트리거 자동 연결");
    }
}