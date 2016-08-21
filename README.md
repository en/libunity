# gonet/2 Unity 客户端网络库

1. 打开Unity, New project
2. 打开project所在目录, 新建文本文件Assets/smcs.rsp, 内容如下:  
(2、3顺序如果错了就要重启Unity)

  ```
  -unsafe
  ```

3. clone网络库

  ```
  $ git clone https://github.com/en/libunity.git Assets/Scripts/NetLib
  ```

4. Create Empty, 新建一个空的GameObject, 选择GameObject, 添加脚本组件Assets/Scripts/NetLib/Sample/Main.cs
5. 部署好gonet/2服务端, 修改Main.cs中的IP地址, 运行Unity测试


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
  * 注意: 快速体验mongodb/elk等暂时不需要, 游戏配置也不需要上传到etcd

4. 获取代码

  ```
  $ mkdir -p ~/gonet2/src
  $ cd ~/gonet2/
  $ export GOPATH=`pwd`
  $ git clone https://github.com/gonet2/agent.git ./src/agent
  $ git clone https://github.com/gonet2/game.git ./src/game
  $ git clone https://github.com/gonet2/snowflake.git ./src/snowflake
  ```

5. 编译并运行
  ```
  $ cd ~/gonet2/
  # 需要go版本支持vendor, 比如1.5.2就不支持
  $ go install snowflake
  # 启动snowflake
  $ ./bin/snowflake

  # 开一个新的终端
  $ cd ~/gonet2/
  $ go install game
  $ ./bin/game

  # 再开一个新的终端...
  $ cd ~/gonet2/
  $ go install agent
  $ ./bin/agent
  
  # Unity观察Console, 服务器自己调整日志级别吧
  ```
  * 注意: 如果服务之间的rpc有改动, 需要运行tools/pb-gen.sh(可能要根据自己情况改一下)更新.pb.go文件;
    如果和客户端的协议有变化, 需要在tools/proto_scripts下生成新的协议文件, 拷贝到服务器和客户端的相应位置

## FAQ

1. 这个库有没有在生产环境跑过?

  有!跑过大约一个多月, 简单的使用, 应该没有问题
  
  但我c#并不太会, 只是实现了基本功能, 没有太注意结构, 很多地方都可以改进
  
  建议用来上手、调试, 之后魔改

2. 有没有改进计划?

  没有!之前那个游戏停了, 改了现在也没机会规模性的测试

3. 支不支持kcp?

  不支持!我想啊...
