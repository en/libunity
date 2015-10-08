using UnityEngine;
using System;

namespace NetProto
{
    public class NetHandle
    {

        public NetHandle()
        {
        }

        public void Register()
        {
            NetCore.Instance.RegisterHandler(Api.ENetMsgId.get_seed_ack, KeyExchangeAck);
            NetCore.Instance.RegisterHandler(Api.ENetMsgId.user_login_succeed_ack, UserLoginSucceedAck);
            NetCore.Instance.RegisterHandler(Api.ENetMsgId.user_login_faild_ack, UserLoginFailedAck);
        }

        // 交换dh密钥请求，将发送客户端公钥到服务器
        public void KeyExchangeReq()
        {
            int sendSeed = NetCore.Instance.GetSendSeed();
            int recvSeed = NetCore.Instance.GetReceiveSeed();
            Proto.seed_info si = new Proto.seed_info();
            si.NetMsgId = (UInt16)Api.ENetMsgId.get_seed_req;
            si.client_receive_seed = recvSeed;
            si.client_send_seed = sendSeed;

            NetCore.Instance.Send(si);
        }

        // 服务器返回公钥
        public object KeyExchangeAck(byte[] data)
        {
            Proto.ByteArray ba = new Proto.ByteArray(data);
            Proto.seed_info si = Proto.seed_info.UnPack(ba);

            // 启用加密通讯
            NetCore.Instance.Encrypt(si.client_send_seed, si.client_receive_seed);
            UserLoginReq();

            return null;
        }

        // 游客登陆
        public void UserLoginReq()
        {
            Proto.user_login_info info = new Proto.user_login_info();
            info.NetMsgId = (UInt16)Api.ENetMsgId.user_login_req;
            info.login_ip = new byte[] { 11, 22, 33, 44, 55, 66, 77 };
            info.udid = SystemInfo.deviceUniqueIdentifier;

            NetCore.Instance.Send(info);
        }

        // 游客信息
        public object UserLoginSucceedAck(byte[] data)
        {
            Proto.ByteArray ba = new Proto.ByteArray(data);
            Proto.user_snapshot snapshot = Proto.user_snapshot.UnPack(ba);

            return null;
        }

        public object UserLoginFailedAck(byte[] data)
        {
            return null;
        }
    }
}