using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapEditorVer2 : EditorWindow
{
    [Header("기믹 프리팹 설정")]
    private GameObject triggerPrefab;
    private GameObject bridgePrefab;
    private GameObject movePlatePrefab;
    private GameObject disappearPlatePrefab;
    private GameObject seesawPrefab;
    private GameObject movableObjectPrefab;

    [Header("배치 환경 설정")]
    private Transform customParent;  // 생성될 기믹 그룹들이 들어갈 맵 내 부모 폴더
    private bool useSnap = true;     // 스냅 사용 여부
    private float snapSize = 1.0f;   // 스냅 간격

    [MenuItem("Tools/GimmickPlacePalette")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorVer2>("기믹 배치 팔레트");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        // ----------------------------------------------------
        // 배치 환경 설정
        // ----------------------------------------------------
        GUILayout.Label("0. 배치 환경 설정", EditorStyles.boldLabel);
        customParent = (Transform)EditorGUILayout.ObjectField("맵 에디터 루트", customParent, typeof(Transform), true);

        GUILayout.BeginHorizontal();
        useSnap = EditorGUILayout.Toggle("그리드 스냅 사용", useSnap);
        if (useSnap)
        {
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            snapSize = EditorGUILayout.FloatField("스냅 크기", snapSize);
            if (snapSize <= 0.1f) snapSize = 0.1f;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        DrawHorizontalLine();
        GUILayout.Space(10);

        // ----------------------------------------------------
        // 프리팹 등록부
        // ----------------------------------------------------
        GUILayout.Label("1. 기믹 프리팹 등록", EditorStyles.boldLabel);

        triggerPrefab = (GameObject)EditorGUILayout.ObjectField("스위치/발판 (Trigger)", triggerPrefab, typeof(GameObject), false);
        bridgePrefab = (GameObject)EditorGUILayout.ObjectField("다리 (Bridge)", bridgePrefab, typeof(GameObject), false);
        movePlatePrefab = (GameObject)EditorGUILayout.ObjectField("이동 발판 (Platform)", movePlatePrefab, typeof(GameObject), false);
        disappearPlatePrefab = (GameObject)EditorGUILayout.ObjectField("무너지는 발판 (ReMove)", disappearPlatePrefab, typeof(GameObject), false);
        seesawPrefab = (GameObject)EditorGUILayout.ObjectField("시소 (Seesaw)", seesawPrefab, typeof(GameObject), false);
        movableObjectPrefab = (GameObject)EditorGUILayout.ObjectField("밀당 오브젝트 (Movable)", movableObjectPrefab, typeof(GameObject), false);

        GUILayout.Space(20);
        DrawHorizontalLine();
        GUILayout.Space(10);

        // ----------------------------------------------------
        // 기믹 설치부
        // ----------------------------------------------------
        GUILayout.Label("2. 기믹 세트 원클릭 배치", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("버튼을 누르면 [그룹 폴더 + 기믹 + 스위치] 세트로 생성", MessageType.Info);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("다리 세트 설치", GUILayout.Height(30))) PlaceGimmickSet(bridgePrefab, "Bridge", true);
        if (GUILayout.Button("이동 발판 세트 설치", GUILayout.Height(30))) PlaceGimmickSet(movePlatePrefab, "MovePlatform", true);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("무너지는 발판 세트 설치", GUILayout.Height(30))) PlaceGimmickSet(disappearPlatePrefab, "ReMovePlatform", true);

        //시소와 밀당 오브젝트는 트리거가 필요 없기 떄문에 false
        if (GUILayout.Button("시소 설치", GUILayout.Height(30))) PlaceGimmickSet(seesawPrefab, "Seesaw", false);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("밀당 오브젝트 설치", GUILayout.Height(30))) PlaceGimmickSet(movableObjectPrefab, "MovableObject", false);

        GUILayout.Space(20);
        DrawHorizontalLine();
        GUILayout.Space(10);

        // ----------------------------------------------------
        // 보조 도구 (트리거 추가 생성)
        // ----------------------------------------------------
        GUILayout.Label("3. 트리거 추가 생성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("선택한 기믹에 추가 트리거 부착 시 사용", MessageType.Info);

        if (GUILayout.Button("트리거 추가 생성", GUILayout.Height(40)))
        {
            GenerateTriggerForSelectedGimmick();
        }
        GUI.backgroundColor = Color.white;
    }

    // ----------------------------------------------------
    // 핵심 로직 구현부
    // ----------------------------------------------------

    private int GetUniqueGimmickID()
    {
        BaseGimmick[] allGimmicks = FindObjectsByType<BaseGimmick>(FindObjectsSortMode.None);
        HashSet<int> existingIDs = new HashSet<int>();
        foreach (var g in allGimmicks) existingIDs.Add(g.gimmickUID);

        int newID;
        int safetyCount = 0;
        do
        {
            newID = Mathf.Abs(System.Guid.NewGuid().GetHashCode()) % 100000;
            if (++safetyCount > 1000) break;
        }
        while (existingIDs.Contains(newID) || newID == 0);

        return newID;
    }

    private float ApplySnap(float value)
    {
        return useSnap ? Mathf.Round(value / snapSize) * snapSize : value;
    }

    private eGimmickKey GetGimmickKeyType(BaseGimmick gimmick)
    {
        if (gimmick is Bridge) return eGimmickKey.Bridge;
        if (gimmick is ReMovePlatform) return eGimmickKey.DisappearPlate;
        if (gimmick is Platform) return eGimmickKey.MovePlate;
        if (gimmick is SeesawTrigger) return eGimmickKey.SeeSaw;
        if (gimmick is MovableGimmick) return eGimmickKey.MovableObject;
        return eGimmickKey.BreakableWall; // 기본값
    }

    private void PlaceGimmickSet(GameObject prefab, string defaultName, bool autoCreateTrigger)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[MapEditor] {defaultName} 프리팹이 등록되지 않았습니다.");
            return;
        }

        // 생성 위치 계산 (스냅 적용)
        SceneView sceneView = SceneView.lastActiveSceneView;
        Vector3 spawnPos = (sceneView != null)
            ? sceneView.camera.transform.position + sceneView.camera.transform.forward * 10f
            : Vector3.zero;

        spawnPos.x = ApplySnap(spawnPos.x);
        spawnPos.y = ApplySnap(spawnPos.y);
        spawnPos.z = ApplySnap(spawnPos.z);

        // 고유 UID 발급
        int uid = GetUniqueGimmickID();

        // 기믹을 임시 생성하여 타입 확인
        GameObject gimmickObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        BaseGimmick gimmickComp = gimmickObj.GetComponent<BaseGimmick>();

        string groupName = defaultName;
        eGimmickKey targetKey = eGimmickKey.BreakableWall;

        if (gimmickComp != null)
        {
            gimmickComp.gimmickUID = uid;
            targetKey = GetGimmickKeyType(gimmickComp);
            groupName = targetKey.ToString();
            EditorUtility.SetDirty(gimmickComp);
        }

        // 그룹 관리를 위한 빈 오브젝트 생성
        GameObject groupObj = new GameObject($"{groupName}_{uid}");
        groupObj.transform.position = spawnPos;
        if (customParent != null) groupObj.transform.SetParent(customParent);

        // 기믹 오브젝트를 그룹 안으로 이동
        gimmickObj.transform.SetParent(groupObj.transform);
        gimmickObj.transform.localPosition = Vector3.zero;

        // 트리거 자동 생성 로직
        if (autoCreateTrigger)
        {
            GameObject triggerObj;
            if (triggerPrefab != null)
            {
                triggerObj = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab);
            }
            else
            {
                triggerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                triggerObj.transform.localScale = new Vector3(1f, 0.1f, 1f);
                triggerObj.GetComponent<Collider>().isTrigger = true;
            }

            // 트리거도 같은 그룹 안으로 넣고, 기믹 앞쪽으로 살짝 빼줌
            triggerObj.transform.SetParent(groupObj.transform);
            triggerObj.transform.localPosition = new Vector3(0f, 0.1f, -3f);
            triggerObj.name = $"Trigger_{uid}";

            GimmickTrigger triggerComp = triggerObj.GetComponent<GimmickTrigger>();
            if (triggerComp == null) triggerComp = triggerObj.AddComponent<GimmickTrigger>();

            triggerComp.targetGimmickID = uid;
            triggerComp.targetGimmickKey = targetKey;
        }

        // Undo 처리 및 그룹 폴더 포커스
        Undo.RegisterCreatedObjectUndo(groupObj, $"Place {groupName} Set");
        Selection.activeGameObject = groupObj;

        Debug.Log($"<color=cyan>[MapEditor]</color> {groupName}_{uid} 생성 ");
    }

    // 기존의 보조 트리거 생성 함수
    private void GenerateTriggerForSelectedGimmick()
    {
        if (Selection.activeGameObject == null) return;

        BaseGimmick targetGimmick = Selection.activeGameObject.GetComponent<BaseGimmick>();
        if (targetGimmick == null)
        {
            Debug.LogWarning("[MapEditor] 선택한 오브젝트가 기믹이 아님");
            return;
        }

        GameObject triggerObj;
        Vector3 targetPos = targetGimmick.transform.position;

        if (triggerPrefab != null)
        {
            triggerObj = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab);
            Vector3 spawnPos = targetPos + new Vector3(0f, 0f, -2f);
            spawnPos.x = ApplySnap(spawnPos.x);
            spawnPos.z = ApplySnap(spawnPos.z);
            triggerObj.transform.position = spawnPos;
        }
        else
        {
            triggerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Vector3 spawnPos = targetPos + new Vector3(0f, 0.1f, -2f);
            spawnPos.x = ApplySnap(spawnPos.x);
            spawnPos.z = ApplySnap(spawnPos.z);
            triggerObj.transform.position = spawnPos;

            triggerObj.transform.localScale = new Vector3(1f, 0.1f, 1f);
            triggerObj.GetComponent<Collider>().isTrigger = true;
        }

        triggerObj.name = $"Trigger_{targetGimmick.gimmickUID}_Sub";

        // 선택한 기믹이 이미 그룹 폴더 안에 있다면 그곳에 종속, 아니면 customParent에 종속
        Transform gimmickParent = targetGimmick.transform.parent;
        if (gimmickParent != null && gimmickParent.name.Contains(targetGimmick.gimmickUID.ToString()))
        {
            triggerObj.transform.SetParent(gimmickParent);
        }
        else if (customParent != null)
        {
            triggerObj.transform.SetParent(customParent);
        }

        GimmickTrigger triggerComp = triggerObj.GetComponent<GimmickTrigger>();
        if (triggerComp == null) triggerComp = triggerObj.AddComponent<GimmickTrigger>();

        triggerComp.targetGimmickID = targetGimmick.gimmickUID;
        triggerComp.targetGimmickKey = GetGimmickKeyType(targetGimmick);

        Undo.RegisterCreatedObjectUndo(triggerObj, "Create Sub Trigger");
        Selection.activeGameObject = triggerObj;

        Debug.Log($"<color=green>[MapEditor]</color> 보조 스위치가 생성");
    }

    private void DrawHorizontalLine(int height = 1)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, height);
        rect.height = height;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1f));
    }
}