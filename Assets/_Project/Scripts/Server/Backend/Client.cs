using System.Net.Sockets;
using UnityEngine;

public static unsafe class Client
{
    public static NetworkClient TCP;
    public static NetworkClient UDP;

    public static bool IS_SERVER_PLAY = true;

    private static GameObject updaterObj;

    public static void Connect(string ip, int tcpPort, int udpPort)
    {
        // 혹시 이미 열려있는 소켓이 있다면 닫고 초기화
        Close();

        TCP = new NetworkClient(ip, tcpPort, ProtocolType.Tcp);
        UDP = new NetworkClient(ip, udpPort, ProtocolType.Udp);

        TCP.Start();
        UDP.Start();

        TCP.OnDisconnect -= OnDisconnect; // 중복 등록 방지
        TCP.OnDisconnect += OnDisconnect;

        Application.wantsToQuit -= OnApplicationQuit; // 중복 등록 방지
        Application.wantsToQuit += OnApplicationQuit;

        Application.runInBackground = true;

        if (updaterObj == null)
        {
            updaterObj = new GameObject("NetworkUpdater");
            Object.DontDestroyOnLoad(updaterObj);
            updaterObj.AddComponent<NetworkUpdater>();
        }
    }

    private static bool OnApplicationQuit()
    {
        Close();
        return true;
    }

    private class NetworkUpdater : MonoBehaviour
    {
        void Update()
        {
            if (TCP != null) TCP.Update();
            if (UDP != null) UDP.Update();
        }
    }

    private static void OnDisconnect()
    {
        Debug.LogWarning("[Network] 서버와 연결이 끊어졌습니다.");
    }

    public static void Close()
    {
        if (TCP != null)
        {
            TCP.Close();
            TCP = null;
        }
        if (UDP != null)
        {
            UDP.Close();
            UDP = null;
        }
    }
}