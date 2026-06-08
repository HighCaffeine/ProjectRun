using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3)]
public struct PacketBaseOld
{
    [MarshalAs(UnmanagedType.U1)]
    public byte packet_id;

    [MarshalAs(UnmanagedType.I2)]
    public short length;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 5)]
public struct PacketBase
{
    [MarshalAs(UnmanagedType.U2)]
    public ushort length;

    [MarshalAs(UnmanagedType.U2)]
    public ushort packet_id;

    [MarshalAs(UnmanagedType.U1)]
    public byte type;

}

public unsafe struct Packet
{
    public PacketBase pbase;
    public byte[] data;
}

public unsafe class NetworkClient
{
    private const int UDP_MAX_DATA_LENGTH = 512;
    private Socket socket;
    private readonly IPEndPoint endPoint;
    private readonly ProtocolType socketProtocol;
    private SynchronizationContext synchronizationContext;
    private readonly List<IPacketReceiver> packetReceivers = new List<IPacketReceiver>();

    private ConcurrentQueue<Packet> packetQueue = new ConcurrentQueue<Packet>();


    private const int MAX_PROCESS_PER_FRAME = 1000; // 한 프레임당 최대 처리 패킷 수
    public event Action OnDisconnect;
    public NetworkClient(string ip, int port, ProtocolType protocol)
    {
        endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        socket = new Socket(endPoint.AddressFamily, (protocol == ProtocolType.Udp) ? SocketType.Dgram : SocketType.Stream, protocol);
        socketProtocol = protocol;
        synchronizationContext = SynchronizationContext.Current;
    }

    public void Start()
    {
        synchronizationContext = SynchronizationContext.Current;
        Application.runInBackground = true;

        if (socketProtocol == ProtocolType.Tcp)
        {
            socket.Connect(endPoint);

            socket.NoDelay = true;

            socket.Blocking = false;
            socket.SendBufferSize = 65536;

            Thread t = new Thread(ReadTcpDataThread);
            t.IsBackground = true; t.Start();
        }
        else if (socketProtocol == ProtocolType.Udp)
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            socket.Blocking = false;

            try
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }
            catch { }

            Thread t = new Thread(ReadUdpDataThread);
            t.IsBackground = true; t.Start();
        }
    }

    public void Update()
    {
        if (socketProtocol == ProtocolType.Udp && packetQueue.Count > 50)
        {
            while (packetQueue.Count > 10)
            {
                packetQueue.TryDequeue(out _);
            }
        }

        int count = 0;
        while (packetQueue.TryDequeue(out Packet packet) && count < MAX_PROCESS_PER_FRAME)
        {
            HandlePacket(packet);
            count++;
        }
    }

    public void HandlePacket(Packet packet)
    {
        for (int i = packetReceivers.Count - 1; i >= 0; i--)
        {
            try
            {
                var receiver = packetReceivers[i];

                // 유니티 오브젝트가 파괴되었는지 체크
                if (receiver == null || receiver.Equals(null))
                {
                    packetReceivers.RemoveAt(i);
                    continue;
                }

                receiver.OnPacketReceived(packet);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Packet Error] ID: {packet.pbase.packet_id} | MSG: {ex.Message}");
            }
        }
    }

    private void ReadUdpDataThread()
    {
        byte[] clientBuffer = new byte[UDP_MAX_DATA_LENGTH];
        EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        if (socket != null) socket.Blocking = true;
        while (socket != null)
        {
            int bytesReceived = 0;
            try
            {
                bytesReceived = socket.ReceiveFrom(clientBuffer, 0, UDP_MAX_DATA_LENGTH, SocketFlags.None, ref ep);
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset) { Thread.Sleep(1); continue; }
                break;
            }
            catch { break; }

            // 무한루프 방지
            if (bytesReceived <= 0) { Thread.Sleep(1); continue; }

            if (bytesReceived >= 5)
            {
                PacketBase packetBase = default;
                packetBase.length = BitConverter.ToUInt16(clientBuffer, 0);
                packetBase.packet_id = BitConverter.ToUInt16(clientBuffer, 2);

                int dataSize = packetBase.length - 5;
                if (dataSize >= 0 && bytesReceived >= packetBase.length)
                {
                    Packet packet = default;
                    packet.pbase = packetBase;
                    packet.data = new byte[dataSize];
                    if (dataSize > 0) Buffer.BlockCopy(clientBuffer, 5, packet.data, 0, dataSize);

                    // [안전장치] context가 null이면 그냥 무시해서 스레드 사망 방지
                    // if (synchronizationContext != null)
                    // {
                    //     synchronizationContext.Post((object state) =>
                    //     {
                    //         HandlePacket((Packet)state);
                    //     }, packet);
                    // }

                    packetQueue.Enqueue(packet);
                }
            }
        }
        Close();
    }

    private void ReadTcpDataThread()
    {
        int offset = 0;
        byte[] packetBaseBuffer = new byte[sizeof(PacketBase)];
        byte[] clientBuffer = null;
        bool bBase = false;
        if (socket != null) socket.Blocking = true;
        while (socket != null && socket.Connected) // Connected 체크 추가
        {
            if (!bBase)
            {
                int packetBaseBytesReceived = 0;
                try { packetBaseBytesReceived = socket.Receive(packetBaseBuffer, offset, sizeof(PacketBase) - offset, SocketFlags.None); }
                catch { break; }

                // 0이 반환되면 연결 끊김 무한 루프 방지
                if (packetBaseBytesReceived <= 0) break;

                offset += packetBaseBytesReceived;
                bBase = (offset == sizeof(PacketBase));
            }
            else
            {
                Packet packet = default;
                packet.pbase = UnsafeCode.ByteArrayToStructure<PacketBase>(packetBaseBuffer);

                // 패킷 길이가 헤더보다 작으면 
                if (packet.pbase.length < sizeof(PacketBase)) break;

                if (clientBuffer == null)
                {
                    clientBuffer = new byte[packet.pbase.length];
                    Buffer.BlockCopy(packetBaseBuffer, 0, clientBuffer, 0, sizeof(PacketBase));
                }

                int bytesReceived = 0;
                try { bytesReceived = socket.Receive(clientBuffer, offset, packet.pbase.length - offset, SocketFlags.None); }
                catch { break; }

                // 여기서도 0 이하 무한 루프 방지
                if (bytesReceived <= 0) break;

                offset += bytesReceived;
                if (offset < packet.pbase.length) continue;

                // data 크기가 0보다 클 때만 자르기
                int dataSize = packet.pbase.length - sizeof(PacketBase);
                if (dataSize > 0)
                {
                    packet.data = UnsafeCode.SubArray(clientBuffer, sizeof(PacketBase), dataSize);
                }
                else
                {
                    packet.data = new byte[0];
                }

                // synchronizationContext.Post((object state) => { HandlePacket(packet); }, null);
                packetQueue.Enqueue(packet);

                clientBuffer = null;
                offset = 0;
                bBase = false;
            }
        }
        Close();
        if (OnDisconnect != null)
        {
            synchronizationContext.Post((object state) => { OnDisconnect(); }, null);
        }
    }


    private void SendData(E_PACKET packetId, byte[] data)
    {
        if (socket != null)
        {
            int sz = data.Length + sizeof(PacketBase);
            byte[] sizeInBytes = BitConverter.GetBytes((short)sz);
            byte[] buff = new byte[sz];
            buff[0] = (byte)packetId;
            Buffer.BlockCopy(sizeInBytes, 0, buff, 1, sizeInBytes.Length);
            Buffer.BlockCopy(data, 0, buff, sizeof(PacketBase), data.Length);
            try
            {
                if (socketProtocol == ProtocolType.Tcp)
                {
                    socket.Send(buff);
                }
                else
                {
                    socket.SendTo(buff, endPoint);
                }
            }
            catch { Close(); }
        }
    }

    private void SendData2(E_PACKET packetId, byte[] data)
    {
        if (socket == null) return;

        int sz = data.Length + 5; // 헤더 5바이트 + 데이터
        byte[] buff = new byte[sz];

        Buffer.BlockCopy(BitConverter.GetBytes((ushort)sz), 0, buff, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)packetId), 0, buff, 2, 2);
        buff[4] = 0; // Type

        // 데이터 복사
        Buffer.BlockCopy(data, 0, buff, 5, data.Length);

        try
        {
            if (socketProtocol == ProtocolType.Tcp) socket.Send(buff);
            else socket.SendTo(buff, endPoint);
        }
        catch (Exception ex)
        {
            if (socketProtocol == ProtocolType.Tcp)
            {
                Debug.LogError($"[TCP Send Error] {ex.Message}");
                Close();
            }
            else
            {
                Debug.LogWarning($"[UDP Send Error] {ex.Message}");
            }
        }
    }




    public void SendPacket(E_PACKET packetId)
    {
        if (socket != null)
        {
            int sz = sizeof(PacketBase);
            byte[] buff = new byte[sz];
            buff[0] = (byte)packetId;
            byte[] sizeInBytes = BitConverter.GetBytes(sz);
            Buffer.BlockCopy(sizeInBytes, 0, buff, sizeof(byte), sizeInBytes.Length);
            try
            {
                socket.Send(buff);
            }
            catch { Close(); }
        }
    }

    // public void SendPacket2(E_PACKET packetId, object packet)
    // {
    //     if (socket == null) return;

    //     byte[] data = PacketSerializer.Serialize(packet);
    //     int sz = data.Length + 5;
    //     byte[] buff = new byte[sz];

    //     Buffer.BlockCopy(BitConverter.GetBytes((ushort)sz), 0, buff, 0, 2);
    //     Buffer.BlockCopy(BitConverter.GetBytes((ushort)packetId), 0, buff, 2, 2);
    //     buff[4] = 0;
    //     Buffer.BlockCopy(data, 0, buff, 5, data.Length);

    //     try
    //     {
    //         if (socketProtocol == ProtocolType.Tcp) socket.Send(buff);
    //         else socket.SendTo(buff, endPoint);
    //     }
    //     catch (SocketException se)
    //     {
    //         if (se.SocketErrorCode == SocketError.WouldBlock) return;
    //         if (socketProtocol == ProtocolType.Tcp) Close();
    //     }
    //     catch { }
    // }

    public void SendPacket2(E_PACKET packetId, object packet)
    {
        if (socket == null) return;

        byte[] data = PacketSerializer.Serialize(packet);
        int sz = data.Length + 5;
        byte[] buff = new byte[sz];

        Buffer.BlockCopy(BitConverter.GetBytes((ushort)sz), 0, buff, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)packetId), 0, buff, 2, 2);
        buff[4] = 0;
        Buffer.BlockCopy(data, 0, buff, 5, data.Length);

        try
        {
            if (socketProtocol == ProtocolType.Tcp)
            {
                socket.BeginSend(buff, 0, buff.Length, SocketFlags.None, (ar) =>
                {
                    try { if (socket != null) socket.EndSend(ar); } catch { Close(); }
                }, null);
            }
            else
            {
                socket.BeginSendTo(buff, 0, buff.Length, SocketFlags.None, endPoint, (ar) =>
                {
                    try { if (socket != null) socket.EndSendTo(ar); } catch { }
                }, null);
            }
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.WouldBlock) return;
            if (socketProtocol == ProtocolType.Tcp) Close();
        }
        catch { }
    }

    public void AddPacketReceiver(IPacketReceiver item)
    {
        if (!packetReceivers.Contains(item))
        {
            packetReceivers.Add(item);
        }
    }

    public void RemovePacketReceiver(IPacketReceiver item)
    {
        packetReceivers.Remove(item);
    }

    public void Close()
    {
        if (socket != null)
        {
            socket.Dispose();
            socket = null;
        }
    }
}
