using System.Collections;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.DebugUI;
public class UiManager : MonoBehaviour
{
    public static UiManager instance;  

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


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            instance.gameObject.SetActive(false);

        }
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
        Debug.Log("Ÿ�̸� ����");
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



}
