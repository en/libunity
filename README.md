# gonet/2 unity 客户端网络库

1. 新建unity project
2. clone 代码

        $ git clone https://github.com/en/unity-lib.git Assets/Scripts/NetProto

3. 新建一个Empty Object, 添加脚本组件Sample/Main.cs
4. Assets下新建文本文件smcs.rsp, 因为mono库中了unsafe关键字(需要重启unity)

        -unsafe

5. 部署好gonet/2服务端，修改Main.cs中的IP地址，运行unity测试

