using TMPro;
using UnityEngine;

public class VillageUiManager : MonoBehaviour
{
    public static VillageUiManager Instance;
    public TextMeshProUGUI player1IDUi;
    public TextMeshProUGUI player2IDUi;


    [SerializeField] private GameObject dialoguePanel;
    public Animator resultWindow;

    [SerializeField] private TextMeshProUGUI userGoldText;
    [SerializeField] private TextMeshProUGUI debtGoldText;


    [SerializeField] private GameObject deadPanel;


    private void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.IsDungeonCleared)
        {
            GameManager.IsDungeonCleared =false;
            OnResult();
        }
    }
    public void UpdatePlayerIDUI()
    {
        if(player1IDUi == null || player2IDUi ==null)
            return;

        if (ActorManager.Instance.p1 != null)
        {
            player1IDUi.text = ActorManager.Instance.p1.gameObject.name;
        }

        if (ActorManager.Instance.p2 != null)
        {
            player2IDUi.text = ActorManager.Instance.p2.gameObject.name;
        }
    }

    private void OnResult()
    {
        if (dialoguePanel.activeSelf)
        {
            return;
        }
        resultWindow.gameObject.SetActive(true);
        dialoguePanel.SetActive(true);
        GameManager.Instance.Calculate();
        resultWindow.Play("Open");
    }

    public void UpdateGoldText(int gold)
    {
        userGoldText.text = $" {gold}";
    }

    public void UpdateDebtText(int gold)
    {
        debtGoldText.text = $"{gold}";
    }

    public void TutorialDialog()
    {
        resultWindow.gameObject.SetActive(false);
        DialogueManager.Instance.StartDialogue("TutorialText");
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
}
    