using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyRoomManager : MonoBehaviour, IPacketReceiver
{
    [Header("UI Slots (대기방 UI용)")]
    public LobbyPlayerInfoUI playerInfoUI; // 내 캐릭터 정보 및 스왑 UI

    private long currentHostUUID = -1;
    private Dictionary<long, bool> userReadyStates = new Dictionary<long, bool>();


    [Header("Room List UI")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private Button refreshButton;

    [Header("Create Room Popup")]
    [SerializeField] private GameObject createRoomPopup;
    [SerializeField] private TMP_InputField createRoomNameInput;
    [SerializeField] private Button createConfirmButton;
    [SerializeField] private Button createCloseButton;

    [Header("Join Room Popup")]
    [SerializeField] private GameObject joinRoomPopup;
    [SerializeField] private TextMeshProUGUI joinRoomNameText;
    [SerializeField] private Button joinConfirmButton;
    [SerializeField] private Button joinCloseButton;

    [Header("Room UI Action Elements")]
    [SerializeField] private GameObject lobbyCreateButtonObj;
    [SerializeField] private GameObject roomActionPanelObj;
    [SerializeField] private Button roomLeaveButton;
    [SerializeField] private Button roomActionButton;
    [SerializeField] private TextMeshProUGUI roomActionText;

    private int selectedRoomNum = -1;
    public bool isInsideRoom = false;
    private bool myRoomIsHost = false;
    private bool isGuestReadyLocal = false;
    private bool pendingIsHost = false;


    void Start()
    {
        if (Client.TCP != null) Client.TCP.AddPacketReceiver(this);

        if (lobbyCreateButtonObj != null)
        {
            Button createBtn = lobbyCreateButtonObj.GetComponent<Button>();
            if (createBtn != null)
            {
                createBtn.onClick.RemoveAllListeners();
                createBtn.onClick.AddListener(ShowCreatePopup);
            }
        }

        refreshButton.onClick.AddListener(RequestRoomList);

        createConfirmButton.onClick.AddListener(RequestCreateRoom);
        createCloseButton.onClick.AddListener(HidePopups);

        joinConfirmButton.onClick.AddListener(RequestJoinRoom);
        joinCloseButton.onClick.AddListener(HidePopups);

        roomLeaveButton.onClick.AddListener(RequestLeaveRoom);
        roomActionButton.onClick.AddListener(OnClickRoomAction);

        HidePopups();
        UpdateBottomUI();
        RequestRoomList();

        InvokeRepeating(nameof(RequestRoomList), 1f, 2f);
    }

    void OnDestroy()
    {
        if (Client.TCP != null) Client.TCP.RemovePacketReceiver(this);
        StopLobbyPolling();
    }

    public void RequestRoomList()
    {
        P_RoomListReq req = new P_RoomListReq { dummy = 0 };
        Client.TCP.SendPacket2(E_PACKET.ROOM_LIST_REQ, req);
    }

    public void ShowJoinPopup(int roomNum, string roomTitle)
    {
        HidePopups();
        selectedRoomNum = roomNum;
        joinRoomNameText.text = roomTitle;
        joinRoomPopup.SetActive(true);
    }

    public void ShowCreatePopup()
    {
        HidePopups();
        createRoomPopup.SetActive(true);
    }

    public void HidePopups()
    {
        createRoomPopup.SetActive(false);
        joinRoomPopup.SetActive(false);
    }

    private void RequestCreateRoom()
    {
        createConfirmButton.interactable = false;
        string inputTitle = createRoomNameInput.text;
        if (string.IsNullOrWhiteSpace(inputTitle))
        {
            inputTitle = $"{LocalPlayerInfo.Name}님의 방";
        }

        pendingIsHost = true;

        P_RoomEnterRequest req = new P_RoomEnterRequest { roomNumber = -1, title = inputTitle };
        Client.TCP.SendPacket2(E_PACKET.ROOM_ENTER_REQUEST, req);
    }

    private void RequestJoinRoom()
    {
        if (selectedRoomNum == -1) return;
        joinConfirmButton.interactable = false;

        pendingIsHost = false;

        P_RoomEnterRequest req = new P_RoomEnterRequest { roomNumber = selectedRoomNum, title = "" };
        Client.TCP.SendPacket2(E_PACKET.ROOM_ENTER_REQUEST, req);
    }

    private void RequestLeaveRoom()
    {
        P_RoomLeaveRequest req = new P_RoomLeaveRequest();
        Client.TCP.SendPacket2(E_PACKET.ROOM_LEAVE_REQUEST, req);
    }

    private void OnClickRoomAction()
    {
        if (myRoomIsHost)
        {
            P_GameStartReq req = new P_GameStartReq { roomNumber = selectedRoomNum };
            Client.TCP.SendPacket2(E_PACKET.GAME_START_REQUEST, req);
            Debug.Log("[Lobby] 방장이 게임 시작을 요청");
        }
        else
        {
            isGuestReadyLocal = !isGuestReadyLocal;
            P_PlayerReadyRequest req = new P_PlayerReadyRequest { isReady = isGuestReadyLocal };
            Client.TCP.SendPacket2(E_PACKET.PLAYER_READY_REQUEST, req);
            roomActionText.text = isGuestReadyLocal ? "준비 취소" : "준비";
        }
    }

    private void UpdateBottomUI()
    {
        if (!isInsideRoom)
        {
            lobbyCreateButtonObj.SetActive(true);
            roomActionPanelObj.SetActive(false);
        }
        else
        {
            lobbyCreateButtonObj.SetActive(false);
            roomActionPanelObj.SetActive(true);

            if (myRoomIsHost)
            {
                roomActionText.text = "게임 시작";
                roomActionButton.interactable = false;
            }
            else
            {
                roomActionText.text = isGuestReadyLocal ? "준비 취소" : "준비";
                roomActionButton.interactable = true;
            }
        }

        var playerInfoUI = FindObjectOfType<LobbyPlayerInfoUI>();
        if (playerInfoUI != null)
        {
            if (isInsideRoom && !myRoomIsHost)
            {
                playerInfoUI.SetInteractable(false); // 게스트는 선택 불가
            }
            else
            {
                playerInfoUI.SetInteractable(true);  // 방장이나 로비 대기자는 선택 가능
            }
        }
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;

        switch ((E_PACKET)packetId)
        {
            case E_PACKET.ROOM_LIST_RES:
                var roomRes = UnsafeCode.ByteArrayToStructure<P_RoomListRes>(packet.data);
                UpdateRoomListUI(roomRes);
                break;

            case E_PACKET.ROOM_ENTER_RESPONSE:
                var enterRes = UnsafeCode.ByteArrayToStructure<P_RoomEnterResponse>(packet.data);
                createConfirmButton.interactable = true;
                joinConfirmButton.interactable = true;

                if (enterRes.result == 0)
                {
                    isInsideRoom = true;
                    myRoomIsHost = pendingIsHost;
                    selectedRoomNum = enterRes.roomNum;

                    P_RoomCharSelectReq req = new P_RoomCharSelectReq { charID = LocalPlayerInfo.CharacterID };
                    Client.TCP.SendPacket2(E_PACKET.ROOM_CHAR_SELECT_REQ, req);

                    HidePopups();
                    UpdateBottomUI();
                    RequestRoomList();
                }
                break;
            case E_PACKET.ROOM_HOST_NTF:
                {
                    var hostNtf = UnsafeCode.ByteArrayToStructure<P_RoomHostNtf>(packet.data);
                    currentHostUUID = hostNtf.hostUUID;
                    Debug.Log($"[HostNTF] hostUUID={hostNtf.hostUUID}, myID={LocalPlayerInfo.ID}, match={LocalPlayerInfo.ID == currentHostUUID}");
                    if (pendingIsHost)
                    {
                        myRoomIsHost = true;
                    }
                    else
                    {
                        myRoomIsHost = (LocalPlayerInfo.ID == currentHostUUID);
                    }

                    UpdateBottomUI();
                    break;
                }
            case E_PACKET.ROOM_LEAVE_RESPONSE:
                var leaveRes = UnsafeCode.ByteArrayToStructure<P_RoomLeaveResponse>(packet.data);
                if (leaveRes.result == 0)
                {
                    isInsideRoom = false;
                    myRoomIsHost = false;
                    isGuestReadyLocal = false;
                    selectedRoomNum = -1;

                    UpdateBottomUI();
                    RequestRoomList();
                }
                break;

            case E_PACKET.ROOM_NEW_USER_NTF:
                if (isInsideRoom && myRoomIsHost)
                {
                    int currentCharID = LocalPlayerInfo.CharacterID;
                    P_RoomCharSelectReq req = new P_RoomCharSelectReq { charID = currentCharID };
                    Client.TCP.SendPacket2(E_PACKET.ROOM_CHAR_SELECT_REQ, req);
                    Debug.Log($"[Lobby] 게스트 입장 - 호스트 캐릭터 재전송: {currentCharID}");
                }
                break;
            case E_PACKET.ROOM_USER_INFO_NTF:
                {
                    var userInfoNtf = UnsafeCode.ByteArrayToStructure<P_RoomUserInfoNotify>(packet.data);

                    if (userInfoNtf.userUUID != LocalPlayerInfo.ID)
                    {
                        int forceCharID = (userInfoNtf.characterID == 0) ? 1 : 0;
                        if (playerInfoUI != null) playerInfoUI.ForceSetCharacter(forceCharID);
                        Debug.Log($"[Lobby] 상대 캐릭터={userInfoNtf.characterID}, 내 캐릭터 강제={forceCharID}");
                    }
                    break;
                }
            case E_PACKET.ROOM_CHAR_SELECT_NTF:
                {
                    var charNtf = UnsafeCode.ByteArrayToStructure<P_RoomCharSelectNtf>(packet.data);
                    Debug.Log($"[Lobby] Char NTF userUUID={charNtf.userUUID}, myID={LocalPlayerInfo.ID}, isMe={charNtf.userUUID == LocalPlayerInfo.ID}");

                    if (!isInsideRoom) break;

                    if (charNtf.userUUID == LocalPlayerInfo.ID)
                    {
                        break;
                    }

                    if (!myRoomIsHost)
                    {
                        int forceCharID = (charNtf.charID == 0) ? 1 : 0;
                        if (playerInfoUI != null) playerInfoUI.ForceSetCharacter(forceCharID);

                        RequestRoomList();
                    }
                    break;
                }

            case E_PACKET.MATCH_START_NTF:
                {
                    var startNtf = UnsafeCode.ByteArrayToStructure<P_MatchStartNtf>(packet.data);

                    Debug.Log($"<color=magenta>[Handover] 게임 서버로 이동 포트: {startNtf.GameServerPort} Token: {startNtf.AuthToken}</color>");

                    LocalPlayerInfo.AuthToken = startNtf.AuthToken;

                    StopLobbyPolling();

                    string serverDomain = "game.chungkang.local";
                    string resolvedIP = ResolveDomain(serverDomain);

                    Client.SafeDisconnectAndReconnect(resolvedIP, startNtf.GameServerPort, this);

                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game_Lobby");
                    break;
                }
        }
    }

    private string ResolveDomain(string domain)
    {
        try
        {
            System.Net.IPAddress[] ips = System.Net.Dns.GetHostAddresses(domain);
            return ips[0].ToString();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Network] 도메인 파싱 실패, 로컬로 전환 Error: {ex.Message}");
            return "127.0.0.1";
        }
    }

    private void UpdateRoomListUI(P_RoomListRes res)
    {
        if (res.roomCount <= 0)
        {
            for (int i = 0; i < roomListContent.childCount; i++)
            {
                roomListContent.GetChild(i).gameObject.SetActive(false);
            }
            return;
        }

        var activeRooms = res.rooms.Take(res.roomCount)
            .OrderBy(r => r.isPlaying)
            .ThenBy(r => r.curUser >= r.maxUser)
            .ThenByDescending(r => r.maxUser - r.curUser)
            .ToList();

        for (int i = 0; i < activeRooms.Count; i++)
        {
            GameObject roomObj;

            if (i < roomListContent.childCount)
            {
                roomObj = roomListContent.GetChild(i).gameObject;
                roomObj.SetActive(true);
            }
            else
            {
                roomObj = Instantiate(roomItemPrefab, roomListContent);
            }

            RoomListItem item = roomObj.GetComponent<RoomListItem>();
            item.Setup(activeRooms[i], this);

            if (isInsideRoom && myRoomIsHost && activeRooms[i].roomNum == selectedRoomNum)
            {
                roomActionButton.interactable = (activeRooms[i].guestReadyState == 2);
            }
        }

        for (int i = activeRooms.Count; i < roomListContent.childCount; i++)
        {
            roomListContent.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void StopLobbyPolling()
    {
        CancelInvoke(nameof(RequestRoomList));
        Debug.Log("[Lobby] 로비 패킷 폴링 중단됨.");
    }
}