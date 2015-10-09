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

//# 基本类型 : integer float string boolean

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

//#用户登陆发包 1代表使用uuid登陆 2代表使用客户端证书登陆
public class user_login_info : NetBase {
	public Int32 login_way;
	public string open_udid;
	public string client_certificate;
	public Int32 client_version;
	public string user_lang;
	public string app_id;
	public string os_version;
	public string device_name;
	public string device_id;
	public Int32 device_id_type;
	public string login_ip;

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.login_way);
		w.WriteUTF(this.open_udid);
		w.WriteUTF(this.client_certificate);
		w.WriteInt32(this.client_version);
		w.WriteUTF(this.user_lang);
		w.WriteUTF(this.app_id);
		w.WriteUTF(this.os_version);
		w.WriteUTF(this.device_name);
		w.WriteUTF(this.device_id);
		w.WriteInt32(this.device_id_type);
		w.WriteUTF(this.login_ip);

	}
	public  static user_login_info UnPack(ByteArray reader){
		user_login_info tbl = new user_login_info();
		tbl.login_way  = reader.ReadInt32();
		tbl.open_udid  = reader.ReadUTFBytes();
		tbl.client_certificate  = reader.ReadUTFBytes();
		tbl.client_version  = reader.ReadInt32();
		tbl.user_lang  = reader.ReadUTFBytes();
		tbl.app_id  = reader.ReadUTFBytes();
		tbl.os_version  = reader.ReadUTFBytes();
		tbl.device_name  = reader.ReadUTFBytes();
		tbl.device_id  = reader.ReadUTFBytes();
		tbl.device_id_type  = reader.ReadInt32();
		tbl.login_ip  = reader.ReadUTFBytes();

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

	public override void Pack(ByteArray w) {
		w.WriteInt32(this.uid);

	}
	public  static user_snapshot UnPack(ByteArray reader){
		user_snapshot tbl = new user_snapshot();
		tbl.uid  = reader.ReadInt32();

		return tbl;
	}
}
}