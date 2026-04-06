using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.DebugUI;
public class UiManager : MonoBehaviour
{
    [SerializeField] private GameObject countPanel;
    [SerializeField] private TextMeshProUGUI countText;
    private Coroutine countdownCoroutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      
    }

    // Update is called once per frame
    void Update()
    {

    }



    public  void StartCount()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(TimeCount(5f));
    }
    public void StopCount()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        countPanel.gameObject.SetActive(false);
    }
    IEnumerator TimeCount(float time)
    {
        countPanel.gameObject.SetActive(true);
        while (time > 0)
        {
            countText.text = time.ToString();
            yield return new WaitForSeconds(1f);
            time--;
        }
        countText.text = "START!";
        yield return new WaitForSeconds(1f);
        countPanel.gameObject.SetActive(false);
    }



}
