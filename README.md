# gonet/2 unity 客户端网络库

1. 新建unity project
2. Assets下新建文本文件smcs.rsp, 因为mono库中使用了unsafe关键字(需要重启unity)

  ```
  -unsafe
  ```

3. clone网络库

  ```
  $ git clone https://github.com/en/libunity.git Assets/Scripts/NetLib
  ```

4. 新建一个Empty Object, 添加脚本组件Sample/Main.cs
5. 部署好gonet/2服务端, 修改Main.cs中的IP地址, 运行unity测试

***

## 简明易懂的服务端部署教程(适用于快速体验或开发环境)

我的机器环境(不同的发行版可能有区别)：

```
$ cat /etc/os-release | head -n +1 | awk -F'"' '{print $2}'
Arch Linux

$ go version
go version go1.6.3 linux/amd64

$ docker -v
Docker version 1.11.2, build b9f10c9
```

1. 首先, 保证机器可以科学上网, 不然会卡在某些步骤
2. 安装和配置docker

  ```
  $ sudo pacman -S docker
  
  # 启动docker服务
  $ sudo systemctl restart docker
  
  # 把普通用户加入docker组, 这样就不用sudo了, 替换ys为你当前在用的普通用户
  $ sudo gpasswd -a ys docker
  
  # 使用上面的用户重新登陆或下面命令使用户组生效
  $ newgrp docker
  
  # 看输出是否正常
  $ docker info
  ```
  
3. 使用docker运行一些基础设施

  关于docker pull镜像慢的问题, 有必要安利一下kcptun+proxychains/proxifier

  ```
  $ docker run -d -p 80:80 -p 8125:8125/udp -p 8126:8126 --name kamon-grafana-dashboard kamon/grafana_graphite
  
  # 如果没有172.17.42.1这个地址, 添加一个:
  $ sudo ip addr add 172.17.42.1/16 dev docker0
  
  # 安装etcd
  $ export HostIP="172.17.42.1"
  $ docker run -d -p 4001:4001 -p 2380:2380 -p 2379:2379 --name etcd quay.io/coreos/etcd:v2.0.3 \
    -name etcd0 \
    -advertise-client-urls http://${HostIP}:2379,http://${HostIP}:4001 \
    -listen-client-urls http://0.0.0.0:2379,http://0.0.0.0:4001 \
    -initial-advertise-peer-urls http://${HostIP}:2380 \
    -listen-peer-urls http://0.0.0.0:2380 \
    -initial-cluster-token etcd-cluster-1 \
    -initial-cluster etcd0=http://${HostIP}:2380 \
    -initial-cluster-state new

  # 填充一些必要的数据到etcd
  $ docker exec etcd /etcdctl -C http://172.17.42.1:2379 set /backends/game/game1 172.17.42.1:51000
  $ docker exec etcd /etcdctl -C http://172.17.42.1:2379 set /backends/snowflake/snowflake1 172.17.42.1:50003
  $ docker exec etcd /etcdctl -C http://172.17.42.1:2379 set /seqs/userid 0
  $ docker exec etcd /etcdctl -C http://172.17.42.1:2379 set /seqs/snowflake-uuid 0
  ```
  * 注意: 快速体验mongodb/elk等暂时不需要, 游戏配置也不需要上传到etcd, 目前线上的代码没有用到游戏配置

4. 获取代码

  ```
  $ mkdir ~/gonet2
  $ cd ~/gonet2/
  $ git clone https://github.com/gonet2/agent.git
  $ git clone https://github.com/gonet2/game.git
  $ git clone https://github.com/gonet2/snowflake.git
  ```

5. 编译并运行
  ```
  $ cd ~/gonet2/snowflake/
  $ export GOPATH=`pwd`
  $ go install snowflake
  # 启动snowflake
  $ ./bin/snowflake

  # 开一个新的终端
  $ cd ~/gonet2/game/
  $ export GOPATH=`pwd`
  $ go install game
  $ ./bin/game

  # 再开一个新的终端...
  $ cd ~/gonet2/agent/
  $ export GOPATH=`pwd`
  $ go install agent
  $ ./bin/agent
  ```
  * 注意: 如果服务之间的rpc有改动, 需要运行tools/pb-gen.sh(可能要根据自己情况改一下)更新.pb.go文件;
  如果和客户端的协议有变化, 需要在tools/proto_scripts下生成新的协议文件, 拷贝到服务器和客户端的相应位置
