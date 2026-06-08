using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DungeonUiManager : GenericSingleton<DungeonUiManager>
{
    [SerializeField]
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
    [SerializeField] private Animator diaryBG;
    [SerializeField] private PlayerActor p1;
    [SerializeField] private PlayerActor p2;

    [SerializeField] private GameObject settingPanel;

    [SerializeField] private GameObject progress;
    private Coroutine progressCoroutine;


    [SerializeField] private GameObject deadPanel;
                        
    [SerializeField] private Button returnToVillageButton;
    private P_DungeonClearNtf finalResultData;

    private new void Awake()
    {
        base.Awake();
        resultUIPanel.SetActive(false);

    }

    private void Start()
    {
        settingPanel.SetActive(false);
        progress.SetActive(false);
        startTime = NetworkTimeManager.Instance.GetServerTime();
        StartCount();
    }
    private void Update()
    {
        if (SceneCutsceneController.Instance != null && SceneCutsceneController.Instance._currentCutscene.director.state == UnityEngine.Playables.PlayState.Playing)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(DialogueManager.Instance.isDialogueActive)
            {
                return;
            }
            ToggleSetting();
        }
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ProGressUi.Instance.instage = true;
            ShowProgress();
        }
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

        }
    }

    private void FindPlayers()
    {
        if (p1 != null && p2 != null)
            return;

        if (ActorManager.Instance == null)
            return;

        p1 ??= ActorManager.Instance.p1;
        p2 ??= ActorManager.Instance.p2;
    }

    private void UpdateTimerUI()
    {
        long elapsedMs = NetworkTimeManager.Instance.GetServerTime() - startTime;

        int minutes = (int)(elapsedMs / 1000 / 60);
        int seconds = (int)(elapsedMs / 1000 % 60);

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

    // public void ShowResult()
    // {
    //     StopCount();

    //     UpdateTimerUI();
    //     UpdatePlayerUI();

    //     if (resultUIPanel != null)
    //     {
    //         resultUIPanel.SetActive(true);
    //     }
    // }

    public void ToggleSetting()
    {
        if (settingPanel == null)
            return;

        settingPanel.SetActive(!settingPanel.activeSelf);
    }
    public void Exit()
    {
        P_DungeonEscapeReq pkt = new P_DungeonEscapeReq();

        Client.TCP.SendPacket2(E_PACKET.DUNGEON_ESCAPE_REQ, pkt);
        //Debug.Log("[System] Ż�� ��û ��Ŷ ���� �Ϸ�");
    }


    public void ShowProgress()
    {
        if (progressCoroutine != null)
        {
            StopCoroutine(progressCoroutine);
        }

        progressCoroutine = StartCoroutine(ShowProgressRoutine());
    }

    private IEnumerator ShowProgressRoutine()
    {
        progress.SetActive(true);
        yield return null;
    }


    public void ProGressDialog()
    {
        DialogueManager.Instance.StartDialogue("ProGressText");
    }


    public void ShowDeadPanel()
    {
        if (deadPanel != null)
        {
            deadPanel.SetActive(true);
        }
    }

    public void HideDeadPanel()
    {
        if (deadPanel != null)
        {
            deadPanel.SetActive(false);
        }
    }

    public void DiaryGather()
    {
        if (diaryBG == null)
            return;

        diaryBG.Play("Gather");
    }

    public void SetFinalResult(P_DungeonClearNtf data)
    {
        finalResultData = data;
    }

    public void ShowResult()
    {
        StopCount();
        FindPlayers(); // ← 강제로 한 번 더 탐색

        int minutes = finalResultData.clearTimeSeconds / 60;
        int seconds = finalResultData.clearTimeSeconds % 60;
        timeText.text = $"{minutes:00}:{seconds:00}";

        if (p1 != null)
        {
            player1PushText.text = finalResultData.p1Push.ToString();
            player1PullText.text = finalResultData.p1Pull.ToString();
            player1FallText.text = finalResultData.p1Fall.ToString();
            player1DestroyText.text = finalResultData.p1Destroy.ToString();
            player1FallKillText.text = finalResultData.p1FallKill.ToString();
        }
        if (p2 != null)
        {
            player2PushText.text = finalResultData.p2Push.ToString();
            player2PullText.text = finalResultData.p2Pull.ToString();
            player2FallText.text = finalResultData.p2Fall.ToString();
            player2DestroyText.text = finalResultData.p2Destroy.ToString();
            player2FallKillText.text = finalResultData.p2FallKill.ToString();
        }

        if (resultUIPanel != null)
        {
            resultUIPanel.SetActive(true);
        }

        if (returnToVillageButton != null)
        {
            returnToVillageButton.interactable = GameManager.Instance.isHost;
        }
    }

    public void OnClick_ReturnToVillage()
    {
        if (!GameManager.Instance.isHost) return;

        P_DungeonReturnVillageReq req = new P_DungeonReturnVillageReq();
        Client.TCP.SendPacket2(E_PACKET.DUNGEON_RETURN_VILLAGE_REQ, req);
    }

    public void OnReceive_DungeonReturnVillageNtf()
    {
        EscapeZone escapeZone = FindFirstObjectByType<EscapeZone>();
        if (escapeZone != null)
            escapeZone.GoToVillage();
        else
        {
            GameManager.Instance.DungeonClear();
            GameManager.Instance.LoadVillage();
        }
    }
}