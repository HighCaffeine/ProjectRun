using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using TMPro;
using System;
using NUnit.Framework.Internal.Filters;

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
    public Dictionary<int, MonsterActor> _monsterCache = new Dictionary<int, MonsterActor>();

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

        if (isDebugMode)
        {
            AddPlayer(LocalPlayerInfo.ID, LocalPlayerInfo.Name, Vector3.zero);
        }
        else
        {
            P_GameAuthReq authReq = new P_GameAuthReq();
            authReq.AuthToken = LocalPlayerInfo.AuthToken;
            Client.TCP.SendPacket2(E_PACKET.GAME_AUTH_REQUEST, authReq);
            Debug.Log("[Match] 게임 서버(11021)에 인증 토큰 제출 승인을 기다립니다...");
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
            case E_PACKET.SYS_TIME_SYNC_RES:
                {
                    var resPkt = UnsafeCode.ByteArrayToStructure<P_TimeSyncRes>(packet.data);

                    if (NetworkTimeManager.Instance != null)
                    {
                        NetworkTimeManager.Instance.OnReceiveTimeSyncResponse(resPkt.clientTimestamp, resPkt.serverTimestamp);
                    }
                    break;
                }

            case E_PACKET.GAME_AUTH_RESPONSE:
                var authRes = UnsafeCode.ByteArrayToStructure<P_GameAuthRes>(packet.data);

                if (authRes.Result != 9999)
                {
                    LocalPlayerInfo.ID = authRes.Result;

                    Debug.Log($"<color=green>[Match] 서버 인증 성공! 새 Session ID: {LocalPlayerInfo.ID}</color>");

                    AddPlayer(LocalPlayerInfo.ID, LocalPlayerInfo.Name, Vector3.zero);
                }
                else
                {
                    Debug.LogError("[Match] 토큰 인증 실패");
                    UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Main_Lobby");
                }
                break;

            case E_PACKET.ROOM_ENTER_RESPONSE:
                P_RoomEnterResponse roomEnterResponse = UnsafeCode.ByteArrayToStructure<P_RoomEnterResponse>(packet.data);
                //Debug.Log($"ROOM_ENTER_RESPONSE result={roomEnterResponse.result}");
                break;

            case E_PACKET.ROOM_NEW_USER_NTF:
                var newUser = UnsafeCode.ByteArrayToStructure<P_RoomNewUserNotify>(packet.data);
                if (newUser.userUUID != LocalPlayerInfo.ID)
                {
                    AddPlayer(newUser.userUUID, newUser.userName, Vector3.zero);
                    Debug.Log($"[Match] 신규 유저 스폰 완료: {newUser.userName}");
                }
                break;

            case E_PACKET.ROOM_USER_INFO_NTF:
                var userInfo = UnsafeCode.ByteArrayToStructure<P_RoomUserInfoNotify>(packet.data);
                if (userInfo.userUUID != LocalPlayerInfo.ID)
                {
                    AddPlayer(userInfo.userUUID, userInfo.userName, userInfo.position.ToVector3());
                    Debug.Log($"[Match] 기존 유저 스폰 완료: {userInfo.userName}");
                }
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
                var leaveUser = UnsafeCode.ByteArrayToStructure<P_RoomLeaveUserNotify>(packet.data);
                RemovePlayer(leaveUser.userUUID);
                break;

            case E_PACKET.ROOM_HOST_NTF:
                {
                    if (GameManager.Instance == null) break;
                    var hostPkt = UnsafeCode.ByteArrayToStructure<P_RoomHostNtf>(packet.data);

                    // 서버가 지정한 방장 UUID가 내 UUID와 같다면 나는 방장
                    GameManager.Instance.isHost = (hostPkt.hostUUID == LocalPlayerInfo.ID);

                    Debug.Log($"[Room Host NTF]{hostPkt.hostUUID}");
                    Debug.Log($"[Room Host NTF]{LocalPlayerInfo.ID}");

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
                {
                    var updatePkt = UnsafeCode.ByteArrayToStructure<P_UpdatePlayerMovement>(packet.data);

                    if (Players.TryGetValue(updatePkt.userUUID, out Player player))
                    {
                        if (updatePkt.userUUID == LocalPlayerInfo.ID)
                        {
                            player.serverPos = updatePkt.currentPos.ToVector3();
                        }
                        else
                        {
                            // 다른 플레이어는 정상적으로 부드럽게 동기화
                            player.OnSyncMovement(updatePkt);
                        }
                    }
                    break;
                }
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
                                                                                                        //case 4: pActor.sm.ChangeState(new DashState(pActor)); break;      // 대쉬
                            case 5:
                                if (Time.time - pActor.lastKnockbackTime < PlayerActor.KNOCKBACK_IMMUNE_TIME)
                                {
                                    break;
                                }

                                // 지연시간 검사
                                long myCurrentTime = NetworkTimeManager.Instance.GetServerTime();
                                float latency = Mathf.Max(0f, (myCurrentTime - statePkt.timestamp) / 1000f);

                                if (latency > 0.5f)
                                {
                                    break;
                                }

                                Vector3 cPos = new Vector3(statePkt.casterPos.x, statePkt.casterPos.y, statePkt.casterPos.z);
                                float currentDist = Vector3.Distance(pActor.transform.position, cPos);
                                float maxValidRange = (statePkt.isPull == 1) ? 15.0f : 5.0f;
                                bool pullFlag = (statePkt.isPull == 1);

                                if (currentDist > maxValidRange)
                                {
                                    break;
                                }

                                pActor.lastKnockbackTime = Time.time;
                                pActor.sm.ChangeState(new KnockbackState(pActor, statePkt.targetDir.ToVector3(), statePkt.param, pullFlag, cPos, latency));
                                break;
                            case 6:
                                if (pActor.ignoreServerPosTimer > 0) return;
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

                    //Debug.Log($"[GIMMICK RAW] ID={ntf.gimmickID}, key={ntf.gimmickKey}, state={ntf.state}");
                    // Next 구역 텔레포트

                    Debug.Log($"<color=yellow>[GIMMICK RAW]</color> 수신된 ID: {ntf.gimmickID}, State: {ntf.state}");
                    if (ntf.state == 2 && ntf.gimmickKey == (byte)eGimmickKey.NextZone)
                    {
                        if (Players.TryGetValue(ntf.activeUUID, out Player targetPlayer))
                        {
                            // float에서 mapID와 sectorIndex 디코딩
                            int encodedValue = (int)ntf.param;
                            int mapID = encodedValue / 100;
                            int nextSpawnIndex = encodedValue % 100;

                            Vector3 destPos = DungeonPointManager.Instance.GetSpawnPosition(mapID, nextSpawnIndex);
                            CharacterController controller = targetPlayer.GetComponent<CharacterController>();
                            if (controller != null) controller.enabled = false;

                            targetPlayer.transform.position = destPos;
                            Physics.SyncTransforms();
                            targetPlayer.SetPos(destPos);
                            if (controller != null) controller.enabled = true;

                            PlayerActor pActor = targetPlayer.GetComponent<PlayerActor>();
                            if (pActor != null)
                            {
                                pActor.ignoreServerPosTimer = 0.5f;
                                pActor.OnUpdatePoint?.Invoke(targetPlayer.gameObject.name, nextSpawnIndex);
                            }

                            Debug.Log($"[NextZone] Player {ntf.activeUUID} teleported to Map{mapID}_Sector{nextSpawnIndex}");
                        }
                        break;
                    }

                    if (ntf.gimmickKey == (byte)eGimmickKey.BreakableWall)
                    {
                        //Debug.Log($"<color=cyan> [Match packet ntf]{ntf.gimmickID} ({(eGimmickKey)ntf.gimmickKey}");
                    }

                    //Debug.Log($"[GIMMICK NTF] {ntf.gimmickID} ({(eGimmickKey)ntf.gimmickKey})");

                    if (_gimmickCache.TryGetValue(ntf.gimmickID, out var targetGimmick))
                    {
                        targetGimmick.Execute(ntf);
                    }
                    else
                    {
                        Debug.LogError($"<color=red>[GIMMICK 에러]</color> ID {ntf.gimmickID} 없음");
                    }

                    // BaseGimmick[] allGimmicks = FindObjectsByType<BaseGimmick>(FindObjectsSortMode.None);
                    // foreach (var gimmick in allGimmicks)
                    // {
                    //     if (gimmick.gimmickUID == ntf.gimmickID)
                    //     {
                    //         gimmick.Execute(ntf);
                    //         break;
                    //     }
                    // }
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

                            PlayerActor pActor = targetPlayer.GetComponent<PlayerActor>();
                            if (pActor != null)
                            {
                                pActor.PlayerDead(respawnPos, ActorManager.Instance.spawnDelay);
                            }

                            targetPlayer.SetPos(respawnPos);

                            Debug.Log($"[System] 다른 유저({ntf.userUUID}) 사망 및 부활 코루틴 실행");
                        }
                    }
                    break;
                }
            case E_PACKET.DUNGEON_CLEAR_NTF:
                {
                    Debug.Log("<color=cyan>[System] 던전 클리어</color>");

                    GameManager.Instance.Invoke("LoadLobby", 10.0f);
                    break;
                }
            // case E_PACKET.MOVE_PATH_RESPONSE:
            //     P_MovePathResponse movePath = UnsafeCode.ByteArrayToStructure<P_MovePathResponse>(packet.data);

            //     if (PathVisualizer.Instance != null)
            //     {
            //         PathVisualizer.Instance.OnReceivePathPacket(movePath.path_count, movePath.path);
            //         Debug.Log($"MOVE_PATH_RESPONSE pathCount={movePath.path_count}");
            //         for (int i = 0; i < movePath.path_count; i++)
            //         {
            //             Debug.Log($"MOVE_PATH_RESPONSE path[{i}]=({movePath.path[i]})");
            //         }
            //     }
            //     break;
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
            case E_PACKET.MONSTER_MOVEMENT:
                {
                    var pkt = UnsafeCode.ByteArrayToStructure<P_MonsterMovement>(packet.data);
                    if (_monsterCache.TryGetValue(pkt.monsterID, out MonsterActor targetMonster))
                    {
                        targetMonster.OnSyncMovement(pkt.currentPos.ToVector3(), pkt.currentRot.ToQuaternion());
                    }
                    break;
                }

            case E_PACKET.MONSTER_DEAD_NTF:
                {
                    var pkt = UnsafeCode.ByteArrayToStructure<P_MonsterDeadNtf>(packet.data);

                    if (_monsterCache.TryGetValue(pkt.monsterID, out MonsterActor targetMonster))
                    {
                        Players.TryGetValue(pkt.userUUID, out Player p);
                        Vector3 dir = targetMonster.transform.position - p.transform.position;

                        targetMonster.ExecuteMonsterDead(dir);
                        _monsterCache.Remove(pkt.monsterID);
                    }
                    break;
                }
            case E_PACKET.MONSTER_STATE_NTF:
                {
                    var statePkt = UnsafeCode.ByteArrayToStructure<P_MonsterStateNtf>(packet.data);

                    if (_monsterCache.TryGetValue(statePkt.monsterID, out MonsterActor actor))
                    {
                        if (actor == null) break;

                        //5번의 경우 스킵 X
                        if (actor.IsLocal && statePkt.newState != 5) break;

                        switch (statePkt.newState)
                        {
                            case 0: actor.sm.ChangeState(new IdleState(actor)); break;
                            case 1: actor.sm.ChangeState(new MoveState(actor)); break;
                            case 2: actor.sm.ChangeState(new ActionState(actor, eState.Push)); break; // 밀기
                                                                                                      //case 3: actor.sm.ChangeState(new ActionState(actor, eState.Pull)); break; // 당기기
                            case 5:
                                if (Time.time - actor.lastKnockbackTime < PlayerActor.KNOCKBACK_IMMUNE_TIME)
                                {
                                    break;
                                }

                                actor.lastKnockbackTime = Time.time;

                                bool pullFlag = (statePkt.isPull == 1);
                                Vector3 cPos = new Vector3(statePkt.casterPos.x, statePkt.casterPos.y, statePkt.casterPos.z);

                                actor.sm.ChangeState(new KnockbackState(actor, statePkt.targetDir.ToVector3(), statePkt.param, pullFlag, cPos));

                                break;
                        }
                    }
                    break;
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
                pActor.isLocal = local;
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
                cameraPivot.SetParent(playerObj.transform);
                cameraPivot.position = pActor.transform.position;
                cameraPivot.gameObject.SetActive(true);
                // if (DashCameraEffect.Instance != null)
                // {
                //     DashCameraEffect.Instance.InitSetup(pActor.transform);
                //     CameraManager.Instance.SetupDashEffectComp(pActor);
                //     CameraManager.Instance.AddPosContraint(pActor.transform);
                // }
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
                // 충돌 꼬임 방지를 위해 콜라이더 제거
                Collider[] cols = playerObj.GetComponents<Collider>();
                foreach (Collider c in cols)
                {
                    if (c.GetType() != typeof(CharacterController)) Destroy(c);
                }
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");
                //Rigidbody rb = playerObj.GetComponent<Rigidbody>();
                //if (rb != null) Destroy(rb);
                Debug.Log($"<color=cyan>AddPlayer_{debugIndex++}</color>");

                ActorManager.Instance.p1 = pActor;

                CameraOcclusionSingleLayerFader co = GetComponent<CameraOcclusionSingleLayerFader>();
                co.SetPlayer(pActor.transform);
            }
            else // 리모트 플레이어
            {
                //임시 테스트 is2p
                pActor.is2p = true;

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
                ActorManager.Instance.p2 = pActor;

            }

            Debug.Log($"<color=yellow>[AddPlayer] id={id}, LocalID={LocalPlayerInfo.ID}, isLocal={local}</color>");
            pActor.isLocal = local;

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

        Vector3 spawnPos = DungeonPointManager.Instance.GetSpawnPosition(DungeonPointManager.Instance.currentMapID, sectorIndex);
        Player p = AddPlayer(LocalPlayerInfo.ID, LocalPlayerInfo.Name, spawnPos);

        if (Client.IS_SERVER_PLAY)
        {
            PlayerActor pActor = p.GetComponent<PlayerActor>();

            P_SceneSyncReq syncReq = new P_SceneSyncReq();
            Client.TCP.SendPacket2(E_PACKET.SCENE_SYNC_REQ, syncReq);
            Debug.Log("<color=cyan>1. [요청] 서버로 SCENE_SYNC_REQ 보냄</color>");

            pActor.SetControllerActive(false);
            pActor.transform.position = spawnPos;
            pActor.SetControllerActive(true);

            Physics.SyncTransforms();

            if (pActor.IsLocal)
            {
                StartCoroutine(SendInitialPositionDelay(pActor));
            }
        }
    }

    private IEnumerator SendInitialPositionDelay(PlayerActor pActor)
    {
        yield return new WaitForFixedUpdate();

        pActor.SendMovePacket(0.0f, 0.0f);
        Debug.Log($"<color=green>[스폰 완료] 내 초기 위치 서버로 동기화 완료: {pActor.transform.position}</color>");
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
