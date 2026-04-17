using System.Collections;
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
        while (true)
        {
            timeText.text = "timer :" + timer.ToString();
            yield return new WaitForSeconds(1f);
            timer++;
        }

    }



}
