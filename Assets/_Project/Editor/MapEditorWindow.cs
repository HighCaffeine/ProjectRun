using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class MapEditorWindow : EditorWindow
{
    private string prefabsPath = "Assets/_Project/Prefabs/MapEditor"; // 맵 오브젝트들 위치

    private List<GameObject> loadedPrefabs = new List<GameObject>();
    private Vector2 scrollPosition;
    private int selectedPaletteIndex = -1; // 현재 선택한 프리팹 인덱스

    private const float buttonSize = 80f;

    //창 띄우기
    [MenuItem("Tools/MapEditorPalette")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorWindow>("MapEditorTool");
    }

    private void OnEnable()
    {
        LoadPrefabsFromPath();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    // 에디터 끄면 이벤트 초기화
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    // 창 포커스 시 프리팹 리스트 갱신
    private void OnFocus() { LoadPrefabsFromPath(); }
    // 씬 키입력 체크
    private void OnSceneGUI(SceneView sceneView)
    {
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.LeftAlt)
        {
            RotateSelectedObjectY(90f);
            currentEvent.Use();
        }
    }

    private void OnGUI()
    {
        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.LeftAlt)
        {
            RotateSelectedObjectY(90f);
            currentEvent.Use();
        }

        // 경로 설정, 새로고침 버튼
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        prefabsPath = EditorGUILayout.TextField("Prefab Folder", prefabsPath);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
        {
            LoadPrefabsFromPath();
        }
        EditorGUILayout.EndHorizontal();

        // 현재 선택한 모듈 확인
        GameObject activeGridObj = FindActiveGridModule();
        if (activeGridObj != null)
        {
            EditorGUILayout.HelpBox($"[{activeGridObj.name}]", MessageType.Info);
        }

        EditorGUILayout.Space(5f);

        // 프리팹 리스트 그리기
        EditorGUILayout.LabelField("Prefab List", EditorStyles.boldLabel);

        // 스크롤 뷰
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 한 줄에 있는 갯수 계산
        int columns = Mathf.FloorToInt(position.width / (buttonSize + 10f));
        if (columns < 1) columns = 1;

        int rowCount = 0;
        EditorGUILayout.BeginHorizontal(); // 첫 번째 줄 시작

        for (int i = 0; i < loadedPrefabs.Count; i++)
        {
            GameObject prefab = loadedPrefabs[i];
            if (prefab == null) continue;

            // 프리팹 버튼
            EditorGUILayout.BeginVertical(GUILayout.Width(buttonSize), GUILayout.Height(buttonSize + 20f));

            // 프리팹 미리보기
            Texture2D previewTexture = AssetPreview.GetAssetPreview(prefab);
            GUIContent btnContent = new GUIContent(previewTexture, prefab.name); // 호버 시 이름 띄움

            // 버튼 색상 변경
            Color originalBgColor = GUI.backgroundColor;
            if (selectedPaletteIndex == i) GUI.backgroundColor = Color.cyan;

            // 버튼 클릭 설치 로직 호출
            if (GUILayout.Button(btnContent, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
            {
                selectedPaletteIndex = i;
                SpawnPrefabInActiveGrid(prefab);
            }
            GUI.backgroundColor = originalBgColor; // 색 복구

            // 프리팹 이름 라벨
            GUIStyle centeredMiniLabel = new GUIStyle(EditorStyles.miniLabel);
            centeredMiniLabel.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField(prefab.name, centeredMiniLabel, GUILayout.Width(buttonSize));
            EditorGUILayout.EndVertical();

            rowCount++;
            if (rowCount >= columns)
            {
                rowCount = 0;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }

        EditorGUILayout.EndHorizontal(); // 마지막 줄 끝
        EditorGUILayout.EndScrollView(); // 스크롤 뷰 끝
    }

    private void RotateSelectedObjectY(float rotY)
    {
        GameObject selectedObj = Selection.activeGameObject;
        if (selectedObj == null || selectedObj.GetComponentInParent<MapModuleGrid>() == null) return;

        Undo.RecordObject(selectedObj.transform, $"Set Y Rotation {selectedObj.name}");
        Vector3 currentEuler = selectedObj.transform.localEulerAngles;
        currentEuler.y = Mathf.Repeat(currentEuler.y + rotY, 360.0f);
        selectedObj.transform.localEulerAngles = currentEuler;
        Repaint();
    }

    // 경로에서 프리팹 로드
    private void LoadPrefabsFromPath()
    {
        loadedPrefabs.Clear();
        if (!Directory.Exists(prefabsPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { prefabsPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (asset != null)
            {
                // 프리팹이거나 fbx인 경우만 리스트에 추가
                PrefabAssetType pType = PrefabUtility.GetPrefabAssetType(asset);
                if (pType == PrefabAssetType.Regular || pType == PrefabAssetType.Model || pType == PrefabAssetType.Variant)
                {
                    loadedPrefabs.Add(asset);
                }
            }
        }
        Repaint();
    }

    // MapModuleGrid 컴포넌트 찾기
    private GameObject FindActiveGridModule()
    {
        MapModuleGrid gridComponent = FindObjectOfType<MapModuleGrid>();
        return gridComponent != null ? gridComponent.gameObject : null;
    }

    // 프리팹 스폰 및 부모 설정
    private void SpawnPrefabInActiveGrid(GameObject prefabToSpawn)
    {
        GameObject targetGrid = FindActiveGridModule();
        if (targetGrid == null)
        {
            EditorUtility.DisplayDialog("warning", "No MapGridModule", "ok");
            return;
        }

        GameObject instantiatedObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
        if (instantiatedObj == null) return;

        instantiatedObj.transform.SetParent(targetGrid.transform);
        instantiatedObj.transform.localPosition = Vector3.zero;

        Selection.activeGameObject = instantiatedObj;
        Undo.RegisterCreatedObjectUndo(instantiatedObj, $"Spawn {prefabToSpawn.name}");

        Debug.Log($"[MapEditor] [{prefabToSpawn.name}] place to [{targetGrid.name}]");
    }
}