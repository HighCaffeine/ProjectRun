using UnityEngine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.AI;

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
public class NavMeshData
{
    public string description;
    public List<_Vector3> vertices = new List<_Vector3>();
    public List<int> indices = new List<int>();
}


[Serializable]
public class GimmickData
{
    public int gimmick_id;
    public string type;
    public _Vector3 position;
    public float rotation_y;
    public Dictionary<GimmickKey, float> properties = new Dictionary<GimmickKey, float>();
}

[Serializable]
public class MapExportData
{
    public MapMeta meta;
    public NavMeshData nav;
    public List<GimmickData> gimmicks = new List<GimmickData>();
}
#endregion

public class MapDataExporter : Editor
{
    [MenuItem("Tools/SelectedMapModuleExport")]
    public static void ExportSelectedModule()
    {
        // 씬에서 선택한 모듈 영역 오브젝트를 가져옴
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null || selectedObj.GetComponent<MapModuleGrid>() == null)
        {
            return;
        }

        MapModuleGrid grid = selectedObj.GetComponent<MapModuleGrid>();
        MapExportData exportData = new MapExportData();
        exportData.meta = new MapMeta { map_id = 101, map_name = selectedObj.name, version = "1.0.0" };

        // NavMesh 데이터
        NavMeshTriangulation navTriangulation = NavMesh.CalculateTriangulation();
        exportData.nav = new NavMeshData();
        foreach (Vector3 v in navTriangulation.vertices)
        {
            exportData.nav.vertices.Add(new _Vector3(v));
        }

        exportData.nav.indices = new List<int>(navTriangulation.indices);

        // 기믹 데이터 추출
        int autoGimmickId = 1000;

        foreach (Transform child in grid.transform)
        {
            //기믹 태그들 다 가져옴
            if (child.CompareTag("Gimmick"))
            {
                GimmickData gimmickData = new GimmickData();
                gimmickData.position = new _Vector3(child.position);
                gimmickData.rotation_y = (float)Math.Round(child.eulerAngles.y, 3);

                GimmickInfo info = child.GetComponent<GimmickInfo>();

                if (info != null)
                {
                    gimmickData.gimmick_id = info.gimmick_id != 0 ? info.gimmick_id : ++autoGimmickId;
                    gimmickData.type = info.gimmick_type;
                    foreach (var prop in info.properties)
                    {
                        if (!gimmickData.properties.ContainsKey(prop.key)) gimmickData.properties.Add(prop.key, prop.value);
                    }
                }

                exportData.gimmicks.Add(gimmickData);
            }
        }

        // JSON 저장
        string jsonOutput = JsonConvert.SerializeObject(exportData, Formatting.Indented);
        string dirPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "ServerData");
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        string filePath = Path.Combine(dirPath, $"{exportData.meta.map_name}_{exportData.meta.version}.json");
        File.WriteAllText(filePath, jsonOutput);

        Debug.Log($"[모듈 추출 완료] {selectedObj.name} 영역 내 기믹 {exportData.gimmicks.Count}개 추출");
        AssetDatabase.Refresh();
    }
}
