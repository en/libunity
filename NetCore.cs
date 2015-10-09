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


public class NetCore : Singleton<NetCore>
{

    public delegate void ConnectionHandler(bool conn);
    public delegate void MessageHandler(string message);
    public ConnectionHandler ConnInput { get; set; }
    public MessageHandler MsgInput { get; set; }
    Socket socket;
    ManualResetEvent connectDone;
    ManualResetEvent receiveDone;
    UInt32 seqid;
    ICryptoTransform encryptor;
    ICryptoTransform decryptor;
    DiffieHellmanManaged enc_dh;
    DiffieHellmanManaged dec_dh;
    Queue msg_queue;
    string salt;
    NetProto.Dispatcher dispatcher;

    class StateObject
    {
        // Packet size.
        public int header = 0;
        // Receive buffer.
        public byte[] buffer = new byte[NetProto.Config.BUFFER_SIZE];
        // Received data string.
        public MemoryStream ms = new MemoryStream();
    }

    public NetProto.NetHandle Handle { get; set; }

    public struct Msg
    {
        public NetProto.Api.ENetMsgId id;
        public byte[] data;
    };

    protected NetCore()
    {

        seqid = 0;
        encryptor = null;
        decryptor = null;
        salt = "DH";

        connectDone = new ManualResetEvent(false);
        receiveDone = new ManualResetEvent(false);

        Byte[] _p = BitConverter.GetBytes(0x7FFFFFC3);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_p);

        Byte[] _g = BitConverter.GetBytes(3);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(_g);

        enc_dh = new DiffieHellmanManaged(_p, _g, 31);
        dec_dh = new DiffieHellmanManaged(_p, _g, 31);

        msg_queue = new Queue();
        Handle = new NetProto.NetHandle();
        dispatcher = new NetProto.Dispatcher();
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

    public void PushMsg(NetProto.Api.ENetMsgId id, byte[] data)
    {
        lock (msg_queue.SyncRoot)
        {
            Msg msg = new Msg();
            msg.id = id;
            msg.data = data;

            msg_queue.Enqueue(msg);
        }
    }

    public Msg PopMsg()
    {
        lock (msg_queue.SyncRoot)
        {
            return (Msg)msg_queue.Dequeue();
        }
    }

    public int MsgQueueCount()
    {
        return msg_queue.Count;
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
        Byte[] _key1 = enc_dh.DecryptKeyExchange(_send);
        BigInteger bi1 = new BigInteger(_key1);
        key1 = salt + bi1.ToString();

        Byte[] _key2 = dec_dh.DecryptKeyExchange(_recv);
        BigInteger bi2 = new BigInteger(_key2);
        key2 = salt + bi2.ToString();

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
        byte[] data = enc_dh.CreateKeyExchange();
        BigInteger i = new BigInteger(data);
        return i % Int32.MaxValue;
    }

    public Int32 GetReceiveSeed()
    {
        byte[] data = dec_dh.CreateKeyExchange();
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
    public void Connect(string host, int port, ConnectionHandler connHandler, MessageHandler msgHandler)
    {
        ConnInput = connHandler;
        MsgInput = msgHandler;

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
            // 注册回调
            Handle.Register();
            // 开始接收数据
            Receive();
            ConnInput(true);
        }
        else
        {
            Debug.Log("Connection timeout");
            ConnInput(false);
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
                MsgInput("Disconnected");
                // Signal that all bytes have been received.
                receiveDone.Set();
            }
        }
        catch (Exception e)
        {
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
            // Complete send.
            socket.EndSend(ar);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    // 发送数据包
    public void Send(NetProto.Proto.NetBase packet)
    {
        NetProto.Proto.ByteArray ba = new NetProto.Proto.ByteArray();
        packet.Pack(ba);
        byte[] data = ba.Data();
        UInt16 id = packet.NetMsgId;

        seqid++;
        UInt16 payloadSize = 6; // sizeof(seqid) + sizeof(opcode)

        if (data != null)
        {
            if (data.Length > UInt16.MaxValue - 6)
            {
                Debug.LogError(data.Length + " > UInt16.MaxValue-6");
                return;
            }
            payloadSize += (UInt16)data.Length;
        }

        // payload
        byte[] payload = new byte[payloadSize];

        // seqid
        Byte[] _seqid = BitConverter.GetBytes(seqid);
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

        if (socket.Connected)
        {
            socket.BeginSend(buffer, 0, buffer.Length, 0, new AsyncCallback(SendCallback), socket);
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

        // 将数据压入到消息队列中.
        PushMsg((NetProto.Api.ENetMsgId)id, data);
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
}
