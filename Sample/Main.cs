using UnityEngine;
using System.Collections;

namespace Sample
{
    public class Main : MonoBehaviour
    {

        // 修改下面的IP和端口
        const string address = "192.168.0.14";
        const int port = 8888;

        // Use this for initialization
        void Start()
        {
            // 连接服务器
            NetCore.Instance.Connect(address, port, ConnHandler, MsgHandler);
        }

        // Update is called once per frame
        void Update()
        {

        }

        // 连接处理
        void ConnHandler(bool conn)
        {
            if (conn)
            {
                // for testing action register
                System.Action<object> action1 = delegate(object obj)
                {
                    NetProto.Proto.seed_info si = (NetProto.Proto.seed_info)obj;
                    Debug.Log("Execute action1");
                    Debug.Log("client_send_seed: " + si.client_send_seed);
                    Debug.Log("client_receive_seed: " + si.client_receive_seed);
                    NetCore.Instance.Handle.UserLoginReq();
                };

                System.Action<object> action2 = delegate(object obj)
                {
                    NetProto.Proto.seed_info si = (NetProto.Proto.seed_info)obj;
                    Debug.Log("Execute action2");
                    Debug.Log("client_send_seed: " + si.client_send_seed);
                    Debug.Log("client_receive_seed: " + si.client_receive_seed);
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
                NetCore.Instance.Handle.KeyExchangeReq();
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
    }
}