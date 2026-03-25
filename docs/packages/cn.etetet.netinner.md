# cn.etetet.netinner — ET.NetInner 内网消息模块

## 概述

**版本**：3.0.0
**作者**：tanghai (ET Framework)
**描述**：负责服务器进程间（内网）通信的消息路由与转发模块。

该模块是 ET 框架多进程架构中的核心网络层，实现了跨进程 Actor 消息的透明路由：同进程消息走内存队列，跨进程消息经由 NetInner 专用 Fiber 通过 TCP/KCP 网络传输。

---

## 目录结构

```
cn.etetet.netinner/
├── package.json
├── Scripts/
│   ├── Model/Server/
│   │   ├── A2NetInner_Message.cs      # 消息类型定义（单向消息/请求/响应）
│   │   ├── MessageSender.cs           # 消息发送器组件（路由决策）
│   │   └── ProcessOuterSender.cs      # 进程外发送器组件（TCP/KCP连接）
│   └── Hotfix/Server/
│       ├── FiberInit_NetInner.cs      # NetInner Fiber 初始化
│       ├── MessageSenderSystem.cs     # MessageSender 扩展方法
│       ├── ProcessOuterSenderSystem.cs # ProcessOuterSender 扩展方法（网络I/O）
│       ├── A2NetInner_MessageHandler.cs # 单向消息处理器
│       └── A2NetInner_RequestHandler.cs # 请求消息处理器
```

---

## 核心类说明

### Model 层

#### `A2NetInner_Message` (MessageId=1)
跨进程单向消息的包装类，继承 `MessageObject`，实现 `IMessage`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `FromAddress` | `Address` | 发送方 Fiber 地址 |
| `ActorId` | `ActorId` | 目标 Actor ID |
| `MessageObject` | `IMessage` | 被包装的原始消息 |

- 使用 `ObjectPool` 对象池管理生命周期，`Dispose()` 回收至池中。

#### `A2NetInner_Request` (MessageId=2)
跨进程 RPC 请求的包装类，继承 `MessageObject`，实现 `IRequest`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `RpcId` | `int` | RPC 唯一标识符 |
| `ActorId` | `ActorId` | 目标 Actor ID |
| `MessageObject` | `IRequest` | 被包装的原始请求 |

- 响应类型：`A2NetInner_Response`（`[ResponseType]` 标注）。

#### `A2NetInner_Response` (MessageId=3)
跨进程 RPC 响应的包装类，实现 `IResponse`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `RpcId` | `int` | 对应请求的 RPC ID |
| `Error` | `int` | 错误码 |
| `Message` | `string` | 错误消息 |
| `MessageObject` | `IResponse` | 被包装的原始响应 |

---

#### `MessageSender` (Component)
**路由决策组件**，挂载于 `Scene`，负责判断消息是走进程内还是跨进程路径。

```
[ComponentOf(typeof(Scene))]
public class MessageSender: Entity, IAwake, IDestroy
```

| 字段 | 说明 |
|------|------|
| `TIMEOUT_TIME = 40000ms` | RPC 超时时间 40 秒 |
| `RpcId` | 自增 RPC ID 计数器 |
| `requestCallback` | `Dictionary<int, MessageSenderStruct>` — 挂起的 RPC 回调表 |

---

#### `ProcessOuterSender` (Component)
**进程外网络发送器**，挂载于 Scene，管理与其他进程的实际 TCP/KCP 网络连接。

```
[ComponentOf(typeof(Scene))]
public class ProcessOuterSender: Entity, IAwake<IPEndPoint>, IUpdate, IDestroy
```

| 字段 | 说明 |
|------|------|
| `TIMEOUT_TIME = 40000ms` | 超时时间 |
| `RpcId` | RPC ID 计数器 |
| `requestCallback` | 挂起的 RPC 回调表 |
| `AService` | 底层网络服务（TService/KService） |
| `InnerProtocol = KCP` | 默认使用 KCP 协议 |

---

### Hotfix 层

#### `FiberInit_NetInner`
NetInner Fiber 的初始化器，在 `SceneType.NetInner` 类型的 Fiber 启动时执行。

**初始化流程**：
1. 添加 `MailBoxComponent`（无序消息邮箱）
2. 添加 `TimerComponent`（计时器）
3. 添加 `CoroutineLockComponent`（协程锁）
4. 读取 `StartProcessConfig` 获取 IP 端点
5. 添加 `ProcessOuterSender`（启动 TCP/KCP 监听服务器）
6. 添加 `ProcessInnerSender`（进程内消息转发）

---

#### `MessageSenderSystem`
`MessageSender` 组件的功能实现（ECS 扩展方法模式）。

**`Send(ActorId, IMessage)`** — 单向消息发送：
```
if (同进程) → ProcessInnerSender.Send()
else        → 包装为 A2NetInner_Message → 发送到本进程 NetInner Fiber
```

**`Call(ActorId, IRequest, needException)`** — RPC 请求：
```
if (同进程) → ProcessInnerSender.Call()
else        → 包装为 A2NetInner_Request → ProcessInnerSender.Call(NetInner Fiber)
             → 解包 A2NetInner_Response.MessageObject 返回
```

---

#### `ProcessOuterSenderSystem`
`ProcessOuterSender` 的完整实现，处理网络 I/O。

**关键方法**：

| 方法 | 说明 |
|------|------|
| `Awake(IPEndPoint)` | 创建 TService 或 KService，注册回调 |
| `Update()` | 驱动 AService 轮询 |
| `Send(ActorId, IMessage)` | 对外发送消息 |
| `Call(ActorId, IRequest)` | 对外 RPC 调用（含40秒超时） |
| `OnRead(channelId, MemoryBuffer)` | 收到网络数据：反序列化 → 路由 |
| `OnAccept(channelId, IPEndPoint)` | 接受新连接 |
| `OnError(channelId, error)` | 连接错误处理 |
| `Get(channelId)` | 获取或创建到目标进程的 Session |
| `HandleIActorResponse(IResponse)` | 匹配 RpcId，唤醒等待的协程 |

**`OnRead` 路由逻辑**：
```
收到消息
├── IResponse → HandleIActorResponse（唤醒 RPC 等待协程）
├── IRequest/ILocationRequest → 异步转发给 ProcessInnerSender.Call()，再把响应发回来源进程
└── IMessage → ProcessInnerSender.Send()
```

---

#### `A2NetInner_MessageHandler`
处理 NetInner Fiber 收到的 `A2NetInner_Message`（单向消息）：
```csharp
root.GetComponent<ProcessOuterSender>().Send(innerMessage.ActorId, innerMessage.MessageObject);
```
将解包后的消息通过 ProcessOuterSender 发送到目标进程。

#### `A2NetInner_RequestHandler`
处理 NetInner Fiber 收到的 `A2NetInner_Request`（RPC 请求）：
```csharp
IResponse res = await root.GetComponent<ProcessOuterSender>().Call(request.ActorId, request.MessageObject, false);
res.RpcId = rpcId;
response.MessageObject = res;
```
将响应包装进 `A2NetInner_Response.MessageObject` 返回。

---

## 架构模式

### 消息路由分层

```
业务代码
    │ MessageSender.Send/Call(ActorId, message)
    ▼
[同进程?]──Yes──→ ProcessInnerSender（内存队列，零网络开销）
    │No
    ▼
封装为 A2NetInner_Message/Request
    │
    ▼ MessageQueue（进程内，发往 NetInner Fiber）
[NetInner Fiber]
    │ A2NetInner_MessageHandler / A2NetInner_RequestHandler
    ▼
ProcessOuterSender
    │ TCP/KCP 网络
    ▼
目标进程的 ProcessOuterSender
    │ OnRead 解包
    ▼
ProcessInnerSender → 目标 Actor
```

### 进程间 Session 管理

`ProcessOuterSender` 以**进程号（Process ID）作为 channelId** 管理 Session：
- 首次通信时，从 `StartProcessConfigCategory` 查找目标进程的 IP 端点
- 使用 `CreateInner()` 创建并缓存 Session
- 后续通信复用已有 Session

### RPC 超时机制

```
Call() {
    1. 分配 RpcId，存入 requestCallback
    2. SendInner() 发出请求
    3. 启动 Timeout() 协程（40秒后触发）
    4. await messageSenderStruct.Wait()

    // Timeout协程：
    if (超时) → requestCallback.Remove(rpcId) → SetException/SetResult(ERR_Timeout)

    // 正常响应：
    OnRead → HandleIActorResponse → requestCallback.Remove → SetResult(response)
}
```

---

## 关键流程图

### 跨进程单向消息流

```
进程A Fiber X
  MessageSender.Send(actorId[进程B], msg)
    → A2NetInner_Message { From=FiberX, ActorId=actorId, Msg=msg }
    → MessageQueue.Send(NetInner Fiber of 进程A)
        → A2NetInner_MessageHandler.Run()
            → ProcessOuterSender.Send(actorId, msg)
                → session[进程B].Send(actorId, msg)  [KCP/TCP]

进程B ProcessOuterSender.OnRead()
  → 解包 message, actorId
  → actorId.Process = 进程B
  → ProcessInnerSender.Send(actorId, message)
      → 进程B Fiber Y（目标Actor）
```

### 跨进程 RPC 调用流

```
进程A MessageSender.Call(actorId[进程B], request)
  → A2NetInner_Request { ActorId=actorId, Msg=request }
  → ProcessInnerSender.Call(NetInner Fiber)  [进程内]
      → NetInner: A2NetInner_RequestHandler.Run()
          → ProcessOuterSender.Call(actorId, request.MessageObject, needException=false)
              → rpcId++, requestCallback[rpcId] = callback
              → SendInner() [KCP/TCP]

进程B ProcessOuterSender.OnRead()
  → IRequest → ProcessInnerSender.Call(actorId, req, false) → 目标Actor
  → 得到 res
  → res.RpcId = rpcId (进程A发来的rpcId)
  → Send(fromProcess, res)  [KCP/TCP 回包]

进程A ProcessOuterSender.OnRead()
  → IResponse → HandleIActorResponse(res)
  → requestCallback[rpcId].SetResult(res)  [唤醒等待协程]

A2NetInner_RequestHandler 得到 res
  → A2NetInner_Response.MessageObject = res
  → 返回给进程A的业务调用方
```

---

## 依赖关系

### 依赖的核心概念/组件
| 依赖 | 来源 | 用途 |
|------|------|------|
| `ProcessInnerSender` | cn.etetet.core | 同进程 Actor 消息路由 |
| `MessageQueue` | cn.etetet.core | Fiber 间消息队列 |
| `AService` / `TService` / `KService` | cn.etetet.core | 底层 TCP/KCP 网络服务 |
| `Session` | cn.etetet.core | 网络连接封装 |
| `MessageSenderStruct` | cn.etetet.core | RPC 等待句柄 |
| `StartProcessConfigCategory` | cn.etetet.startconfig | 进程配置（IP端点） |
| `TimerComponent` | cn.etetet.core | 超时计时器 |
| `MailBoxComponent` | cn.etetet.core | 邮箱系统 |
| `MessageSerializeHelper` | cn.etetet.core | 消息序列化/反序列化 |
| `ObjectPool` | cn.etetet.core | 对象池 |

### 被依赖关系
- 所有需要**跨进程 Actor 通信**的模块都通过 `MessageSender.Send/Call()` 接口使用本模块
- 本模块的 `ProcessOuterSender` 被 `FiberInit_NetInner` 独家挂载，运行于专用的 `SceneType.NetInner` Fiber

---

## 关键设计决策

### 1. 专用 NetInner Fiber
跨进程通信被隔离到一个专用 Fiber（SceneType.NetInner），避免网络 I/O 阻塞业务 Fiber，保证业务逻辑的单线程安全性。

### 2. 消息自动二次包装
业务代码对路由完全透明：只需调用 `MessageSender.Send/Call()`，框架自动判断是否需要包装为 `A2NetInner_*` 消息并跨网络传输。

### 3. Process 地址替换技巧
```csharp
// 发送时：将本进程号填入 actorId.Process（让目标进程知道来自哪里）
actorId.Process = fiber.Process;
session.Send(actorId, message);

// 接收时：将 actorId.Process 替换为本进程号（让 ProcessInnerSender 路由到本进程内部）
int fromProcess = actorId.Process;
actorId.Process = fiber.Process;  // 改为本进程
```
通过这种地址替换，同一个 ActorId 结构在跨进程传输中携带了来源进程信息。

### 4. 默认 KCP 协议
内网通信使用 KCP（基于 UDP）而非 TCP，追求低延迟的同时保留可靠传输。

### 5. 超时警告
RPC 调用超过 200ms 会触发 `Log.Warning`，帮助排查性能问题和死锁。

---

## Round 2 补充：代码示例与细节修正

### 典型使用示例

#### 跨进程单向消息发送
```csharp
// 业务代码（任意 Fiber 中）
// MessageSender 挂载于 Scene 根节点，由框架统一注册
MessageSender messageSender = scene.GetComponent<MessageSender>();
messageSender.Send(targetActorId, new G2Map_PlayerEnter { ... });
// 框架自动判断：同进程 → ProcessInnerSender，跨进程 → NetInner Fiber → KCP/TCP
```

#### 跨进程 RPC 调用
```csharp
MessageSender messageSender = scene.GetComponent<MessageSender>();
IResponse response = await messageSender.Call(targetActorId, new G2Map_QueryPlayer { ... });
// needException=true (默认)：错误码非零时抛出 RpcException
```

#### 禁止抛异常的 RPC 调用（中转场景）
```csharp
// ProcessOuterSender 内部中转时使用 needException=false，避免中间节点抛异常
IResponse res = await processOuterSender.Call(actorId, req, needException: false);
```

### AssemblyReference 说明
- `Scripts/Model/Server/AssemblyReference.asmref` → 引用服务端 Model 程序集
- `Scripts/Hotfix/Server/AssemblyReference.asmref` → 引用服务端 Hotfix 热更程序集
- 两个 `.asmref` 文件确保代码仅在服务端编译，不进入客户端程序集

### Round 1 修正
- `MessageSender` 的 `requestCallback` 字典实际用于**进程内 RPC 等待**（虽然字段与 `ProcessOuterSender` 相同，但 `MessageSenderSystem` 对跨进程请求使用的是 `ProcessInnerSender.Call` 中间路径，而不是直接操作该字典）
- `ProcessOuterSender` 的 `requestCallback` 才是实际存储**跨进程 RPC 回调**的容器，由 `OnRead` 中的 `HandleIActorResponse` 匹配

### 性能考量
| 场景 | 路径 | 开销 |
|------|------|------|
| 同进程 Actor 消息 | MessageQueue（内存） | 极低 |
| 同进程 Actor RPC | ProcessInnerSender（内存） | 极低 |
| 跨进程单向消息 | MessageQueue → NetInner Fiber → KCP | 中等 |
| 跨进程 RPC | MessageQueue → NetInner Fiber → KCP → 目标进程 → 回包 | 较高 |

超时阈值：
- 200ms：触发 `Log.Warning` 警告
- 40,000ms：触发超时，`ERR_Timeout` 错误码或 RpcException
