using UnityEngine;
using System;
using System.Collections.Generic;
namespace NetProto.Proto {

public abstract class NetBase{
	public ushort NetMsgId;
	public abstract void Pack(ByteArray w);
}


//# 该文件规定客户端和服务之间的通信结构体模式.注释必须独占一行!!!!!

//#

//# 基本类型 : integer float string boolean bytes

//# 格式如下所示.若要定义数组，查找array看看已有定义你懂得.

//#

//# 每一个定义以'

//# 紧接一行注释 #描述这个逻辑结构用来干啥.

//# 然后定义结构名字，以'='结束，这样可以grep '=' 出全部逻辑名字.

//# 之后每一行代表一个成员定义.

//#

//# 发布代码前请确保这些部分最新.

//#

//#公共结构， 用于只传id,或一个数字的结构
public class auto_id : NetBase {
	public Int32 id;

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.id);

	}
	public  static auto_id UnPack(ByteArray reader){
		auto_id tbl = new auto_id();
		tbl.id  = reader.ReadInt32();

		return tbl;
	}
}

//#一般性回复payload,0代表成功
public class error_info : NetBase {
	public Int32 code;
	public string msg;

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.code);
		w.WriteUTF(this.msg);

	}
	public  static error_info UnPack(ByteArray reader){
		error_info tbl = new error_info();
		tbl.code  = reader.ReadInt32();
		tbl.msg  = reader.ReadUTFBytes();

		return tbl;
	}
}

//#用户登陆发包
public class user_login_info : NetBase {
	public byte[] login_ip;
	public string udid;

	public override void Pack(ByteArray w) {
		w.WriteBytes(this.login_ip);
		w.WriteUTF(this.udid);

	}
	public  static user_login_info UnPack(ByteArray reader){
		user_login_info tbl = new user_login_info();
		tbl.login_ip  = reader.ReadBytes();
		tbl.udid  = reader.ReadUTFBytes();

		return tbl;
	}
}

//#通信加密种子
public class seed_info : NetBase {
	public Int32 client_send_seed;
	public Int32 client_receive_seed;

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.client_send_seed);
		w.WriteInt32(this.client_receive_seed);

	}
	public  static seed_info UnPack(ByteArray reader){
		seed_info tbl = new seed_info();
		tbl.client_send_seed  = reader.ReadInt32();
		tbl.client_receive_seed  = reader.ReadInt32();

		return tbl;
	}
}

//#用户信息包
public class user_snapshot : NetBase {
	public Int32 uid;
	public byte[] name;

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.uid);
		w.WriteBytes(this.name);

	}
	public  static user_snapshot UnPack(ByteArray reader){
		user_snapshot tbl = new user_snapshot();
		tbl.uid  = reader.ReadInt32();
		tbl.name  = reader.ReadBytes();

		return tbl;
	}
}

//#对阵信息

//#frame 开局多少帧
public class vs_info : NetBase {
	public Int32 selfMatrixUid;
	public byte[] param;

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.selfMatrixUid);
		w.WriteBytes(this.param);

	}
	public  static vs_info UnPack(ByteArray reader){
		vs_info tbl = new vs_info();
		tbl.selfMatrixUid  = reader.ReadInt32();
		tbl.param  = reader.ReadBytes();

		return tbl;
	}
}

//#命令
public class command : NetBase {
	public byte[] cmd;

	public override void Pack(ByteArray w) {
		w.WriteBytes(this.cmd);

	}
	public  static command UnPack(ByteArray reader){
		command tbl = new command();
		tbl.cmd  = reader.ReadBytes();

		return tbl;
	}
}

//#同步
public class sync_info : NetBase {
	public Int32 frame;
	public command[] cmds;

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.frame);
		w.WriteUnsignedInt16((UInt16)this.cmds.Length);
	foreach (command k in this.cmds) {
		k.Pack(w);
	}

	}
	public  static sync_info UnPack(ByteArray reader){
		sync_info tbl = new sync_info();
		tbl.frame  = reader.ReadInt32();
		{
		UInt16 narr = reader.ReadUnsignedInt16();
		tbl.cmds = new command[narr];
		for (int i = 0; i < narr; i++){
			tbl.cmds[i] = command.UnPack(reader);
		}
		}

		return tbl;
	}
}

//#序列化
public class serialize_data : NetBase {
	public Int32 frame;
	public byte[] data;

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.frame);
		w.WriteBytes(this.data);

	}
	public  static serialize_data UnPack(ByteArray reader){
		serialize_data tbl = new serialize_data();
		tbl.frame  = reader.ReadInt32();
		tbl.data  = reader.ReadBytes();

		return tbl;
	}
}
}