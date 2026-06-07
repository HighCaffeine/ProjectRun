using UnityEngine;
using UnityEngine.Playables;

public class DungeonIntroController : GenericSingleton<DungeonIntroController>
{
    public PlayableDirector director;

    [Header("컷씬 전용 가상 카메라들")]
    public GameObject[] cutsceneCameras;

    void Start()
    {
        
        director.stopped += OnCutsceneFinished;

        foreach (var cam in cutsceneCameras)
        {
            if (cam != null) cam.SetActive(true);
        }

        if (director != null && director.state != PlayState.Playing)
        {
            director.Play();
        }
    }

    private void OnCutsceneFinished(PlayableDirector pd)
    {
        foreach (var cam in cutsceneCameras)
        {
            if (cam != null) cam.SetActive(false);
        }

        Match.Instance.SpawnLocalPlayer(0); // 플레이어 스폰
        DungeonPointManager.Instance.SetCurrentMap(DungeonPointManager.Instance.currentMapID); // UI 업데이트
        Debug.Log("[DungeonIntroController] 컷씬 종료, 플레이어 스폰 및 UI 업데이트");
        
    }

    void OnDestroy()
    {
        if (director != null) director.stopped -= OnCutsceneFinished;
    }
}