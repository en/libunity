using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using Mono.Math;
using Mono.Security.Cryptography;


public class NetCore : NetProto.Singleton<NetCore>
{

    public delegate void AfterConnHook(bool conn);
    public delegate void BeforeSendHook(NetProto.Api.ENetMsgId msgId, byte[] data);
    public delegate void AfterRecvHook(NetProto.Api.ENetMsgId msgId);
    public AfterConnHook _afterConnHook { get; set; }
    public BeforeSendHook _beforeSendHook { get; set; }
    public AfterRecvHook _afterRecvHook { get; set; }
    // Client socket.
    Socket socket = null;

    // ManualResetEvent instances signal completion.
    ManualResetEvent connectDone = new ManualResetEvent(false);
    ManualResetEvent sendDone = new ManualResetEvent(false);
    ManualResetEvent receiveDone = new ManualResetEvent(false);

    UInt32 seqId;
    ICryptoTransform encryptor;
    ICryptoTransform decryptor;
    DiffieHellmanManaged dhEnc;
    DiffieHellmanManaged dhDec;
    Queue msgQueue;
    NetProto.Dispatcher dispatcher;

    class StateObject
    {
        // Packet size.
        public int header = 0;
        // Receive buffer.
        public byte[] buffer = new byte[NetProto.Config.BUFFER_SIZE];
        // Received data.
        public MemoryStream ms = new MemoryStream();
    }

    public NetProto.NetHandle Handle { get; set; }

    struct Msg
    {
        public NetProto.Api.ENetMsgId id;
        public byte[] data;
    };

    protected NetCore()
    {
        seqId = 0;
        encryptor = null;
        decryptor = null;

        Byte[] _p = BitConverter.GetBytes(NetProto.Config.DH1PRIME);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_p);

        Byte[] _g = BitConverter.GetBytes(NetProto.Config.DH1BASE);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_g);

        dhEnc = new DiffieHellmanManaged(_p, _g, 31);
        dhDec = new DiffieHellmanManaged(_p, _g, 31);

        msgQueue = new Queue();
        Handle = new NetProto.NetHandle();
        dispatcher = new NetProto.Dispatcher();
        // 注册回调
        dispatcher.Register(Handle);
    }

    // Update is called once per frame
    void Update()
    {
        while (MsgQueueCount() > 0)
        {
            Msg msg = PopMsg();
            dispatcher.InvokeHandler(msg.id, msg.data);
        }
    }

    void PushMsg(NetProto.Api.ENetMsgId id, byte[] data)
    {
        lock (msgQueue.SyncRoot)
        {
            Msg msg = new Msg();
            msg.id = id;
            msg.data = data;

            msgQueue.Enqueue(msg);
        }
    }

    Msg PopMsg()
    {
        lock (msgQueue.SyncRoot)
        {
            return (Msg)msgQueue.Dequeue();
        }
    }

    int MsgQueueCount()
    {
        return msgQueue.Count;
    }

    // 加密通讯
    public void Encrypt(Int32 send_seed, Int32 receive_seed)
    {
        Byte[] _send = BitConverter.GetBytes(send_seed);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_send);
        Byte[] _recv = BitConverter.GetBytes(receive_seed);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_recv);
        string key1;
        string key2;
        Byte[] _key1 = dhEnc.DecryptKeyExchange(_send);
        BigInteger bi1 = new BigInteger(_key1);
        key1 = NetProto.Config.SALT + bi1.ToString();

        Byte[] _key2 = dhDec.DecryptKeyExchange(_recv);
        BigInteger bi2 = new BigInteger(_key2);
        key2 = NetProto.Config.SALT + bi2.ToString();

        RC4 rc4enc = RC4.Create();
        RC4 rc4dec = RC4.Create();

        Byte[] seed1 = Encoding.ASCII.GetBytes(key1);
        Byte[] seed2 = Encoding.ASCII.GetBytes(key2);

        // en/decryptor不为null时自动启动加密

        // Get an encryptor.
        encryptor = rc4enc.CreateEncryptor(seed1, null);
        // Get a decryptor.
        decryptor = rc4dec.CreateDecryptor(seed2, null);
    }

    public Int32 GetSendSeed()
    {
        byte[] data = dhEnc.CreateKeyExchange();
        BigInteger i = new BigInteger(data);
        return i % Int32.MaxValue;
    }

    public Int32 GetReceiveSeed()
    {
        byte[] data = dhDec.CreateKeyExchange();
        BigInteger i = new BigInteger(data);
        return i % Int32.MaxValue;
    }

    // handles the completion of the prior asynchronous
    // connect call.
    void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            // Complete the connection.
            socket.EndConnect(ar);

            Debug.Log("Socket connected to " + socket.RemoteEndPoint.ToString());

            // Signal that the connection has been made.
            connectDone.Set();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    public void Connect(string host, int port)
    {
        BeginConnect(host, port);
    }

    // Asynchronous connect using host name (resolved by the
    // BeginConnect call.)
    void BeginConnect(string host, int port)
    {
        socket = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        connectDone.Reset();
        socket.BeginConnect(host, port,
            new AsyncCallback(ConnectCallback), socket);

        // wait here until the connect finishes.  The callback
        // sets connectDone.
        bool signalled = connectDone.WaitOne(NetProto.Config.CONNECTION_TIMEOUT, true);

        if (signalled)
        {
            // 开始接收数据
            Receive();
        }
        else
        {
            Debug.Log("Connection timeout");
        }

        if (_afterConnHook != null)
        {
            _afterConnHook(signalled);
        }
    }

    void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            // Read data from the remote device.
            int bytesRead = socket.EndReceive(ar);
            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.ms.Write(state.buffer, 0, bytesRead);
                // Handle data.
                TryHandleData(state);
                //  Get the rest of the data.
                socket.BeginReceive(state.buffer, 0, NetProto.Config.BUFFER_SIZE, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            else
            {
                Debug.Log("Disconnected from server");
                // Signal that all bytes have been received.
                receiveDone.Set();
            }
        }
        catch (ObjectDisposedException)
        {
            // 主动调用了Close()
        }
        catch (Exception e)
        {
            // 如果socket已经断了，报这个异常
            //  System.Net.Sockets.SocketException: Connection timed out
            Debug.Log(e.ToString());
        }
    }

    void Receive()
    {
        try
        {
            // Create the state object.
            StateObject state = new StateObject();
            receiveDone.Reset();
            // Begin receiving the data from the remote device.
            socket.BeginReceive(state.buffer, 0, NetProto.Config.BUFFER_SIZE, 0,
                new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void TryHandleData(StateObject state)
    {
        state.ms.Seek(0, SeekOrigin.Begin);
        long length = state.ms.Length;
        BinaryReader reader = new BinaryReader(state.ms);

        if (state.header == 0 && length >= NetProto.Config.HEADER_SIZE)
        {
            byte[] _size = reader.ReadBytes(NetProto.Config.HEADER_SIZE);
            length -= NetProto.Config.HEADER_SIZE;
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_size);
            }
            state.header = BitConverter.ToUInt16(_size, 0);
        }
        if (state.header > 0 && length >= state.header)
        {
            byte[] data = reader.ReadBytes(state.header);
            length -= state.header;
            DecryptStream(data);
            // Reset header.
            state.header = 0;
        }

        if (state.ms.Length != length)
        {
            byte[] data = reader.ReadBytes((int)(state.ms.Length - length));
            state.ms.Seek(0, SeekOrigin.Begin);
            state.ms = new MemoryStream();
            state.ms.Write(data, 0, data.Length);
        }

        reader.Close();
        state.ms.Seek(0, SeekOrigin.End);
    }

    void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Complete sending the data to the remote device.
            int bytesSent = socket.EndSend(ar);
            Debug.Log("Sent " + bytesSent + " bytes to server.");
            // Signal that all bytes have been sent.
            sendDone.Set();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    // 发送数据包
    public void Send(NetProto.Api.ENetMsgId msgId, NetProto.Proto.NetBase packet)
    {
        Int16 id = (Int16)msgId;
        seqId++;
        UInt16 payloadSize = 6; // sizeof(seqid) + sizeof(msgid)
        byte[] data = null;

        if (packet != null)
        {
            NetProto.Proto.ByteArray ba = new NetProto.Proto.ByteArray();
            packet.Pack(ba);
            data = ba.Data();
            ba.Dispose();
            if (data.Length > UInt16.MaxValue - 6)
            {
                Debug.LogError(data.Length + " > UInt16.MaxValue-6");
                return;
            }
            payloadSize += (UInt16)data.Length;
        }

        if (_beforeSendHook != null)
        {
            _beforeSendHook(msgId, data);
        }

        // payload
        byte[] payload = new byte[payloadSize];

        // seqid
        Byte[] _seqid = BitConverter.GetBytes(seqId);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_seqid);
        _seqid.CopyTo(payload, 0);

        // opcode
        Byte[] _opcode = BitConverter.GetBytes(id);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_opcode);
        _opcode.CopyTo(payload, 4);

        // data
        if (data != null)
        {
            data.CopyTo(payload, 6);
        }

        // try encrypt
        byte[] encrypted = EncryptStream(payload);

        // =>pack
        byte[] buffer = new byte[2 + payloadSize]; // sizeof(header) + payloadSize

        // =>header
        Byte[] _header = BitConverter.GetBytes(payloadSize);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_header);
        _header.CopyTo(buffer, 0);

        // =>payload
        encrypted.CopyTo(buffer, 2);

        sendDone.Reset();
        try
        {
            socket.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), socket);
        }
        catch (Exception e)
        {
            // 如果socket已经断了，报这个异常
            // System.Net.Sockets.SocketException: The socket is not connected
            Debug.Log(e.ToString());
        }
    }

    void DecryptStream(byte[] encrypted)
    {
        Stream s;
        if (decryptor != null)
        {
            MemoryStream msDecrypt = new MemoryStream(encrypted);
            s = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        }
        else
        {
            s = new MemoryStream(encrypted);
        }

        BinaryReader binReader = new BinaryReader(s);
        byte[] opcode = binReader.ReadBytes(2);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(opcode);
        }
        Int16 id = BitConverter.ToInt16(opcode, 0);
        byte[] data = binReader.ReadBytes(encrypted.GetLength(0) - 2);

        NetProto.Api.ENetMsgId msgId = (NetProto.Api.ENetMsgId)id;
        if (_afterRecvHook != null)
        {
            _afterRecvHook(msgId);
        }

        // 将数据压入到消息队列中.
        PushMsg(msgId, data);
        s.Close();
    }

    byte[] EncryptStream(byte[] toEncrypt)
    {

        if (encryptor != null)
        {
            // Create the streams used for encryption.
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    // Write all data to the crypto stream and flush it.
                    csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
                    csEncrypt.FlushFinalBlock();

                    // Get the encrypted array of bytes.
                    return msEncrypt.ToArray();
                }
            }
        }
        else
        {
            return toEncrypt;
        }
    }

    // 注册收到消息后的数据处理
    public void RegisterHandler(NetProto.Api.ENetMsgId opc, NetProto.Dispatcher.MsgHandler handler)
    {
        dispatcher.RegisterHandler(opc, handler);
    }

    // 注册数据处理完后的一次性动作
    public bool RegisterAction(NetProto.Api.ENetMsgId id, System.Action<object> act)
    {
        return dispatcher.RegisterAction(id, act);
    }

    // 处理消息
    public bool Invoke(NetProto.Api.ENetMsgId id, byte[] data)
    {
        return dispatcher.InvokeHandler(id, data);
    }

    // 关闭网络连接
    public void Close()
    {
        if (socket != null)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
            try
            {
                socket.Close();
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }
        }
        seqId = 0;
        encryptor = null;
        decryptor = null;
    }
}
