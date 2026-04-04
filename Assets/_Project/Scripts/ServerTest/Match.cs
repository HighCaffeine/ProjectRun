using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public unsafe class Match : MonoBehaviour, IPacketReceiver
{
    public static Match Instance;
    public Dictionary<long, Player> Players;

    public Transform cameraDefaultPos;

    [Header("Player Settings")]
    public GameObject playerPrefab;
    public Camera mainCamera;
    public Transform cameraPivot;

    void Awake()
    {
        Debug.Log("Match started");
        Instance = this;
        Players = new Dictionary<long, Player>();
        Client.TCP.AddPacketReceiver(this);
        Client.UDP.AddPacketReceiver(this);
    }

    void Start()
    {
        AddPlayer(LocalPlayerInfo.ID, LocalPlayerInfo.Name);

        if (!Client.IS_SERVER_PLAY)
        {
            //오프라인용 더미
            Player dummy = AddPlayer(9999, "Dummy_Sandbag");

            if (dummy != null) dummy.transform.position = new Vector3(3, 1, 3);
        }
        else
        {
            P_RoomEnterRequest request = new P_RoomEnterRequest()
            {
                roomNumber = 0
            };
            Client.TCP.SendPacket2(E_PACKET.ROOM_ENTER_REQUEST, request);
        }
    }

    // private void OnGUI()
    // {
    //     foreach (Player player in Players.Values)
    //     {
    //         if (player.ID != LocalPlayerInfo.ID)
    //         {
    //             Vector3 scpos = GameObject.Find("Player Camera").GetComponent<Camera>().WorldToScreenPoint(player.transform.position);
    //             if (scpos.z > 0)
    //             {
    //                 GUI.contentColor = Color.cyan;
    //                 GUI.Label(new Rect(scpos.x, Screen.height - scpos.y, 100, 25), player.Name);
    //             }
    //         }
    //     }
    // }

    void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
        Client.UDP.RemovePacketReceiver(this);
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;
        string msg = string.Empty;
        switch ((E_PACKET)packetId)
        {
            case E_PACKET.ROOM_ENTER_RESPONSE:
                P_RoomEnterResponse roomEnterResponse = UnsafeCode.ByteArrayToStructure<P_RoomEnterResponse>(packet.data);
                Debug.Log($"ROOM_ENTER_RESPONSE result={roomEnterResponse.result}");
                break;

            case E_PACKET.ROOM_NEW_USER_NTF:
                P_RoomNewUserNotify roomNewUserNotify = UnsafeCode.ByteArrayToStructure<P_RoomNewUserNotify>(packet.data);
                AddPlayer(roomNewUserNotify.userUUID, roomNewUserNotify.userName);
                Debug.Log($"Player {roomNewUserNotify.userName} has joined");
                break;

            case E_PACKET.ROOM_USER_INFO_NTF:
                P_RoomUserInfoNotify roomUserInfoNotify = UnsafeCode.ByteArrayToStructure<P_RoomUserInfoNotify>(packet.data);
                Player newPlayer = AddPlayer(roomUserInfoNotify.userUUID, roomUserInfoNotify.userName);

                if (newPlayer != null)
                {
                    newPlayer.transform.position = roomUserInfoNotify.position.ToVector3();
                    newPlayer.transform.rotation = roomUserInfoNotify.rotation.ToQuaternion();

                    newPlayer.SetPos(roomUserInfoNotify.position.ToVector3());
                }
                Debug.Log($"[AOI] Spawn User {roomUserInfoNotify.userUUID}");
                break;
            // case E_PACKET.ROOM_USER_INFO_NTF:
            //     P_RoomUserInfoNotify roomUserInfoNotify = UnsafeCode.ByteArrayToStructure<P_RoomUserInfoNotify>(packet.data);
            //     Player newPlayer = AddPlayer(roomUserInfoNotify.userUUID, roomUserInfoNotify.userName);
            //     Debug.Log($"[Packet Raw] Server Sent X: {roomUserInfoNotify.position.x}");
            //     if (newPlayer != null)
            //     {
            //         Vector3 spawnPos = roomUserInfoNotify.position.ToVector3();
            //         CharacterController cc = newPlayer.GetComponent<CharacterController>();

            //         if (cc != null) cc.enabled = false;

            //         newPlayer.transform.position = spawnPos;
            //         newPlayer.serverPos = spawnPos;
            //         newPlayer.transform.rotation = roomUserInfoNotify.rotation.ToQuaternion();

            //         if (cc != null) cc.enabled = true;
            //     }
            //     break;

            case E_PACKET.ROOM_LEAVE_USER_NTF:
                P_RoomLeaveUserNotify roomLeaveUserNotify = UnsafeCode.ByteArrayToStructure<P_RoomLeaveUserNotify>(packet.data);

                //버그로 우선 주석처리
                //RemovePlayer(roomLeaveUserNotify.userUUID);
                break;

            case E_PACKET.ROOM_HOST_NTF:
                {
                    var hostPkt = UnsafeCode.ByteArrayToStructure<P_RoomHostNtf>(packet.data);

                    // 서버가 지정한 방장 UUID가 내 UUID와 같다면 나는 방장!
                    GameManager.Instance.isHost = (hostPkt.hostUUID == LocalPlayerInfo.ID);

                    if (GameManager.Instance.isHost)
                    {
                        Debug.Log("<color=green>[System] 내가 이 방의 호스트(방장)가 되었습니다!</color>");
                        // TODO: UI 매니저 호출해서 [게임 시작] 버튼 활성화
                    }
                    else
                    {
                        Debug.Log($"<color=yellow>[System] 현재 방장은 {hostPkt.hostUUID} 입니다.</color>");
                        // TODO: [게임 시작] 버튼 비활성화 (대기 상태)
                    }
                    break;
                }

            case E_PACKET.GAME_START_COUNTDOWN_NTF:
                {
                    var pkt = UnsafeCode.ByteArrayToStructure<P_GameStartCountdownNtf>(packet.data);
                    Debug.Log($"<color=orange>[System] {pkt.remainSeconds}초 뒤 던전으로 출발합니다!</color>");
                    // TODO: 화면 중앙에 숫자 텍스트 표시
                    break;
                }

            case E_PACKET.GAME_READY_CANCEL_NTF:
                {
                    Debug.Log("<color=red>[System] 누군가 이탈하여 출발이 취소되었습니다.</color>");
                    // TODO: 카운트다운 UI 숨기기
                    break;
                }

            case E_PACKET.GAME_START_NTF:
                {
                    var pkt = UnsafeCode.ByteArrayToStructure<P_GameStartNtf>(packet.data);
                    Debug.Log("<color=green>[System] 던전 입장!</color>");

                    // 비동기 씬 로딩 시작 (던전 씬 이름이 Dungeon_1 구조임)
                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Dungeon_" + pkt.mapId);
                    break;
                }

            case E_PACKET.UPDATE_PLAYER_MOVEMENT:
                // P_UpdatePlayerMovement updateMovement = UnsafeCode.ByteArrayToStructure<P_UpdatePlayerMovement>(packet.data);
                // if (Players.TryGetValue(updateMovement.player_id, out Player player) && player != null)
                // {
                //     player.Movement.Move(updateMovement.motion);
                // }

                var updatePkt = UnsafeCode.ByteArrayToStructure<P_UpdatePlayerMovement>(packet.data);
                if (Players.TryGetValue(updatePkt.userUUID, out Player player))
                {
                    // 서버 확정 데이터 반영
                    player.OnSyncMovement(updatePkt);
                }
                break;
            case E_PACKET.PLAYER_STATUS_NTF:
                {
                    var statePkt = UnsafeCode.ByteArrayToStructure<P_PlayerStatusNtf>(packet.data);

                    if (Players.TryGetValue(statePkt.userUUID, out Player targetPlayer))
                    {
                        PlayerActor pActor = targetPlayer.GetComponent<PlayerActor>();
                        if (pActor == null || pActor.IsLocal) break;

                        // 리모트 플레이어의 상태를 강제로 전환 (애니메이션, 이펙트 동기화)
                        switch (statePkt.newState)
                        {
                            case 0: pActor.sm.ChangeState(new IdleState(pActor)); break;
                            case 1: pActor.sm.ChangeState(new MoveState(pActor)); break;
                            case 2: pActor.sm.ChangeState(new ActionState(pActor, 0)); break; // 밀기
                            case 3: pActor.sm.ChangeState(new ActionState(pActor, 1)); break; // 당기기
                            case 4: pActor.sm.ChangeState(new DashState(pActor)); break;      // 대쉬
                            case 5:
                                pActor.sm.ChangeState(new KnockbackState(pActor, statePkt.targetDir.ToVector3(), statePkt.powerOrTime, false, Vector3.zero));
                                break;
                        }
                    }
                    break;
                }
            case E_PACKET.PLAYER_ACTION_NTF:
                {
                    var actionNtf = UnsafeCode.ByteArrayToStructure<P_PlayerActionNtf>(packet.data);

                    // 맞은 유저 찾기
                    if (Players.TryGetValue(actionNtf.targetUUID, out Player targetPlayer))
                    {
                        // attacker 찾기
                        if (Players.TryGetValue(actionNtf.attackerUUID, out Player attackerPlayer))
                        {
                            // 넉백 적용
                            targetPlayer.ApplyKnockback(attackerPlayer.transform.position, actionNtf.actionType);
                        }
                    }
                    break;
                }
            case E_PACKET.GIMMICK_INTERACT_NTF:
                {
                    var ntf = UnsafeCode.ByteArrayToStructure<P_GimmickInteractNtf>(packet.data);

                    // 맵에 있는 모든 GimmickInfo를 뒤져서 ID가 일치하는 녀석을 찾음
                    GimmickInfo[] allGimmicks = FindObjectsOfType<GimmickInfo>();
                    foreach (GimmickInfo gimmick in allGimmicks)
                    {
                        if (gimmick.gimmick_id == ntf.gimmickID)
                        {
                            // 기믹 종류별 동작
                            if (gimmick.gimmick_type == "breakable_wall" && ntf.state == 0)
                            {
                                Destroy(gimmick.gameObject);
                            }
                            else if (gimmick.gimmick_type == "moving_platform" && ntf.state == 1)
                            {
                                // 플랫폼 이동 (도착 좌표 = ntf.targetPos.ToVector3(), 속도 = ntf.param)
                                // StartCoroutine(MovePlatform(gimmick.transform, ntf.targetPos.ToVector3(), ntf.param));
                            }
                            else if (gimmick.gimmick_type == "magnetic_force" && ntf.state == 1)
                            {
                                // 특정 범위 내 유저 자력으로 당기기 등
                            }
                        }
                    }
                    break;
                }
            case E_PACKET.PLAYER_STATUS_NTF:
                {
                    var statusPkt = UnsafeCode.ByteArrayToStructure<P_PlayerStatusNtf>(packet.data);
                    if (Players.TryGetValue(statusPkt.userUUID, out Player targetPlayer))
                    {
                        // 속도 및 상태 플래그 갱신
                        targetPlayer.SetSpeed(statusPkt.moveSpeed);
                        // UI나 이펙트 처리 로직 추가
                    }
                    break;
                }

            case E_PACKET.DUNGEON_CLEAR_NTF:
                {
                    Debug.Log("<color=cyan>[System] 던전 클리어</color>");

                    // 결과 UI 창 띄우기 (몇 초간 대기)
                    // 비동기 마을 씬 로딩 호출
                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game_Lobby");
                    break;
                }
            case E_PACKET.MOVE_PATH_RESPONSE:
                P_MovePathResponse movePath = UnsafeCode.ByteArrayToStructure<P_MovePathResponse>(packet.data);

                if (PathVisualizer.Instance != null)
                {
                    PathVisualizer.Instance.OnReceivePathPacket(movePath.path_count, movePath.path);
                    Debug.Log($"MOVE_PATH_RESPONSE pathCount={movePath.path_count}");
                    for (int i = 0; i < movePath.path_count; i++)
                    {
                        Debug.Log($"MOVE_PATH_RESPONSE path[{i}]=({movePath.path[i]})");
                    }
                }
                break;
            case E_PACKET.INVENTORY_INFO:
                var invenInfo = UnsafeCode.ByteArrayToStructure<P_InventoryInfo>(packet.data);
                Inventory.Instance.SetInventory(invenInfo.itemIDs);

                Debug.Log("[Inventory] Update");
                break;
            case E_PACKET.SHOP_INFO:
                var shopInfo = UnsafeCode.ByteArrayToStructure<P_ShopInfo>(packet.data);
                ShopManager.Instance.SetTargetShopTime(shopInfo.nextUpdateTime, shopInfo.itemID);

                Debug.Log($"[Shop] Update ItemID: {shopInfo.itemID}, Next Update Time: {shopInfo.nextUpdateTime}");
                break;
            case E_PACKET.SHOP_BUY_RESPONSE:
                var buyInfo = UnsafeCode.ByteArrayToStructure<P_ShopBuyResponse>(packet.data);
                ShopManager.Instance.SetItemBuyState(buyInfo.isSuccess);

                msg = buyInfo.isSuccess ? "Success" : "Failed";

                Debug.Log($"[Shop] Item Buy Result : {msg}");
                break;
            case E_PACKET.TRADE_REQUEST_NTF:
                var reqNtf = UnsafeCode.ByteArrayToStructure<P_TradeRequestNtf>(packet.data);
                TradeManager.Instance.ShowRequestPopup(reqNtf.requesterName, reqNtf.requesterUUID);

                //Debug.Log($"[Trade] Trade Request From {reqNtf.requesterName}({reqNtf.requesterUUID})");
                Chat.Instance.AddMessage($"<color=yellow>[System] Trade Request From {reqNtf.requesterName}({reqNtf.requesterUUID})");
                break;
            case E_PACKET.TRADE_RESPONSE:
                var res = UnsafeCode.ByteArrayToStructure<P_TradeResponse>(packet.data);

                if (res.isAccept == false)
                {
                    Chat.Instance.AddMessage($"<color=yellow>[System] Trade Rejected by {Players[res.requesterUUID]}");
                    if (TradeManager.Instance.tradeReqPanel.activeSelf)
                    {
                        TradeManager.Instance.tradeReqPanel.SetActive(false);
                    }
                }
                break;
            case E_PACKET.TRADE_START_NTF:
                var startNtf = UnsafeCode.ByteArrayToStructure<P_TradeStartNtf>(packet.data);
                TradeManager.Instance.OpenTradeWindow(startNtf.partnerUUID, startNtf.userName);

                Debug.Log($"[Trade] Trade Start With {startNtf.partnerUUID} User");
                break;
            case E_PACKET.TRADE_ITEM_NTF:
                var itemNtf = UnsafeCode.ByteArrayToStructure<P_TradeItemNtf>(packet.data);
                TradeManager.Instance.SetPartnerItem(itemNtf.slotIndex, itemNtf.itemID);

                Debug.Log($"[Trade] Add {itemNtf.itemID} item to Partner {itemNtf.slotIndex} Slot");
                break;
            case E_PACKET.TRADE_LOCK_NTF:
                var lockNtf = UnsafeCode.ByteArrayToStructure<P_TradeLockNtf>(packet.data);
                TradeManager.Instance.SetPartnerLockState(lockNtf.isLocked);
                TradeManager.Instance.CheckConfirmState();

                Debug.Log("[Trade] Partner Trade Lock State: " + lockNtf.isLocked);
                break;
            case E_PACKET.TRADE_CONFIRM_NTF:
                var confirmNtf = UnsafeCode.ByteArrayToStructure<P_TradeConfirmNtf>(packet.data);

                Debug.Log($"[Trade] Partner Confirmed State: {confirmNtf.isConfirmed}");

                Debug.Log($"/{confirmNtf.confirmUserUUID}/, /{LocalPlayerInfo.ID}/");
                if (confirmNtf.confirmUserUUID == LocalPlayerInfo.ID)
                {
                    TradeManager.Instance.SetMyConfirmState(confirmNtf.isConfirmed);
                }
                else
                {
                    TradeManager.Instance.SetPartnerConfirmState(confirmNtf.isConfirmed);
                }

                break;
            case E_PACKET.TRADE_RESULT:
                msg = string.Empty;
                var result = UnsafeCode.ByteArrayToStructure<P_TradeResult>(packet.data);

                if (result.isSuccess)
                {
                    Debug.Log("[Trade] Trade Success");
                    msg = "[Trade] Trade Success";
                    TradeManager.Instance.CloseTradeWindow(msg, true);
                }
                else
                {
                    Debug.Log("[Trade] Trade Fail");
                    msg = "[Trade] Trade Fail";
                    TradeManager.Instance.CloseTradeWindow(msg);
                }
                break;
            default:
                break;

        }
    }

    private Player AddPlayer(long id, string playerName)
    {
        if (Players == null || Players.ContainsKey(id)) return null;

        bool local = LocalPlayerInfo.ID == id;

        GameObject playerObj;
        if (playerPrefab != null)
        {
            playerObj = Instantiate(playerPrefab, new Vector3(0.0f, 1.0f, 0.0f), Quaternion.identity);
        }
        else
        {
            playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.transform.position = new Vector3(0.0f, 1.0f, 0.0f);
        }

        playerObj.name = playerName;

        // if (local)
        // {
        //     //GameObject cameraObject = new GameObject("Main Camera");
        //     //Camera playerCamera = cameraObject.AddComponent<Camera>();
        //     //cameraObject.tag = "MainCamera";
        //     //CameraFollow camFollow = cameraObject.AddComponent<CameraFollow>();
        //     //camFollow.target = playerObj.transform;
        //     //cameraObject.transform.position = playerObj.transform.position + camFollow.offset;
        //     //cameraObject.transform.rotation = Quaternion.Euler(camFollow.lookAngle, 0, 0);
        // }

        // PlayerMovement playerMovement = playerObj.GetComponent<PlayerMovement>();
        // if (playerMovement == null) playerMovement = playerObj.AddComponent<PlayerMovement>();
        // playerMovement.IsLocal = local;

        PlayerActor pActor = playerObj.GetComponent<PlayerActor>();
        if (pActor != null)
        {
            pActor.IsLocal = local;
        }

        if (local)
        {
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null) cc = playerObj.AddComponent<CharacterController>();
            cc.radius = 1f;
            cc.height = 5.5f;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.5f;
            cc.center = new Vector3(0.0f, 2.75f, 0.0f);
            cc.slopeLimit = 60f;
            pActor.SetController(cc);

            pActor.SetPlayerPivot(playerObj.transform.GetChild(0));

            //카메라 세팅
            cameraPivot.SetParent(playerObj.transform);
            cameraPivot.localPosition = Vector3.zero;

            // 충돌 꼬임 방지를 위해 콜라이더 제거
            Collider[] cols = playerObj.GetComponents<Collider>();
            foreach (Collider c in cols)
            {
                if (c.GetType() != typeof(CharacterController)) Destroy(c);
            }

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
        }
        else
        {
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (Client.IS_SERVER_PLAY) if (cc != null) Destroy(cc);

            Collider col = playerObj.GetComponent<Collider>();
            if (col == null) col = playerObj.AddComponent<CapsuleCollider>();
            col.isTrigger = true;

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb == null) rb = playerObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Player player = playerObj.GetComponent<Player>();
        if (player == null) player = playerObj.AddComponent<Player>();
        player.Init(pActor, playerName, id, local, playerObj.transform.position);
        Players.Add(id, player);

        return player;
    }

    private void RemovePlayer(long id)
    {
        if (Players != null && Players.TryGetValue(id, out Player player) && player != null)
        {
            Destroy(player.gameObject);
            Players.Remove(id);
        }
    }
}
