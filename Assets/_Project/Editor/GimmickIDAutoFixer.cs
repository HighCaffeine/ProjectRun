using UnityEngine;
using UnityEditor;

public class GimmickAutoFixer : Editor
{
    // 기믹 타입을 판별하는 헬퍼 함수
    private static int GetGimmickKeyType(BaseGimmick gimmick)
    {
        if (gimmick is Bridge) return 4;           // Bridge
        if (gimmick is ReMovePlatform) return 7;   // FallingPlatform
        if (gimmick is Platform) return 8;         // MovePlatform
        if (gimmick is SeesawTrigger) return 5;    // SeeSaw
        // if (gimmick is MovableGimmick) return 3; // MovableObject
        return 1; // BreakableWall (기본값)
    }

    private static string GetGimmickTypeName(BaseGimmick gimmick)
    {
        if (gimmick is Bridge) return "Bridge";
        if (gimmick is ReMovePlatform) return "FallingPlatform";
        if (gimmick is Platform) return "MovePlatform";
        if (gimmick is SeesawTrigger) return "SeeSaw";
        return "BreakableWall";
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

            GimmickInfo info = gimmick.GetComponent<GimmickInfo>();
            if (info != null)
            {
                info.gimmick_id = uid;
                info.gimmick_type = GetGimmickTypeName(gimmick);
                EditorUtility.SetDirty(info);
            }

            GimmickTrigger trigger = gimmick.GetComponent<GimmickTrigger>();
            if (trigger == null) trigger = gimmick.GetComponentInChildren<GimmickTrigger>();

            if (trigger != null)
            {
                TargetGimmickInfo tInfo = new TargetGimmickInfo();
                tInfo.gimmickID = uid;
                
                tInfo.gimmickKey = (GimmickKey)GetGimmickKeyType(gimmick); 

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