using UnityEngine;
using System.Collections;

namespace Sample
{
    public class Main : MonoBehaviour
    {

        // 修改下面的IP和端口
        const string address = "192.168.0.14";
        const int port = 8888;
        float heartbeatCount = 0;

        // Use this for initialization
        void Start()
        {
            // 注册hooks
            NetCore.Instance._afterConnHook = AfterConnHook;
            NetCore.Instance._beforeSendHook = BeforeSendHook;
            NetCore.Instance._afterRecvHook = AfterRecvHook;
            // 连接服务器
            NetCore.Instance.Connect(address, port);
        }

        // Update is called once per frame
        void Update()
        {
            heartbeatCount += Time.deltaTime;
            if (heartbeatCount > 30) {
                NetCore.Instance.Handle.HeartBeatReq();
            }
        }

        // 连接处理
        void AfterConnHook(bool conn)
        {
            if (conn)
            {
                System.Action<object> loggedInAction = delegate(object obj)
                {
                    // 登陆成功
                    Debug.Log("Welcome userid: " + (int)obj);
                };

                System.Action<object> loginAction = delegate(object obj)
                {
                    // 注册登陆成功后初始化游戏数据
                    NetCore.Instance.RegisterAction(NetProto.Api.ENetMsgId.user_login_succeed_ack, loggedInAction);
                    // 加密连接成功，用户登陆
                    NetCore.Instance.Handle.UserLoginReq();
                };

                // 注册加密连接成功后的回调
                NetCore.Instance.RegisterAction(NetProto.Api.ENetMsgId.get_seed_ack, loginAction);

                // 连接成功，发送交换密钥的请求
                // GetSeedReq向服务器发送请求，该请求的回调处理写在NetHandle.cs的GetSeedAck中
                // 如果有注册action, 会在GetSeedAck后执行，action的输入为GetSeedAck的返回值
                // 其他消息使用方法类似
                NetCore.Instance.Handle.GetSeedReq();
            }
            else
            {
                // 连接失败，退出
                Application.Quit();
            }
        }

        void BeforeSendHook(NetProto.Api.ENetMsgId msgId, byte[] data)
        {
            Debug.Log("SEND " + msgId);
            // 发出一条消息后重置心跳时间
            heartbeatCount = 0;
        }

        void AfterRecvHook(NetProto.Api.ENetMsgId msgId)
        {
            Debug.Log("RECV " + msgId);
        }

        // In the editor this is called when the user stops playmode. In the web player it is called when the web view is closed.
        void OnApplicationQuit()
        {
            NetCore.Instance.Close();
        }
    }
}