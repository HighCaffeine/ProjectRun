using System.Collections;
using TMPro;
using UnityEngine;

public class DungeonUiManager : GenericSingleton<DungeonUiManager>
{
    private Coroutine countdownCoroutine;

    private float timer = 0f;

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
        timer = 0f;
    }

    public void StartCount()
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

    private IEnumerator TimeCount()
    {
        while (true)
        {
            FindPlayers();

            UpdateTimerUI();
            UpdatePlayerUI();

            yield return new WaitForSeconds(1f);

            timer++;
        }
    }

    private void FindPlayers()
    {
        if (p1 == null && ActorManager.Instance != null)
        {
            p1 = ActorManager.Instance.GetPlayer(false);
        }

        if (p2 == null && ActorManager.Instance != null)
        {
            p2 = ActorManager.Instance.GetPlayer(true);
        }
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.FloorToInt(timer % 60);

        timeText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdatePlayerUI()
    {
        if (p1 != null)
        {
          //  player1DestroyText.text = p1.destroyObjectCount.ToString();
            player1PushText.text = p1.pushCount.ToString();
            player1PullText.text = p1.pullCount.ToString();
            player1FallText.text = p1.fallDeathCount.ToString();
         //   player1FallKillText.text = p1.fallKillCount.ToString();
        }

        if (p2 != null)
        {
         //   player2DestroyText.text = p2.destroyObjectCount.ToString();
            player2PushText.text = p2.pushCount.ToString();
            player2PullText.text = p2.pullCount.ToString();
            player2FallText.text = p2.fallDeathCount.ToString();
         //   player2FallKillText.text = p2.fallKillCount.ToString();
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