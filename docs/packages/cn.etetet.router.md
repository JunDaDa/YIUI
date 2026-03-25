# cn.etetet.router — ET 软路由（KCP 软路由转发）

## 概述

**版本**：3.0.1
**描述**：ET 框架的软路由（Soft Router）实现，用于防网络攻击、隐藏内网服务端真实地址。
**作者**：tanghai（ET 框架）
**PackageType**：Router = 15

软路由处于客户端与游戏服务器之间，作为 KCP/TCP 双协议代理层。客户端不直接连接内网服务器，而是先通过 HTTP 向 RouterManager 查询路由列表，然后与其中一台 Router 建立 KCP 连接，由 Router 转发数据至内网服务器，从而隐藏真实业务服务器地址、过滤恶意流量。

---

## 目录结构

```
cn.etetet.router/
├── Proto/
│   └── RouterProto_C_1100.proto      # HttpGetRouterResponse 消息定义
├── Scripts/
│   ├── Model/
│   │   ├── Server/
│   │   │   ├── RouterComponent.cs    # 路由组件（外网监听 + 内网转发）
│   │   │   └── RouterNode.cs         # 单条路由连接节点
│   │   └── Share/
│   │       ├── ErrorCode.cs          # 路由相关错误码
│   │       └── PackageType.cs        # PackageType = 15
│   └── Hotfix/
│       └── Server/
│           ├── FiberInit_Router.cs          # Router Fiber 初始化
│           ├── FiberInit_RouterManager.cs   # RouterManager Fiber 初始化
│           ├── RouterComponentSystem.cs     # RouterComponent 行为系统（核心逻辑）
│           ├── RouterNodeSystem.cs          # RouterNode 行为系统
│           └── HttpGetRouterHandler.cs      # HTTP /get_router 接口
└── package.json
```

---

## 核心类/接口

### RouterComponent（Model/Server）

`[ComponentOf(typeof(Scene))]` — 挂载在 Router 场景根节点上，负责监听外网流量、持有内网 UDP 出口。

| 字段 | 类型 | 说明 |
|------|------|------|
| `OuterUdp` | `IKcpTransport` | 外网 UDP 套接字（非 WebGL） |
| `OuterTcp` | `IKcpTransport` | 外网 TCP/WS 套接字 |
| `InnerSocket` | `IKcpTransport` | 内网 UDP 转发出口（绑定 InnerIP:0） |
| `IPEndPoint` | `EndPoint` | 临时缓冲，接收每条消息时更新为来源 IP |
| `Cache` | `byte[1500]` | 消息复用缓冲区（MTU 大小） |
| `checkTimeout` | `Queue<uint>` | 待超时检测的 OuterConn 队列 |
| `LastCheckTime` | `long` | 上次超时检查时间戳 |

### RouterNode（Model/Server）

`[ChildOf(typeof(RouterComponent))]` — 代表一条外网客户端到内网服务器的连接映射。

| 字段 | 类型 | 说明 |
|------|------|------|
| `InnerAddress` | `string` | 内网服务器地址字符串 |
| `InnerIpEndPoint` | `IPEndPoint` | 内网服务器地址（已解析） |
| `OuterIpEndPoint` | `IPEndPoint` | 客户端外网地址 |
| `KcpTransport` | `IKcpTransport` | 当前使用的外网传输（UDP/TCP，最后收到包的一路） |
| `OuterConn` | `uint`（只读） | 外网连接 ID，等于 Entity.Id（低 32 位） |
| `InnerConn` | `uint` | 内网连接 ID（由内网服务器 ACK 返回赋值） |
| `ConnectId` | `uint` | 防伪连接 ID（握手期间校验） |
| `LastRecvOuterTime` | `long` | 最后收到外网包时间戳（毫秒） |
| `LastRecvInnerTime` | `long` | 最后收到内网包时间戳（毫秒） |
| `RouterSyncCount` | `int` | RouterSYN/RouterReconnectSYN 计数（>40 则断开） |
| `SyncCount` | `int` | SYN 计数（>20 则断开） |
| `LimitCountPerSecond` | `int` | 每秒包计数（>1000 则断开） |
| `Status` | `RouterStatus` | 当前状态：Sync / Msg |

### RouterStatus（枚举）

```csharp
public enum RouterStatus
{
    Sync,   // 握手同步阶段
    Msg,    // 正常消息转发阶段
}
```

### RouterComponentSystem（Hotfix/Server）

`RouterComponent` 的实体系统，包含全部转发逻辑。

| 方法 | 说明 |
|------|------|
| `Awake(IPEndPoint, string)` | 初始化外网 UDP/TCP 监听、内网 UDP 出口 |
| `Destroy` | 释放所有传输套接字 |
| `Update` | 每帧驱动收包、超时检测 |
| `RecvOuterUdp/RecvOuterTcp` | 从外网接收 UDP/TCP 数据并分发 |
| `RecvInner` | 从内网接收数据并转发外网 |
| `RecvOuterHandler` | 外网数据包协议分发（RouterSYN/SYN/FIN/MSG...） |
| `RecvInnerHandler` | 内网数据包协议分发（ACK/FIN/MSG...） |
| `CheckConnectTimeout` | 每秒最多检查 10 个节点，Sync 超 10s、Msg 超 Session+10s 则断开 |
| `New` | 创建新 RouterNode 并加入超时队列 |
| `OnError(id, error)` | 记录错误并移除节点 |

### RouterNodeSystem（Hotfix/Server）

| 方法 | 说明 |
|------|------|
| `Awake` | 初始化时间戳 |
| `Destroy` | 清零所有字段（对象池友好） |
| `CheckOuterCount` | 每秒限流：>1000 包/秒返回 false |

### FiberInit_Router（Hotfix/Server）

```csharp
[Invoke(SceneType.Router)]
```
Router Fiber 启动时，从 `StartSceneConfig` / `StartProcessConfig` 读取外网端口和内网 IP，创建 `RouterComponent`。

### FiberInit_RouterManager（Hotfix/Server）

```csharp
[Invoke(SceneType.RouterManager)]
```
RouterManager Fiber 启动时，添加 `TimerComponent` 和 `HttpComponent`（HTTP 服务端），监听配置端口提供路由查询服务。

### HttpGetRouterHandler（Hotfix/Server）

```csharp
[HttpHandler(SceneType.RouterManager, "/get_router")]
```
响应客户端 HTTP GET `/get_router`，返回 `HttpGetRouterResponse`（Realm 地址列表 + Router 地址列表），并延迟 1 秒应答（防爬虫），支持 CORS。

---

## 协议消息

### HttpGetRouterResponse（Proto）

```protobuf
message HttpGetRouterResponse {
    repeated string Realms  = 1;  // Realm 服务器内网地址列表
    repeated string Routers = 2;  // Router 外网地址列表 (OuterIP:Port)
}
```

---

## 错误码

| 常量 | 值基础 | 说明 |
|------|--------|------|
| `ERR_KcpRouterConnectFail` | Router*1000+1 | 握手阶段超时（>10s 无外网包） |
| `ERR_KcpRouterTimeout` | Router*1000+2 | 消息阶段超时（Session 超时+10s） |
| `ERR_KcpRouterSame` | Router*1000+3 | 同 outerConn 但 connectId 不同（可能路由数量太少） |
| `ERR_KcpRouterRouterSyncCountTooMuchTimes` | Router*1000+4 | RouterSYN 次数超过 40 次 |
| `ERR_KcpRouterTooManyPackets` | Router*1000+5 | 单节点每秒包数超过 1000 |
| `ERR_KcpRouterSyncCountTooMuchTimes` | Router*1000+6 | SYN 次数超过 20 次 |

---

## 实现原理

### 软路由架构

```
客户端
  │  HTTP GET /get_router
  ▼
RouterManager (HTTP 服务)
  │  返回 Realm列表 + Router列表
  ▼
客户端随机选一台 Router
  │
  │  UDP/TCP (外网)
  ▼
Router (RouterComponent)
  │  UDP (内网 InnerIP:0)
  ▼
内网 Realm/Gate 服务器
```

### 双协议支持

- **非 WebGL**：同时监听 UDP（`OuterUdp`）和 TCP（`OuterTcp`），客户端 UDP 优先，可降级 TCP
- **WebGL**：仅 TCP（WebSocketTransport）

### 连接状态机

```
[New RouterNode]
    Status = Sync
        │
        │  RouterSYN ─────────→ 内网（转发给 Realm/Gate）
        │  内网 ACK ←─────────── 内网返回
        │  转发 ACK 给客户端
        │
    Status = Msg
        │
        │  MSG ⇄ 内网转发
        │  FIN ⇄ 内网转发
```

### 握手流程（RouterSYN 路径）

1. 客户端发 `RouterSYN`（outerConn + innerConn=0 + connectId + realAddress）
2. Router 创建/查找 `RouterNode`，校验 connectId、内网地址后，回复 `RouterACK`（innerConn=0, outerConn）给客户端
3. 客户端发标准 `SYN`（outerConn + innerConn）
4. Router 将 SYN 连同客户端 IP 地址转发给内网服务器
5. 内网服务器回 `ACK`（innerConn + outerConn）
6. Router 记录 InnerConn，状态转为 `Msg`，将 ACK 转发给客户端
7. 后续 MSG / FIN 双向透明转发

### 断线重连（RouterReconnectSYN 路径）

客户端换了 IP/端口后，发 `RouterReconnectSYN`（携带 outerConn+innerConn+connectId+realAddress），Router 通过 outerConn 找到已有节点，校验 innerConn+outerConn+connectId 及内网地址后，继续转发，无需重建内网连接。

### 防攻击机制

| 机制 | 实现 |
|------|------|
| 隐藏内网地址 | 客户端只知道 Router 外网地址 |
| IP 校验 | SYN 阶段检测 IP 变化 |
| InnerConn/OuterConn 双校验 | 防止伪造连接劫持 |
| ConnectId 校验 | 防止重放旧连接 |
| RouterSyncCount 限制（40次） | 防止握手洪水 |
| SyncCount 限制（20次） | 防止 SYN 泛洪 |
| 每秒 1000 包限制 | 防止流量攻击 |
| 超时清理 | Sync 10s / Msg Session+10s 自动断开 |

---

## 关键流程图

### 正常连接建立

```
Client              Router              Inner Server
  │                   │                      │
  ├─RouterSYN────────→│                      │
  │                   ├─RouterSYN────────────→│(转发至内网)
  │←─RouterACK────────┤                      │
  ├─SYN──────────────→│                      │
  │                   ├─SYN+ClientIP─────────→│
  │                   │←─ACK──────────────────┤
  │←─ACK──────────────┤(InnerConn记录, Status=Msg)
  │                   │                      │
  ├─MSG──────────────→│                      │
  │                   ├─MSG──────────────────→│
  │                   │←─MSG──────────────────┤
  │←─MSG──────────────┤                      │
```

---

## 依赖关系

### 运行时依赖（来自代码引用）

| 依赖 | 用途 |
|------|------|
| `cn.etetet.core` | Entity/Scene/Fiber 基础、IKcpTransport、KcpProtocalType、NetworkHelper、TimeInfo |
| `cn.etetet.startconfig` | StartSceneConfigCategory、StartProcessConfig（读取服务器配置） |
| `cn.etetet.http` | HttpComponent、IHttpHandler、HttpHandler 特性 |
| `cn.etetet.netinner` | SessionIdleCheckerComponentSystem.SessionTimeoutTime（超时基准值） |
| `cn.etetet.proto` | MongoHelper.ToJson（序列化 HTTP 响应） |

### 被依赖关系

Router 是独立的网络基础设施 package，不被其他业务 package 依赖。客户端通过 `cn.etetet.login` 间接查询 RouterManager HTTP 接口获得路由列表。

---

## 性能考量

- **零拷贝转发**：使用固定 `Cache[1500]` 缓冲区，外→内、内→外直接 Send，无额外内存分配
- **超时检测分批**：每秒最多检查 10 个节点（`Queue` + 批量出队），避免大量节点时单帧卡顿
- **限流精度**：每秒重置计数（精度 1 秒滑动窗口），简单高效
- **对象池友好**：`RouterNode.Destroy` 清零所有字段，支持 ET 对象池复用

---

## Round 2 补充

### 细节修正

1. **限流阈值实际为 1001**：`CheckOuterCount` 中判断为 `>1000`，即第 1001 个包才触发限流，实际通过数为 1000。
2. **内网套接字端口为 0**：`InnerSocket` 绑定 `InnerIP:0`（系统分配随机端口），每台 Router 仅用一个 UDP 套接字与所有内网服务器通信（通过 `InnerIpEndPoint` 路由到具体服务器）。
3. **SYN 转发携带客户端 IP**：Router 将外网 `SYN` 转给内网时，在 9 字节头部后附加客户端 IP:Port 字符串，内网服务器可由此获知真实客户端地址。
4. **RouterReconnectSYN 转内网仅 9 字节**：重连时只转发 `[flag(1)+outerConn(4)+innerConn(4)]`，不再携带 realAddress。
5. **HTTP 应答故意延迟 1 秒**：`HttpGetRouterHandler` 在写入响应后再等 1000ms，目的是防止爬虫/高频请求耗尽路由列表接口。

### 代码示例

#### RouterNode 创建（New 方法关键路径）

```csharp
// 外网收到 RouterSYN 后创建节点
RouterNode routerNode = self.AddChildWithId<RouterNode>(outerConn); // outerConn 即 Entity.Id
routerNode.InnerConn    = 0;           // 握手阶段为 0，ACK 后赋值
routerNode.ConnectId    = connectId;   // 客户端随机生成，用于防重放
routerNode.InnerIpEndPoint = NetworkHelper.ToIPEndPoint(innerAddress); // 如 "10.0.0.1:10003"
routerNode.OuterIpEndPoint = outerIpEndPoint; // 客户端外网 IP:Port
routerNode.Status       = RouterStatus.Sync;
self.checkTimeout.Enqueue(outerConn);  // 加入超时检测队列
```

#### 外网包限流检查

```csharp
public static bool CheckOuterCount(this RouterNode self, long timeNow)
{
    if (timeNow - self.LastCheckTime > 1000)  // 每秒重置
    {
        self.LimitCountPerSecond = 0;
        self.LastCheckTime = timeNow;
    }
    if (++self.LimitCountPerSecond > 1000)    // 超过 1000 包/秒则拒绝
        return false;
    return true;
}
```

#### RouterACK 应答（RouterSYN 处理核心段）

```csharp
// RouterSYN 握手阶段：Router 回复 RouterACK 给客户端
self.Cache.WriteTo(0, KcpProtocalType.RouterACK);
self.Cache.WriteTo(1, routerNode.InnerConn); // 此时为 0
self.Cache.WriteTo(5, routerNode.OuterConn); // 客户端自己的 outerConn
routerNode.KcpTransport.Send(self.Cache, 0, 9, routerNode.OuterIpEndPoint, ChannelType.Accept);
```

### 跨 Package 交互补充

| 交互 | 说明 |
|------|------|
| `cn.etetet.netinner` → Router | `SessionIdleCheckerComponentSystem.SessionTimeoutTime` 决定 Msg 阶段超时基准（Router 比 Session 多 10s 才断） |
| `cn.etetet.startconfig` → Router | `StartSceneConfigCategory.GetBySceneType(SceneType.Router/Realm)` 用于 HTTP 接口枚举所有路由/Realm 地址 |
| `cn.etetet.login` → RouterManager | 客户端登录流程通过 HTTP GET `/get_router` 获取路由和 Realm 列表 |

### 异常处理总结

| 场景 | 处理方式 |
|------|----------|
| 解析异常（Recv 抛出） | `catch(Exception e)` 记录日志并继续循环，不中断整个 Update |
| outerConn 冲突 | Log.Warning + break（静默忽略，客户端会重试换其他路由） |
| innerConn 不匹配 | Log.Warning + break |
| connectId 不匹配（reconnect） | OnError 断开老节点 |
| connectId 不匹配（新连接） | Log.Warning + break |
| 同节点 outerConn 但 connectId 不同 | `ERR_KcpRouterSame`，表示路由数量可能太少导致 outerConn 碰撞 |
