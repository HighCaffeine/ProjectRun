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
    public List<GimmickData> gimmicks = new List<GimmickData>(); // 5개 레벨 기믹 통합
}
#endregion

public class MapDataExporter : Editor
{
    [MenuItem("Tools/SelectedMapModuleExport")]
    public static void ExportSelectedModule()
    {
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null)
        {
            Debug.LogWarning("MapModuleGrid 루트 오브젝트를 선택해주세요.");
            return;
        }

        MapModuleGrid[] grids = selectedObj.GetComponentsInChildren<MapModuleGrid>();
        if (grids.Length == 0)
        {
            Debug.LogWarning("선택한 오브젝트 내에 MapModuleGrid가 없습니다.");
            return;
        }

        MapExportData exportData = new MapExportData();
        exportData.meta = new MapMeta { map_id = 101, map_name = selectedObj.name, version = "1.0.0" };

        int autoGimmickId = 1000;

        // 찾은 모든 레벨을 순회하며 기믹을 하나로 합침
        foreach (MapModuleGrid grid in grids)
        {
            GimmickInfo[] allGimmicks = grid.GetComponentsInChildren<GimmickInfo>(true);

            foreach (GimmickInfo info in allGimmicks)
            {
                Transform child = info.transform;

                if (child.CompareTag("Gimmick") || child.CompareTag("Breakable"))
                {
                    GimmickData gd = new GimmickData();
                    gd.position = new _Vector3(child.position); 
                    gd.rotation_y = (float)Math.Round(child.eulerAngles.y, 3);

                    gd.gimmick_id = info.gimmick_id;
                    gd.type = info.gimmick_type;

                        foreach (var prop in info.properties)
                        {
                            string keyStr = prop.key.ToString();
                            if (!gd.properties.ContainsKey(keyStr)) 
                                gd.properties.Add(keyStr, prop.value);
                        }

                    Transform startPivot = child.Find("StartPos");
                    Transform endPivot = child.Find("EndPos");
                    if (startPivot != null && endPivot != null)
                    {
                        gd.start_pos = new _Vector3(startPivot.position);
                        gd.end_pos = new _Vector3(endPivot.position);
                    }

                    exportData.gimmicks.Add(gd);
                }
            }
        }

        // JSON 저장
        string jsonOutput = JsonConvert.SerializeObject(exportData, Formatting.Indented);
        string dirPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ServerData");
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        string filePath = Path.Combine(dirPath, $"{exportData.meta.map_name}_{exportData.meta.version}.json");
        File.WriteAllText(filePath, jsonOutput);

        Debug.Log($"[추출 완료] {selectedObj.name} 스테이지 내 {grids.Length}개 레벨에서 총 {exportData.gimmicks.Count}개의 기믹 추출");
        AssetDatabase.Refresh();
    }
}