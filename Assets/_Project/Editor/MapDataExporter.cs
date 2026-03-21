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
        exportData.nav = new NavMeshData();
        if (grid.includeNavMesh)
        {
            NavMeshTriangulation navTriangulation = NavMesh.CalculateTriangulation();


            // 모듈의 영역(Bounds) 설정 (Y축 높이는 무시하고 X, Z 넓이만 검사하기 위해 넉넉하게 잡음)
            Bounds moduleBounds = new Bounds(grid.transform.position, grid.areaSize);
            //moduleBounds.Expand(new Vector3(0, 1000f, 0)); // 위아래 높이는 무제한으로 판정

            // 중복 정점을 방지
            Dictionary<int, int> vertexMap = new Dictionary<int, int>();
            List<Vector3> localVertices = new List<Vector3>();
            List<int> localIndices = new List<int>();

            // 유니티 NavMesh는 삼각형(정점 3개) 단위로 이루어져 있으므로 3개씩 건너뛰며 검사
            for (int i = 0; i < navTriangulation.indices.Length; i += 3)
            {
                int i1 = navTriangulation.indices[i];
                int i2 = navTriangulation.indices[i + 1];
                int i3 = navTriangulation.indices[i + 2];

                Vector3 v1 = navTriangulation.vertices[i1];
                Vector3 v2 = navTriangulation.vertices[i2];
                Vector3 v3 = navTriangulation.vertices[i3];

                // 삼각형의 중심점을 구해서 그 중심이 녹색 박스 안에 있는지 검사
                Vector3 centroid = (v1 + v2 + v3) / 3f;

                if (moduleBounds.Contains(centroid))
                {
                    // 박스 안에 있는 삼각형이라면 새로운 로컬 정점 리스트에 추가
                    if (!vertexMap.ContainsKey(i1)) { vertexMap[i1] = localVertices.Count; localVertices.Add(v1); }
                    if (!vertexMap.ContainsKey(i2)) { vertexMap[i2] = localVertices.Count; localVertices.Add(v2); }
                    if (!vertexMap.ContainsKey(i3)) { vertexMap[i3] = localVertices.Count; localVertices.Add(v3); }

                    localIndices.Add(vertexMap[i1]);
                    localIndices.Add(vertexMap[i2]);
                    localIndices.Add(vertexMap[i3]);
                }
            }

            // 필터링된 최종 데이터를 exportData에 넣기
            foreach (Vector3 v in localVertices)
            {
                exportData.nav.vertices.Add(new _Vector3(v));
            }
            exportData.nav.indices = localIndices;
        }

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
