using System;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class TitleManager : MonoBehaviour, IPacketReceiver
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pressAnyKeyPanel;
    [SerializeField] private GameObject nicknamePanel;

    [Header("Inputs & Buttons")]
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button connectButton;
    [SerializeField] private Button cancelButton;

    [Header("Server Configuration")]
    [Tooltip("hosts 파일에 등록할 우회 도메인 주소")]
    [SerializeField] private string serverDomain = "game.chungkang.local";

    private bool isAnyKeyPressed = false;
    private bool isSceneLoading = false;

    void Start()
    {
        if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(true);
        if (nicknamePanel != null) nicknamePanel.SetActive(false);

        connectButton.onClick.AddListener(OnConnectButtonClicked);
        cancelButton.onClick.AddListener(OnBackButtonClicked);
        isSceneLoading = false;

        if (nicknameInput != null)
        {
            nicknameInput.characterLimit = 10;
            nicknameInput.onValueChanged.AddListener(ValidateNickname);
        }
    }

    private void ValidateNickname(string text)
    {
        // 영어와 숫자가 아닌 모든 문자를 찾아 빈칸으로 교체
        string filteredText = Regex.Replace(text, "[^a-zA-Z0-9]", "");

        if (text != filteredText)
        {
            nicknameInput.text = filteredText;
        }
    }
    void Update()
    {
        //if (!isAnyKeyPressed && Input.anyKeyDown)
        if (!isAnyKeyPressed && (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)))
        {
            isAnyKeyPressed = true;
            if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(false);
            if (nicknamePanel != null) nicknamePanel.SetActive(true);

            if (nicknameInput != null) nicknameInput.ActivateInputField();
        }

        if (isAnyKeyPressed && !isSceneLoading)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnConnectButtonClicked();
            }
        }
    }

    void OnConnectButtonClicked()
    {
        string nick = nicknameInput.text;

        if (string.IsNullOrWhiteSpace(nick))
        {
            Debug.LogWarning("[System] 닉네임을 입력해야 합니다");
            return;
        }

        connectButton.interactable = false;
        cancelButton.interactable = false;
        LocalPlayerInfo.Name = nick;

        string resolvedIP = ResolveDomain(serverDomain);

        try
        {
            Client.Connect(resolvedIP, 11020, 5025);
            Client.TCP.AddPacketReceiver(this);

            Debug.Log($"<color=green>[Network] 서버 연결 성공! (Domain: {serverDomain} -> IP: {resolvedIP})</color>");

            P_LoginReq loginReq = default;
            loginReq.userID = nick;
            loginReq.userPW = "dummy";
            Client.TCP.SendPacket2(E_PACKET.LOGIN_REQUEST, loginReq);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Network] 서버 연결 실패: {ex.Message}");
            connectButton.interactable = true;
            cancelButton.interactable = true;
        }
    }

    public void OnBackButtonClicked()
    {
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
        if (pressAnyKeyPanel != null) pressAnyKeyPanel.SetActive(true);

        isAnyKeyPressed = false;

        if (nicknameInput != null) nicknameInput.text = "";

        if (connectButton != null) connectButton.interactable = true;
        if (cancelButton != null) cancelButton.interactable = true;
    }

    string ResolveDomain(string domain)
    {
        try
        {
            IPAddress[] ips = Dns.GetHostAddresses(domain);
            return ips[0].ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Network] 도메인 파싱 실패, 로컬로 전환 Error: {ex.Message}");
            return "127.0.0.1";
        }
    }

    // 로비 서버의 로그인 승인 응답 수신
    public unsafe void OnPacketReceived(Packet packet)
    {
        if (packet.pbase.packet_id == (ushort)E_PACKET.LOGIN_RESPONSE)
        {
            P_LoginRes loginRes = UnsafeCode.ByteArrayToStructure<P_LoginRes>(packet.data);

            if (loginRes.result == 0) // 성공
            {
                LocalPlayerInfo.ID = loginRes.userUUID;
                Debug.Log("<color=cyan>[Client] 로그인 성공! 로비 씬으로 진입합니다.</color>");

                if (!isSceneLoading)
                {
                    isSceneLoading = true;
                    Client.TCP.RemovePacketReceiver(this);
                    SceneManager.LoadSceneAsync("Main_Lobby");
                }
            }
            else
            {
                Debug.LogError($"[Client] 로그인 실패 에러 코드: {loginRes.result}");
                connectButton.interactable = true;
                cancelButton.interactable = true;
            }
        }
    }

    void OnDestroy()
    {
        if (Client.TCP != null)
        {
            Client.TCP.RemovePacketReceiver(this);
        }
    }
}