using UnityEngine;
using System.Collections.Generic;

namespace NetProto
{
    public class Dispatcher
    {
        public delegate object MsgHandler(byte[] data);

        class Group
        {
            public MsgHandler handler;
            // action注册一次，执行一次，它在handler之后被执行
            // action的输入为handler的返回值
            public System.Action<object> action;
        }

        Dictionary<Api.ENetMsgId, Group> msgMap = new Dictionary<Api.ENetMsgId, Group>();

        public Dispatcher()
        {
        }

        public void Register(NetHandle handle)
        {
            foreach(var v in handle.handlerMap)
            {
                RegisterHandler(v.Key, v.Value);
            }
        }

        public bool RegisterHandler(Api.ENetMsgId id, MsgHandler handler)
        {
            if (msgMap.ContainsKey(id))
            {
                Debug.LogError(id + " is already registered");
                return false;
            }
            Group g = new Group();
            g.handler = new MsgHandler(handler);
            g.action = null;
            msgMap.Add(id, g);

            return true;
        }

        public bool RegisterAction(Api.ENetMsgId id, System.Action<object> act)
        {
            if (!msgMap.ContainsKey(id))
            {
                Debug.LogError("register handler of " + id + " first");
                return false;
            }

            msgMap[id].action = act;

            return true;
        }

        public bool InvokeHandler(Api.ENetMsgId id, byte[] data)
        {
            if (!msgMap.ContainsKey(id))
            {
                Debug.LogError(id + " is not registered");
                return false;
            }

            Group g = msgMap[id];

            object ret = g.handler(data);

            if (g.action != null)
            {
                g.action.Invoke(ret);
                g.action = null;
            }

            return true;
        }
    }
}
