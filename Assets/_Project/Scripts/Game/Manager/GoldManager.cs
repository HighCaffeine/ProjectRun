using UnityEngine;

public class GoldManager : GenericSingleton<GoldManager>
{
    int userGold;
    int debtGold;
    int resultGold;
    public int UserGold
    {
        get => userGold;
        set
        {
            userGold = value;
            VliageUiManager.Instance.UpdateGoldText(userGold);
        }
    }

    public int DebtGold
        {
        get => debtGold;
        set
        {
            debtGold = value;
            VliageUiManager.Instance.UpdateDebtText(debtGold);
        }
    }

    public int ResultGold
    {
        get => resultGold;
        set
        {
            resultGold = value;
            VliageUiManager.Instance.resultWindow.GetComponent<AniEvent>().gold = resultGold; 
        }
    }

    void Start()
    {
        UserGold = 0;
        DebtGold = 1000;
    }
    public void ApplyResultGold(int gold)
    {
        ResultGold = gold;
        DebtGold -= gold;
    }
}
