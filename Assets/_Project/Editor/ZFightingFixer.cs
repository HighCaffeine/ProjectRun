using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ZFightingFixer : EditorWindow
{
    [MenuItem("Tools/Fix Z-Fighting")]
    public static void FixZFighting()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("최상위 루트 오브젝트를 선택해주세요");
            return;
        }

        List<Transform> targetsToRecord = new List<Transform>();

        foreach (GameObject root in selectedObjects)
        {
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                if (child == root.transform) continue;

                if (child.GetComponent<MeshRenderer>() == null) continue;

                if (!targetsToRecord.Contains(child))
                {
                    targetsToRecord.Add(child);
                }
            }
        }

        if (targetsToRecord.Count == 0)
        {
            Debug.LogWarning("조정할 하위 오브젝트가 없습니다.");
            return;
        }

        Undo.RecordObjects(targetsToRecord.ToArray(), "Fix Z-Fighting");

        for (int i = 0; i < targetsToRecord.Count; i++)
        {
            Vector3 pos = targetsToRecord[i].position;
            pos.y += ((i + 1) * 0.0001f);
            targetsToRecord[i].position = pos;
        }

        Debug.Log($"<color=green>[Z-Fighting Fix]</color> 루트 제외 총 {targetsToRecord.Count}개의 하위 오브젝트 Y값 미세 조정");
    }
}