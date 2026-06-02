using TMPro;
using UnityEngine;

public class VliageUiManager : MonoBehaviour
{
    public static VliageUiManager Instance;
    public TextMeshProUGUI player1IDUi;
    public TextMeshProUGUI player2IDUi;


    [SerializeField] private GameObject dialoguePanel;
    public Animator resultWindow;

    [SerializeField] private TextMeshProUGUI userGoldText;
    [SerializeField] private TextMeshProUGUI debtGoldText;
    private void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.IsDungeonCleared)
        {
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
        GameManager.Instance.Calculate();
        dialoguePanel.SetActive(true);
        resultWindow.Play("Open");
    }

    public void UpdateGoldText(int gold)
    {
        userGoldText.text = $" : {gold}";
    }

    public void UpdateDebtText(int gold)
    {
        debtGoldText.text = $": {gold}";
    }
}
