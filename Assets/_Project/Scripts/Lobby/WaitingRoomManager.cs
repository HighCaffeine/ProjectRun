using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WaitingRoomManager : MonoBehaviour, IPacketReceiver
{
    public LobbyPlayerInfoUI playerInfoUI;

    [Header("UI Panels")]
    public GameObject lobbyPanel;       // 기존 방 목록 패널
    public GameObject waitingRoomPanel; // 현재 대기방 패널

    [Header("Room Info")]
    public TextMeshProUGUI roomTitleText;
    public TextMeshProUGUI myReadyButtonText;

    [Header("Player Slots")]
    public WaitingRoomSlot mySlot;
    public WaitingRoomSlot otherSlot;

    private bool isMyReady = false;
    private long currentHostUUID = -1;

    private Dictionary<long, bool> userReadyStates = new Dictionary<long, bool>();

    void Start()
    {
        if (Client.TCP != null) Client.TCP.AddPacketReceiver(this);
    }

    void OnDestroy()
    {
        if (Client.TCP != null) Client.TCP.RemovePacketReceiver(this);
    }

    // 방 입장에 성공했을 때 LobbyRoomManager에서 호출
    public void EnterWaitingRoom(string roomTitle)
    {
        lobbyPanel.SetActive(false);
        waitingRoomPanel.SetActive(true);

        roomTitleText.text = roomTitle;
        isMyReady = false;

        // 초기화
        mySlot.InitEmpty();
        otherSlot.InitEmpty();

        mySlot.SetUser(LocalPlayerInfo.ID, LocalPlayerInfo.Name, LocalPlayerInfo.CharacterID);

        UpdateBottomButton();
    }

    // 준비 / 게임 시작 
    public void OnClickReadyOrStart()
    {
        isMyReady = !isMyReady;

        // 서버로 준비 상태 전송 (방장, 파티원 동일하게 사용)
        P_PlayerReadyRequest req = new P_PlayerReadyRequest { isReady = isMyReady };
        Client.TCP.SendPacket2(E_PACKET.PLAYER_READY_REQUEST, req);

        mySlot.SetReady(isMyReady);
        UpdateBottomButton();
    }

    public void OnClickLeaveRoom()
    {
        P_RoomLeaveRequest req = new P_RoomLeaveRequest();
        Client.TCP.SendPacket2(E_PACKET.ROOM_LEAVE_REQUEST, req);
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;

        switch ((E_PACKET)packetId)
        {
            case E_PACKET.ROOM_USER_INFO_NTF:
                {
                    var userInfo = UnsafeCode.ByteArrayToStructure<P_RoomUserInfoNotify>(packet.data);
                    if (userInfo.userUUID != LocalPlayerInfo.ID)
                    {
                        bool isReady = userReadyStates.ContainsKey(userInfo.userUUID) ? userReadyStates[userInfo.userUUID] : false;
                        bool isHost = (userInfo.userUUID == currentHostUUID);

                        otherSlot.SetUser(userInfo.userUUID, userInfo.userName, userInfo.characterID, isReady, isHost);
                    }
                    break;
                }
            case E_PACKET.ROOM_NEW_USER_NTF:
                {
                    var newUser = UnsafeCode.ByteArrayToStructure<P_RoomNewUserNotify>(packet.data);
                    if (newUser.userUUID != LocalPlayerInfo.ID)
                    {
                        bool isReady = userReadyStates.ContainsKey(newUser.userUUID) ? userReadyStates[newUser.userUUID] : false;
                        bool isHost = (newUser.userUUID == currentHostUUID);

                        otherSlot.SetUser(newUser.userUUID, newUser.userName, newUser.characterID, isReady, isHost);
                    }

                    if (currentHostUUID == LocalPlayerInfo.ID)
                    {
                        P_RoomCharSelectReq req = new P_RoomCharSelectReq { charID = LocalPlayerInfo.CharacterID };
                        Client.TCP.SendPacket2(E_PACKET.ROOM_CHAR_SELECT_REQ, req);
                    }
                    break;
                }
            case E_PACKET.ROOM_LEAVE_USER_NTF:
                var leaveUser = UnsafeCode.ByteArrayToStructure<P_RoomLeaveUserNotify>(packet.data);
                if (leaveUser.userUUID == otherSlot.userUUID)
                {
                    otherSlot.InitEmpty();
                }

                if (playerInfoUI != null) playerInfoUI.SetInteractable(true);
                break;
            case E_PACKET.ROOM_READY_STATUS_NTF:
                {
                    var readyStatus = UnsafeCode.ByteArrayToStructure<P_RoomReadyStatusNtf>(packet.data);

                    userReadyStates[readyStatus.userUUID] = readyStatus.isReady;

                    if (readyStatus.userUUID == otherSlot.userUUID)
                    {
                        otherSlot.SetReady(readyStatus.isReady);
                    }
                    else if (readyStatus.userUUID == mySlot.userUUID)
                    {
                        mySlot.SetReady(readyStatus.isReady);
                    }
                    break;
                }
            case E_PACKET.ROOM_HOST_NTF:
                {
                    var hostNtf = UnsafeCode.ByteArrayToStructure<P_RoomHostNtf>(packet.data);
                    currentHostUUID = hostNtf.hostUUID;

                    mySlot.SetHost(mySlot.userUUID == currentHostUUID);
                    otherSlot.SetHost(otherSlot.userUUID == currentHostUUID);
                    UpdateBottomButton();

                    bool isHost = (mySlot.userUUID == currentHostUUID);
                    if (playerInfoUI != null) playerInfoUI.SetInteractable(isHost);
                    break;
                }
            case E_PACKET.ROOM_LEAVE_RESPONSE:
                waitingRoomPanel.SetActive(false);
                lobbyPanel.SetActive(true);
                FindObjectOfType<LobbyRoomManager>()?.RequestRoomList();
                break;
            case E_PACKET.ROOM_CHAR_SELECT_NTF:
                var charNtf = UnsafeCode.ByteArrayToStructure<P_RoomCharSelectNtf>(packet.data);

                if (charNtf.userUUID == currentHostUUID && currentHostUUID != LocalPlayerInfo.ID)
                {
                    int forceCharID = (charNtf.charID == 0) ? 1 : 0; // 방장이 0이면 난 1

                    if (playerInfoUI != null)
                    {
                        playerInfoUI.ForceSetCharacter(forceCharID);
                    }
                    Debug.Log($"[Lobby] 방장이 {charNtf.charID}번, {forceCharID}번으로 자동 전환됨.");
                }
                break;
            case E_PACKET.MATCH_START_NTF:
                var matchStart = UnsafeCode.ByteArrayToStructure<P_MatchStartNtf>(packet.data);
                Debug.Log($"[Handover] 게임 서버로 이사갑니다! 포트: {matchStart.GameServerPort}, 토큰: {matchStart.AuthToken}");

                // TODO: 로딩씬 띄우기 -> 로비 소켓 Disconnect -> 게임 서버 포트로 재접속 -> 토큰 전송
                break;
        }
    }

    private void UpdateBottomButton()
    {
        if (mySlot.userUUID == currentHostUUID)
        {
            myReadyButtonText.text = isMyReady ? "시작 대기중..." : "게임 시작";
        }
        else
        {
            myReadyButtonText.text = isMyReady ? "준비 완료" : "준비 하기";
        }
    }
}