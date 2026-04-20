using System.Collections;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
public class UiManager : GenericSingleton<UiManager>
{
    private Coroutine countdownCoroutine;

    float timer=0f;

    [SerializeField]
    private TextMeshProUGUI timeText;
    [SerializeField]
    private TextMeshProUGUI goldText;
    [SerializeField]
    private TextMeshProUGUI player1Text;
    [SerializeField]
    private TextMeshProUGUI player2Text;

    [SerializeField]
    PlayerActor p1;
    [SerializeField]
    PlayerActor p2;


    public GameObject resultUIPanel;

    private new void Awake()
    {
        base.Awake();
        timer = 0f;
    }


    public  void StartCount()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(TimeCount());
    }
    public void StopCount()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            
            countdownCoroutine = null;
        }
    }
    IEnumerator TimeCount()
    {
        while (true)
        {
            if (p1 == null || p2 == null)
            {
                if (ActorManager.Instance != null)
                {
                    if (p1 == null)
                        p1 = ActorManager.Instance.GetPlayer(false);

                    if (p2 == null)
                        p2 = ActorManager.Instance.GetPlayer(true);
                }
            }
            Debug.Log(timer);
            timeText.text =$"탐험 시간 :{timer.ToString()}";

            if (p1 != null)
            {
                player1Text.text = $"{p1.transform.name}\nDIE: {p1.fallDeathCount}\nPull: {p1.pullCount}\nPush: {p1.pushCount}";
            }

            if (p2 != null)
            {
                player2Text.text = $"{p2.transform.name}\nDIE: {p2.fallDeathCount}\nPull: {p2.pullCount}\nPush: {p2.pushCount}";
            }
            yield return new WaitForSeconds(1f);
            timer++;
        }

    }

    public void ShowResult()
    {
        StopCount(); 
        
        ForceUpdateUI(); 

        // 결과창 패널 켜기!
        if(resultUIPanel != null)
            resultUIPanel.SetActive(true); 
    }

    private void ForceUpdateUI()
    {
        timeText.text = $"탐험 시간 : {timer}초";

        if (p1 != null) player1Text.text = $"{p1.transform.name}\nDIE: {p1.fallDeathCount}\nPull: {p1.pullCount}\nPush: {p1.pushCount}";

        if (p2 != null) player2Text.text = $"{p2.transform.name}\nDIE: {p2.fallDeathCount}\nPull: {p2.pullCount}\nPush: {p2.pushCount}";
    }

}
