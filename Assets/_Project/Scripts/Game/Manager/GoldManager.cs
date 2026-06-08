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
            VillageUiManager.Instance.UpdateGoldText(userGold);
        }
    }

    public int DebtGold
        {
        get => debtGold;
        set
        {
            debtGold = value;
            VillageUiManager.Instance.UpdateDebtText(debtGold);
        }
    }

    public int ResultGold
    {
        get => resultGold;
        set
        {
            resultGold = value;
            VillageUiManager.Instance.resultWindow.GetComponent<AniEvent>().gold = resultGold; 
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
        if(debtGold - gold >0)
        {
          DebtGold -= gold;           
        }
        else if(debtGold - gold<0)
        {
            debtGold =0;
            userGold = gold -debtGold;
        }
    }
}
