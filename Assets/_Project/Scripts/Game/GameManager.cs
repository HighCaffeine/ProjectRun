using UnityEngine;

public class GameManager : GenericSingleton<GameManager>
{
    public enum PlayMode { Offline_Test, Server_Online }

    [Header("테스트 환경 설정")]
    public PlayMode currentMode = PlayMode.Offline_Test;

    [Header("세션 정보")]
    public bool isHost = false;

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
    }
}