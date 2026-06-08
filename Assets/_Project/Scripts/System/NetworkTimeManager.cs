using UnityEngine;
using System;
using System.Collections;

public class NetworkTimeManager : GenericSingleton<NetworkTimeManager>
{
    // 서버 시간 - 클라이언트 시간 오차
    private long _serverTimeOffset = 0;

    // 동기화가 한 번이라도 완료되었는지 여부
    public bool IsSynchronized { get; private set; } = false;

    public long CurrentRTT { get; private set; } = 0;
    private void Start()
    {
        StartCoroutine(TimeSyncLoopRoutine());
    }

// NetworkTimeManager의 RTT 계산 부분
    void Update()
    {
        if (Time.deltaTime > 0.05f)
        {
            Debug.LogWarning($"[Frame Drop] deltaTime: {Time.deltaTime * 1000f:F1}ms, RTT: {CurrentRTT}ms");
        }
    }
    //현재 Unix 타임스탬프
    public long GetCurrentUnixTimeMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // 동기화된 서버 시간
    public long GetServerTime()
    {
        return GetCurrentUnixTimeMs() + _serverTimeOffset;
    }

    public void OnReceiveTimeSyncResponse(long clientTimestamp, long serverTimestamp, long arrivalTime)
    {
        long rtt = arrivalTime - clientTimestamp;
        CurrentRTT = rtt;  // ← 추가
        
        long estimatedServerTime = serverTimestamp + (rtt / 2);
        _serverTimeOffset = estimatedServerTime - arrivalTime;
        IsSynchronized = true;

        Debug.Log($"<color=lime>[TimeSync]</color> RTT: {rtt}ms, Offset: {_serverTimeOffset}ms");
    }

    // 서버 타임 동기화
    private IEnumerator TimeSyncLoopRoutine()
    {
        while (true)
        {
            if (Client.TCP != null && Client.IS_SERVER_PLAY)
            {
                SendTimeSyncRequest();
            }

            yield return new WaitForSecondsRealtime(10f);
        }
    }

    // 서버로 동기화 요청 패킷 송신
    public void SendTimeSyncRequest()
    {
        P_TimeSyncReq req = new P_TimeSyncReq
        {
            clientTimestamp = GetCurrentUnixTimeMs()
        };

        //Client.TCP.SendPacket2(E_PACKET.SYS_TIME_SYNC_REQ, req);
        Client.UDP.SendPacket2(E_PACKET.SYS_TIME_SYNC_REQ, req);
        //Debug.Log($"[TimeSync] 서버로 시간 동기화 요청 송신: {req.clientTimestamp}");
    }

    public void OnReceiveTimeSyncResponse(long clientTimestamp, long serverTimestamp)
    {
        long now = GetCurrentUnixTimeMs();
        long rtt = now - clientTimestamp;
        // 패킷이 왔을 때 예상 서버 시간 -> 서버가 패킷 생성한 시간 + RTT / 2
        long estimatedServerTime = serverTimestamp + (rtt / 2);

        // 오프셋 계산
        _serverTimeOffset = estimatedServerTime - now;
        IsSynchronized = true;

        Debug.Log($"<color=lime>[TimeSync 완료]</color> RTT: {rtt}ms, 오차 보정값(Offset): {_serverTimeOffset}ms");
    }
}