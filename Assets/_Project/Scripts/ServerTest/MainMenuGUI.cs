using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuGUI : MonoBehaviour, IPacketReceiver
{
    void Awake()
    {
        Application.targetFrameRate = 60;

        Client.Start();
    }

    // Start is called before the first frame update
    void Start()
    {
        Client.TCP.AddPacketReceiver(this);
    }

    // public unsafe void OnPacketReceived(Packet packet)
    // {
    //     ushort packetId = packet.pbase.packet_id;
    //     switch ((E_PACKET)packetId)
    //     {
    //         case E_PACKET.LOGIN_RESPONSE:
    //             P_LoginRes loginRes = UnsafeCode.ByteArrayToStructure<P_LoginRes>(packet.data);

    //             string inputName = GameObject.Find("NameInput").GetComponent<InputField>().text;

    //             Debug.Log($"login {loginRes.result}");

    //             LocalPlayerInfo.ID = loginRes.result;
    //             LocalPlayerInfo.Name = inputName;
    //             SceneManager.LoadSceneAsync("Game_Lobby", LoadSceneMode.Single);
    //             break;
    //     }
    // }

    private bool isSceneLoading = false; // 플래그 추가

    // public unsafe void OnPacketReceived(Packet packet)
    // {
    //     if (isSceneLoading || SceneManager.GetActiveScene().name == "Game_Lobby") return;

    //     ushort packetId = packet.pbase.packet_id;
    //     if ((E_PACKET)packetId == E_PACKET.LOGIN_RESPONSE)
    //     {
    //         if (isSceneLoading) return; // 이미 로딩 중이면 무시

    //         P_LoginRes loginRes = UnsafeCode.ByteArrayToStructure<P_LoginRes>(packet.data);
    //         isSceneLoading = true; // 로딩 시작 알림

    //         LocalPlayerInfo.ID = loginRes.result;
    //         SceneManager.LoadSceneAsync("Game_Lobby");
    //     }
    // }

    public unsafe void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;
        Debug.Log($"[Client] Packet Received: ID = {packetId}, Size = {packet.pbase.length}");

        switch ((E_PACKET)packetId)
        {
            case E_PACKET.LOGIN_RESPONSE:
                try
                {
                    P_LoginRes loginRes = UnsafeCode.ByteArrayToStructure<P_LoginRes>(packet.data);
                    Debug.Log($"[Client] Login Result: {loginRes.result}");

                    // 씬 로딩 중복 호출 방지
                    if (isSceneLoading) return;
                    isSceneLoading = true;

                    LocalPlayerInfo.ID = loginRes.result;
                    //SceneManager.LoadSceneAsync("Game_Lobby");
                    SceneManager.LoadSceneAsync("Dungeon_1");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Client] Login Parsing Error: {ex.Message}");
                }
                break;
        }
    }

    public void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
        if (AkUnitySoundEngine.IsInitialized()) AkUnitySoundEngine.Term();
    }

    public void OnJoinButtonClick()
    {
        GameObject.Find("JoinButton").GetComponent<Button>().interactable = false;

        string inputName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        P_LoginReq loginReq = default;
        loginReq.userID = inputName;
        loginReq.userPW = inputName;
        Client.TCP.SendPacket2(E_PACKET.LOGIN_REQUEST, loginReq);
    }
}
