using System.Runtime.InteropServices;
using UnityEngine;

public enum E_PACKET
{
    LOGIN_REQUEST = 201, // LOGIN_REQ = 201,
    LOGIN_RESPONSE = 202, // LOGIN_RES = 202,

    // Enter
    ROOM_ENTER_REQUEST = 206, // PLAYER_JOINED
    ROOM_ENTER_RESPONSE = 207,
    ROOM_NEW_USER_NTF = 208, // PLAYER_JOINED (일단 자기 자신에도 전송하고 사용된다)
    ROOM_USER_INFO_NTF = 209, // CREATE_MATCH_PLAYER,Zone 안에 있는 유저 정보

    // Leave
    ROOM_LEAVE_REQUEST = 215,
    ROOM_LEAVE_RESPONSE = 216,
    ROOM_LEAVE_USER_NTF = 217, // PLAYER_LEFT
    ROOM_HOST_NTF = 218,             // 방장 알림

    // Chat
    ROOM_CHAT_REQUEST = 221, // SEND_CHAT_MESSAGE
    ROOM_CHAT_RESPONSE = 222,
    ROOM_CHAT_NOTIFY = 223, // RECEIVE_CHAT_MESSAGE

    // Move
    PLAYER_MOVEMENT = 231,
    UPDATE_PLAYER_MOVEMENT = 232,
    PLAYER_STATUS_NTF = 233,

    // Path
    MOVE_PATH_REQUEST = 241,
    MOVE_PATH_RESPONSE = 242,
    MOVE_PATH_NOTIFY = 243,

    //물리 처리용
    PLAYER_ACTION_REQUEST = 251,
    PLAYER_ACTION_NTF = 252,
    GIMMICK_INTERACT_REQ = 261,
    GIMMICK_INTERACT_NTF = 262,

    PLAYER_READY_REQUEST = 271,      // 준비 구역 진입
    ROOM_READY_STATUS_NTF = 272,
    GAME_START_COUNTDOWN_NTF = 273,  // 5, 4, 3 카운트다운
    GAME_READY_CANCEL_NTF = 274,     // 카운트 취소
    GAME_START_NTF = 275,            // 씬 전환 시작
    SCENE_SYNC_REQ = 276,           //씬 로딩 후 동기화

    DUNGEON_ESCAPE_REQ = 281,        // 탈출 구역 진입
    DUNGEON_CLEAR_NTF = 282,         // 던전 클리어 알림
    PLAYER_DEAD_REQ = 283,
    PLAYER_DEAD_NTF = 284,

    //인벤 / 상점용
    INVENTORY_INFO = 301,       // 접속갱신 시 인벤토리 정보 전송
    SHOP_INFO = 302,            // 상점 정보 - 현재 판매 아이템, 다음 갱신 시간
    SHOP_BUY_REQUEST = 303,     //아이템 구매 요청
    SHOP_BUY_RESPONSE = 304,	//아이템 구매 결과

    //거래용
    TRADE_REQUEST = 310,        // A -> Server: 교환 요청
    TRADE_REQUEST_NTF = 311,    // Server -> B: A가 요청함 
    TRADE_RESPONSE = 312,       // B -> Server: 거래 수락 / 거절
    TRADE_START_NTF = 313,      // Server -> A, 
                                // A : 거래 거절 시 거래창 닫기 B: 거래창 열기

    TRADE_ITEM_UPDATE = 314,    // A,B -> Server: 아이템 등록 
    TRADE_ITEM_NTF = 315,       // Server -> A,B: A / B가 아이템 올렸으니 업데이트 
    TRADE_LOCK = 316,           // A,B -> Server: 아이템 확정 
                                //(2번째 온 애 거를 기준으로 confirm 패킷 전송)
    TRADE_LOCK_NTF = 317,       // Server -> A,B: A / B의 Lock 상태 받음

    TRADE_CONFIRM = 318,        // A,B -> Server: 최종 교환 버튼
    TRADE_RESULT = 319,         // Server -> A, B: 거래 성공/실패 결과
    TRADE_CONFIRM_NTF = 320,
}



#region Login Packet
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_PlayerName
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string userName;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_LoginReq
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userPW;
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_LoginRes
{
    // UInt16 Result;
    [MarshalAs(UnmanagedType.U2)]
    public ushort result;

}
#endregion

#region Room, PlayerJoin, CreateMatch
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_PlayerJoined
{
    [MarshalAs(UnmanagedType.I8)]
    public long id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string userName;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_RoomEnterRequest
{
    [MarshalAs(UnmanagedType.I4)]
    public int roomNumber;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_RoomEnterResponse
{
    [MarshalAs(UnmanagedType.I2)]
    public short result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_RoomNewUserNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userName;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_CreateMatchPlayer
{
    [MarshalAs(UnmanagedType.I8)]
    public long id;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    public string userName;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 position;

    [MarshalAs(UnmanagedType.Struct)]
    public Quaternion rotation;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_RoomUserInfoNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userName;

    [MarshalAs(UnmanagedType.Struct)]
    public P_PacketVector3 position;

    [MarshalAs(UnmanagedType.Struct)]
    public P_PacketQuaternion rotation;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_RoomLeaveUserNotify
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userName;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_DungeonEscapeReq
{
    public byte dummy;
}
#endregion

#region Player Move Packet
#region Old Version
// [StructLayout(LayoutKind.Sequential, Size = 32)]
// struct P_PlayerMovement
// {
//     [MarshalAs(UnmanagedType.I8)]
//     public long player_id;

//     [MarshalAs(UnmanagedType.R4)]
//     public float dx;

//     [MarshalAs(UnmanagedType.R4)]
//     public float dy;

//     [MarshalAs(UnmanagedType.Struct)]
//     public Quaternion rotation;
// }

// [StructLayout(LayoutKind.Sequential, Size = 36)]
// struct P_UpdatePlayerMovement
// {
//     [MarshalAs(UnmanagedType.I8)]
//     public long userUUID;

//     [MarshalAs(UnmanagedType.Struct)]
//     public Quaternion rotation;

//     [MarshalAs(UnmanagedType.Struct)]
//     public Vector3 motion;

// }
#endregion

#region Sync Modifier Move Packet
//Axis 입력 버전
// [StructLayout(LayoutKind.Sequential, Pack = 1)]
// public struct P_PlayerMovement
// {
//     [MarshalAs(UnmanagedType.I8)]
//     public long userUUID;
//     [MarshalAs(UnmanagedType.I4)]
//     public uint inputSeq;    // 클라이언트 입력 번호 (보정용)
//     [MarshalAs(UnmanagedType.R4)]
//     public float dx;         // Horizontal 입력 (-1 ~ 1)
//     [MarshalAs(UnmanagedType.R4)]
//     public float dz;         // Vertical 입력 (-1 ~ 1)
// }

// client to server 이동 패키
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerMovement
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.U4)] public uint inputSeq;       // 클라이언트 입력 번호 (보정용)
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 currentPos;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketQuaternion currentRot;
    [MarshalAs(UnmanagedType.R4)] public float axisH;
    [MarshalAs(UnmanagedType.R4)] public float axisV;
}

// server to client 이동 동기화 패킷
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_UpdatePlayerMovement
{
    [MarshalAs(UnmanagedType.I4)] public uint lastInputSeq; // 서버가 처리한 마지막 입력 번호
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 currentPos;  //서버 확정 좌표
    [MarshalAs(UnmanagedType.Struct)] public P_PacketQuaternion currentRot;   //서버 확정 회전치
    [MarshalAs(UnmanagedType.R4)] public float currentSpeed; // 모디파이어 적용 속도
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

}
#endregion
#endregion

#region Physics Packet
// [StructLayout(LayoutKind.Sequential, Pack = 1)]
// public struct P_PlayerActionRequest
// {
//     [MarshalAs(UnmanagedType.I8)]
//     public long userUUID;
//     [MarshalAs(UnmanagedType.I1)]
//     public byte actionType;
// }

// [StructLayout(LayoutKind.Sequential, Pack = 1)]
// public struct P_PlayerActionNtf
// {
//     [MarshalAs(UnmanagedType.I8)]
//     public long attackerUUID;
//     [MarshalAs(UnmanagedType.I8)]
//     public long targetUUID;
//     [MarshalAs(UnmanagedType.I1)]
//     public byte actionType;
// }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_GimmickInteractReq
{
    [MarshalAs(UnmanagedType.I8)] public long activeUUID;               //활성화 시킨 유저 ID
    [MarshalAs(UnmanagedType.I4)] public int gimmickID;                 //활성화된 기믹 ID
    [MarshalAs(UnmanagedType.I1)] public byte gimmickKey;
    [MarshalAs(UnmanagedType.I1)] public byte state;                    // 0:Off, 1:On 등 상태값
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 targetPos; // 이동할 목표 좌표 or 밀려날 방향
    [MarshalAs(UnmanagedType.R4)] public float param;                   // 추가 데이터 (속도, 밀어내는 힘 등)
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

#region Game Status

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_RoomHostNtf { [MarshalAs(UnmanagedType.I8)] public long hostUUID; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerStateNtf
{
    [MarshalAs(UnmanagedType.I8)] public long userUUID;
    [MarshalAs(UnmanagedType.I1)] public byte newState;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 targetDir;
    [MarshalAs(UnmanagedType.R4)] public float powerOrTime;
    [MarshalAs(UnmanagedType.I1)] public byte isPull;
    [MarshalAs(UnmanagedType.Struct)] public P_PacketVector3 casterPos;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_PlayerReadyRequest
{
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
public struct P_SceneSyncReq
{
    public byte dummy;
}
#endregion

#region Chat Packet
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_RoomChatRequest
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
    public string message;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_RoomChatNotify
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userID;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
    public string message;
}

#endregion
#region Path Packet
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct P_MovePathRequest
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 startPos;

    [MarshalAs(UnmanagedType.Struct)]
    public Vector3 endPos;
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
struct P_MovePathResponse
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public P_PacketVector3[] path;

    [MarshalAs(UnmanagedType.I2)]
    public short path_count;
}
#endregion

#region Inventory
//인벤토리 패킷
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_InventoryInfo
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public int[] itemIDs;
}
#endregion

#region Trade Packet
//상점 패킷
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_ShopInfo
{
    [MarshalAs(UnmanagedType.I4)]
    public int itemID;
    [MarshalAs(UnmanagedType.I8)]
    public long nextUpdateTime;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_ShopBuyRequest
{
    [MarshalAs(UnmanagedType.I8)]
    public long userUUID;
    [MarshalAs(UnmanagedType.I4)]
    public int itemID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_ShopBuyResponse
{
    [MarshalAs(UnmanagedType.I1)]
    public bool isSuccess;
}
#endregion

#region Trade Packet
//거래 패킷
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeRequest
{
    [MarshalAs(UnmanagedType.I8)]
    public long targetUUID;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeRequestNtf
{
    [MarshalAs(UnmanagedType.I8)]
    public long requesterUUID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string requesterName;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeResponse
{
    [MarshalAs(UnmanagedType.I8)]
    public long requesterUUID;
    [MarshalAs(UnmanagedType.I1)]
    public bool isAccept;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeStartNtf
{
    [MarshalAs(UnmanagedType.I8)]
    public long partnerUUID;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
    public string userName;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeItemUpdate
{
    [MarshalAs(UnmanagedType.I4)]
    public int tradeSlot;
    [MarshalAs(UnmanagedType.I4)]
    public int invenSlot;
    [MarshalAs(UnmanagedType.I4)]
    public int itemID;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeItemNtf
{
    [MarshalAs(UnmanagedType.I4)]
    public int slotIndex;
    [MarshalAs(UnmanagedType.I4)]
    public int itemID;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeLock
{
    [MarshalAs(UnmanagedType.I1)]
    public bool isLocked;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeLockNtf
{
    [MarshalAs(UnmanagedType.I1)]
    public bool isLocked;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeConfirm
{
    [MarshalAs(UnmanagedType.I1)]
    public bool isConfirmed;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeConfirmNtf
{
    [MarshalAs(UnmanagedType.I1)]
    public bool isConfirmed;
    [MarshalAs(UnmanagedType.I8)]
    public long confirmUserUUID;
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct P_TradeResult
{
    [MarshalAs(UnmanagedType.I1)]
    public bool isSuccess;
}
#endregion