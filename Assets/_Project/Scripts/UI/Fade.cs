using System.Collections;
using UnityEngine;

public class Fade : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float start = 1f;
    [SerializeField] private float end = 0f;
    CanvasGroup canvasGroup;
    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }
    private void OnEnable()
    {
        StartCoroutine(CrossFade(canvasGroup, fadeDuration, start, end));
    }
    private IEnumerator CrossFade(CanvasGroup fadeObject ,float duration,float start,float end)
    {
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            fadeObject.alpha = Mathf.Lerp(start, end, time / duration);
            yield return null;
        }
        fadeObject.alpha = end;

    }

}
