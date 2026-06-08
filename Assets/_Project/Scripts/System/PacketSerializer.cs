using System;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

public static class PacketSerializer
{
    public static byte[] Serialize(object packet)
    {
        switch (packet)
        {
            case P_TimeSyncReq p: return SerializeUnixTimeSync(p);
            case P_LoginReq p: return SerializeLoginReq(p);
            case P_RoomEnterRequest p: return SerializeRoomEnterRequest(p);
            case P_RoomListReq p: return new byte[] { p.dummy };
            case P_RoomCharSelectReq p: return SerializeCharacterSelectRequest(p);
            case P_RoomLeaveRequest p: return new byte[] { p.dummy };
            case P_GameStartReq p: return SerializeGameStartReq(p);
            case P_GameAuthReq p: return SerializeGameAuthReq(p);
            case P_PlayerMovement p: return SerializePlayerMovement(p);
            case P_PlayerStatusNtf p: return SerializePlayerStatus(p);
            case P_DungeonEscapeReq p: return SerializeDungeonEscapeReq(p);
            case P_GimmickInteractReq p: return SerializeGimmickInteractReq(p);
            case P_GimmickBulkResetReq p: return SerializeGimmickBulkResetReq(p);
            case P_PlayerDeadReq p: return SerializePlayerDeadReq(p);
            case P_PlayerReadyRequest p: return SerializePlayerReadyRequest(p);
            case P_GameStartNtf p: return SerializeGameStartNtf(p);
            case P_RoomChatRequest p: return SerializeRoomChatRequest(p);
            case P_ShopBuyRequest p: return SerializeShopBuyRequest(p);
            case P_TradeRequest p: return SerializeTradeRequest(p);
            case P_TradeResponse p: return SerializeTradeResponse(p);
            case P_TradeItemUpdate p: return SerializeTradeItemUpdate(p);
            case P_TradeLock p: return SerializeTradeLock(p);
            case P_TradeConfirm p: return SerializeTradeConfirm(p);
            case P_SceneSyncReq _: return new byte[] { 0 };
            case P_MonsterStateNtf p: return SerializeMonsterStateNtf(p);
            case P_MonsterDeadReq p: return SerializeMonsterDeadReq(p);
            case P_MonsterMovement p: return SerializeMonsterMovement(p);
            default:
                Debug.LogError($"[PacketSerializer] 미등록 패킷: {packet.GetType().Name}");
                return new byte[0];
        }
    }

    // ── 직렬화 함수들 ──────────────────────────────────────

    static byte[] SerializeUnixTimeSync(P_TimeSyncReq p)
    {
        var buf = new byte[12];
        int o = 0;
        Write(buf, ref o, p.clientTimestamp);
        Write(buf, ref o, p.currentPing);
        return buf;
    }

    static byte[] SerializeLoginReq(P_LoginReq p)
    {
        var buf = new byte[66]; // 33 + 33
        int o = 0;
        WriteString(buf, ref o, p.userID, 33);
        WriteString(buf, ref o, p.userPW, 33);
        return buf;
    }

    static byte[] SerializeRoomEnterRequest(P_RoomEnterRequest p)
    {
        var buf = new byte[36]; // 4 + 32
        int o = 0;
        Write(buf, ref o, p.roomNumber);
        WriteString(buf, ref o, p.title, 32);
        return buf;
    }

    static byte[] SerializeCharacterSelectRequest(P_RoomCharSelectReq p)
    {
        var buf = new byte[4];
        int o = 0;
        Write(buf, ref o, p.charID);
        return buf;
    }

    static byte[] SerializeGameStartReq(P_GameStartReq p)
    {
        var buf = new byte[4];
        int o = 0;
        Write(buf, ref o, p.roomNumber);
        return buf;
    }

    static byte[] SerializeGameAuthReq(P_GameAuthReq p)
    {
        var buf = new byte[101];
        int o = 0;
        WriteString(buf, ref o, p.AuthToken, 64);
        WriteString(buf, ref o, p.userName, 33);
        Write(buf, ref o, p.characterID);
        return buf;
    }

    static byte[] SerializePlayerMovement(P_PlayerMovement p)
    {
        var buf = new byte[48]; // 8+4+12+16+4+4
        int o = 0;
        Write(buf, ref o, p.userUUID);
        Write(buf, ref o, p.inputSeq);
        Write(buf, ref o, p.currentPos.x);
        Write(buf, ref o, p.currentPos.y);
        Write(buf, ref o, p.currentPos.z);
        Write(buf, ref o, p.currentRot.x);
        Write(buf, ref o, p.currentRot.y);
        Write(buf, ref o, p.currentRot.z);
        Write(buf, ref o, p.currentRot.w);
        Write(buf, ref o, p.axisH);
        Write(buf, ref o, p.axisV);
        return buf;
    }

    static byte[] SerializePlayerStatus(P_PlayerStatusNtf p)
    {
        var buf = new byte[54]; //8+1+12+4+1+12+8+8
        int o = 0;
        Write(buf, ref o, p.userUUID);
        buf[o++] = p.newState;
        Write(buf, ref o, p.targetDir.x);
        Write(buf, ref o, p.targetDir.y);
        Write(buf, ref o, p.targetDir.z);
        Write(buf, ref o, p.param);
        buf[o++] = p.isPull;
        Write(buf, ref o, p.casterPos.x);
        Write(buf, ref o, p.casterPos.y);
        Write(buf, ref o, p.casterPos.z);
        Write(buf, ref o, p.timestamp);
        Write(buf, ref o, p.casterUUID);
        return buf;
    }

    static byte[] SerializeDungeonEscapeReq(P_DungeonEscapeReq p)
    {
        var buf = new byte[40];
        int o = 0;
        Write(buf, ref o, p.p1Push);
        Write(buf, ref o, p.p1Pull);
        Write(buf, ref o, p.p1Fall);
        Write(buf, ref o, p.p1Destroy);
        Write(buf, ref o, p.p1FallKill);
        Write(buf, ref o, p.p2Push);
        Write(buf, ref o, p.p2Pull);
        Write(buf, ref o, p.p2Fall);
        Write(buf, ref o, p.p2Destroy);
        Write(buf, ref o, p.p2FallKill);
        return buf;
    }

    static byte[] SerializeGimmickInteractReq(P_GimmickInteractReq p)
    {
        var buf = new byte[38]; // 8+4+1+1+12+4+8
        int o = 0;
        Write(buf, ref o, p.activeUUID);
        Write(buf, ref o, p.gimmickID);
        buf[o++] = p.gimmickKey;
        buf[o++] = p.state;
        Write(buf, ref o, p.targetPos.x);
        Write(buf, ref o, p.targetPos.y);
        Write(buf, ref o, p.targetPos.z);
        Write(buf, ref o, p.param);
        Write(buf, ref o, p.timestamp);
        return buf;
    }

    static byte[] SerializeGimmickBulkResetReq(P_GimmickBulkResetReq p)
    {
        int size = 4 + (20 * 4);
        var buf = new byte[size];
        int o = 0;

        Write(buf, ref o, p.count);

        for (int i = 0; i < 20; i++)
        {
            int gimmickID = (p.gimmickIDs != null && i < p.gimmickIDs.Length) ? p.gimmickIDs[i] : 0;
            Write(buf, ref o, gimmickID);
        }

        return buf;
    }

    static byte[] SerializePlayerDeadReq(P_PlayerDeadReq p)
    {
        var buf = new byte[12];
        int o = 0;
        Write(buf, ref o, p.respawnPos.x);
        Write(buf, ref o, p.respawnPos.y);
        Write(buf, ref o, p.respawnPos.z);
        return buf;
    }

    static byte[] SerializePlayerReadyRequest(P_PlayerReadyRequest p)
    {
        return new byte[] { p.isReady ? (byte)1 : (byte)0 };
    }

    static byte[] SerializeGameStartNtf(P_GameStartNtf p)
    {
        var buf = new byte[4];
        int o = 0;
        Write(buf, ref o, p.mapId);
        return buf;
    }

    static byte[] SerializeRoomChatRequest(P_RoomChatRequest p)
    {
        var buf = new byte[257];
        int o = 0;
        WriteString(buf, ref o, p.message, 257);
        return buf;
    }

    static byte[] SerializeShopBuyRequest(P_ShopBuyRequest p)
    {
        var buf = new byte[12]; // 8+4
        int o = 0;
        Write(buf, ref o, p.userUUID);
        Write(buf, ref o, p.itemID);
        return buf;
    }

    static byte[] SerializeTradeRequest(P_TradeRequest p)
    {
        var buf = new byte[8];
        int o = 0;
        Write(buf, ref o, p.targetUUID);
        return buf;
    }

    static byte[] SerializeTradeResponse(P_TradeResponse p)
    {
        var buf = new byte[9]; // 8+1
        int o = 0;
        Write(buf, ref o, p.requesterUUID);
        buf[o++] = p.isAccept ? (byte)1 : (byte)0;
        return buf;
    }

    static byte[] SerializeTradeItemUpdate(P_TradeItemUpdate p)
    {
        var buf = new byte[12]; // 4+4+4
        int o = 0;
        Write(buf, ref o, p.tradeSlot);
        Write(buf, ref o, p.invenSlot);
        Write(buf, ref o, p.itemID);
        return buf;
    }

    static byte[] SerializeTradeLock(P_TradeLock p)
    {
        return new byte[] { p.isLocked ? (byte)1 : (byte)0 };
    }

    static byte[] SerializeTradeConfirm(P_TradeConfirm p)
    {
        return new byte[] { p.isConfirmed ? (byte)1 : (byte)0 };
    }

    static byte[] SerializeMonsterStateNtf(P_MonsterStateNtf p)
    {
        var buf = new byte[58]; //8+4+1+12+4+1+12+8
        int o = 0;
        Write(buf, ref o, p.userUUID);
        Write(buf, ref o, p.monsterID);
        buf[o++] = p.newState;
        Write(buf, ref o, p.targetDir.x);
        Write(buf, ref o, p.targetDir.y);
        Write(buf, ref o, p.targetDir.z);
        Write(buf, ref o, p.param);
        buf[o++] = p.isPull;
        Write(buf, ref o, p.casterPos.x);
        Write(buf, ref o, p.casterPos.y);
        Write(buf, ref o, p.casterPos.z);
        Write(buf, ref o, p.timestamp);
        return buf;
    }

    static byte[] SerializeMonsterDeadReq(P_MonsterDeadReq p)
    {
        var buf = new byte[12]; //8+4
        int o = 0;
        Write(buf, ref o, p.userUUID);
        Write(buf, ref o, p.monsterID);
        return buf;
    }

    static byte[] SerializeMonsterMovement(P_MonsterMovement p)
    {
        var buf = new byte[40]; //8+4+12+16
        int o = 0;
        Write(buf, ref o, p.userUUID);
        Write(buf, ref o, p.monsterID);
        Write(buf, ref o, p.currentPos.x);
        Write(buf, ref o, p.currentPos.y);
        Write(buf, ref o, p.currentPos.z);
        Write(buf, ref o, p.currentRot.x);
        Write(buf, ref o, p.currentRot.y);
        Write(buf, ref o, p.currentRot.z);
        Write(buf, ref o, p.currentRot.w);
        return buf;
    }
    // ── 헬퍼 ───────────────────────────────────────────────

    static void Write(byte[] buf, ref int o, long v)
    { Array.Copy(BitConverter.GetBytes(v), 0, buf, o, 8); o += 8; }

    static void Write(byte[] buf, ref int o, ulong v)
    { Array.Copy(BitConverter.GetBytes(v), 0, buf, o, 8); o += 8; }

    static void Write(byte[] buf, ref int o, int v)
    { Array.Copy(BitConverter.GetBytes(v), 0, buf, o, 4); o += 4; }

    static void Write(byte[] buf, ref int o, uint v)
    { Array.Copy(BitConverter.GetBytes(v), 0, buf, o, 4); o += 4; }

    static void Write(byte[] buf, ref int o, float v)
    { Array.Copy(BitConverter.GetBytes(v), 0, buf, o, 4); o += 4; }

    static void WriteString(byte[] buf, ref int o, string s, int size)
    {
        byte[] strBytes = new byte[size];
        if (!string.IsNullOrEmpty(s))
        {
            byte[] encoded = Encoding.UTF8.GetBytes(s);
            int copyLen = Math.Min(encoded.Length, size - 1);
            Buffer.BlockCopy(encoded, 0, strBytes, 0, copyLen);
        }
        Array.Copy(strBytes, 0, buf, o, size);
        o += size;
    }
}