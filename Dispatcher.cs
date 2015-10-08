using UnityEngine;
using System.Collections.Generic;

public class Dispatcher
{
    public delegate object MsgHandler(byte[] data);

    public class HandlerData
    {
        public NetProto.Api.ENetMsgId id;
        public MsgHandler handler;


        public class CallBack
        {
            public System.Action<object> action;
            public bool autoRemove;
        }

        public List<CallBack> callBackList;
    }

    Dictionary<NetProto.Api.ENetMsgId, HandlerData> msgTable = new Dictionary<NetProto.Api.ENetMsgId, HandlerData>();

    public Dispatcher()
    {
    }

    public bool RegisterHandler(NetProto.Api.ENetMsgId id, MsgHandler handler)
    {
        HandlerData data = new HandlerData();
        data.id = id;
        data.handler = new MsgHandler(handler);

        if (!msgTable.ContainsKey(id))
        {
            msgTable.Add(id, data);
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool RegisterCallback(NetProto.Api.ENetMsgId id, System.Action<object> func, bool autoRemove = true)
    {
        if (!msgTable.ContainsKey(id))
        {
            return false;
        }

        if (msgTable[id].callBackList == null)
        {
            msgTable[id].callBackList = new List<HandlerData.CallBack>();
        }

        HandlerData.CallBack callback = new HandlerData.CallBack();
        callback.autoRemove = autoRemove;
        callback.action = func;

        msgTable[id].callBackList.Add(callback);
        return true;
    }

    public bool UnregisterCallback(NetProto.Api.ENetMsgId id, System.Action<object> func)
    {
        if (!msgTable.ContainsKey(id))
        {
            return false;
        }

        for (int i = 0; i < msgTable[id].callBackList.Count; i++)
        {
            HandlerData.CallBack callback = msgTable[id].callBackList[i];
            if (callback.action == func)
            {
                msgTable[id].callBackList.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool InvokeHandler(NetProto.Api.ENetMsgId id, byte[] data)
    {
        if (!msgTable.ContainsKey(id))
        {
            Debug.LogError("Invoke handler id:" + id);
            return false;
        }

        HandlerData handler_data = msgTable[id];

        object ret = handler_data.handler(data);

        if (handler_data.callBackList != null)
        {
            for (int i = 0; i < handler_data.callBackList.Count; i++)
            {
                HandlerData.CallBack callback = handler_data.callBackList[i];
                callback.action.Invoke(ret);
                if (callback.autoRemove)
                {
                    handler_data.callBackList.RemoveAt(i);
                    --i;
                }
            }
        }

        return true;
    }
}
