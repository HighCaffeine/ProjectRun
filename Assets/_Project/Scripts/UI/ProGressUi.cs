using System.Collections;
using UnityEngine;
using UnityEngine.AdaptivePerformance.Provider;
using UnityEngine.UI;
public class ProGressUi : GenericSingleton<ProGressUi>
{
    [SerializeField] private RawImage[] stageImages;
    [SerializeField] private Texture passed;
    [SerializeField] private Texture now;

    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float start = 0f;
    [SerializeField] private float end = 1f;


    private RawImage targetImage;

    public bool instage = false;


    private void OnEnable()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        StartCoroutine(CrossFade(canvasGroup, fadeDuration, start, end));
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

        targetImage = stageImages[mapID];

        Debug.Log($"[ProGressUi] StageUpdate: 현재 MapID = {mapID}, UI 업데이트 완료"); 
    }
    private IEnumerator CrossFade(CanvasGroup fadeObject, float duration, float start, float end)
    {
        float time = 0;

        fadeObject.alpha = start;

        if (instage)
        { 
            targetImage.color = new Color(255, 255, 255, 255);
        }
        else
        {
            targetImage.color = new Color(255, 255, 255, 0);
        }

            while (time < duration)
            {
                time += Time.deltaTime;
                fadeObject.alpha = Mathf.Lerp(start, end, time / duration);
                yield return null;
            }
        fadeObject.alpha = end;

        if (targetImage != null)
        {
            if (!instage)
            {
                time = 0f;

                while (time < duration)
                {
                    time += Time.deltaTime;
                    Color color = targetImage.color;
                    color.a = Mathf.Lerp(start, end, time / duration);  
                    targetImage.color = color;
                    yield return null;
                }
            }
            
        }
        

        time = duration;

        yield return new WaitForSeconds(1f);

        while (time > 0)
        {
            time -= Time.deltaTime;
            fadeObject.alpha = Mathf.Lerp(end, start, 1 - (time / duration));
            yield return null;
        }

        instage= false;

        gameObject.SetActive(false);
    }

}
