using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyRoomManager : MonoBehaviour, IPacketReceiver
{
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

        // 방 액션 버튼 기능 연결
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
        CancelInvoke(nameof(RequestRoomList));
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

        pendingIsHost = true; // 방 생성이므로 호스트 예정

        P_RoomEnterRequest req = new P_RoomEnterRequest { roomNumber = -1, title = inputTitle };
        Client.TCP.SendPacket2(E_PACKET.ROOM_ENTER_REQUEST, req);
    }

    private void RequestJoinRoom()
    {
        if (selectedRoomNum == -1) return;
        joinConfirmButton.interactable = false;

        pendingIsHost = false; // 참가를 눌렀으므로 게스트 예정

        P_RoomEnterRequest req = new P_RoomEnterRequest { roomNumber = selectedRoomNum, title = "" };
        Client.TCP.SendPacket2(E_PACKET.ROOM_ENTER_REQUEST, req);
    }

    // 방 나가기 요청
    private void RequestLeaveRoom()
    {
        P_RoomLeaveRequest req = new P_RoomLeaveRequest();
        Client.TCP.SendPacket2(E_PACKET.ROOM_LEAVE_REQUEST, req);
    }

    private void OnClickRoomAction()
    {
        if (myRoomIsHost)
        {
            Debug.Log("[Lobby] 호스트가 게임 시작 요청 인게임 진입");
        }
        else
        {
            // 파티원인 경우 -> 준비 상태 토글
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

                if (enterRes.result == 0) // 입장/생성 성공
                {
                    isInsideRoom = true;
                    myRoomIsHost = pendingIsHost;

                    HidePopups();
                    UpdateBottomUI();
                    RequestRoomList();
                }
                else
                {
                    Debug.LogError($"[Lobby] 방 입장 실패 : {enterRes.result}");
                    HidePopups();
                }
                break;

            case E_PACKET.ROOM_LEAVE_RESPONSE:
                var leaveRes = UnsafeCode.ByteArrayToStructure<P_RoomLeaveResponse>(packet.data);
                if (leaveRes.result == 0)
                {
                    isInsideRoom = false;
                    myRoomIsHost = false;
                    isGuestReadyLocal = false;

                    UpdateBottomUI();
                    RequestRoomList();
                }
                break;
        }
    }

    private void UpdateRoomListUI(P_RoomListRes res)
    {
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        if (res.roomCount <= 0) return;

        var activeRooms = res.rooms.Take(res.roomCount)
            .OrderBy(r => r.isPlaying)
            .ThenBy(r => r.curUser >= r.maxUser)
            .ThenByDescending(r => r.maxUser - r.curUser)
            .ToList();

        foreach (var roomInfo in activeRooms)
        {
            GameObject go = Instantiate(roomItemPrefab, roomListContent);
            RoomListItem item = go.GetComponent<RoomListItem>();
            item.Setup(roomInfo, this);

            if (isInsideRoom && myRoomIsHost && roomInfo.title.Contains(LocalPlayerInfo.Name))
            {
                roomActionButton.interactable = (roomInfo.guestReadyState == 2);
            }
        }
    }
}