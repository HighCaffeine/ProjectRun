using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;
using System;

public unsafe class Match : MonoBehaviour, IPacketReceiver
{
    [Header("UI Settings")]
    public TextMeshProUGUI countdownText;
    [Header("Debug Settings")]
    public bool isDebugMode = false;

    public static Match Instance;
    public Dictionary<long, Player> Players;

    public Transform cameraDefaultPos;

    [Header("Player Settings")]
    public GameObject playerPrefab;
    public Transform cameraPivot;

    private Dictionary<int, BaseGimmick> _gimmickCache = new Dictionary<int, BaseGimmick>();


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
        foreach (var g in FindObjectsByType<BaseGimmick>(FindObjectsSortMode.None))
        {
            _gimmickCache[g.gimmickUID] = g;
        }
        if (!Client.IS_SERVER_PLAY || isDebugMode)
        {
            Debug.Log("[Debug] 서버 연결 없이 로컬 테스트 모드로 시작합니다.");

            // 던전 씬이라면 0번 위치에 즉시 스폰
            if (DungeonPointManager.Instance != null)
            {
                SpawnLocalPlayer(0);
            }
            else
            {
                AddPlayer(LocalPlayerInfo.ID, "a", Vector3.zero);
            }
            return; // 서버 입장 요청 패킷을 보내지 않고 여기서 종료
        }

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentSceneName.StartsWith("Dungeon"))
        {
            Debug.Log("[Match] 던전 씬 컷씬 종료 대기");
        }
        else
        {
            Debug.Log("[Match] 마을 씬 감지. 기본 위치에서 스폰합니다.");
            AddPlayer(LocalPlayerInfo.ID, LocalPlayerInfo.Name, Vector3.zero);

            if (Client.IS_SERVER_PLAY)
            {
                P_RoomEnterRequest request = new P_RoomEnterRequest { roomNumber = 0 };
                Client.TCP.SendPacket2(E_PACKET.ROOM_ENTER_REQUEST, request);
            }
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
                //Debug.Log($"ROOM_ENTER_RESPONSE result={roomEnterResponse.result}");
                break;

            case E_PACKET.ROOM_NEW_USER_NTF:
                {
                    P_RoomNewUserNotify roomNewUserNotify = UnsafeCode.ByteArrayToStructure<P_RoomNewUserNotify>(packet.data);
                    Vector3 pos = DungeonPointManager.Instance != null ? DungeonPointManager.Instance.GetSpawnPosition(0) : Vector3.zero;
                    AddPlayer(roomNewUserNotify.userUUID, roomNewUserNotify.userName, pos);
                    //Debug.Log($"Player {roomNewUserNotify.userName} has joined");
                    break;
                }
            case E_PACKET.ROOM_USER_INFO_NTF:
                {
                    P_RoomUserInfoNotify roomUserInfoNotify = UnsafeCode.ByteArrayToStructure<P_RoomUserInfoNotify>(packet.data);
                    Vector3 pos = roomUserInfoNotify.position.ToVector3();
                    Player newPlayer = AddPlayer(roomUserInfoNotify.userUUID, roomUserInfoNotify.userName, pos);

                    if (newPlayer != null)
                    {
                        newPlayer.transform.position = roomUserInfoNotify.position.ToVector3();
                        newPlayer.transform.rotation = roomUserInfoNotify.rotation.ToQuaternion();

                        newPlayer.SetPos(roomUserInfoNotify.position.ToVector3());
                    }
                    //Debug.Log($"[AOI] Spawn User {roomUserInfoNotify.userUUID}");
                    break;
                }
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
                RemovePlayer(roomLeaveUserNotify.userUUID);
                break;

            case E_PACKET.ROOM_HOST_NTF:
                {
                    if (GameManager.Instance == null) break;
                    var hostPkt = UnsafeCode.ByteArrayToStructure<P_RoomHostNtf>(packet.data);

                    // 서버가 지정한 방장 UUID가 내 UUID와 같다면 나는 방장
                    GameManager.Instance.isHost = (hostPkt.hostUUID == LocalPlayerInfo.ID);

                    if (GameManager.Instance.isHost)
                    {
                        Debug.Log("[System] 호스트가 되었습니다");
                        // TODO: UI 매니저 호출해서 [게임 시작] 버튼 활성화
                    }
                    else
                    {
                        Debug.Log($"[System]현재 방장 {hostPkt.hostUUID} ");
                        // TODO: [게임 시작] 버튼 비활성화 (대기 상태)
                    }
                    break;
                }

            case E_PACKET.GAME_START_COUNTDOWN_NTF:
                {
                    var pkt = UnsafeCode.ByteArrayToStructure<P_GameStartCountdownNtf>(packet.data);
                    Debug.Log($"[System] {pkt.remainSeconds}초 뒤 던전으로 출발합니다");
                    // TODO: 화면 중앙에 숫자 텍스트 표시
                    if (countdownText != null)
                    {
                        countdownText.gameObject.SetActive(true);
                        countdownText.text = pkt.remainSeconds.ToString();
                    }
                    break;
                }

            case E_PACKET.GAME_READY_CANCEL_NTF:
                {
                    Debug.Log("[System] 플레이어가 준비 구역을 이탈하여 취소되었습니다.");
                    // TODO: 카운트다운 UI 숨기기
                    if (countdownText != null)
                    {
                        countdownText.gameObject.SetActive(false);
                    }
                    break;
                }

            case E_PACKET.GAME_START_NTF:
                {
                    var pkt = UnsafeCode.ByteArrayToStructure<P_GameStartNtf>(packet.data);
                    Debug.Log("[System] 던전 입장");

                    Players.Clear();
                    if (countdownText != null)
                    {
                        countdownText.gameObject.SetActive(false);
                    }

                    if (pkt.mapId == 0)
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Game_Lobby");
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Dungeon_1");
                        //UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Dungeon_" + pkt.mapId);
                    }
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
                        if (pActor == null) break;

                        //5번의 경우 스킵 X
                        if (pActor.IsLocal && statePkt.newState != 5) break;

                        switch (statePkt.newState)
                        {
                            case 0: pActor.sm.ChangeState(new IdleState(pActor)); break;
                            case 1: pActor.sm.ChangeState(new MoveState(pActor)); break;
                            case 2: pActor.sm.ChangeState(new ActionState(pActor, eState.Push)); break; // 밀기
                            case 3: pActor.sm.ChangeState(new ActionState(pActor, eState.Pull)); break; // 당기기
                            case 4: pActor.sm.ChangeState(new DashState(pActor)); break;      // 대쉬
                            case 5:
                                pActor.sm.ChangeState(new KnockbackState(pActor, statePkt.targetDir.ToVector3(), statePkt.param, false, Vector3.zero));
                                break;
                            case 6:
                                pActor.sm.ChangeState(new TeleportState(pActor, statePkt.targetDir.ToVector3()));
                                break;
                        }
                    }
                    break;
                }
            // case E_PACKET.PLAYER_ACTION_NTF:
            //     {
            //         var actionNtf = UnsafeCode.ByteArrayToStructure<P_PlayerActionNtf>(packet.data);

            //         // 맞은 유저 찾기
            //         if (Players.TryGetValue(actionNtf.targetUUID, out Player targetPlayer))
            //         {
            //             // attacker 찾기
            //             if (Players.TryGetValue(actionNtf.attackerUUID, out Player attackerPlayer))
            //             {
            //                 // 넉백 적용
            //                 targetPlayer.ApplyKnockback(attackerPlayer.transform.position, actionNtf.actionType);
            //             }
            //         }
            //         break;
            //     }
            case E_PACKET.GIMMICK_INTERACT_NTF:
                {
                    var ntf = UnsafeCode.ByteArrayToStructure<P_GimmickInteractNtf>(packet.data);

                    // Next 구역 텔레포트
                    if (ntf.state == 2 && ntf.gimmickKey == (byte)eGimmickKey.NextZone) // 예외 처리용
                    {
                        if (Players.TryGetValue(ntf.activeUUID, out Player targetPlayer))
                        {
                            Vector3 destPos = ntf.targetPos.ToVector3();

                            CharacterController controller = targetPlayer.GetComponent<CharacterController>();
                            if (controller != null) controller.enabled = false;

                            targetPlayer.transform.position = destPos;
                            targetPlayer.SetPos(destPos);

                            if (controller != null) controller.enabled = true;

                            PlayerActor pActor = targetPlayer.GetComponent<PlayerActor>();
                            if (pActor != null)
                            {
                                int nextSpawnIndex = (int)ntf.param;
                                pActor.OnUpdatePoint?.Invoke(targetPlayer.gameObject.name, nextSpawnIndex);
                            }
                        }
                        break;
                    }

                    BaseGimmick[] allGimmicks = FindObjectsByType<BaseGimmick>(FindObjectsSortMode.None);
                    foreach (var gimmick in allGimmicks)
                    {
                        if (gimmick.gimmickUID == ntf.gimmickID)
                        {
                            gimmick.Execute(ntf);
                            break;
                        }
                    }
                    break;
                }
            case E_PACKET.PLAYER_DEAD_NTF:
                {
                    var ntf = UnsafeCode.ByteArrayToStructure<P_PlayerDeadNtf>(packet.data);

                    if (Players.TryGetValue(ntf.userUUID, out Player targetPlayer))
                    {
                        // 내 캐릭터는 ActorManager에서 이미 처리했으므로 리모트 유저만 처리
                        if (ntf.userUUID != LocalPlayerInfo.ID)
                        {
                            Vector3 respawnPos = ntf.respawnPos.ToVector3();

                            CharacterController cc = targetPlayer.GetComponent<CharacterController>();
                            if (cc != null) cc.enabled = false;

                            targetPlayer.transform.position = respawnPos;
                            targetPlayer.SetPos(respawnPos); // Player 보정 클래스 위치도 덮어쓰기

                            if (cc != null) cc.enabled = true;

                            Debug.Log($"[System] 다른 유저({ntf.userUUID})가 부활 위치로 강제 이동됨");
                        }
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
                if (GameManager.Instance == null) break;
                if (ShopManager.Instance == null)
                {
                    Debug.LogWarning("ShopManager가 없어서 패킷을 무시합니다.");
                    break;
                }
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

    private Player AddPlayer(long id, string playerName, Vector3 spawnPos)
    {
        if (Players == null || Players.ContainsKey(id)) return null;

        bool local = LocalPlayerInfo.ID == id;

        GameObject playerObj;

        try
        {


            if (playerPrefab != null)
            {
                playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObj.transform.position = spawnPos;
            }

            int debugIndex = 0;
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

            Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");

            if (local)
            {
                CharacterController cc = playerObj.GetComponent<CharacterController>();
                if (cc == null) cc = playerObj.AddComponent<CharacterController>();
                cc.radius = 0.5f;
                cc.height = 2.0f;
                cc.center = Vector3.zero;
                cc.stepOffset = 0.5f;
                cc.center = new Vector3(0.0f, 1.0f, 0.0f);
                cc.slopeLimit = 60f;
                pActor.SetController(cc);
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
                pActor.SetPlayerPivot(playerObj.transform.GetChild(0));

                //카메라 세팅
                //cameraPivot.SetParent(playerObj.transform);
                cameraPivot.position = pActor.transform.position;
                cameraPivot.gameObject.SetActive(true);
                if (DashCameraEffect.Instance != null)
                {
                    DashCameraEffect.Instance.InitSetup(pActor.transform);
                    CameraManager.Instance.SetupDashEffectComp(pActor);
                    CameraManager.Instance.AddPosContraint(pActor.transform);
                }
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
                // 충돌 꼬임 방지를 위해 콜라이더 제거
                Collider[] cols = playerObj.GetComponents<Collider>();
                foreach (Collider c in cols)
                {
                    if (c.GetType() != typeof(CharacterController)) Destroy(c);
                }
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
                Rigidbody rb = playerObj.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
            }
            else // 리모트 플레이어
            {
                Collider[] existingCols = playerObj.GetComponents<Collider>();
                foreach (var c in existingCols) Destroy(c);
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
                CharacterController cc = playerObj.GetComponent<CharacterController>();
                if (cc != null) Destroy(cc);
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
                CapsuleCollider col = playerObj.AddComponent<CapsuleCollider>();
                col.isTrigger = true;
                col.radius = 0.5f;
                col.height = 2f;
                col.center = new Vector3(0, 1f, 0);
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
                Rigidbody rb = playerObj.GetComponent<Rigidbody>();
                if (rb == null) rb = playerObj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
            }

            Player player = playerObj.GetComponent<Player>();
            if (player == null) player = playerObj.AddComponent<Player>();
            player.Init(pActor, playerName, id, local, spawnPos);
            Players.Add(id, player);
            Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
            return player;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AddPlayer CRASH] {e.StackTrace}");
            return null;
        }
    }

    public void SpawnLocalPlayer(int sectorIndex)
    {
        if (DungeonPointManager.Instance == null) return;

        Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(sectorIndex);
        AddPlayer(LocalPlayerInfo.ID, LocalPlayerInfo.Name, spawnPos);

        if (Client.IS_SERVER_PLAY)
        {
            P_SceneSyncReq syncReq = new P_SceneSyncReq();
            Client.TCP.SendPacket2(E_PACKET.SCENE_SYNC_REQ, syncReq);
            Debug.Log("<color=cyan>1. [요청] 서버로 SCENE_SYNC_REQ 보냄</color>");
        }
    }

    private void RemovePlayer(long id)
    {
        if (Players != null && Players.TryGetValue(id, out Player player) && player != null)
        {
            Debug.Log($"<color=orange>[Match]</color> 유저 퇴장 및 삭제 완료 (UUID: {id})");
            Destroy(player.gameObject);
            Players.Remove(id);
        }
        else
        {
            Debug.LogWarning($"<color=red>[Match]</color> 삭제하려는 유저를 찾을 수 없습니다. (UUID: {id})");
        }
    }

    // 공통 베이스 스크립트 사용
    // private void FindGimmickAndExecute<T>(int id, Action<T> action) where T : MonoBehaviour
    // {
    //     T[] gimmicks = FindObjectsByType<T>(FindObjectsSortMode.None);

    //     foreach (var g in gimmicks)
    //     {
    //         // 각 스크립트에 gimmickUID 변수가 있어야 함
    //         var prop = g.GetType().GetField("gimmickUID");
    //         if (prop != null && (int)prop.GetValue(g) == id)
    //         {
    //             action(g);
    //             return;
    //         }
    //     }
    // }
}
