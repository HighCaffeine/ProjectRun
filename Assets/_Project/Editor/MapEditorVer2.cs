using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapEditorVer2 : EditorWindow
{
    [Header("기믹 프리팹 설정")]
    private GameObject triggerPrefab;
    private GameObject bridgePrefab;
    private GameObject MovePlatformPrefab;
    private GameObject FallingPlatformPrefab;
    private GameObject seesawPrefab;
    private GameObject movableObjectPrefab;
    private GameObject breakableObjectPrefab;
    private GameObject breakableWallPrefab;
    private GameObject bombObjectPrefab;
    private GameObject monsterSpawnAreaPrefab;

    [Header("배치 환경 설정")]
    private Transform customParent;  // 생성될 기믹 그룹들이 들어갈 맵 내 부모 폴더
    private bool useSnap = true;     // 스냅 사용 여부
    private float snapSize = 1.0f;   // 스냅 간격

    [MenuItem("Tools/GimmickPlacePalette")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorVer2>("GimmickPlacePalette");
    }

    void OnEnable()
    {
        triggerPrefab = LoadPrefab("MapEditor_trigger");
        bridgePrefab = LoadPrefab("MapEditor_bridge");
        MovePlatformPrefab = LoadPrefab("MapEditor_MovePlatform");
        FallingPlatformPrefab = LoadPrefab("MapEditor_FallingPlatform");
        seesawPrefab = LoadPrefab("MapEditor_seesaw");
        movableObjectPrefab = LoadPrefab("MapEditor_movable");
        breakableObjectPrefab = LoadPrefab("MapEditor_breakableObj");
        breakableWallPrefab = LoadPrefab("MapEditor_breakableWall");
        bombObjectPrefab = LoadPrefab("MapEditor_bomb");
        monsterSpawnAreaPrefab = LoadPrefab("MapEditor_monsterSpawnArea");

        useSnap = EditorPrefs.GetBool("MapEditor_useSnap", true);
        snapSize = EditorPrefs.GetFloat("MapEditor_snapSize", 1.0f);

        string parentName = EditorPrefs.GetString("MapEditor_customParent", "");
        if (!string.IsNullOrEmpty(parentName))
        {
            GameObject obj = GameObject.Find(parentName);
            if (obj != null) customParent = obj.transform;
        }
    }

    void OnDisable()
    {
        SavePrefab("MapEditor_trigger", triggerPrefab);
        SavePrefab("MapEditor_bridge", bridgePrefab);
        SavePrefab("MapEditor_MovePlatform", MovePlatformPrefab);
        SavePrefab("MapEditor_FallingPlatform", FallingPlatformPrefab);
        SavePrefab("MapEditor_seesaw", seesawPrefab);
        SavePrefab("MapEditor_movable", movableObjectPrefab);
        SavePrefab("MapEditor_breakableObj", breakableObjectPrefab);
        SavePrefab("MapEditor_breakableWall", breakableWallPrefab);
        SavePrefab("MapEditor_bomb", bombObjectPrefab);
        SavePrefab("MapEditor_monsterSpawnArea", monsterSpawnAreaPrefab);

        EditorPrefs.SetBool("MapEditor_useSnap", useSnap);
        EditorPrefs.SetFloat("MapEditor_snapSize", snapSize);

        if (customParent != null)
        {
            EditorPrefs.SetString("MapEditor_customParent", customParent.name);
        }
        else
        {
            EditorPrefs.DeleteKey("MapEditor_customParent");
        }
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

        bridgePrefab = (GameObject)EditorGUILayout.ObjectField("1. 다리 (Bridge)", bridgePrefab, typeof(GameObject), false);
        MovePlatformPrefab = (GameObject)EditorGUILayout.ObjectField("2. 이동 발판 (Platform)", MovePlatformPrefab, typeof(GameObject), false);
        FallingPlatformPrefab = (GameObject)EditorGUILayout.ObjectField("3. 무너지는 발판 (Falling)", FallingPlatformPrefab, typeof(GameObject), false);
        movableObjectPrefab = (GameObject)EditorGUILayout.ObjectField("4. 밀당 오브젝트 (Movable)", movableObjectPrefab, typeof(GameObject), false);
        breakableObjectPrefab = (GameObject)EditorGUILayout.ObjectField("5. 파괴가능 오브젝트 (Breakable)", breakableObjectPrefab, typeof(GameObject), false);
        breakableWallPrefab = (GameObject)EditorGUILayout.ObjectField("6. 파괴가능 벽 (BreakableWall)", breakableWallPrefab, typeof(GameObject), false);
        bombObjectPrefab = (GameObject)EditorGUILayout.ObjectField("7. 폭탄 (Bomb)", bombObjectPrefab, typeof(GameObject), false);
        monsterSpawnAreaPrefab = (GameObject)EditorGUILayout.ObjectField("8. 몬스터 스폰 구역 (MonsterSpawn)", monsterSpawnAreaPrefab, typeof(GameObject), false);

        //triggerPrefab = (GameObject)EditorGUILayout.ObjectField("8. 스위치/발판 (Trigger)", triggerPrefab, typeof(GameObject), false);
        //seesawPrefab = (GameObject)EditorGUILayout.ObjectField("시소 (Seesaw)", seesawPrefab, typeof(GameObject), false);


        GUILayout.Space(20);
        DrawHorizontalLine();
        GUILayout.Space(10);

        // ----------------------------------------------------
        // 기믹 설치부
        // ----------------------------------------------------
        GUILayout.Label("2. 기믹 세트 원클릭 배치", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("버튼을 누르면 [그룹 폴더 + 기믹 + 스위치] 세트로 생성", MessageType.Info);

        GimmickButtonGUI("01.다리 세트 설치", 30, bridgePrefab, eGimmickKey.Bridge, true);
        GimmickButtonGUI("02.이동 발판 세트 설치", 30, MovePlatformPrefab, eGimmickKey.MovePlatform, true);
        GimmickButtonGUI("03.무너지는 발판 세트 설치", 30, FallingPlatformPrefab, eGimmickKey.FallingPlatform, true);
        //GimmickButtonGUI("04.시소 설치", 30, seesawPrefab, eGimmickKey.SeeSaw, true);
        GimmickButtonGUI("04.밀당 오브젝트 설치", 30, movableObjectPrefab, eGimmickKey.MovableObject, true);
        GimmickButtonGUI("05.부서지는 오브젝트 설치", 30, breakableObjectPrefab, eGimmickKey.BreakableObj, true);
        GimmickButtonGUI("06.부서지는 벽 설치", 30, breakableWallPrefab, eGimmickKey.BreakableWall, true);
        GimmickButtonGUI("07.폭탄 설치", 30, bombObjectPrefab, eGimmickKey.Bomb, true);
        GimmickButtonGUI("08.몬스터 스폰 구역 설치", 30, monsterSpawnAreaPrefab, eGimmickKey.MonsterSpawnArea, false);

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

        GUILayout.Label("4. 몬스터 스폰 트리거 생성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("하이라키에서 선택한 모든 MonsterSpawnArea를 한 번에 트리거에 등록", MessageType.Info);

        if (GUILayout.Button("선택한 MonsterSpawnArea용 트리거 생성", GUILayout.Height(40)))
        {
            GenerateMonsterSpawnTrigger();
        }

        GUI.backgroundColor = Color.white;
    }

    //기믹 생성 및 처리 

    // private int GetUniqueGimmickID()
    // {
    //     BaseGimmick[] allGimmicks = FindObjectsByType<BaseGimmick>(FindObjectsSortMode.None);
    //     HashSet<int> existingIDs = new HashSet<int>();
    //     foreach (var g in allGimmicks) existingIDs.Add(g.gimmickUID);

    //     int newID;
    //     int safetyCount = 0;
    //     do
    //     {
    //         newID = Mathf.Abs(System.Guid.NewGuid().GetHashCode()) % 100000;
    //         if (++safetyCount > 1000) break;
    //     }
    //     while (existingIDs.Contains(newID) || newID == 0);

    //     return newID;
    // }

    private void GimmickButtonGUI(string info, int height, GameObject prefab, eGimmickKey eGimmickKey, bool autoCreateTrigger)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(info, GUILayout.Height(height))) PlaceGimmickSet(prefab, eGimmickKey, autoCreateTrigger);
        GUILayout.EndHorizontal();
    }

    private int GetNextGlobalGimmickUID()
    {
        int currentUID = EditorPrefs.GetInt("GlobalGimmickUID_Counter", 1000);

        EditorPrefs.SetInt("GlobalGimmickUID_Counter", currentUID + 1);

        return currentUID;
    }

    private float ApplySnap(float value)
    {
        return useSnap ? Mathf.Round(value / snapSize) * snapSize : value;
    }

    private eGimmickKey GetGimmickKeyType(BaseGimmick gimmick)
    {
        if (gimmick is Bridge) return eGimmickKey.Bridge;
        if (gimmick is ReMovePlatform) return eGimmickKey.FallingPlatform;
        if (gimmick is Platform) return eGimmickKey.MovePlatform;
        if (gimmick is SeesawTrigger) return eGimmickKey.SeeSaw;
        if (gimmick is MovableGimmick) return eGimmickKey.MovableObject;
        if (gimmick is BreakableObj) return eGimmickKey.BreakableObj;
        if (gimmick is BreakableWall) return eGimmickKey.BreakableWall;
        if (gimmick is Bomb) return eGimmickKey.Bomb;
        if (gimmick is MonsterSpawnArea) return eGimmickKey.MonsterSpawnArea;

        return eGimmickKey.BreakableWall; // 기본값
    }

    private void PlaceGimmickSet(GameObject prefab, eGimmickKey defaultName, bool autoCreateTrigger)
    {
        if (prefab == null)
        {
            //Debug.LogWarning($"[MapEditor] {defaultName} 프리팹이 등록되지 않았습니다.");
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
        int uid = GetNextGlobalGimmickUID();

        // 기믹을 임시 생성하여 타입 확인
        GameObject gimmickObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        BaseGimmick gimmickComp = gimmickObj.GetComponentInChildren<BaseGimmick>();

        string groupName = defaultName.ToString();
        eGimmickKey targetKey = eGimmickKey.BreakableWall;

        if (gimmickComp != null)
        {
            gimmickComp.gimmickUID = uid;
            targetKey = GetGimmickKeyType(gimmickComp);
            groupName = targetKey.ToString();
            EditorUtility.SetDirty(gimmickComp);
        }
        GimmickInfo info = gimmickObj.GetComponentInChildren<GimmickInfo>();
        if (info != null)
        {
            info.gimmick_id = uid;
            info.gimmick_type = targetKey;
            EditorUtility.SetDirty(gimmickObj);
        }
        // 그룹 관리를 위한 빈 오브젝트 생성
        GameObject groupObj = new GameObject($"{groupName}_{uid}");
        groupObj.tag = "Gimmick";
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

            GimmickTrigger triggerComp = triggerObj.GetComponentInChildren<GimmickTrigger>();
            if (triggerComp == null) triggerComp = triggerObj.AddComponent<GimmickTrigger>();

            TargetGimmickInfo targetInfo = new TargetGimmickInfo();
            targetInfo.gimmickID = uid;
            targetInfo.gimmickKey = (eGimmickKey)targetKey;

            triggerComp.targetGimmicks.Clear();
            triggerComp.targetGimmicks.Add(targetInfo);
        }

        if (defaultName == eGimmickKey.MovePlatform || defaultName == eGimmickKey.FallingPlatform)
        {
            var startPivot = CreatePivot("StartPos");
            var endPivot = CreatePivot("EndPos");

            startPivot.transform.SetParent(groupObj.transform);
            endPivot.transform.SetParent(groupObj.transform);

            switch (defaultName)
            {
                case eGimmickKey.MovePlatform:
                    var movableObj = gimmickObj.GetComponentInChildren<Platform>();
                    if (movableObj != null)
                    {
                        movableObj.SetStartPos(startPivot);
                        movableObj.SetEndPos(endPivot);
                    }
                    break;

                case eGimmickKey.FallingPlatform:
                    var fallingObj = gimmickObj.GetComponentInChildren<ReMovePlatform>();
                    if (fallingObj != null)
                    {
                        // fallingObj.SetStartPos(startPivot);
                        // fallingObj.SetEndPos(endPivot);
                    }
                    break;
            }
        }

        // Undo 처리 및 그룹 폴더 포커스
        Undo.RegisterCreatedObjectUndo(groupObj, $"Place {groupName} Set");
        Selection.activeGameObject = groupObj;

        //Debug.Log($"<color=cyan>[MapEditor]</color> {groupName}_{uid} 생성 ");
    }

    private GameObject CreatePivot(string name)
    {
        GameObject obj = new GameObject(name);

        return obj;
    }

    // 기존의 보조 트리거 생성 함수
    private void GenerateTriggerForSelectedGimmick()
    {
        if (Selection.activeGameObject == null) return;

        BaseGimmick targetGimmick = Selection.activeGameObject.GetComponentInChildren<BaseGimmick>();
        if (targetGimmick == null)
        {
            //Debug.LogWarning("[MapEditor] 선택한 오브젝트가 기믹이 아님");
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

        Transform gimmickParent = targetGimmick.transform.parent;
        if (gimmickParent != null && gimmickParent.name.Contains(targetGimmick.gimmickUID.ToString()))
        {
            triggerObj.transform.SetParent(gimmickParent);
        }
        else if (customParent != null)
        {
            triggerObj.transform.SetParent(customParent);
        }

        GimmickTrigger triggerComp = triggerObj.GetComponentInChildren<GimmickTrigger>();
        if (triggerComp == null) triggerComp = triggerObj.AddComponent<GimmickTrigger>();

        TargetGimmickInfo info = new TargetGimmickInfo();
        info.gimmickID = targetGimmick.gimmickUID;

        info.gimmickKey = GetGimmickKeyType(targetGimmick);

        triggerComp.targetGimmicks.Clear();
        triggerComp.targetGimmicks.Add(info);

        Undo.RegisterCreatedObjectUndo(triggerObj, "Create Sub Trigger");
        Selection.activeGameObject = triggerObj;

        //Debug.Log($"<color=green>[MapEditor]</color> 보조 스위치가 생성");
    }

    private void GenerateMonsterSpawnTrigger()
    {
        // 하이라키에서 선택한 모든 오브젝트 가져오기
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            //Debug.LogWarning("[MapEditor] 선택한 오브젝트가 없습니다.");
            return;
        }

        // MonsterSpawnArea만 필터링
        List<MonsterSpawnArea> spawnAreas = new List<MonsterSpawnArea>();

        foreach (GameObject obj in selectedObjects)
        {
            MonsterSpawnArea area = obj.GetComponentInChildren<MonsterSpawnArea>();
            if (area != null)
            {
                spawnAreas.Add(area);
            }
        }

        if (spawnAreas.Count == 0)
        {
            //Debug.LogWarning("[MapEditor] 선택한 오브젝트 중 MonsterSpawnArea가 없습니다.");
            return;
        }

        // 트리거 생성
        GameObject triggerObj;
        Vector3 avgPos = Vector3.zero;

        // 선택한 스폰 구역들의 중심 위치 계산
        foreach (var area in spawnAreas)
        {
            avgPos += area.transform.position;
        }
        avgPos /= spawnAreas.Count;
        avgPos.y = ApplySnap(avgPos.y + 0.1f);
        avgPos.x = ApplySnap(avgPos.x);
        avgPos.z = ApplySnap(avgPos.z - 3f); // 약간 앞쪽에 배치

        if (triggerPrefab != null)
        {
            triggerObj = (GameObject)PrefabUtility.InstantiatePrefab(triggerPrefab);
            triggerObj.transform.position = avgPos;
        }
        else
        {
            triggerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            triggerObj.transform.position = avgPos;
            triggerObj.transform.localScale = new Vector3(2f, 0.1f, 2f);
            triggerObj.GetComponent<Collider>().isTrigger = true;
        }

        triggerObj.name = $"MonsterSpawnTrigger_{spawnAreas.Count}Areas";

        if (customParent != null)
        {
            triggerObj.transform.SetParent(customParent);
        }

        // GimmickTrigger 컴포넌트 추가 및 설정
        GimmickTrigger triggerComp = triggerObj.GetComponentInChildren<GimmickTrigger>();
        if (triggerComp == null) triggerComp = triggerObj.AddComponent<GimmickTrigger>();

        triggerComp.targetGimmicks.Clear();

        // 선택한 모든 MonsterSpawnArea를 targetGimmicks에 추가
        foreach (var area in spawnAreas)
        {
            TargetGimmickInfo info = new TargetGimmickInfo();
            info.gimmickID = area.gimmickUID;
            info.gimmickKey = eGimmickKey.MonsterSpawnArea;

            triggerComp.targetGimmicks.Add(info);
        }

        Undo.RegisterCreatedObjectUndo(triggerObj, "Create Monster Spawn Trigger");
        Selection.activeGameObject = triggerObj;

        //Debug.Log($"<color=green>[MapEditor]</color> 몬스터 스폰 트리거 생성 ({spawnAreas.Count}개 구역 등록)");
    }

    private void DrawHorizontalLine(int height = 1)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, height);
        rect.height = height;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1f));
    }

    private void SavePrefab(string key, GameObject prefab)
    {
        if (prefab != null)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string guid, out long localId))
            {
                EditorPrefs.SetString(key, guid);
            }
        }
        else
        {
            EditorPrefs.DeleteKey(key);
        }
    }

    private GameObject LoadPrefab(string key)
    {
        string guid = EditorPrefs.GetString(key, "");
        if (!string.IsNullOrEmpty(guid))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        return null;
    }
}