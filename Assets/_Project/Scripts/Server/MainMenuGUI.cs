using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;

public class MainMenuGUI : MonoBehaviour, IPacketReceiver
{
    [Header("UI Panels")]
    public GameObject connectionPanel; // 서버 접속 패널
    public GameObject loginPanel;      // 로그인 패널

    [Header("Connection Inputs")]
    public InputField ipInput;
    public Button connectButton;

    [Header("Login Inputs")]
    public InputField nameInput;
    public Button joinButton;

    private bool isSceneLoading = false;

    void Start()
    {
        if (connectionPanel != null) connectionPanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);

        // 이전 접속 IP/Port
        string savedIP = PlayerPrefs.GetString("ServerIP", "127.0.0.1");
        IPInputManager.Instance.SetIP(savedIP);

        isSceneLoading = false;
    }

    // 서버 접속 처리
    public void OnConnectButtonClick()
    {
        string ipStr = IPInputManager.Instance.GetFullIP();

        if (!IPAddress.TryParse(ipStr, out IPAddress parsedIp))
        {
            Debug.LogError("[System] 올바른 IP 주소 형식이 아닙니다.");
            return;
        }

        connectButton.interactable = false;

        try
        {
            Client.Connect(parsedIp.ToString(), 11021, 5025);

            Client.TCP.AddPacketReceiver(this);
            Debug.Log($"[Network] 서버 연결 성공 (IP: {parsedIp})");

            PlayerPrefs.SetString("ServerIP", parsedIp.ToString());

            connectionPanel.SetActive(false);
            loginPanel.SetActive(true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Network] 서버 연결 실패: {ex.Message}");
            connectButton.interactable = true;
        }
    }

    // 로그인 처리
    public void OnJoinButtonClick()
    {
        if (Client.TCP == null)
        {
            Debug.LogError("[System] 서버에 먼저 접속해야 합니다.");
            return;
        }

        joinButton.interactable = false;

        string inputName = nameInput.text;
        LocalPlayerInfo.Name = inputName;

        P_LoginReq loginReq = default;
        loginReq.userID = inputName;
        loginReq.userPW = inputName;

        Client.TCP.SendPacket2(E_PACKET.LOGIN_REQUEST, loginReq);
        Debug.Log($"[Client] 로그인 요청 전송: {inputName}");
    }

    // 패킷 수신 처리
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
                    if (isSceneLoading)
                    {
                        Debug.LogWarning("씬 중복 로딩 처리 차단");
                        return;
                    }
                    isSceneLoading = true;

                    LocalPlayerInfo.ID = loginRes.result;
                    SceneManager.LoadSceneAsync("Game_Lobby");
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
        if (Client.TCP != null)
        {
            Client.TCP.RemovePacketReceiver(this);
        }

        //if (AkUnitySoundEngine.IsInitialized()) AkUnitySoundEngine.Term();
    }
}