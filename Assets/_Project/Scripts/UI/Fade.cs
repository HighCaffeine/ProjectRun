using System.Collections;
using UnityEngine;

public class Fade : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float start = 1f;
    [SerializeField] private float end = 0f;

    private void Start()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        StartCoroutine(CrossFade(canvasGroup, fadeDuration, start, end));


    }
    private IEnumerator CrossFade(CanvasGroup fadeObject ,float duration,float start,float end)
    {
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;

            yield return null;
        }

    }

}
