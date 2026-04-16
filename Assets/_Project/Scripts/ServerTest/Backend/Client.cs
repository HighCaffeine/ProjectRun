using System.Net.Sockets;
using UnityEngine;

public static unsafe class Client
{
    //private const string IP = "127.0.0.1";
    /// <summary>
    private const string IP = "20.196.66.227";
    //private const string IP = "192.168.31.36";
    /// </summary>
    //public static NetworkClient TCP = new NetworkClient(IP, 5004, ProtocolType.Tcp);
    public static NetworkClient TCP = new NetworkClient(IP, 11021, ProtocolType.Tcp);
    public static NetworkClient UDP = new NetworkClient(IP, 5025, ProtocolType.Udp);

    public static bool IS_SERVER_PLAY = true;


    public static void Start()
    {
        TCP.Start();
        UDP.Start();
        TCP.OnDisconnect += OnDisconnect;
        Application.wantsToQuit += OnApplicationQuit;

        Application.runInBackground = true;

        GameObject updaterObj = new GameObject("NetworkUpdater");
        Object.DontDestroyOnLoad(updaterObj);
        updaterObj.AddComponent<NetworkUpdater>();
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
        // do stuff
        // maybe display a message or something
    }

    public static void Close()
    {
        if (TCP != null)
        {
            TCP.Close();
        }
        if (UDP != null)
        {
            UDP.Close();
        }
    }
}
