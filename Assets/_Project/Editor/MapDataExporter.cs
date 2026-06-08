using UnityEngine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

[Serializable]
public class _Vector3
{
    public float x, y, z;
    public _Vector3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
}

#region Data Format
[Serializable]
public class MapMeta
{
    public int map_id;
    public string map_name;
    public string version;
}

[Serializable]
public class GimmickData
{
    public int gimmick_id;
    public string type;
    public _Vector3 position;
    public float rotation_y;
    public _Vector3 start_pos;
    public _Vector3 end_pos;
    public Dictionary<string, float> properties = new Dictionary<string, float>();
}

[Serializable]
public class MapExportData
{
    public MapMeta meta;
    public List<GimmickData> gimmicks = new List<GimmickData>();
}
#endregion

public class MapDataExporter : Editor
{
    [MenuItem("Tools/SelectedMapModuleExport")]
    public static void ExportSelectedModule()
    {
        GameObject selectedObj = Selection.activeGameObject;

        // 선택 확인
        if (selectedObj == null)
        {
            Debug.LogError("[Export] 오브젝트가 선택되지 않았습니다!");
            return;
        }
        Debug.Log($"[Export] 선택된 오브젝트: {selectedObj.name}");

        // MapModuleGrid 찾기
        MapModuleGrid[] grids = selectedObj.GetComponentsInChildren<MapModuleGrid>();
        Debug.Log($"[Export] 찾은 MapModuleGrid 개수: {grids.Length}");

        if (grids.Length == 0)
        {
            Debug.LogError("[Export] MapModuleGrid 컴포넌트를 찾을 수 없습니다!");
            Debug.Log($"[Export Tip] '{selectedObj.name}' 또는 그 자식에 MapModuleGrid 컴포넌트가 있는지 확인하세요.");
            return;
        }

        GimmickInfo[] allGimmicksInMap = selectedObj.GetComponentsInChildren<GimmickInfo>(true);
        HashSet<int> usedIds = new HashSet<int>();
        List<GimmickInfo> gimmicksToAssign = new List<GimmickInfo>();

        // 이미 사용 중인 고유 ID 수집
        foreach (var info in allGimmicksInMap)
        {
            if (info.gimmick_id > 0 && !usedIds.Contains(info.gimmick_id))
            {
                usedIds.Add(info.gimmick_id); // 안전한 기존 ID 등록
            }
            else
            {
                gimmicksToAssign.Add(info); // ID가 0이거나 중복된 녀석들은 대기열로
            }
        }

        // 빈 번호를 찾아서 새로 할당
        int autoGimmickId = 1000;
        foreach (var info in gimmicksToAssign)
        {
            // 사용 중인 ID를 피해서 빈 번호 찾기
            while (usedIds.Contains(autoGimmickId))
            {
                autoGimmickId++;
            }

            info.gimmick_id = autoGimmickId;
            usedIds.Add(autoGimmickId);
            EditorUtility.SetDirty(info);

            BaseGimmick baseGimmick = info.GetComponent<BaseGimmick>();
            if (baseGimmick != null)
            {
                baseGimmick.gimmickUID = autoGimmickId;
                EditorUtility.SetDirty(baseGimmick);
            }

            EditorUtility.SetDirty(info);
            Debug.Log($"<color=cyan>[Export ID 발급]</color> '{info.gameObject.name}'에 새 ID 할당 -> {autoGimmickId}");
        }


        MapExportData exportData = new MapExportData();
        exportData.meta = new MapMeta
        {
            map_id = 101,
            map_name = selectedObj.name,
            version = "1.0.0"
        };

        int totalGimmicksFound = 0;

        HashSet<int> exportedIds = new HashSet<int>();

        // 각 Grid 순회
        foreach (MapModuleGrid grid in grids)
        {
            Debug.Log($"[Export] Grid 처리 중: {grid.gameObject.name}");

            GimmickInfo[] allGimmicks = grid.GetComponentsInChildren<GimmickInfo>(true);
            Debug.Log($"[Export]   └─ 찾은 GimmickInfo 개수: {allGimmicks.Length}");

            foreach (GimmickInfo info in allGimmicks)
            {
                if (exportedIds.Contains(info.gimmick_id)) continue;

                Transform child = info.transform;
                string tag = child.tag;

                // Tag 확인
                if (child.CompareTag("Gimmick") || child.CompareTag("Breakable"))
                {
                    GimmickData gd = new GimmickData();
                    gd.position = new _Vector3(child.position);
                    gd.rotation_y = (float)Math.Round(child.eulerAngles.y, 3);
                    gd.gimmick_id = info.gimmick_id;
                    gd.type = info.gimmick_type.ToString();

                    foreach (var prop in info.properties)
                    {
                        string keyStr = prop.key.ToString();
                        if (!gd.properties.ContainsKey(keyStr)) gd.properties.Add(keyStr, prop.value);
                    }

                    Platform platform = child.GetComponent<Platform>();
                    if (platform != null)
                    {
                        if (platform.startPos != null) gd.start_pos = new _Vector3(platform.startPos.position);
                        if (platform.endPos != null) gd.end_pos = new _Vector3(platform.endPos.position);
                    }
                    else
                    {
                        Transform startPivot = child.Find("StartPos");
                        Transform endPivot = child.Find("EndPos");
                        if (startPivot != null && endPivot != null)
                        {
                            gd.start_pos = new _Vector3(startPivot.position);
                            gd.end_pos = new _Vector3(endPivot.position);
                        }
                    }

                    exportedIds.Add(info.gimmick_id);
                    exportData.gimmicks.Add(gd);
                    totalGimmicksFound++;
                }
                else
                {
                    Debug.LogWarning($"[Export] 스킵됨: Tag가 {tag}. (Gimmick 또는 Breakable이어야 함)");
                }
            }
        }

        // 결과 확인
        if (totalGimmicksFound == 0)
        {
            Debug.LogError("[Export] 추출된 기믹이 0개입니다!");
            Debug.LogError("[Export] 확인사항:");
            Debug.LogError("  1. GimmickInfo 컴포넌트가 붙어있는지");
            Debug.LogError("  2. Tag가 'Gimmick' 또는 'Breakable'인지");
            return;
        }

        // JSON 저장
        try
        {
            string jsonOutput = JsonConvert.SerializeObject(exportData, Formatting.Indented);
            string dirPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ServerData");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                Debug.Log($"[Export] 디렉토리 생성: {dirPath}");
            }

            string filePath = Path.Combine(dirPath, $"{exportData.meta.map_name}_{exportData.meta.version}.json");
            File.WriteAllText(filePath, jsonOutput);

            Debug.Log($"<color=green>[추출 완료]</color> {selectedObj.name} 스테이지 내 {grids.Length}개 레벨에서 총 {exportData.gimmicks.Count}개의 기믹 추출");
            Debug.Log($"<color=green>[파일 위치]</color> {filePath}");

            AssetDatabase.Refresh();

            // 파일 열기
            EditorUtility.RevealInFinder(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Export] JSON 저장 실패: {e.Message}");
        }
    }
}