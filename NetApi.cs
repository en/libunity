using UnityEngine;
using System;
using System.Collections.Generic;
namespace NetProto.Api {
public enum ENetMsgId {
	heart_beat_req	= 0,	// 心跳包..
	heart_beat_ack	= 1,	// 心跳包回复
	user_login_req	= 10,	// 登陆
	user_login_succeed_ack	= 11,	// 登陆成功
	user_login_faild_ack	= 12,	// 登陆失败
	client_error_ack	= 13,	// 客户端错误
	get_seed_req	= 30,	// socket通信加密使用
	get_seed_ack	= 31,	// socket通信加密使用
	start_game_req	= 1001,	// 玩家请求加入一局游戏
	start_game_succ_ack	= 1002,	// 加入一局游戏成功
	start_game_fail_ack	= 1003,	// 加入游戏失败
	cmd_req	= 1004,	//  指令
	sync_notify	= 1005,	//  同步广播
	serialize_req	= 1006,	// 客户端序列化请求
};

}