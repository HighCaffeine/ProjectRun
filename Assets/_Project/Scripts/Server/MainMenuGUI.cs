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

        // 이전 접속 IP
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
            Client.Connect(parsedIp.ToString(), 11020, 5025);

            Client.TCP.AddPacketReceiver(this);
            Debug.Log($"[Network] 로비 서버(11020) 연결 성공 (IP: {parsedIp})");

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
        loginReq.userPW = inputName; // 더미

        Client.TCP.SendPacket2(E_PACKET.LOGIN_REQUEST, loginReq);
        Debug.Log($"[Client] 로비로 로그인 요청 전송: {inputName}");
    }

    // 패킷 수신 처리
    public unsafe void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;

        switch ((E_PACKET)packetId)
        {
            case E_PACKET.LOGIN_RESPONSE:
                try
                {
                    P_LoginRes loginRes = UnsafeCode.ByteArrayToStructure<P_LoginRes>(packet.data);

                    if (loginRes.result == 0) // 성공
                    {
                        Debug.Log("<color=cyan>[Client] 로그인 성공! Main_Lobby 씬으로 이동합니다.</color>");
                        LocalPlayerInfo.ID = loginRes.result;

                        // 씬 중복 로딩 방지
                        if (isSceneLoading) return;
                        isSceneLoading = true;

                        SceneManager.LoadSceneAsync("Main_Lobby");
                    }
                    else
                    {
                        Debug.LogError($"[Client] 로그인 실패. ErrorCode: {loginRes.result}");
                        joinButton.interactable = true; // 실패 시 버튼 다시 활성화
                    }
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
    }
}