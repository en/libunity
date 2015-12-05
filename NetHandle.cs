using UnityEngine;
using System;
using System.Collections.Generic;

namespace NetProto
{
    public class NetHandle
    {
        public Dictionary<Api.ENetMsgId, Dispatcher.MsgHandler> handlerMap = new Dictionary<Api.ENetMsgId, Dispatcher.MsgHandler>();

        public NetHandle()
        {
            handlerMap.Add(Api.ENetMsgId.heart_beat_ack, HeartBeatAck);
            handlerMap.Add(Api.ENetMsgId.user_login_succeed_ack, UserLoginSucceedAck);
            handlerMap.Add(Api.ENetMsgId.user_login_faild_ack, UserLoginFaildAck);
            handlerMap.Add(Api.ENetMsgId.client_error_ack, ClientErrorAck);
            handlerMap.Add(Api.ENetMsgId.get_seed_ack, GetSeedAck);
            handlerMap.Add(Api.ENetMsgId.proto_ping_ack, ProtoPingAck);
        }

        public void HeartBeatReq()
        {
            Proto.auto_id ai = new Proto.auto_id();
            ai.id = 777;

            NetCore.Instance.Send(Api.ENetMsgId.heart_beat_req, ai);
        }

        public object HeartBeatAck(byte[] data)
        {
            return null;
        }

        public void UserLoginReq()
        {
            Proto.user_login_info uli = new Proto.user_login_info();
            uli.login_way = 1;
            uli.open_udid = "1021868db6647de4e63d5742baed1e7e44ef265d";
            uli.client_certificate = "";
            uli.client_version = 123;
            uli.user_lang = "zh_CN";
            uli.app_id = "foobar1234";
            uli.os_version = "Android 6.0 Marshmallow";
            uli.device_name = "iPhone 6sp Limited Edition";
            uli.device_id = SystemInfo.deviceUniqueIdentifier;
            uli.device_id_type = 3;
            uli.login_ip = "1.2.3.4";

            NetCore.Instance.Send(Api.ENetMsgId.user_login_req, uli);
        }

        public object UserLoginSucceedAck(byte[] data)
        {
            Proto.ByteArray ba = new Proto.ByteArray(data);
            Proto.user_snapshot snapshot = Proto.user_snapshot.UnPack(ba);
            ba.Dispose();

            return snapshot.uid;
        }

        public object UserLoginFaildAck(byte[] data)
        {
            return null;
        }

        public object ClientErrorAck(byte[] data)
        {
            return null;
        }

        // 交换dh密钥请求，将发送客户端公钥到服务器
        public void GetSeedReq()
        {
            int sendSeed = NetCore.Instance.GetSendSeed();
            int recvSeed = NetCore.Instance.GetReceiveSeed();
            Proto.seed_info si = new Proto.seed_info();
            si.client_receive_seed = recvSeed;
            si.client_send_seed = sendSeed;

            NetCore.Instance.Send(Api.ENetMsgId.get_seed_req, si);
        }

        // 服务器返回密钥并加密连接
        public object GetSeedAck(byte[] data)
        {
            Proto.ByteArray ba = new Proto.ByteArray(data);
            Proto.seed_info si = Proto.seed_info.UnPack(ba);
            ba.Dispose();

            // 启用加密通讯
            NetCore.Instance.Encrypt(si.client_send_seed, si.client_receive_seed);

            return null;
        }

        public void ProtoPingReq()
        {
        }

        public object ProtoPingAck(byte[] data)
        {
            return null;
        }
    }
}
