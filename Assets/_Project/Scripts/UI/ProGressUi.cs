using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.UI;
public class ProGressUi : MonoBehaviour
{
    [SerializeField] private RawImage[] stageImages;
    [SerializeField] private Texture passed;
    [SerializeField] private Texture now;

    private void Start()
    {
        StageUpdate(DungeonPointManager.Instance.currentMapID );
        DungeonUiManager.Instance.ShowProgress();
    }


    public void StageUpdate(int mapID)
    {
        foreach (RawImage image in stageImages)
        {
            image.gameObject.SetActive(false);
        }

        for (int i = 0; i < mapID; i++)
        {
            stageImages[i].gameObject.SetActive(true);
            stageImages[i].texture = passed;
        }

        stageImages[mapID].gameObject.SetActive(true);
        stageImages[mapID].texture = now;
    }
}
