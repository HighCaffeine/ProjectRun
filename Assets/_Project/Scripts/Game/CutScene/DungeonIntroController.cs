using UnityEngine;
using UnityEngine.Playables;

public class DungeonIntroController : MonoBehaviour
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

        Debug.Log("[System] 컷씬 종료 메인 카메라로 전환됩니다.");
        //Match.Instance.SpawnLocalPlayer(0); // 플레이어 스폰
    }

    void OnDestroy()
    {
        if (director != null) director.stopped -= OnCutsceneFinished;
    }
}