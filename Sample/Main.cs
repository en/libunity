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
            // 连接服务器
            NetCore.Instance.Connect(address, port, ConnHandler, MsgHandler);
        }

        // Update is called once per frame
        void Update()
        {
            heartbeatCount += Time.deltaTime;
            if (heartbeatCount > 30) {
                NetCore.Instance.Handle.HeartBeatReq();
                heartbeatCount = 0;
            }
        }

        // 连接处理
        void ConnHandler(bool conn)
        {
            if (conn)
            {
                // for testing action register
                System.Action<object> action1 = delegate(object obj)
                {
                    Debug.Log("in action1");
                    NetCore.Instance.Handle.UserLoginReq();
                };

                System.Action<object> action2 = delegate(object obj)
                {
                    Debug.Log("in action2");
                    NetCore.Instance.Handle.UserLoginReq();
                };

                int r = UnityEngine.Random.Range(0, 100);
                Debug.Log("r = " + r);
                // 注册一次性回调动作
                if (r > 50)
                {
                    NetCore.Instance.RegisterAction(NetProto.Api.ENetMsgId.get_seed_ack, action1);
                }
                else
                {
                    NetCore.Instance.RegisterAction(NetProto.Api.ENetMsgId.get_seed_ack, action2);
                }

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

        // 服务器通知处理
        // 这个方法不是在主线程调用的
        void MsgHandler(string message)
        {
            Debug.Log(message);
        }

        // In the editor this is called when the user stops playmode. In the web player it is called when the web view is closed.
        void OnApplicationQuit()
        {
            NetCore.Instance.Close();
        }
    }
}