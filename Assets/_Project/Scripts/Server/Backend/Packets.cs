using System.Runtime.InteropServices;
using UnityEngine;

public enum E_PACKET : ushort
{
    // --- 1. System & Time Sync (10 ~ 199) ---
    SYS_USER_CONNECT = 11,
    SYS_USER_DISCONNECT = 12,
    SYS_END = 30,

    SYS_TIME_SYNC_REQ = 101,
    SYS_TIME_SYNC_RES = 102,

    DB_END = 199,

    // --- 2. Lobby & Room Management (200 ~ 239) ---
    LOGIN_REQUEST = 201,
    LOGIN_RESPONSE = 202,
    ROOM_LIST_REQ = 203,
    ROOM_LIST_RES = 204,
    ROOM_ENTER_REQUEST = 206,
    ROOM_ENTER_RESPONSE = 207,
    ROOM_NEW_USER_NTF = 208,
    ROOM_USER_INFO_NTF = 209,

    ROOM_CHAR_SELECT_REQ = 211,
    ROOM_CHAR_SELECT_NTF = 212,

    GAME_START_REQUEST = 215,

    MATCH_START_NTF = 220,
    ROOM_FULL_SYNC_NTF = 222,
    GAME_AUTH_REQUEST = 223,
    GAME_AUTH_RESPONSE = 224,

    ROOM_LEAVE_REQUEST = 225,
    ROOM_LEAVE_RESPONSE = 226,
    ROOM_LEAVE_USER_NTF = 227,
    ROOM_HOST_NTF = 228,
    GAME_CLEAR_RANKING_REQ = 229,

    ROOM_CHAT_REQUEST = 231,
    ROOM_CHAT_RESPONSE = 232,
    ROOM_CHAT_NOTIFY = 233,

    // --- 3. In-Game Physics & Action (240 ~ 269) ---
    PLAYER_MOVEMENT = 241,
    UPDATE_PLAYER_MOVEMENT = 242,
    PLAYER_STATUS_NTF = 243,

    PLAYER_ACTION_REQUEST = 251,
    PLAYER_ACTION_NTF = 252,

    GIMMICK_INTERACT_REQ = 261,
    GIMMICK_INTERACT_NTF = 262,

    // --- 4. Game Flow & Dungeon State (270 ~ 299) ---
    PLAYER_READY_REQUEST = 271,
    ROOM_READY_STATUS_NTF = 272,
    GAME_START_COUNTDOWN_NTF = 273,
    GAME_READY_CANCEL_NTF = 274,
    GAME_START_NTF = 275,
    SCENE_SYNC_REQ = 276,

    DUNGEON_ESCAPE_REQ = 281,
    DUNGEON_CLEAR_NTF = 282,
    PLAYER_DEAD_REQ = 283,
    PLAYER_DEAD_NTF = 284,

    // --- 5. Shop, Inventory, Trade (300 ~ 399) ---
    INVENTORY_INFO = 301,
    SHOP_INFO = 302,
    SHOP_BUY_REQUEST = 303,
    SHOP_BUY_RESPONSE = 304,

    TRADE_REQUEST = 310,
    TRADE_REQUEST_NTF = 311,
    TRADE_RESPONSE = 312,
    TRADE_START_NTF = 313,
    TRADE_ITEM_UPDATE = 314,
    TRADE_ITEM_NTF = 315,
    TRADE_LOCK = 316,
    TRADE_LOCK_NTF = 317,
    TRADE_CONFIRM = 318,
    TRADE_RESULT = 319,
    TRADE_CONFIRM_NTF = 320,

    // --- 6. Monster Sync (400 ~ ) ---
    MONSTER_SPAWN_NTF = 401,
    MONSTER_MOVEMENT = 402,
    MONSTER_STATE_NTF = 403,
    MONSTER_DEAD_REQ = 404,
    MONSTER_DEAD_NTF = 405,
}

// ============================================================
// 유틸리티 구조체 (Vector3, Quaternion)
// ============================================================
#region Common Types
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PacketVector3
{
    [MarshalAs(UnmanagedType.R4)] public float x;
    [MarshalAs(UnmanagedType.R4)] public float y;
    [MarshalAs(UnmanagedType.R4)] public float z;

    public void Set(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PacketQuaternion
{
    [MarshalAs(UnmanagedType.R4)] public float x;
    [MarshalAs(UnmanagedType.R4)] public float y;
    [MarshalAs(UnmanagedType.R4)] public float z;
    [MarshalAs(UnmanagedType.R4)] public float w;

    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
    public void Set(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
}
#endregion

// ============================================================
// 기능별 패킷 구조체 모음
// ============================================================
#region [1] Server Unix Time Sync
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TimeSyncReq
{
    [MarshalAs(UnmanagedType.I8)] public long clientTimestamp;
    [MarshalAs(UnmanagedType.I4)] public int currentPing;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TimeSyncRes
{
    [MarshalAs(UnmanagedType.I8)] public long clientTimestamp;
    [MarshalAs(UnmanagedType.I8)] public long serverTimestamp;
}
#endregion

#region [2] Lobby & Room Management
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_LoginReq
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string userID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string userPW;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_LoginRes
{
    [MarshalAs(UnmanagedType.U2)] public ushort result;
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomInfo
{
    [MarshalAs(UnmanagedType.I4)] public int roomNum;
    [MarshalAs(UnmanagedType.I4)] public int curUser;
    [MarshalAs(UnmanagedType.I4)] public int maxUser;
    [MarshalAs(UnmanagedType.I1)] public byte isPlaying;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string title;
    [MarshalAs(UnmanagedType.I4)] public int hostPing;
    [MarshalAs(UnmanagedType.I1)] public byte guestReadyState;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomListReq
{
    public byte dummy; // 빈 패킷 전송용
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomListRes
{
    [MarshalAs(UnmanagedType.I4)] public int roomCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public P_RoomInfo[] rooms;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomCharSelectReq
{
    [MarshalAs(UnmanagedType.I4)] public int charID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomCharSelectNtf
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.I4)] public int charID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GameStartReq
{
    [MarshalAs(UnmanagedType.I4)] public int roomNumber;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomEnterRequest
{
    [MarshalAs(UnmanagedType.I4)] public int roomNumber;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string title;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomEnterResponse
{
    [MarshalAs(UnmanagedType.I2)] public short result;
    [MarshalAs(UnmanagedType.I4)] public int roomNum;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomNewUserNotify
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string userName;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomUserInfoNotify
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string userName;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 position;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketQuaternion rotation;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomLeaveRequest
{
    public byte dummy;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomLeaveResponse
{
    [MarshalAs(UnmanagedType.I2)]
    public short result;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomLeaveUserNotify
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string userName;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomHostNtf
{
    [MarshalAs(UnmanagedType.I8)] public long hostUUID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomChatRequest
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)] public string message;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomChatNotify
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string userID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)] public string message;
}
#endregion

#region [3] Game Flow & Handover
// 로비 서버가 던져주는 게임서버 입장권 (포트번호와 토큰)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_MatchStartNtf
{
    [MarshalAs(UnmanagedType.U2)] public ushort GameServerPort;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string AuthToken;
}

// 게임 서버 접속 직후 보내는 토큰 제출 패킷
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GameAuthReq
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string AuthToken;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GameAuthRes
{
    [MarshalAs(UnmanagedType.U2)] public ushort Result; // 0이면 성공
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerReadyRequest
{
    [MarshalAs(UnmanagedType.I1)] public bool isReady;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomReadyStatusNtf
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.I1)] public bool isReady;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GameStartCountdownNtf
{
    [MarshalAs(UnmanagedType.I4)] public int remainSeconds;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GameStartNtf
{
    [MarshalAs(UnmanagedType.I4)] public int mapId;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GameReadyCancelNtf
{
    public byte dummy;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_SceneSyncReq
{
    public byte dummy;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_DungeonEscapeReq
{
    public byte dummy;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_DungeonClearNtf
{
    public byte dummy;
}

// 클리어 후 랭킹 집계 요청
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GameClearRankingReq
{
    [MarshalAs(UnmanagedType.R4)] public float clearTime;
    [MarshalAs(UnmanagedType.I4)] public int deathCount;
}
#endregion

#region [4] In-Game Physics & Action
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerMovement
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.U4)] public uint inputSeq;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 currentPos;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketQuaternion currentRot;
    [MarshalAs(UnmanagedType.R4)] public float axisH;
    [MarshalAs(UnmanagedType.R4)] public float axisV;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_UpdatePlayerMovement
{
    [MarshalAs(UnmanagedType.U4)] public uint lastInputSeq;
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 currentPos;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketQuaternion currentRot;
    [MarshalAs(UnmanagedType.R4)] public float currentSpeed;
    [MarshalAs(UnmanagedType.R4)] public float axisH;
    [MarshalAs(UnmanagedType.R4)] public float axisV;
    [MarshalAs(UnmanagedType.I1)] public bool isMoving;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerStatusNtf
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.I1)] public byte newState;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 targetDir;
    [MarshalAs(UnmanagedType.R4)] public float param;
    [MarshalAs(UnmanagedType.I1)] public byte isPull;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 casterPos;
    [MarshalAs(UnmanagedType.I8)] public long timestamp;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerActionRequest
{
    [MarshalAs(UnmanagedType.I8)] public long targetUUID;
    [MarshalAs(UnmanagedType.I1)] public byte actionType;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerActionNtf
{
    [MarshalAs(UnmanagedType.I8)] public long attackerUUID;
    [MarshalAs(UnmanagedType.I8)] public long targetUUID;
    [MarshalAs(UnmanagedType.I1)] public byte actionType;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GimmickInteractReq
{
    [MarshalAs(UnmanagedType.I8)] public long activeUUID;
    [MarshalAs(UnmanagedType.I4)] public int gimmickID;
    [MarshalAs(UnmanagedType.I1)] public byte gimmickKey;
    [MarshalAs(UnmanagedType.I1)] public byte state;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 targetPos;
    [MarshalAs(UnmanagedType.R4)] public float param;
    [MarshalAs(UnmanagedType.I8)] public long timestamp;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GimmickInteractNtf
{
    [MarshalAs(UnmanagedType.I8)] public long activeUUID;
    [MarshalAs(UnmanagedType.I4)] public int gimmickID;
    [MarshalAs(UnmanagedType.I1)] public byte gimmickKey;
    [MarshalAs(UnmanagedType.I1)] public byte state;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 targetPos;
    [MarshalAs(UnmanagedType.R4)] public float param;
    [MarshalAs(UnmanagedType.I8)] public long timestamp;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerDeadReq
{
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 respawnPos;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerDeadNtf
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 respawnPos;
}
#endregion

#region [5] Monster Sync Packet
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_MonsterSpawnNtf
{
    [MarshalAs(UnmanagedType.I4)] public int monsterID;
    [MarshalAs(UnmanagedType.I4)] public int monsterType;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 spawnPos;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketQuaternion spawnRot;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_MonsterMovement
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.I4)] public int monsterID;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 currentPos;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketQuaternion currentRot;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_MonsterStateNtf
{
    [MarshalAs(UnmanagedType.I4)] public int monsterID;
    [MarshalAs(UnmanagedType.I1)] public byte newState;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 targetDir;
    [MarshalAs(UnmanagedType.R4)] public float param;
    [MarshalAs(UnmanagedType.I1)] public byte isPull;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 casterPos;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_MonsterDeadReq
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.I4)] public int monsterID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_MonsterDeadNtf
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.I4)] public int monsterID;
}
#endregion

#region [6] Shop, Inventory, Trade
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_InventoryInfo
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)] public int[] itemIDs;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_ShopInfo
{
    [MarshalAs(UnmanagedType.I4)] public int itemID;
    [MarshalAs(UnmanagedType.I8)] public long nextUpdateTime;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_ShopBuyRequest
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.I4)] public int itemID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_ShopBuyResponse
{
    [MarshalAs(UnmanagedType.I1)] public bool isSuccess;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeRequest
{
    [MarshalAs(UnmanagedType.I8)] public long targetUUID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeRequestNtf
{
    [MarshalAs(UnmanagedType.I8)] public long requesterUUID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string requesterName;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeResponse
{
    [MarshalAs(UnmanagedType.I8)] public long requesterUUID;
    [MarshalAs(UnmanagedType.I1)] public bool isAccept;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeStartNtf
{
    [MarshalAs(UnmanagedType.I8)] public long partnerUUID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string userName;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeItemUpdate
{
    [MarshalAs(UnmanagedType.I4)] public int tradeSlot;
    [MarshalAs(UnmanagedType.I4)] public int invenSlot;
    [MarshalAs(UnmanagedType.I4)] public int itemID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeItemNtf
{
    [MarshalAs(UnmanagedType.I4)] public int slotIndex;
    [MarshalAs(UnmanagedType.I4)] public int itemID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeLock
{
    [MarshalAs(UnmanagedType.I1)] public bool isLocked;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeLockNtf
{
    [MarshalAs(UnmanagedType.I1)] public bool isLocked;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeConfirm
{
    [MarshalAs(UnmanagedType.I1)] public bool isConfirmed;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeConfirmNtf
{
    [MarshalAs(UnmanagedType.I1)] public bool isConfirmed;
    [MarshalAs(UnmanagedType.I8)] public long confirmUserUUID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeResult
{
    [MarshalAs(UnmanagedType.I1)] public bool isSuccess;
}
#endregion