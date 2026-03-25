# cn.etetet.login

## 概述

`cn.etetet.login` 是 MMOGame 的登录系统核心包，实现了客户端到服务器（Realm → Gate）的完整登录流程。采用 ET 框架的 Fiber/Actor 多线程架构，通过 KCP/UDP（或 WebSocket/WebGL）协议连接，并利用 Router 中间层解决 NAT 穿透问题。

登录流程涉及三个层次的通信：
1. **客户端 Main Fiber** → **NetClient Fiber**（进程内 Actor 消息）
2. **NetClient Fiber** → **Realm Server**（带 Router 的 KCP Session）
3. **NetClient Fiber** → **Gate Server**（带 Router 的 KCP Session）

---

## 目录结构

```
cn.etetet.login/
├── Proto/
│   ├── LoginOuter_C_1000.proto     # 客户端外部协议（C2R/R2C/C2G/G2C/Ping）
│   └── LoginInner_S_20001.proto    # 服务器内部协议（R2G/G2R/G2M）
├── Scripts/
│   ├── Model/
│   │   ├── Client/
│   │   │   ├── ClientSenderComponent.cs       # 客户端消息发送组件（挂载于 Main Scene）
│   │   │   ├── ClientSessionErrorComponent.cs # Gate Session 错误标记组件
│   │   │   ├── FiberParentComponent.cs        # NetClient Fiber 的父 Fiber ID 引用
│   │   │   ├── LoginEvent.cs                  # 登录完成事件定义（空 struct LoginFinish）
│   │   │   ├── PlayerComponent.cs             # 客户端玩家ID存储
│   │   │   ├── SessionComponent.cs            # Gate Session 持有组件
│   │   │   └── NetClient/
│   │   │       ├── A2NetClient_Message.cs     # Actor 单向消息包装（含 MessageObject）
│   │   │       ├── PingComponent.cs           # Ping 延迟组件（存储 Ping 值和时间差）
│   │   │       └── Router/
│   │   │           ├── RouterAddressComponent.cs  # 路由地址信息（含 Realm/Router 列表）
│   │   │           ├── RouterCheckComponent.cs    # 路由健康检测组件
│   │   │           └── RouterConnector.cs         # KCP 路由连接器（持有 Flag 字段）
│   │   └── Server/
│   │       ├── Gate/
│   │       │   ├── Player.cs                  # Gate 层玩家实体（含 Account 字段）
│   │       │   ├── PlayerComponent.cs         # Gate Scene 上的玩家管理器（account→Player 字典）
│   │       │   ├── GateSessionKeyComponent.cs # 一次性登录 Key 存储（20s 超时）
│   │       │   ├── GateMapComponent.cs        # Gate 地图映射
│   │       │   ├── PlayerSessionComponent.cs  # 玩家持有的 Session 引用
│   │       │   ├── SessionPlayerComponent.cs  # Session 持有的 Player 引用
│   │       │   └── LocationType.cs            # 位置类型枚举（Unit/Player/GateSession）
│   │       └── MailBoxType.cs                 # MailBox 类型常量（GateSession/UnOrderedMessage）
│   └── Hotfix/
│       ├── Client/
│       │   ├── ClientSessionErrorComponentSystem.cs  # Session 销毁时向 Main Fiber 发送断线通知
│       │   ├── NetClient2Main_SessionDisposeHandler.cs # Main Fiber 接收断线通知处理
│       │   └── Login/
│       │       ├── LoginHelper.cs                    # 登录流程入口（静态辅助类）
│       │       └── ClientSenderComponentSystem.cs    # 登录 + Send/Call 封装
│       │   └── NetClient/
│       │       ├── FiberInit_NetClient.cs            # NetClient Fiber 初始化
│       │       ├── Main2NetClient_LoginHandler.cs    # 核心登录处理（在 NetClient Fiber 执行）
│       │       ├── A2NetClient_MessageHandler.cs     # 单向消息转发
│       │       ├── A2NetClient_RequestHandler.cs     # RPC 请求转发
│       │       ├── NetComponentOnReadInvoker_NetClient.cs # 网络读取 Invoker
│       │       ├── PingComponentSystem.cs            # Ping 心跳逻辑（2s 间隔，同步服务器时间）
│       │       └── Router/
│       │           ├── RouterAddressComponentSystem.cs   # 从 HTTP 获取 Router 列表（5min 刷新）
│       │           ├── RouterCheckComponentSystem.cs     # 检测 Router 连通性（1s轮询，7s无包则切换）
│       │           ├── RouterConnectorSystem.cs          # KCP Router 连接发送（注册 RouterAck 回调）
│       │           └── RouterHelper.cs                   # CreateRouterSession + Connect 底层握手
│       └── Server/
│           ├── HttpHelper.cs                         # HTTP 辅助工具
│           ├── Realm/
│           │   ├── FiberInit_Realm.cs                # Realm Fiber 初始化
│           │   ├── C2R_LoginHandler.cs               # Realm 处理登录（分配 Gate，1s后关闭Session）
│           │   ├── NetComponentOnReadInvoker_Realm.cs # Realm 网络读取 Invoker
│           │   └── RealmGateAddressHelper.cs         # Gate 地址选择辅助（按 account hash 选区）
│           └── Gate/
│               ├── FiberInit_Gate.cs                 # Gate Fiber 初始化
│               ├── C2G_LoginGateHandler.cs           # Gate 验证 Key 并创建玩家
│               ├── C2G_PingHandler.cs                # Gate Ping 处理（返回服务器时间）
│               ├── GateSessionKeyComponentSystem.cs  # Key 管理（增删查 + 20s 超时）
│               ├── MailBoxType_GateSessionHandler.cs # GateSession MailBox 消息处理
│               ├── PlayerComponentSystem.cs          # 玩家集合 CRUD（Add/Remove/GetByAccount）
│               ├── PlayerSystem.cs                   # Player Awake（设置 Account）
│               ├── R2G_GetLoginKeyHandler.cs         # Realm 请求 Gate 发放 Key（随机 int64）
│               └── SessionPlayerComponentSystem.cs   # Session 销毁时向 Unit 发送 G2M_SessionDisconnect
```

---

## 核心类 / 接口说明

### 客户端 Model 层

| 类名 | 描述 |
|------|------|
| `ClientSenderComponent` | 挂载于 Main Scene，持有 `fiberId`（NetClient Fiber ID）和 `netClientActorId`（Actor 地址），作为客户端网络通信的统一出口 |
| `RouterAddressComponent` | 挂载于 NetClient Scene，存储从 HTTP 获取的 Router 列表和 Realm 列表，5分钟周期刷新。字段：`Address`（HTTP服务器地址）、`AddressFamily`、`RouterIndex`（Round-Robin索引）、`Info`（HttpGetRouterResponse） |
| `RouterCheckComponent` | 挂载于 Gate Session，每秒检测 `session.LastRecvTime`，若 7s 无包则重新查询可用 Router 地址并调用 `session.AService.ChangeAddress()` 切换 |
| `RouterConnector` | KCP 路由连接器，注册 RouterAck 回调（通过 KService），`Flag` 字段为 0 表示未收到确认，非 0 表示握手成功 |
| `PingComponent` | 挂载于 Gate Session，每 2s 发送 `C2G_Ping`，计算往返延迟存入 `Ping` 字段，并更新 `TimeInfo.Instance.ServerMinusClientTime` |
| `SessionComponent` | 挂载于 NetClient Scene（非 Main Scene），保存已建立的 Gate Session |
| `ClientSessionErrorComponent` | 挂载于 Gate Session，销毁时读取 `session.Error` 并向 Main Fiber 发送 `NetClient2Main_SessionDispose` 通知断线 |
| `FiberParentComponent` | 记录 NetClient Fiber 的父 Fiber ID（Main Fiber），用于 `ClientSessionErrorComponentSystem` 的反向通知 |
| `LoginFinish` | 空 struct，登录完成后通过 `EventSystem.PublishAsync` 广播，供 UI 等系统监听 |

### 服务器 Model 层（Gate）

| 类名 | 描述 |
|------|------|
| `Player` | Gate 玩家实体，子节点属于 `PlayerComponent`，持有 `Account` 字符串（由 `PlayerSystem.Awake` 设置） |
| `PlayerComponent` | 挂载于 Gate Scene，以 account 为 key 的 `Dictionary<string, EntityRef<Player>>` 玩家字典，支持 Add/Remove/GetByAccount |
| `GateSessionKeyComponent` | 挂载于 Gate Scene，存储一次性登录 Key（`long→string account` 映射），20s 自动超时清理 |
| `SessionPlayerComponent` | 挂载于 Session，引用对应的 `Player`；Destroy 时通过 `MessageLocationSenderComponent.Get(LocationType.Unit).Send()` 向 Unit 发送 `G2M_SessionDisconnect` |
| `PlayerSessionComponent` | 挂载于 Player，引用当前的 Session（`Session` 字段） |

---

## 协议定义

### 外部协议（LoginOuter_C_1000.proto）

```protobuf
// 进程内 RPC：Main Fiber → NetClient Fiber，触发完整登录流程
message Main2NetClient_Login   // fields: RpcId, OwnerFiberId, Address, Account, Password
message NetClient2Main_Login   // fields: RpcId, Error, Message, PlayerId

// 客户端→Realm：验证账号密码，返回 Gate 地址+Key
message C2R_Login              // fields: RpcId, Account, Password
message R2C_Login              // fields: RpcId, Error, Message, Address, Key(int64), GateId(int64)

// 客户端→Gate：凭 Key 完成 Gate 登录，返回 PlayerId
message C2G_LoginGate          // fields: RpcId, Key(int64), GateId(int64)
message G2C_LoginGate          // fields: RpcId, Error, Message, PlayerId(int64)

// 客户端→Gate：心跳保活 + 时间同步
message C2G_Ping               // fields: RpcId
message G2C_Ping               // fields: RpcId, Error, Message, Time(int64)
```

### 内部协议（LoginInner_S_20001.proto）

```protobuf
// Realm→Gate：申请一次性登录 Key
message R2G_GetLoginKey        // fields: Account
message G2R_GetLoginKey        // fields: Key(int64), GateId(int64)

// Gate→Map(Unit)：通知玩家断线
message G2M_SessionDisconnect  // (无额外字段，由 LocationSender 路由)
```

---

## 实现原理

### 登录流程（完整时序）

```
客户端 Main Fiber
    │
    ├─ LoginHelper.Login(root, address, account, password)
    │      │
    │      ├─ root.RemoveComponent<ClientSenderComponent>()  ← 清理旧连接
    │      ├─ root.AddComponent<ClientSenderComponent>()
    │      └─ ClientSenderComponent.LoginAsync(address, account, password)
    │             │
    │             ├─ FiberManager.Create(SchedulerType.ThreadPool, SceneType.NetClient)
    │             ├─ netClientActorId = new ActorId(process, fiberId)
    │             └─ ProcessInnerSender.Call(Main2NetClient_Login)  ← Actor RPC（跨Fiber）
    │
    └──────────────────────────────────────────────────────────────────
                        NetClient Fiber（Main2NetClient_LoginHandler）
                              │
                              ├─ HTTP GET http://{address}/get_router?v={random}
                              │    → 解析 HttpGetRouterResponse (Routers列表 + Realms列表)
                              │    → RouterAddressComponent.Init()（随机打乱Router列表）
                              │
                              ├─ AddComponent<NetComponent>(UdpTransport 或 WebSocketTransport)
                              ├─ FiberParentComponent.ParentFiberId = request.OwnerFiberId
                              │
                              ├─ GetRealmAddress(account) ← account.Mode(realms.Count) 选择Realm
                              │
                              ├─ CreateRouterSession(realmAddress, account, password)
                              │    ├─ localConn = account.GetLongHashCode()^password.GetLongHashCode()^Random
                              │    ├─ RouterHelper.Connect(routerAddress, realAddress, localConn, 0)
                              │    │    ├─ 发送 RouterSYN 包（含 localConn/remoteConn/connectId/realAddress）
                              │    │    ├─ 每300ms重试，最多20次（6s超时）
                              │    │    └─ RouterConnector.Flag 非0 时确认成功
                              │    ├─ netComponent.Create(routerAddress, realAddress, recvLocalConn)
                              │    ├─ session.AddComponent<PingComponent>()
                              │    └─ session.AddComponent<RouterCheckComponent>()
                              │
                              ├─ session.Call(C2R_Login{Account, Password})
                              │    └─ Realm: C2R_LoginHandler
                              │         ├─ GetGate(zone=3, account) ← 选 Gate
                              │         ├─ MessageSender.Call(R2G_GetLoginKey{Account}) → Gate
                              │         │    └─ Gate: R2G_GetLoginKeyHandler
                              │         │         ├─ key = RandomGenerator.RandInt64()
                              │         │         └─ GateSessionKeyComponent.Add(key, account)  ← 20s超时
                              │         └─ 返回 R2C_Login{Address(Gate), Key, GateId}
                              │    关闭 Realm Session（1s延迟后dispose）
                              │
                              ├─ CreateRouterSession(gateAddress, account, password)
                              │    ├─ （同上 RouterSYN 握手流程）
                              │    ├─ gateSession.AddComponent<ClientSessionErrorComponent>()
                              │    └─ SessionComponent.Session = gateSession
                              │
                              ├─ gateSession.Call(C2G_LoginGate{Key, GateId})
                              │    └─ Gate: C2G_LoginGateHandler
                              │         ├─ GateSessionKeyComponent.Get(key) → account
                              │         ├─ session.RemoveComponent<SessionAcceptTimeoutComponent>()
                              │         ├─ player = playerComponent.AddChild<Player>(account)
                              │         ├─ player.AddComponent<PlayerSessionComponent>()
                              │         │    .AddComponent<MailBoxComponent>(MailBoxType.GateSession)
                              │         │    .AddLocation(LocationType.GateSession)
                              │         ├─ player.AddComponent<MailBoxComponent>(MailBoxType.UnOrderedMessage)
                              │         │    .AddLocation(LocationType.Player)
                              │         ├─ session.AddComponent<SessionPlayerComponent>().Player = player
                              │         └─ 返回 G2C_LoginGate{PlayerId}
                              │
                              └─ response.PlayerId = g2CLoginGate.PlayerId
    │
    ├─ PlayerComponent.MyId = playerId  （Main Scene 上的 PlayerComponent）
    └─ EventSystem.PublishAsync(root, new LoginFinish())  ← 通知 UI 等系统
```

### Router 握手协议（RouterHelper.Connect）

```
客户端                          Router                          Realm/Gate
   │                              │                              │
   │── RouterSYN ──────────────→  │                              │
   │   (synFlag=0x10/0x30,        │── 转发连接请求 ─────────────→ │
   │    localConn, remoteConn,    │                              │
   │    connectId, realAddress)   │←──── RouterACK ─────────────│
   │←── RouterACK ───────────────│                              │
   │   (Flag 变为 non-zero)        │                              │
   │                              │                              │
   │ 若 300ms 无响应，最多重试20次    │                              │
```

- `synFlag`：初次连接为 `RouterSYN(0x10)`，断线重连为 `RouterReconnectSYN(0x30)`
- `localConn` 由 `account hash ^ password hash ^ random uint32` 生成，确保唯一性

### Router 健康监控（RouterCheckComponentSystem）

```
每1秒检测：
  if (ClientFrameTime - session.LastRecvTime > 7000ms):
      localConn, remoteConn = session.AService.GetChannelConn(sessionId)
      (recvLocalConn, routerAddress) = await netComponent.GetRouterAddress(realAddress, localConn, remoteConn)
      if recvLocalConn != 0:
          session.LastRecvTime = now  ← 重置检测计时器
          session.AService.ChangeAddress(sessionId, routerAddress)  ← 无缝切换Router
```

### Ping 心跳（PingComponentSystem）

```
每 2000ms：
    time1 = ClientNow()
    response = await session.Call(C2G_Ping.Create(pooled=true))
    time2 = ClientNow()
    self.Ping = time2 - time1
    TimeInfo.ServerMinusClientTime = response.Time + (time2-time1)/2 - time2
    ← 利用 RTT 一半估算客户端→服务器延迟，实现时间同步
```

### Key 验证机制（防重放）

```
Realm 向 Gate 请求 Key：R2G_GetLoginKey → 随机 int64 作为 Key
Gate 存储：GateSessionKeyComponent.Add(key, account)  ← 20s 超时自动删除
客户端凭 Key 登录：C2G_LoginGate → GateSessionKeyComponent.Get(key) 验证
验证成功后 Key 保留至自然超时（不立即删除，但一个账号只允许创建一个Player）
Key 20s 内有效，防止重放攻击
```

### 断线处理（双向通知）

```
方向1：Gate Session → Map Unit（服务端）
    Gate Session 销毁
        └─ SessionPlayerComponentSystem.Destroy
             └─ root.GetComponent<MessageLocationSenderComponent>()
                  .Get(LocationType.Unit)
                  .Send(player.Id, G2M_SessionDisconnect.Create())
                       └─ 通过 Location 路由找到 Unit 所在 Map Fiber，处理玩家下线

方向2：Gate Session → Main Fiber（客户端）
    Gate Session 销毁
        └─ ClientSessionErrorComponentSystem.Destroy
             └─ 读取 session.Error
             └─ ProcessInnerSender.Send(ActorId(process, SceneType.Main), NetClient2Main_SessionDispose{Error})
                  └─ Main Fiber 接收断线通知，触发重连或UI提示
```

---

## 关键流程图

### Fiber 架构

```
Process
├── Main Fiber (Scene: Main)
│   ├── ClientSenderComponent  ─────────────────────────────┐
│   │   ├── fiberId → NetClient Fiber                        │ ProcessInnerSender
│   │   └── netClientActorId                                 │ Actor RPC (in-process)
│   ├── PlayerComponent (MyId = int64)                       │
│   └── （SessionComponent 在 NetClient Fiber）               ↓
│
└── NetClient Fiber (Scene: NetClient)  ←─── 由 ClientSenderComponent.LoginAsync 动态创建
    ├── RouterAddressComponent (HTTP 获取的 Router/Realm 列表)
    ├── NetComponent (KCP + UdpTransport / WebSocketTransport)
    ├── FiberParentComponent (parentFiberId = Main Fiber ID)
    ├── ProcessInnerSender (与 Main Fiber 通信)
    └── SessionComponent → Gate Session
        ├── PingComponent (2s 心跳，时间同步)
        ├── RouterCheckComponent (1s 轮询，7s 无包切换 Router)
        └── ClientSessionErrorComponent (销毁时通知 Main Fiber)
```

### 服务端 Gate 组件结构

```
Gate Scene (Root)
├── PlayerComponent (Dictionary<account, Player>)
│   └── Player (Entity, account字段)
│       └── PlayerSessionComponent (Session引用)
│           └── MailBoxComponent(GateSession)  ← 注册 Location
└── GateSessionKeyComponent (Dictionary<long, string>)

Gate Session (每个客户端连接一个)
└── SessionPlayerComponent (Player引用)  ← 销毁时发 G2M_SessionDisconnect
```

---

## 代码示例

### 示例1：调用登录（外部调用方式）

```csharp
// UI 层或其他业务层调用登录
await LoginHelper.Login(
    root,               // Scene (Main Fiber Root)
    "127.0.0.1:8080",  // HTTP 服务器地址（用于获取 Router 列表）
    "testuser",         // 账号
    "password123"       // 密码
);
// LoginFinish 事件被发布，UI 可以通过 IEvent<LoginFinish> 接收
```

### 示例2：登录后发送游戏消息

```csharp
// 通过 ClientSenderComponent 发送单向消息
ClientSenderComponent sender = root.GetComponent<ClientSenderComponent>();
sender.Send(new C2M_SomeGameMessage { ... });

// 通过 ClientSenderComponent 发送 RPC 请求
IResponse response = await sender.Call(new C2M_SomeGameRequest { ... });
```

### 示例3：Gate 服务器监听 LoginFinish 后的 Player 访问

```csharp
// Gate Fiber 上通过 Location 发消息给 Player
Scene gateRoot = ...;
PlayerComponent playerComponent = gateRoot.GetComponent<PlayerComponent>();
Player player = playerComponent.GetByAccount("testuser");
// player.Id 可用于 Location 路由
```

### 示例4：RouterHelper.CreateRouterSession 的使用

```csharp
// 在 Main2NetClient_LoginHandler 中实际调用
Session realmSession = await netComponent.CreateRouterSession(realmAddress, account, password);
// CreateRouterSession 内部：
//   1. 生成 localConn = hash(account) ^ hash(password) ^ random
//   2. 向随机 Router 发 RouterSYN 包，最多重试20次（约6秒超时）
//   3. 收到 RouterACK 后创建 Session，挂载 PingComponent + RouterCheckComponent
using (realmSession) {
    R2C_Login r2cLogin = (R2C_Login)await realmSession.Call(c2rLogin);
}
// using 确保 Realm Session 在 RPC 完成后被 Dispose
```

---

## 依赖关系

### 依赖的其他 Package

| 依赖 | 用途 |
|------|------|
| `cn.etetet.core` | ETTask、Entity、Scene、Fiber、FiberManager、EventSystem、TimerComponent、ProcessInnerSender |
| `cn.etetet.netinner` | NetComponent、Session、KService（ChannelConn/ChangeAddress/RouterAck）、MessageSender、MessageLocationSenderComponent |
| `cn.etetet.router` | RouterHelper.CreateRouterSession、GetRouterAddress（Router 握手协议） |
| `cn.etetet.startconfig` | StartSceneConfigCategory、StartSceneConfig（Gate 地址配置，用于 RealmGateAddressHelper） |
| `cn.etetet.proto` | 协议序列化（Protobuf/MongoDB） |
| `cn.etetet.http` | HttpClientHelper.Get（HTTP 获取 Router 列表） |

### 被其他 Package 依赖

- `cn.etetet.ui` — 监听 `LoginFinish` 事件，切换登录后界面
- `cn.etetet.unit` — 处理 `G2M_SessionDisconnect`，执行玩家下线逻辑
- `cn.etetet.statesync` / `cn.etetet.move` — 通过 `ClientSenderComponent.Send/Call` 发送游戏消息

---

## SceneType

此包新增/使用的场景类型：

| SceneType | 说明 |
|-----------|------|
| `SceneType.NetClient` | 客户端网络 Fiber，每次登录由 `ClientSenderComponent.LoginAsync` 创建 |
| `SceneType.Realm` | Realm 服务器，验证账号密码，分配 Gate |
| `SceneType.Gate` | Gate 服务器，管理在线玩家 Session |

---

## 错误码

| 错误码 | 说明 |
|--------|------|
| `ERR_ConnectGateKeyError` | Gate Key 验证失败（Key 不存在或已超时，20s 有效期） |
| `ERR_MessageTimeout` | RPC 超时（`ClientSenderComponent.Call` 中检查并抛出 `RpcException`） |

---

## 注意事项 & 已知限制

1. **WebGL 平台**：`Main2NetClient_LoginHandler` 中条件编译 `#if UNITY_WEBGL` 使用 `WebSocketTransport` 替代 `UdpTransport`
2. **Key 安全**：登录 Key 仅有效 20 秒，防止中间人攻击或重放
3. **Router 刷新**：Router 列表每 5 分钟重新获取（代码注释写 10min，实际 WaitAsync 为 5min），确保负载均衡
4. **Gate 已登录判断**：当前 `C2G_LoginGateHandler` 中，账号已存在时直接 `throw new Exception("not write")`，**重连逻辑尚未实现**
5. **Session 关闭时序**：Realm Session 在响应后 1s 延迟关闭（`CloseSession`），确保响应已送达
6. **Router 握手超时**：`RouterHelper.Connect` 最多重试 20 次，每次间隔 300ms，总超时约 6 秒；失败返回 `recvLocalConn == 0`，会抛异常
7. **RouterSYN vs RouterReconnectSYN**：`remoteConn == 0` 时发 RouterSYN（初次），否则发 RouterReconnectSYN（重连），支持 Router 切换后的无缝恢复
8. **SessionComponent 挂载位置**：登录后的 `SessionComponent`（持有 Gate Session）挂载在 **NetClient Scene**，不在 Main Scene；外部访问需通过 `ClientSenderComponent.netClientActorId` 跨 Fiber 通信
9. **对象池使用**：`C2G_Ping.Create(true)` 启用对象池，`G2C_Ping` 用 `using` 确保归还；`A2NetClient_Response` 也使用 `using`
