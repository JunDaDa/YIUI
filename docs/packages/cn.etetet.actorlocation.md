# cn.etetet.actorlocation

## 概述

**Actor Location** 机制是 ET 框架分布式服务器架构的核心组件之一。它解决了跨服务器 Actor 寻址问题：当一个 Actor（如玩家角色）可以在不同服务器节点之间迁移时，其他服务器如何找到该 Actor 并向其发送消息。

该包实现了一个集中式位置注册/查询服务，并提供了带自动重试的消息发送机制。

- **版本**：3.0.0
- **作者**：tanghai（ET 框架）
- **描述**：实现了 actor location 机制
- **依赖**：无显式外部包依赖（依赖 cn.etetet.core 的 Entity/Scene 系统、消息系统、协程锁等）

---

## 目录结构

```
cn.etetet.actorlocation/
├── Proto/
│   └── ActorLocation_S_20100.proto        # Location 服务的 RPC 消息定义（消息号 20100 段）
├── Scripts/
│   ├── Model/
│   │   ├── Server/
│   │   │   ├── LocationComponent.cs           # Location 核心数据组件（LocationManagerComoponent、LocationOneType、LockInfo）
│   │   │   ├── LocationProxyComponent.cs      # Location 代理组件（数据，挂载于非 Location Scene）
│   │   │   ├── MessageLocationSender.cs       # 单个 Actor 发送器（数据，缓存 ActorId）
│   │   │   ├── MessageLocationSenderOneType.cs# 按类型分组的发送器管理（数据，含超时常量）
│   │   │   ├── MailBoxType.cs                 # 邮箱类型：OrderedMessage = PackageType.ActorLocation * 1000 + 1
│   │   │   ├── CoroutineLockType.cs           # 协程锁类型：Location / MessageLocationSender
│   │   │   ├── TimerInvokeType.cs             # 定时器类型：MessageLocationSenderChecker
│   │   │   └── ConsoleMode.cs                 # 控制台模式常量：CreateDB / DropDB / UpdateDB
│   │   └── Share/
│   │       ├── ErrorCode.cs                   # 错误码定义（6个，ERR_NotFoundActor 等）
│   │       ├── PackageType.cs                 # 包类型常量：ActorLocation = 3
│   │       └── SceneType.cs                   # 场景类型：Location = 3001
│   └── Hotfix/
│       └── Server/
│           ├── FiberInit_Location.cs                  # Location Fiber 初始化
│           ├── LocationOneTypeSystem.cs               # 位置数据操作逻辑（Add/Remove/Lock/UnLock/Get）
│           ├── LocationProxyComponentSystem.cs        # 位置代理逻辑（对外接口，含哈希分片）
│           ├── MailBoxType_OrderedMessageHandler.cs   # 有序消息邮箱处理器（串行化同一 Actor 的消息）
│           ├── MessageLocationHandler.cs              # Location 消息处理抽象基类（两个泛型变体）
│           ├── MessageLocationSenderComponentSystem.cs# 发送器管理逻辑（核心发送/重试逻辑）
│           ├── MessageLocationSenderSystem.cs         # 发送器生命周期逻辑（Awake/Destroy）
│           ├── ObjectAddRequestHandler.cs             # 添加位置注册处理器（Location Scene 专用）
│           ├── ObjectGetRequestHandler.cs             # 查询位置处理器
│           ├── ObjectLockRequestHandler.cs            # 锁定位置处理器
│           ├── ObjectRemoveRequestHandler.cs          # 移除位置注册处理器
│           └── ObjectUnLockRequestHandler.cs          # 解锁位置处理器
└── package.json
```

---

## 核心类/接口说明

### Model 层（数据定义）

#### `LocationManagerComoponent` (ComponentOf Scene)
Location 服务器端的顶级管理组件，挂载在 Location Scene 根节点上。通过 `Get(int locationType)` 获取或懒创建指定类型的 `LocationOneType`（以 locationType 作为 Id 创建子 Entity）。

#### `LocationOneType` (ChildOf LocationManagerComoponent)
按类型分组的位置数据存储。内含：
- `Dictionary<long, ActorId> locations`：key → ActorId 的映射表（key 通常是 Entity.Id / PlayerId）
- `Dictionary<long, EntityRef<LockInfo>> lockInfos`：当前正在被锁定的记录（迁移中的 Actor）

#### `LockInfo` (ChildOf LocationOneType)
描述一次锁定操作的元数据：
- `ActorId LockActorId`：发起锁定的 Actor（用于解锁时校验）
- `CoroutineLock CoroutineLock`：持有的协程锁（Dispose 时自动释放，解除对该 key 的 CoroutineLock 阻塞）

#### `LocationProxyComponent` (ComponentOf Scene)
非 Location 服务器上的代理组件，将 Add/Remove/Lock/UnLock/Get 请求转发给真正的 Location 服务器（通过 `key % locationConfigs.Count` 哈希选择目标 Location Scene）。

#### `MessageLocationSenderComponent` (ComponentOf Scene)
发送器管理器顶层组件，按 `locationType` 分组管理 `MessageLocationSenderOneType`（懒创建）。

#### `MessageLocationSenderOneType` (ChildOf MessageLocationSenderComponent)
针对某一 locationType 的发送器集合。提供：
- 超时常量 `TIMEOUT_TIME = 60000ms`（60秒）
- `CheckTimer`：每 10 秒触发一次 `MessageLocationSenderChecker` 定时器，扫描超时发送器
- 核心方法：`Send/Call(entityId, IMessage/IRequest)` 无重试版、`Send/Call(entityId, ILocationMessage/ILocationRequest)` 有重试版

#### `MessageLocationSender` (ChildOf MessageLocationSenderOneType)
缓存单个目标 Actor 的位置信息（以目标 EntityId 为 Id 创建）：
- `ActorId ActorId`：上次查到的 ActorId（可能已过期，发送失败时清空重新查询）
- `long LastSendOrRecvTime`：上次通信时间（用于超时回收）

---

### Hotfix 层（逻辑实现）

#### `LockInfoSystem`
`LockInfo` 的生命周期系统：
- `Awake(lockActorId, coroutineLock)`：记录持有者 ActorId 和协程锁
- `Destroy()`：释放协程锁（`CoroutineLock.Dispose()`），触发所有等待该 key 的协程恢复执行

#### `LocationOneTypeSystem`
操作 `LocationOneType` 的扩展方法系统，所有操作均通过 `CoroutineLockType.Location` 协程锁保证并发安全：
- `Add(key, actorId)`：注册 Actor 位置，`locations[key] = actorId`
- `Remove(key)`：注销 Actor 位置，`locations.Remove(key)`
- `Lock(key, actorId, time)`：获取协程锁（阻塞 Add/Remove/Get）并创建 LockInfo；`time > 0` 时启动超时自动解锁协程
- `UnLock(key, oldActorId, newActorId)`：验证 LockActorId 匹配后更新 `locations[key] = newActorId`，Dispose LockInfo 释放锁
- `Get(key)`：查询当前 ActorId

#### `LocationComoponentSystem`
操作 `LocationManagerComoponent` 的扩展方法：
- `Get(int locationType)`：懒创建 `LocationOneType`，以 locationType 作为 ChildId

#### `LocationProxyComponentSystem`
代理层扩展方法，对其他服务器暴露简洁接口：
- `GetLocationSceneId(key)` *(private)*：从 `StartSceneConfigCategory` 获取所有 Location Scene 配置，按 `key % count` 哈希选择
- `Add/Remove/Lock/UnLock/Get`：封装为对应 `ObjectXxxRequest` RPC 发往 Location 服务器
- `AddLocation(type)` / `RemoveLocation(type)`：Entity 扩展方法，快捷注册/注销自身位置（`self.Id` 作为 key，`self.GetActorId()` 作为 ActorId）

#### `MessageLocationSenderComponentSystem`
核心发送逻辑（含两套机制）：

**无重试版**（`IMessage` / `IRequest`）：
```csharp
// Send - 不阻塞，fire and forget
void Send(long entityId, IMessage message)
// Call - 异步，返回 IResponse
async ETTask<IResponse> Call(long entityId, IRequest request)
```
- 若 ActorId 为空，先通过 `LocationProxyComponent.Get()` 查询
- 查询/发送均在 `CoroutineLockType.MessageLocationSender` 锁保护下
- 找不到 Actor 时**不重试**，直接抛出异常或返回错误

**有重试版**（`ILocationMessage` / `ILocationRequest`）：
```csharp
// Send - 不阻塞
void Send(long entityId, ILocationMessage message)
// Call - 异步，带重试
async ETTask<IResponse> Call(long entityId, ILocationRequest request)
```
- 通过 `CallInner` 循环重试（最多 20 次）
- 遇到 `ERR_NotFoundActor`：等待 500ms，清空 `ActorId` 缓存，重新查 Location，继续尝试
- 遇到 `ERR_MessageTimeout`：直接抛出异常（不重试）
- 超过 20 次失败后：Dispose `MessageLocationSender`，返回错误响应

**超时清理**（`Check`）：
- 每 10 秒扫描，`LastSendOrRecvTime + 60s < now` 则 Dispose 该 `MessageLocationSender`

#### `MailBoxType_OrderedMessageHandler`
有序邮箱处理器（`[Invoke(MailBoxType.OrderedMessage)]`），通过 `CoroutineLockType.Mailbox` 保证同一 Actor 的消息**串行**处理（FIFO 顺序）。与 `MailBoxType.UnOrderedMessage`（并发处理）相对。

#### `MessageLocationHandler<E, Message>`
单向消息处理基类（`ILocationMessage`）：
- 先发送 `MessageResponse`（ACK），再执行 `Run()`
- 保证响应立即返回，业务逻辑异步执行（不阻塞发送方等待）

#### `MessageLocationHandler<E, Request, Response>`
请求-响应处理基类（`ILocationRequest`/`ILocationResponse`）：
- 执行 `Run()`，捕获 `RpcException` 和一般 `Exception`
- 通过 `CoroutineLockType.MessageLocationSender` 锁确保**响应在 handler 处理完成后**才发出（避免因 Location 查询导致响应先于消息处理返回）

#### Request Handlers（均注册于 `SceneType.Location`）
| 类名 | 处理消息 | 操作 |
|---|---|---|
| `ObjectAddRequestHandler` | `ObjectAddRequest` | 注册 Actor 位置 |
| `ObjectRemoveRequestHandler` | `ObjectRemoveRequest` | 注销 Actor 位置 |
| `ObjectLockRequestHandler` | `ObjectLockRequest` | 锁定 Actor 位置（阻塞其他操作） |
| `ObjectUnLockRequestHandler` | `ObjectUnLockRequest` | 解锁并更新 ActorId |
| `ObjectGetRequestHandler` | `ObjectGetRequest` | 查询 ActorId |

---

## 常量定义

| 常量类 | 常量名 | 数值（PackageType.ActorLocation=3） | 含义 |
|---|---|---|---|
| `PackageType` | `ActorLocation` | `3` | 包类型 ID |
| `SceneType` | `Location` | `3001` | Location 服务器 Scene 类型 |
| `MailBoxType` | `OrderedMessage` | `3001` | 有序消息邮箱类型 |
| `CoroutineLockType` | `Location` | `3001` | Location 进程内 key 操作锁 |
| `CoroutineLockType` | `MessageLocationSender` | `3002` | 消息发送队列锁（防止并发查询 Location） |
| `TimerInvokeType` | `MessageLocationSenderChecker` | `3002` | 发送器超时检查定时器类型 |
| `ErrorCode` | `ERR_NotFoundActor` | ERR_WithException + 3001 | Actor 不存在（触发重试） |
| `ErrorCode` | `ERR_RpcFail` | ERR_WithException + 3002 | RPC 执行失败 |
| `ErrorCode` | `ERR_MessageTimeout` | ERR_WithException + 3003 | 消息超时（不重试） |
| `ErrorCode` | `ERR_ActorLocationSenderTimeout2` | ERR_WithException + 3004 | 查 Location 时 Sender 被销毁 |
| `ErrorCode` | `ERR_ActorLocationSenderTimeout3` | ERR_WithException + 3005 | 发送后 Sender 被销毁 |
| `ErrorCode` | `ERR_ActorLocationSenderTimeout4` | ERR_WithException + 3006 | 重试等待中 Sender 被销毁 |
| `ConsoleMode` | `CreateDB` / `DropDB` / `UpdateDB` | string | 控制台命令模式（数据库维护） |

---

## 实现原理

### Actor Location 核心思想

在 ET 框架的分布式服务器中，Actor 是可以在不同服务器节点（Fiber）上存在并接收消息的实体。当一个 Actor 从一个服务器迁移到另一个服务器时，其他服务器缓存的 ActorId 就会失效。

Actor Location 机制通过一个**集中式位置服务**解决这个问题：
1. Actor 创建/迁移时向 Location 服务注册（`AddLocation`）
2. 其他服务器需要发消息时，先查 Location 服务获取最新 ActorId（懒查询，缓存在 `MessageLocationSender` 中）
3. 缓存 ActorId，下次可直接使用（避免每次都查）
4. 若发送失败（`ERR_NotFoundActor`），清除缓存重新查 Location 重试（最多 20 次）

### Location 服务器的分片

一个大战区可以配置多个 Location 服务器（通过 `StartSceneConfigCategory` 配置），按 `key % locationConfigs.Count` 哈希分片。每个 Location 服务器管理一部分 key 的位置信息，实现水平扩展。

### 锁机制详解

Location 提供协程锁定功能，用于 Actor 迁移场景。**Lock 的本质是在 LocationOneType 中持有一个 CoroutineLock**，使得对该 key 的 Add/Remove/Get 操作（也需要相同 CoroutineLockType.Location 的锁）全部排队等待：

1. **迁移前**：旧服务器调用 `Lock(key, actorId, time)` → Location 服务器获取 key 的协程锁，创建 `LockInfo` 持有之
2. **期间**：任何其他服务器查询该 key 的 `Get()` 都会阻塞等待（协程锁排队）
3. **迁移后**：新服务器调用 `UnLock(key, oldActorId, newActorId)` → 更新 `locations[key] = newActorId`，Dispose `LockInfo` 释放锁 → 之前阻塞的查询恢复，返回新 ActorId

这保证了迁移过程中消息不会发到旧的服务器，也不会丢失。

### 消息类型区分

该包引入了两类消息接口（来自 cn.etetet.core/proto）：

| 消息接口 | 发送方式 | 特点 |
|---|---|---|
| `IMessage` / `IRequest` | `Send/Call(entityId, IMessage/IRequest)` | 无重试，需确保 ActorId 有效 |
| `ILocationMessage` / `ILocationRequest` | `Send/Call(entityId, ILocationMessage/ILocationRequest)` | 有重试（最多20次），Actor 迁移时自动重新定位 |

**适用场景**：
- 不迁移的 Actor（如静态 NPC、区域管理器）→ 用 `IMessage/IRequest`（性能更高，无锁队列限制）
- 可迁移的 Actor（如玩家 Unit）→ 用 `ILocationMessage/ILocationRequest`（容错，自动处理迁移）

### OrderedMessage 邮箱

`MailBoxType.OrderedMessage` 通过 `CoroutineLockType.Mailbox` + `ParentInstanceId` 确保同一 Entity 的所有消息串行处理（即使是异步消息也不并发执行）。这对于状态敏感的 Actor（如玩家角色）至关重要，防止并发消息导致状态竞争。

---

## 关键流程

### 流程一：Actor 注册位置

```
Entity（如 Player Unit）启动
  └─ self.AddLocation(locationType)
       └─ LocationProxyComponent.Add(type, self.Id, self.GetActorId())
            └─ ObjectAddRequest → Location 服务器（哈希选择）
                 └─ ObjectAddRequestHandler.Run()
                      └─ LocationManagerComoponent.Get(type).Add(key, actorId)
                           └─ CoroutineLock(Location, key)
                                └─ locations[key] = actorId
```

### 流程二：发送 ILocationRequest（带重试）

```
调用方
  └─ MessageLocationSenderOneType.Call(entityId, ILocationRequest)
       ├─ GetOrCreate(entityId) → MessageLocationSender（懒创建）
       ├─ CoroutineLock(MessageLocationSender, entityId)  // 队列化同一 entityId 的并发请求
       └─ CallInner(messageLocationSender, request)
            ├─ [若 ActorId == default] LocationProxyComponent.Get(type, id) → 查 Location
            ├─ MessageSender.Call(actorId, request, needException: false)
            ├─ [若 ERR_NotFoundActor]
            │    ├─ failTimes++（> 20 次则返回错误）
            │    ├─ WaitAsync(500ms)
            │    ├─ ActorId = default（清除缓存）
            │    └─ continue（重新查 Location）
            ├─ [若 ERR_MessageTimeout] throw RpcException（不重试）
            └─ 成功 → 返回响应
```

### 流程三：Actor 迁移（Lock/UnLock）

```
旧服务器
  └─ LocationProxyComponent.Lock(type, key, oldActorId, time=60000ms)
       └─ ObjectLockRequest → Location 服务器
            └─ LocationOneType.Lock(key, actorId, time)
                 ├─ 获取 CoroutineLock（阻塞其他 Add/Remove/Get）
                 ├─ 创建 LockInfo（持有协程锁）
                 └─ [time > 0] 启动超时自动解锁协程

[数据迁移至新服务器，ActorId 变化]

新服务器
  └─ LocationProxyComponent.UnLock(type, key, oldActorId, newActorId)
       └─ ObjectUnLockRequest → Location 服务器
            └─ LocationOneType.UnLock(key, oldActorId, newActorId)
                 ├─ 验证 LockActorId == oldActorId
                 ├─ locations[key] = newActorId
                 ├─ lockInfos.Remove(key)
                 └─ LockInfo.Dispose() → 释放 CoroutineLock
                      └─ 之前阻塞的 Get/Add/Remove 协程恢复执行，返回新 ActorId
```

### 流程四：发送器超时清理

```
TimerComponent（每10秒）
  └─ MessageLocationSenderChecker.Run(MessageLocationSenderOneType)
       └─ Check()
            └─ 遍历所有 MessageLocationSender
                 └─ now > LastSendOrRecvTime + 60000ms → self.Remove(id) → Dispose()
```

### 流程五：无重试消息发送（IMessage/IRequest）

```
调用方
  └─ MessageLocationSenderOneType.Send(entityId, IMessage)
       └─ SendInner(entityId, message) [NoContext - 不等待]
            ├─ GetOrCreate(entityId) → MessageLocationSender
            ├─ CoroutineLock(MessageLocationSender, entityId)
            ├─ [若 ActorId == default] LocationProxyComponent.Get(type, id)
            └─ MessageSender.Send(actorId, message)  // 不等待响应
```

---

## 依赖关系

### 依赖的外部组件（来自 cn.etetet.core）

| 组件/接口 | 用途 |
|---|---|
| `CoroutineLockComponent` | 所有 Location 操作和消息发送的并发安全保证 |
| `TimerComponent` | 超时自动解锁（Lock time 参数）、定时清理 MessageLocationSender |
| `MessageSender` | 实际向目标 ActorId 发送消息 |
| `ProcessInnerSender` | 进程内消息回复（Response） |
| `MailBoxComponent` | Location 服务器的消息接收（UnOrderedMessage 类型） |
| `StartSceneConfigCategory` | 查询 Location 服务器地址列表（用于哈希分片） |
| `Entity` / `Scene` / `Fiber` | 实体框架基础设施 |
| `ObjectPool` | Response 对象复用 |

### 被依赖（使用此包的模块）

任何需要跨服发送消息给可迁移 Actor 的服务器模块，例如：
- `cn.etetet.login`：玩家登录/跨服 Gate 切换时注册/迁移位置
- `cn.etetet.move`：玩家跨场景移动时 Lock/UnLock
- `cn.etetet.statesync`：状态同步时发送 ILocationRequest
- `cn.etetet.unit`：Unit 创建/销毁时 Add/Remove Location

### Proto 消息（S端，20100段）

| 消息对 | 方向 | 字段说明 |
|---|---|---|
| `ObjectAddRequest/Response` | 任意→Location | Type: locationType, Key: entityId, ActorId: 目标 ActorId |
| `ObjectRemoveRequest/Response` | 任意→Location | Type + Key |
| `ObjectLockRequest/Response` | 任意→Location | Type + Key + ActorId + Time（ms） |
| `ObjectUnLockRequest/Response` | 任意→Location | Type + Key + OldActorId + NewActorId |
| `ObjectGetRequest/Response` | 任意→Location | 请求 Type+Key，响应 ActorId |

---

## Location Fiber 初始化

`FiberInit_Location` 在 `SceneType.Location`（值 3001）的 Fiber 初始化时（`[Invoke(SceneType.Location)]`）添加以下组件：
- `MailBoxComponent(MailBoxType.UnOrderedMessage)`：接收外部 RPC 消息（无序并发处理）
- `TimerComponent`：支持锁超时自动解锁
- `CoroutineLockComponent`：支持 Location 操作的并发安全
- `ProcessInnerSender`：进程内消息回复
- `MessageSender`：可主动发出消息
- `LocationManagerComoponent`：Location 数据存储的顶层管理

---

## 代码示例

### 示例一：Actor 启动时注册位置
```csharp
// Entity 扩展方法（来自 LocationProxyComponentSystem）
await self.AddLocation(locationType: LocationType.Unit);
// 等价于：
await self.Root().GetComponent<LocationProxyComponent>().Add(LocationType.Unit, self.Id, self.GetActorId());
```

### 示例二：向可迁移 Actor 发送消息（带重试）
```csharp
// 获取对应 locationType 的发送器
MessageLocationSenderOneType sender = scene.GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit);

// 发送 ILocationRequest（自动重试，可跨迁移）
IResponse response = await sender.Call(targetEntityId, new C2M_TestRequest { ... });

// 发送 ILocationMessage（fire and forget，不等待响应）
sender.Send(targetEntityId, new G2M_PlayerOnline { ... });
```

### 示例三：Actor 迁移流程
```csharp
// 1. 锁定（最长等60秒自动解锁）
await locationProxy.Lock(LocationType.Unit, unit.Id, unit.GetActorId(), time: 60000);

// 2. 迁移（在新服务器创建 Unit）
ActorId newActorId = await MigrateToNewServer(unit);

// 3. 解锁并更新地址
await locationProxy.UnLock(LocationType.Unit, unit.Id, unit.GetActorId(), newActorId);
```

### 示例四：实现 Location 消息处理器
```csharp
// 处理不需要响应的 ILocationMessage
[MessageHandler(SceneType.Map)]
public class G2M_TestHandler : MessageLocationHandler<Unit, G2M_Test>
{
    protected override async ETTask Run(Unit unit, G2M_Test message)
    {
        // 业务逻辑
    }
}

// 处理 ILocationRequest（需要响应）
[MessageHandler(SceneType.Map)]
public class C2M_TestRequestHandler : MessageLocationHandler<Unit, C2M_TestRequest, C2M_TestResponse>
{
    protected override async ETTask Run(Unit unit, C2M_TestRequest request, C2M_TestResponse response)
    {
        response.Data = unit.GetData();
    }
}
```

---

## 注意事项与边界情况

### 并发安全
- `LocationOneType` 的所有操作都在 `CoroutineLockType.Location` 下执行，同一 key 的操作串行化
- `MessageLocationSenderOneType` 的 `CallInner` 在 `CoroutineLockType.MessageLocationSender` 下执行，防止多个协程同时查询 Location 并覆盖 ActorId 缓存

### 发送器被并发销毁的检测
代码中多处通过比对 `instanceId == messageLocationSender.InstanceId` 来检测在 await 期间 Sender 是否被销毁（超时清理或主动 Remove），若已销毁则抛出对应超时错误码（2/3/4）。

### 手动 Remove 场景
如玩家断线重连到新 Gate，旧的 `MessageLocationSender` 缓存了错误的 ActorId。调用方需主动调用 `Remove(entityId)` 清除缓存，下次发送时重新查询 Location。

### Lock 超时保护
`Lock(key, actorId, time)` 支持超时自动解锁，防止迁移进程崩溃后 Location 被永久锁死。默认 `LocationProxyComponent.Lock` 传入 `time=60000`（60秒）。

### Location 服务器为 UnOrderedMessage
Location Fiber 使用 `MailBoxType.UnOrderedMessage`，即消息并发处理。但 Location 操作内部已通过 `CoroutineLockType.Location` 保证同一 key 的操作串行，不同 key 可并发处理（高效）。
