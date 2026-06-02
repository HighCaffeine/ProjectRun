using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AniEvent : MonoBehaviour
{
    [SerializeField] private MoneyEffect moneyEffect;
    public int gold;

    [SerializeField] private Animator stamp;

    [SerializeField] private DialogueManager dialogueManager;

    [SerializeField] private GameObject diaryDimmed;
    [SerializeField] private Animator diaryBG;
    [SerializeField] private Animator diary;
    private void Start()
    {
        StampOff();
        DiaryOff();
        DiaryDimmedOff();
       /* if(diaryDimmed != null)
        {
            Color color = diaryDimmed.color;

            color.a = 0f;

            diaryDimmed.color = color;
        }*/
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayDiaryDimmed();     
        }

    }

    public void GoleEnvet()
    {
        StartCoroutine(moneyEffect.PlayRoutine(gold));
    }

    public void StampOff()
    {
        if (stamp == null) 
            return;
        stamp.gameObject.SetActive(false);
    }
    private void DiaryDimmedOff()
    {
        if (diaryDimmed == null)
            return;

        diaryDimmed.gameObject.SetActive(false);
    }
    private void DiaryOff()
    {
        if (diary == null)
            return;

        diary.gameObject.SetActive(false);
    }
    public void StampPlay()
    {
        if (stamp == null)
            return;
        stamp.gameObject.SetActive(true);
        stamp.Play("Stamp");
    }

    public void StratDialogue()
    {
        if (dialogueManager == null)
            return;
        dialogueManager.StartDialogue("ResultText");
    }

    public void StartDiaryBGAni()
    {
        if (diaryBG == null)
            return;
        
        diaryBG.Play("Spread");
    }
    public void PlayDiaryDimmed()
    {
        if(diaryDimmed == null) 
            return ;


        diaryDimmed.gameObject.SetActive(true);
    }
   /* private IEnumerator DiaryDimmedRoutine()
    {
        if (diaryDimmed == null)
            yield break;

        Color color = diaryDimmed.color;

        float startAlpha = 0f;

        float targetAlpha = 150f / 255f;

        float duration = 0.3f;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;

            t = Mathf.SmoothStep(0f, 1f, t);

            color.a =Mathf.Lerp(startAlpha, targetAlpha, t);

            diaryDimmed.color = color;

            yield return null;
        }

        color.a = targetAlpha;

        diaryDimmed.color = color;

        yield return null;

        diary.gameObject.SetActive(true);
    }*/
   /* public void DiaryUp()
    {
        if (diary == null)
            return;
        diary.Play("Up");
    }*/



}
