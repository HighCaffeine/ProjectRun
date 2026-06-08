using UnityEngine;
using UnityEngine.Playables;
using System;

public enum ECutsceneType
{
    None,           
    VillageEnter,   // 마을 입장
    DungeonEnter,   // 던전 입장
    DungeonEscape   // 던전 탈출
}

[Serializable]
public struct CutsceneData
{
    public ECutsceneType cutsceneType;
    public PlayableDirector director;
    public GameObject[] dummys; 

    public bool spawnAtEnd; 
    
    public int spawnSectorIndex;
}

public class SceneCutsceneController : MonoBehaviour
{
    public static SceneCutsceneController Instance;
    public static ECutsceneType NextReservedCutscene = ECutsceneType.None;

    [Header("기본 컷씬")]
    public ECutsceneType defaultCutscene = ECutsceneType.None;

    [Tooltip("재생할 컷씬이 없을 때 씬 시작 시 플레이어 스폰할지 여부")]
    public bool autoSpawnIfNoCutscene = true;
    public int defaultSpawnSector = 0;

    [Header("현재 씬에 존재하는 컷씬 데이터들")]
    public CutsceneData[] sceneCutscenes;

    public CutsceneData _currentCutscene;
    private bool _isPlayingCutscene = false;

    public bool IsPlayingCutscene => _isPlayingCutscene;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ECutsceneType cutsceneToPlay = (NextReservedCutscene != ECutsceneType.None) ? NextReservedCutscene : defaultCutscene;
        NextReservedCutscene = ECutsceneType.None;

        if (cutsceneToPlay != ECutsceneType.None)
        {
            PlayCutscene(cutsceneToPlay);
        }
        else
        {
            if (autoSpawnIfNoCutscene && Match.Instance != null)
            {
                Match.Instance.SpawnLocalPlayer(defaultSpawnSector); 
            }
        }
    }

    public void PlayCutscene(ECutsceneType type)
    {
        bool found = false;
        foreach (var data in sceneCutscenes)
        {
            if (data.cutsceneType == type)
            {
                _currentCutscene = data;
                found = true;
                break;
            }
        }

        if (!found)
        {
            if (autoSpawnIfNoCutscene && Match.Instance != null) 
            {
                Match.Instance.SpawnLocalPlayer(defaultSpawnSector);
            }
            return;
        }

        _isPlayingCutscene = true;

        // foreach (var dummy in _currentCutscene.dummys)
        // {
        //     if (dummy != null) dummy.SetActive(true);
        // }

        if (_currentCutscene.director != null)
        {
            _currentCutscene.director.stopped -= OnCutsceneFinished; 
            _currentCutscene.director.stopped += OnCutsceneFinished;
            _currentCutscene.director.Play();
        }
        else
        {
            OnCutsceneFinished(null);
        }
    }

    private void OnCutsceneFinished(PlayableDirector pd)
    {
        if (!_isPlayingCutscene) return;
        _isPlayingCutscene = false;

        if (_currentCutscene.director != null)
        {
            _currentCutscene.director.stopped -= OnCutsceneFinished;
        }

        foreach (var dummy in _currentCutscene.dummys)
        {
            if (dummy != null) dummy.SetActive(false);
        }

        if (_currentCutscene.cutsceneType == ECutsceneType.DungeonEscape)
        {
            EscapeZone escapeZone = FindFirstObjectByType<EscapeZone>();
            if (escapeZone != null)
            {
                escapeZone.OnActiveResult();
            }
            else
            {
                //Debug.LogWarning("[Cutscene] EscapeZone을 찾을 수 없습니다.");
            }
            return;
        }

        if (_currentCutscene.cutsceneType == ECutsceneType.DungeonEnter)
        {
            DungeonPointManager.Instance.SetCurrentMap(DungeonPointManager.Instance.currentMapID); 
        }

        if (_currentCutscene.spawnAtEnd && Match.Instance != null)
        {
            SpawnLocalPlayerOnMatch();
            //Debug.Log($"[Cutscene] 종료. 플레이어 스폰: {_currentCutscene.spawnSectorIndex}");
        }
        else
        {
            //Debug.Log($"[Cutscene] {_currentCutscene.cutsceneType} 종료. (자동 스폰 안 함)");
        }
    }

    public void SpawnLocalPlayerOnMatch()
    {
        Match.Instance.SpawnLocalPlayer(_currentCutscene.spawnSectorIndex);
    }

    private void OnDestroy()
    {
        foreach (var data in sceneCutscenes)
        {
            if (data.director != null)
            {
                data.director.stopped -= OnCutsceneFinished;
            }
        }
    }
}