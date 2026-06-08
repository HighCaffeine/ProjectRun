using UnityEngine;

public class GameManager : GenericSingleton<GameManager>
{
    public enum PlayMode { Offline_Test, Server_Online }

    [Header("테스트 환경 설정")]
    public PlayMode currentMode = PlayMode.Offline_Test;

    [Header("세션 정보")]
    public bool isHost = false;


    [Header("Test")]
    public Transform playerDashAnchor;

    private static bool isDungeonCleared;

    public static bool IsDungeonCleared
    {
        get { return isDungeonCleared; }
        set { isDungeonCleared = value; }
    }
    private static long clearTime;

    public bool hasShownDungeonIntro = false;


    protected override void Awake()
    {
        base.Awake();

        if (currentMode == PlayMode.Offline_Test)
        {
            Client.IS_SERVER_PLAY = false;
        }
        else
        {
            Client.IS_SERVER_PLAY = true;
        }

        DontDestroyOnLoad(gameObject);

        //if (UiManager.Instance != null) UiManager.Instance.StartCount();
    }

    public void LoadVillage()
    {
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game_Lobby");

    }
    public void DungeonClear()
    {
        isDungeonCleared = true;
    }

    public void GetClearTime(long DungeonClearTime)
    {
        clearTime = DungeonClearTime;
    }

    public void Calculate()
    {
        float clearMinutes = clearTime / 60f;

        int baseGold = 1000;
        int minGold = 300;

        float extraTime = clearMinutes - 20f;

        int calculatedGold = baseGold;

        if (extraTime > 0)
        {
            int penaltyCount = Mathf.FloorToInt(extraTime / 2f);

            calculatedGold -= penaltyCount * 100;
        }

        calculatedGold = Mathf.Max(calculatedGold, minGold);

        GoldManager.Instance.ApplyResultGold(calculatedGold);
    }
}