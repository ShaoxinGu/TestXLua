using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// 数据包类型
/// </summary>
public enum PacketType
{
    /// <summary>
    /// 包类型未被初始化
    /// </summary>
    None = 0,

    /// <summary>
    /// 连接服务器成功
    /// </summary>
    ConnectSuccess = 1,

    /// <summary>
    /// 连接服务器失败
    /// </summary>
    ConnectFail = 2,

    /// <summary>
    /// 收到新的TCP数据包
    /// </summary>
    TcpPacket = 3,

    /// <summary>
    /// 服务器断开连接
    /// </summary>
    Disconnect = 4,
}

/// <summary>
/// 网络包定义
/// </summary>
public class NetPacket
{
    /// <summary>
    /// 网络包构造函数
    /// </summary>
    /// <param name="packetType">网络包类型</param>
    public NetPacket(PacketType packetType)
    {
        this.packetType = packetType;
        protoCode = 0;
        currentRecv = 0;
    }

    /// <summary>
    /// 包的类型
    /// </summary>
    public PacketType packetType = PacketType.None;

    /// <summary>
    /// 如果包的类型是TcpPacket，表示这个包的协议号；否则无意义
    /// </summary>
    public int protoCode = 0;

    /// <summary>
    /// 如果是在接收包头时，表示包头收到多少字节了；如果是在接收包体时，表示包体收到多少字节了
    /// </summary>
    public int currentRecv;

    /// <summary>
    /// 包头数据 接收时调用
    /// </summary>
    public byte[] PacketHeaderBytes = null;

    /// <summary>
    /// 包体数据 接收时调用
    /// </summary>
    public byte[] PacketBodyBytes = null;

    /// <summary>
    /// 完整包数据 发送时调用
    /// </summary>
    public byte[] PacketBytes = null;

    /// <summary>
    /// 定义一个配置变量 包头占用8个字节 前4个字节表示包体的长度（不含包头部分），后四个字节表示这个包的协议号
    /// </summary>
    public static int HEADER_SIZE = 8;
}

/// <summary>
/// 网络包队列 线程安全
/// </summary>
public class PacketQueue
{
    private Queue<NetPacket> netPackets = new Queue<NetPacket>();

    /// <summary>
    /// 网络包入队
    /// </summary>
    /// <param name="netPacket"></param>
    public void Enqueue(NetPacket netPacket)
    {
        lock (netPackets)
        {
            netPackets.Enqueue(netPacket);
        }
    }

    /// <summary>
    /// 网络包出队
    /// </summary>
    /// <returns></returns>
    public NetPacket Dequeue()
    {
        lock (netPackets)
        {
            if (netPackets.Count > 0)
            {
                return netPackets.Dequeue();
            }

            return null;
        }
    }

    /// <summary>
    /// 清空网络包队列
    /// </summary>
    public void Clear()
    {
        lock (netPackets)
        {
            netPackets.Clear();
        }
    }
}

/// <summary>
/// TCP客户端类
/// </summary>
public class TCPClient
{
    /// <summary>
    /// 这个TCPClient对象管理的客户端socket
    /// </summary>
    private Socket socket = null;

    /// <summary>
    /// 推送给主线程接收的网络包队列
    /// </summary>
    private PacketQueue packetQueue = new PacketQueue();

    /// <summary>
    /// 当前的网络状态 true表示已连接 false表示未连接
    /// </summary>
    private bool socketState = false;

    /// <summary>
    /// 请求连接服务器，这个函数在主线程调用
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="port">服务器端口</param>
    public void Connect(string address, int port)
    {
        lock (this)
        {
            if (!socketState)
            {
                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.BeginConnect(address, port, ConnectCallback, socket);
                }
                catch
                {
                    packetQueue.Enqueue(new NetPacket(PacketType.ConnectFail));
                }
            }
        }
    }

    /// <summary>
    /// 请求连接服务器的回调函数
    /// </summary>
    /// <param name="asyncResult"></param>
    private void ConnectCallback(IAsyncResult asyncResult)
    {
        lock (this)
        {
            if (socketState)
                return;

            try
            {
                // 连接成功
                socket = (Socket)asyncResult.AsyncState;
                socketState = true;
                socket.EndConnect(asyncResult);
                packetQueue.Enqueue(new NetPacket(PacketType.ConnectSuccess));

                ReadPacket();
            }
            catch
            {
                socket = null;
                socketState = false;
                packetQueue.Enqueue(new NetPacket(PacketType.ConnectFail));
            }
        }
    }

    /// <summary>
    /// 获取当前网络状态
    /// </summary>
    /// <returns></returns>
    public bool GetSocketState()
    {
        lock(this)
        {
            return socketState;
        }
    }

    /// <summary>
    /// 接收数据包
    /// </summary>
    private void ReadPacket()
    {
        // 创建一个TCP的空包
        NetPacket netPacket = new NetPacket(PacketType.TcpPacket);

        // 约定的是包头8个字节
        netPacket.PacketHeaderBytes = new byte[NetPacket.HEADER_SIZE];

        // 开始接收远端发来的数据包头
        socket.BeginReceive(netPacket.PacketHeaderBytes, 0, NetPacket.HEADER_SIZE, SocketFlags.None, ReceiveHeader, netPacket);
    }

    /// <summary>
    /// 接收到数据包包头的回调函数
    /// </summary>
    /// <param name="asyncResult"></param>
    private void ReceiveHeader(IAsyncResult asyncResult)
    {
        lock (this)
        {
            try
            {
                NetPacket netPacket = (NetPacket)asyncResult.AsyncState;

                // 实际读取到的字节数
                int readSize = socket.EndReceive(asyncResult);

                // 服务器主动断开网络
                if (readSize == 0)
                {
                    Disconnect();
                    return;
                }
                netPacket.currentRecv += readSize;

                if (netPacket.currentRecv == NetPacket.HEADER_SIZE)
                {
                    // 收到了约定的包头的长度，重置标记，方便后面接收包体
                    netPacket.currentRecv = 0;

                    // 此包的包体大小
                    int bodySize = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(netPacket.PacketHeaderBytes, 0));

                    // 此包的协议号
                    int protoCode = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(netPacket.PacketHeaderBytes, 4));

                    // 校验异常包
                    if (bodySize < 0)
                    {
                        Disconnect();
                        return;
                    }

                    // 开始接收包体
                    netPacket.PacketBodyBytes = new byte[bodySize];

                    // 注意：有些协议确实没有包体部分，比如心跳包，此时bodySize为0
                    if (bodySize == 0)
                    {
                        packetQueue.Enqueue(netPacket);

                        //开始读取下一个包
                        ReadPacket();
                        return;
                    }

                    socket.BeginReceive(netPacket.PacketBodyBytes, 0, bodySize, SocketFlags.None, ReceiveBody, netPacket);
                }
                else
                {
                    //包头数据还没有接收完，继续接收包头
                    int remainSize = NetPacket.HEADER_SIZE - netPacket.currentRecv;
                    socket.BeginReceive(netPacket.PacketHeaderBytes, netPacket.currentRecv, remainSize, SocketFlags.None, ReceiveHeader, netPacket);
                }
            }
            catch
            {
                Disconnect();
            }
        }
    }

    /// <summary>
    /// 接收到数据包包体的回调函数
    /// </summary>
    /// <param name="ar"></param>
    private void ReceiveBody(IAsyncResult asyncResult)
    {
        lock (this)
        {
            try
            {
                NetPacket netPacket = (NetPacket)asyncResult.AsyncState;

                // 实际读取到的字节数
                int readSize = socket.EndReceive(asyncResult);

                // 服务器主动断开网络
                if (readSize == 0)
                {
                    Disconnect();
                    return;
                }
                netPacket.currentRecv += readSize;

                if (netPacket.currentRecv == netPacket.PacketBodyBytes.Length)
                {
                    // 收到了约定的包体的长度，重置标记
                    netPacket.currentRecv = 0;

                    packetQueue.Enqueue(netPacket);

                    //开始读取下一个包
                    ReadPacket();
                }
                else
                {
                    //包体数据还没有接收完，继续接收包体
                    int remainSize = netPacket.PacketBodyBytes.Length - netPacket.currentRecv;
                    socket.BeginReceive(netPacket.PacketHeaderBytes, netPacket.currentRecv, remainSize, SocketFlags.None, ReceiveBody, netPacket);
                }
            }
            catch
            {
                Disconnect();
            }
        }
    }

    /// <summary>
    /// 断开网络连接，有可能是io线程调用，也可能是主线程调用
    /// </summary>
    private void Disconnect()
    {
        lock(this)
        {
            if(socketState)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                    socket.Close();
                    socket = null;
                    socketState = false;
                    packetQueue.Clear();

                    packetQueue.Enqueue(new NetPacket(PacketType.Disconnect));
                }
            }
        }
    }

    /// <summary>
    /// 主线程主动取走队列中的所有网络包
    /// </summary>
    /// <returns></returns>
    public List<NetPacket> GetPackets()
    {
        List<NetPacket> packetList = new List<NetPacket>();
        NetPacket one = packetQueue.Dequeue();
        while (one != null)
        {
            packetList.Add(one);
            one = packetQueue.Dequeue();
        }
        return packetList;
    }

    /// <summary>
    /// 主线程调用，发送网络包
    /// </summary>
    /// <param name="protoCode">协议号</param>
    /// <param name="body">包体字节流</param>
    public void SendPacket(int protoCode, byte[] body)
    {
        byte[] bodySizeBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));
        byte[] protoCodeBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(protoCode));

        byte[] package = new byte[bodySizeBytes.Length + protoCodeBytes.Length + body.Length];

        Array.Copy(bodySizeBytes, 0, package, 0, bodySizeBytes.Length);
        Array.Copy(protoCodeBytes, 0,  package, bodySizeBytes.Length, protoCodeBytes.Length);
        Array.Copy(body, 0, package, bodySizeBytes.Length + protoCodeBytes.Length, body.Length);

        SendAsync(package);
    }

    /// <summary>
    /// 主线程调用，发送网络字节流
    /// </summary>
    /// <param name="bytes"></param>
    private void SendAsync(byte[] bytes)
    {
        lock(this)
        {
            try
            {
                if(socketState)
                {
                    socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, socket);
                }
            }
            catch
            {
                Disconnect();
            }
        }
    }

    /// <summary>
    /// 发送回调
    /// </summary>
    /// <param name="asyncResult"></param>
    private void SendCallback(IAsyncResult asyncResult)
    {
        lock (this)
        {
            try
            {
                Socket socket = (Socket)asyncResult.AsyncState;
                socket.EndSend(asyncResult);
            }
            catch
            {
                Disconnect();
            }
        }
    }
}
