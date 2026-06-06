using System.Collections;
using TMPro;
using UnityEngine;

public class DungeonUiManager : GenericSingleton<DungeonUiManager>
{
    private Coroutine countdownCoroutine;

    private long startTime;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Player 1")]
    [SerializeField] private TextMeshProUGUI player1DestroyText;
    [SerializeField] private TextMeshProUGUI player1PushText;
    [SerializeField] private TextMeshProUGUI player1PullText;
    [SerializeField] private TextMeshProUGUI player1FallText;
    [SerializeField] private TextMeshProUGUI player1FallKillText;

    [Header("Player 2")]
    [SerializeField] private TextMeshProUGUI player2DestroyText;
    [SerializeField] private TextMeshProUGUI player2PushText;
    [SerializeField] private TextMeshProUGUI player2PullText;
    [SerializeField] private TextMeshProUGUI player2FallText;
    [SerializeField] private TextMeshProUGUI player2FallKillText;

    [Header("Result UI")]
    [SerializeField] private GameObject resultUIPanel;

    private PlayerActor p1;
    private PlayerActor p2;

    private new void Awake()
    {
        base.Awake();
        resultUIPanel.SetActive(false);
    }

    public void StartCount()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        startTime = NetworkTimeManager.Instance.GetServerTime();

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

    private IEnumerator TimeCount()
    {
        while (true)
        {
            FindPlayers();

            UpdateTimerUI();
            UpdatePlayerUI();

            yield return new WaitForSeconds(1f);

        }
    }

    private void FindPlayers()
    {
        if (p1 == null && ActorManager.Instance != null)
        {
            p1 = ActorManager.Instance.p1;
        }

        if (p2 == null && ActorManager.Instance != null)
        {
            p2 = ActorManager.Instance.p2;
        }
    }

    private void UpdateTimerUI()
    {
        long elapsedTime = NetworkTimeManager.Instance.GetServerTime() - startTime;

        if (elapsedTime < 0) elapsedTime = 0;

        int minutes = (int)(elapsedTime / 60);
        int seconds = (int)(elapsedTime % 60);

        timeText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdatePlayerUI()
    {
        // p1 (여캐) 업데이트
        if (p1 != null)
        {
            player1PushText.text = p1.pushCount.ToString();
            player1PullText.text = p1.pullCount.ToString();
            player1FallText.text = p1.fallDeathCount.ToString();
        }
        else
        {
            player1PushText.text = "-";
            player1PullText.text = "-";
            player1FallText.text = "-";
        }

        // p2 (남캐) 업데이트
        if (p2 != null)
        {
            player2PushText.text = p2.pushCount.ToString();
            player2PullText.text = p2.pullCount.ToString();
            player2FallText.text = p2.fallDeathCount.ToString();
        }
        else
        {
            player2PushText.text = "-";
            player2PullText.text = "-";
            player2FallText.text = "-";
        }
    }

    public void ShowResult()
    {
        StopCount();

        UpdateTimerUI();
        UpdatePlayerUI();

        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(true);
        }
    }
}