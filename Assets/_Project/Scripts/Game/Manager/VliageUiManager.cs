using TMPro;
using UnityEngine;

public class VliageUiManager : MonoBehaviour
{
    public static VliageUiManager Instance;
    public TextMeshProUGUI player1IDUi;
    public TextMeshProUGUI player2IDUi;
    private void Awake()
    {
        Instance = this;
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
}
