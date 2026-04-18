using UnityEngine;
using UnityEditor;

public class GimmickAutoFixer : Editor
{
    [MenuItem("Tools/GimmickIDAutoFixer")]
    public static void FixAllExistingGimmicks()
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

                // 타입 자동 추론
                if (gimmick is Bridge) info.gimmick_type = "Bridge";
                else if (gimmick is ReMovePlatform) info.gimmick_type = "FallingPlatform";
                else if (gimmick is Platform) info.gimmick_type = "MovePlatform";
                else if (gimmick is SeesawTrigger) info.gimmick_type = "SeeSaw";
                // else if (gimmick is MovableGimmick) info.gimmick_type = "MovableObject";
                else info.gimmick_type = "BreakableWall";

                EditorUtility.SetDirty(info);
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

        Debug.Log($"<color=cyan>[완료]</color> 총 {fixedCount}개의 기믹에 ID, Type, Tag 설정 완료");
    }
}