# gonet/2 unity 客户端网络库

1. 新建unity project
2. clone 代码

        $ git clone https://github.com/en/unity-lib.git $PROJECT_ROOT/Assets/Scripts/NetProto

3. 新建一个Empty Object 并添加脚本组件Main.cs

        using UnityEngine;
        using System.Collections;
        
        public class Main : MonoBehaviour {
            const string address = "192.168.86.140";
            const int port = 8888;
        
            // Use this for initialization
            void Start () {
                
                // 连接服务器
                NetCore.Instance.Connect(address, port,
                delegate(bool conn) {
                    if (conn) {
                        // 开始接受数据
                        NetCore.Instance.Receive();
                        // 发送交换密钥的请求
                        NetCore.Instance.Handle.KeyExchangeReq();
                    } else {
                        Application.Quit();
                    }
                },
                delegate(string msg) {
                    Debug.Log(msg);
                }
                );
        
            }
            
            // Update is called once per frame
            void Update () {
                // 网络数据处理
                NetCore.Instance.Update();
            }
        }

4. Assets下新建文本文件smcs.rsp, 因为mono库中了unsafe关键字

        -unsafe

5. 部署好gonet/2服务端，修改Main.cs中的IP地址，运行unity测试

