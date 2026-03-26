# LocalServer 模式

## 概述

LocalServer 模式让客户端和服务端在同一个 Unity 进程内通过内存管道通信，取代真实网络（UDP/KCP）。消息协议完全不变，**服务端代码零改动**，切换模式只需改一个配置。

## 架构

```
Local模式（对服务端透明）:
  服务端: new UdpTransport(ipEndPoint)  ──内部自动──>  InMemoryKcpTransport
  客户端: new UdpTransport(addressFamily) ──内部自动──>  InMemoryMuxTransport
                                                          ├→ 按端口路由 → Realm InMemoryKcpTransport
                                                          └→ 按端口路由 → Gate  InMemoryKcpTransport

Remote模式（原始行为）:
  服务端: new UdpTransport(ipEndPoint)  ──> 真实 UDP Socket.Bind
  客户端: new UdpTransport(addressFamily) ──> 真实 UDP Socket
```

核心设计：`UdpTransport` 构造函数内部检测 Local 模式，自动委托给 InMemory 实现。**服务端代码写 `new UdpTransport(ipEndPoint)` 和以前一模一样**，未来新增任何 Fiber 不需要知道有 Local 模式。

## 切换方式

在 Unity Inspector 中选择 GlobalConfig 资源：
- **NetworkMode = Remote**：走真实网络（Router + UDP），需要服务端进程
- **NetworkMode = Local**：走内存管道，单进程运行

> GlobalConfig 路径：`Packages/cn.etetet.loader/Resources/GlobalConfig.asset`

## 启动流程

### Remote 模式（原有流程，完全不变）

1. `Init.cs` 加载 GlobalConfig，设置 `Options.Instance.NetworkMode = 0`
2. `EntryEvent2_InitServer` 创建 NetInner + 所有 server fibers（ThreadPool 调度）
3. `EntryEvent3_InitClient` 注册 `RemoteNetSessionCreator`
4. 登录时通过 Router 握手 + UDP 连接 Realm/Gate

### Local 模式

1. `Init.cs` 加载 GlobalConfig，设置 `Options.Instance.NetworkMode = 1`
2. `EntryEvent2_InitServer`：
   - 跳过 NetInner fiber（不需要跨进程通信）
   - 跳过 Router/RouterManager fiber（不需要路由）
   - 创建 `InMemoryMuxTransport` 并注册到 `InMemoryTransportRegistry`
   - 以 **Main 线程调度** 创建 server fibers
   - 各 Fiber 的 `new UdpTransport(ipEndPoint)` 自动创建 InMemoryKcpTransport 并注册到 MuxTransport
3. `EntryEvent3_InitClient` 注册 `LocalNetSessionCreator`
4. 登录时 `new UdpTransport(addressFamily)` 自动获取 MuxTransport，直连 Realm/Gate

## 关键设计决策

### 1. UdpTransport 工厂模式（服务端零感知）

`UdpTransport` 构造函数内部检测 `Options.Instance.IsLocalNetwork`：
- **服务端 `new UdpTransport(IPEndPoint)`**：自动创建 `InMemoryKcpTransport`，绑定到 `InMemoryMuxTransport`，按端口路由
- **客户端 `new UdpTransport(AddressFamily)`**：自动返回 `InMemoryMuxTransport` 委托

服务端代码（FiberInit_Realm、FiberInit_Gate 等）完全不变：
```csharp
// 这行代码 Local 和 Remote 模式下都一样，无需任何修改
root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(startSceneConfig.InnerIPPort));
```

### 2. INetSessionCreator 抽象（客户端连接层）

客户端需要区分连接方式（Router 握手 vs 直连），通过 `INetSessionCreator` 抽象：

```csharp
// LoginHandler 里的代码，Local 和 Remote 模式完全相同
using (Session session = await NetSessionCreator.Instance.ConnectRealm(...))
{
    r2CLogin = (R2C_Login)await session.Call(c2RLogin);
}
Session gateSession = await NetSessionCreator.Instance.ConnectGate(...);
```

两个实现：
- `RemoteNetSessionCreator`：RouterAddressComponent + CreateRouterSession
- `LocalNetSessionCreator`：UdpTransport（自动委托到 MuxTransport）+ netComponent.Create

### 3. 序列化拷贝

InMemory 传输层做了完整的字节拷贝（`InMemoryPacket.CopyFrom`），不共享对象引用。确保 Local 和 Remote 模式下行为完全一致，避免迁移时出现隐蔽 bug。

### 4. Buffer Pool

`InMemoryPacket` 使用 `ConcurrentBag` 池化，减少 GC 分配。封装了实际数据长度（`Length`）和缓冲区容量（`Capacity`）。

### 5. 心跳保活

Local 模式下 Gate Session 添加了 `PingComponent`，防止 `SessionIdleCheckerComponent` 超时断连。切后台安全——因为同进程，服务端时间也暂停。

### 6. Server Fiber 调度

Local 模式下 server fibers 使用 `SchedulerType.Main`（主线程），可通过配置切换为 `SchedulerType.ThreadPool`。主线程调度简化调试，但服务端重计算可能卡帧。

## 文件清单

### 新增文件

| 文件 | 位置 | 说明 |
|------|------|------|
| `INetSessionCreator.cs` | login/Model/Client/NetClient/ | 客户端连接抽象接口 |
| `RemoteNetSessionCreator.cs` | login/Hotfix/Client/NetClient/ | Remote 模式连接实现 |
| `LocalNetSessionCreator.cs` | login/Hotfix/Client/NetClient/ | Local 模式连接实现 |
| `InMemoryPacket.cs` | core/Core/Share/Network/ | 池化字节缓冲区 |
| `InMemoryKcpTransport.cs` | core/Core/Share/Network/ | 服务端侧 InMemory transport |
| `InMemoryMuxTransport.cs` | core/Core/Share/Network/ | 客户端多路复用 transport |
| `InMemoryTransportRegistry.cs` | core/Core/Share/Network/ | MuxTransport 注册中心 |

### 修改文件

| 文件 | 说明 |
|------|------|
| `IKcpTransport.cs` (UdpTransport) | 构造函数加 Local 模式委托（核心改动） |
| `GlobalConfig.cs` | 新增 `NetworkMode` 枚举和字段 |
| `GlobalConfigEditor.cs` | Inspector 显示 NetworkMode 下拉框 |
| `Options.cs` | 新增 `NetworkMode` 属性和 `IsLocalNetwork` |
| `Init.cs` | 启动时从 GlobalConfig 读取 NetworkMode |
| `EntryEvent2_InitServer.cs` | Local 模式：创建 MuxTransport、跳过 Router、主线程调度 |
| `EntryEvent3_InitClient.cs` | 根据 NetworkMode 注册对应 Creator |
| `Main2NetClient_LoginHandler.cs` | 使用 INetSessionCreator 接口 |

### 未修改的服务端文件（零改动）

| 文件 | 说明 |
|------|------|
| `FiberInit_Realm.cs` | 原始代码，`new UdpTransport` 自动适配 |
| `FiberInit_Gate.cs` | 原始代码，`new UdpTransport` 自动适配 |
| `FiberInit_Map.cs` | 原始代码，无 NetComponent |
| `FiberInit_Location.cs` | 原始代码，无 NetComponent |

## 未来扩展

### P2P 模式（Host）

主机玩家跑 Local 模式 + 开放网络监听，其他玩家通过真实网络连入。`UdpTransport` 可扩展为同时支持 InMemory（本机）+ UDP（远程）双通道。

### Client-Server 模式

直接把 GlobalConfig.NetworkMode 改为 Remote，配合独立服务端进程即可。服务端代码零改动。
