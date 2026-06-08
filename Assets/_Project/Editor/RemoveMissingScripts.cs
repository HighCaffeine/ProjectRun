using UnityEngine;
using UnityEditor;

public class RemoveMissingScripts
{
    [MenuItem("Tools/Cleanup/Remove Missing Scripts")]
    static void CleanUpAllMissingScripts()
    {
        int missingCount = 0;

        // 1. 현재 씬 검사
        GameObject[] sceneObjects = GameObject.FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in sceneObjects)
        {
            missingCount += CleanObject(obj, "Scene Object");
        }

        // 2. 프로젝트 폴더 내의 모든 프리팹 검사
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            
            if (prefab != null)
            {
                missingCount += CleanObject(prefab, "Prefab");
            }
        }

        AssetDatabase.SaveAssets(); // 변경사항 저장
        Debug.Log($"<color=cyan>[완료]</color> 씬 & 프리팹 전체 검사 완료 총 {missingCount}개의 Missing Script 제거됨.");
    }

    static int CleanObject(GameObject obj, string type)
    {
        int removedCount = 0;
        int countBefore = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
        
        if (countBefore > 0)
        {
            Undo.RegisterCompleteObjectUndo(obj, "Remove missing scripts");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            
            int countAfter = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
            removedCount = countBefore - countAfter;

            if (removedCount > 0)
            {
                Debug.LogWarning($"[{type}] {obj.name} 에서 {removedCount}개의 Missing Script 제거됨!", obj);
                EditorUtility.SetDirty(obj); // 프리팹 변경사항 마킹
            }
        }

        // 자식 오브젝트들도 재귀적으로 검사
        foreach (Transform child in obj.transform)
        {
            removedCount += CleanObject(child.gameObject, type);
        }

        return removedCount;
    }
}