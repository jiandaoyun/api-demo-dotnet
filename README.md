# 简道云 API接口调用演示

此项目为.net开发环境下，调用简道云API接口进行表单字段查询和数据增删改查的示例。

具体API接口参数请参考帮助文档： https://hc.jiandaoyun.com/doc/10993

## 演示代码

演示工程使用 ASP.NET Core 框架，经过 dotnet 2.0 环境测试。
[安装链接](https://www.microsoft.com/net/core)

使用前请安装相关依赖:

```bash
dotnet restore
```

修改appId、entryId和APIKey

```
string appId = "5b1747e93b708d0a80667400";
string entryId = "5b1749ae3b708d0a80667408";
string apiKey = "CTRP5jibfk7qnnsGLCCcmgnBG6axdHiX";
```

修改各个请求的参数与表单中的配置相对应

启动运行

```bash
dotnet run
```
