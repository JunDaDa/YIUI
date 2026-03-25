# cn.etetet.core — ET框架核心包

**版本**: 3.0.3
**显示名**: ET.Core
**作者**: tanghai (ET框架)
**描述**: ET框架核心，实现了纤程、网络、Entity等ET基础功能

---

## 概述

`cn.etetet.core` 是整个 ET MMO 框架的基石，实现了以下核心机制：

1. **Entity 组件系统（ECS 变种）** — 基于 Entity + Component + System 的架构
2. **Fiber（纤程）系统** — 轻量级并发执行单元，支持多线程/主线程/线程池三种调度模式
3. **ETTask 异步系统** — 自定义 async/await 实现，避免 Unity Task 的 GC 压力
4. **World 与 Singleton 系统** — 全局单例管理容器
5. **Event/Invoke 系统** — 发布订阅与精确调用两种通信模式
6. **网络系统** — 封装 KCP/TCP 两种传输协议
7. **Actor 消息系统** — 基于 ActorId 的跨 Fiber 消息分发
8. **协程锁（CoroutineLock）** — 异步场景下的互斥保护
9. **Timer 系统** — 定时器组件
10. **对象池（ObjectPool）** — 线程安全的无锁对象池
11. **ID 生成器** — 全局唯一分布式 ID（进程号+时间+序号）

---

## 目录结构

```
cn.etetet.core/
├── package.json
├── Scripts/
│   ├── Core/
│   │   └── Share/
│   │       ├── Entity/          # Entity 核心类
│   │       ├── ETTask/          # 异步任务系统
│   │       ├── Fiber/           # Fiber 每帧更新 & EntitySystem
│   │       ├── Network/         # 网络层（KCP/TCP）
│   │       ├── Object/          # 基类对象（DisposeObject等）
│   │       ├── Serialize/       # 序列化（Bson/MemoryPack）
│   │       ├── World/           # 全局系统（World/Singleton/EventSystem等）
│   │       ├── Helper/          # 工具类
│   │       └── ...             # 集合工具（MultiMap/UnOrderMultiMap等）
│   ├── Model/
│   │   └── Share/
│   │       ├── Actor/           # Actor 消息分发
│   │       ├── CoroutineLock/   # 协程锁
│   │       ├── Message/         # 网络 Session
│   │       ├── Timer/           # 定时器模型
│   │       └── ...
│   └── Hotfix/
│       └── Share/
│           ├── Actor/           # 进程内 Actor 处理
│           ├── CoroutineLock/   # 协程锁 System 实现
│           ├── Message/         # Session/NetComponent System
│           └── Timer/           # Timer System 实现
└── Editor/
    └── ComponentViewEditor/     # Unity 编辑器 Entity 调试视图
```

---

## 核心类/接口详解

### 1. Entity（`Scripts/Core/Share/Entity/Entity.cs`）

所有游戏对象/组件的基类，混合了 ECS 中 Entity 与 Component 的概念。

```csharp
public abstract partial class Entity: DisposeObject, IPool
```

**关键字段与属性**：
- `InstanceId: long` — 运行时唯一 ID（不持久化）
- `Id: long` — 持久化 ID（由 IdGenerater 生成）
- `Parent: Entity` — 父 Entity
- `IScene: IScene` — 所属 Scene（用于访问 Fiber 上下文）
- `Components: ComponentsCollection` — 组件字典
- `Children: ChildrenCollection` — 子 Entity 字典
- `IsDisposed: bool`
- `IsFromPool: bool` — 是否来自对象池
- `IsComponent: bool` — 是否作为组件添加
- `IsNew: bool` — 是否为新创建（非反序列化恢复）

**核心方法**：
- `AddComponent<T>()` / `AddChild<T>()` — 添加组件或子Entity，触发 Awake
- `GetComponent<T>()` — 获取组件
- `RemoveComponent<T>()` / `Dispose()` — 移除组件，触发 Destroy
- `Fiber()` — 扩展方法，获取所属 Fiber
- `Scene<T>()` — 扩展方法，获取指定类型的 Scene 组件

**EntityStatus 枚举**（位标志）：
```
IsFromPool, IsRegister, IsComponent, IsNew, IsSerilizeWithParent
```

---

### 2. Scene（`Scripts/Core/Share/Entity/Scene.cs`）

特殊的 Entity，代表一个纤程的根节点。

```csharp
[EnableMethod][ChildOf]
public class Scene: Entity, IScene
```

- 持有 `Fiber` 引用
- 拥有 `SceneType: int`（由 `SceneTypeSingleton` 管理名称映射）
- 所有 Entity 都通过 Scene 找到 Fiber 上下文

---

### 3. Fiber（`Scripts/Core/Share/Fiber/Fiber.cs`）

ET 框架的并发执行单元，类似"轻量线程"或"Actor"。

```csharp
public class Fiber: IDisposable
```

**关键字段**：
- `Id: int` — 纤程 ID
- `Zone: int` — 所属区服编号
- `Root: Scene` — 根 Scene
- `EntitySystem: EntitySystem` — 该 Fiber 的实体系统（管理 Update/LateUpdate 队列）
- `Mailboxes: Mailboxes` — 邮箱系统，用于接收跨 Fiber 消息
- `ThreadSynchronizationContext` — 线程同步上下文，用于将回调 Post 到 Fiber 线程

**生命周期方法**（内部调用）：
- `Update()` — 发布 UpdateEvent
- `LateUpdate()` — 发布 LateUpdateEvent，处理帧结束任务，更新同步上下文
- `WaitFrameFinish()` — 等待当前帧结束

**注意**：`Fiber.Instance` 是 ThreadStatic，每个线程只能访问当前执行的 Fiber。

---

### 4. FiberManager（`Scripts/Core/Share/World/Fiber/FiberManager.cs`）

全局管理所有 Fiber 的单例。

```csharp
public class FiberManager: Singleton<FiberManager>, ISingletonAwake, ISingletonReverseDispose
```

**调度器类型**：
- `SchedulerType.Main` — 主线程调度（Unity 主线程）
- `SchedulerType.Thread` — 独立线程调度
- `SchedulerType.ThreadPool` — 线程池调度

**重要设计**：
- WebGL / Unity Editor 模式下，所有调度器均为 MainThreadScheduler
- Fiber 之间不能直接获取引用（`Get()` 为 internal），只能通过消息通信

---

### 5. World（`Scripts/Core/Share/World/World.cs`）

全局单例容器，管理所有 `ASingleton` 的生命周期。

```csharp
public class World: IDisposable
```

- 通过 `World.Instance.AddSingleton<T>()` 注册单例
- 支持最多 4 个参数的 Awake 初始化
- 通过 `ISingletonReverseDispose` 标记需要逆序销毁的单例（如 FiberManager）
- Dispose 时栈式销毁（逆序），再清理剩余单例

---

### 6. Singleton<T>（`Scripts/Core/Share/World/Singleton.cs`）

线程不安全但高性能的单例基类。

```csharp
public abstract class Singleton<T>: ASingleton where T: Singleton<T>
```

- `Instance` 为静态属性，由 `World.AddSingleton` 设置
- 子类可重写 `Destroy()` 做清理

---

### 7. EventSystem（`Scripts/Core/Share/World/EventSystem/EventSystem.cs`）

全局事件发布与 Invoke 分发系统。

**两种通信模式**：

| 模式 | 特点 | 使用场景 |
|------|------|---------|
| `Publish` | 广播，可以无订阅者 | 跨模块通知（如道具使用通知任务系统） |
| `Invoke` | 必须有被调用者，否则异常 | 同模块内的策略分发（如客户端/服务端加载Config方式不同） |

**Publish 流程**：
1. 通过 `[EventAttribute]` 标记的类自动注册
2. 发布时过滤 `SceneType`，只调用匹配的处理器
3. 支持异步（`PublishAsync`）和同步（`Publish`）

**Invoke 流程**：
1. 通过 `[InvokeAttribute(type)]` 标记，`type` 为 long 区分
2. 调用时必须有对应 handler，否则抛异常

---

### 8. EntitySystemSingleton（`Scripts/Core/Share/Entity/EntitySystemSingleton.cs`）

管理所有 Entity System 的注册与执行。

```csharp
[CodeProcess]
public class EntitySystemSingleton: Singleton<EntitySystemSingleton>, ISingletonAwake
```

- 启动时扫描所有带 `[EntitySystemAttribute]` 的 System 类并注册到 `TypeSystems`
- 为每个 Entity 类型维护 `OneTypeSystems`（System 类型 → SystemObject 列表）
- 提供 `Awake()`, `Destroy()`, `Serialize()`, `Deserialize()` 等分发方法

**TypeSystems**：
- `Dictionary<Type, OneTypeSystems>` — Entity 类型 → 对应的所有 System
- `OneTypeSystems.Map` — System 类型 → SystemObject 实例列表
- `OneTypeSystems.ClassType` — 需要进入 Update 队列的 System 类型

---

### 9. EntitySystem（`Scripts/Core/Share/Fiber/EntitySystem.cs`）

每个 Fiber 私有的，管理 Update/LateUpdate 队列的类。

- `queues: Dictionary<Type, Queue<EntityRef<Entity>>>` — 按 System 类型分组的 Entity 队列
- `RegisterSystem(Entity)` — 将 Entity 注册入 Update 队列
- `Publish<T>(T)` — 每帧遍历队列执行对应 System（如 `UpdateEvent`, `LateUpdateEvent`）

---

### 10. ETTask（`Scripts/Core/Share/ETTask/ETTask.cs`）

ET 框架自定义的 async/await 实现。

```csharp
[AsyncMethodBuilder(typeof(ETAsyncTaskMethodBuilder))]
public class ETTask: ICriticalNotifyCompletion, IETTask
```

**设计要点**：
- 带对象池支持（`ETTask.Create(fromPool: true)`），减少 GC
- `ETCancellationToken` — 取消令牌，可传递取消信号
- `TaskType` 枚举：`Common`, `WithContext`, `ContextTask`
- `ETTaskHelper.WaitAll()` — 并发等待多个 ETTask
- `ETVoid` — 不等待的异步方法返回类型（类似 `async void`）

**注意**：使用对象池的 ETTask，await 之后不能再操作，否则可能操作到池中分配给其他人的实例。

---

### 11. 网络系统

#### AService / AChannel
- `AService` — 网络服务抽象基类（KCP/TCP 实现）
- `AChannel` — 网络通道抽象基类

#### KService（`Scripts/Core/Share/Network/KService.cs`）
基于 KCP 协议的网络服务实现。
- 支持 `ServiceType.Outer`（外部，面向客户端）和 `ServiceType.Inner`（内部，服务器间）
- KCP 协议类型：SYN/ACK/FIN/MSG/Router 系列

#### TService（TCP 服务）
基于 TCP 协议的网络服务实现。

#### Session（`Scripts/Model/Share/Message/Session.cs`）
代表一个网络连接会话。
- 管理 RPC 请求回调（`requestCallbacks: Dictionary<int, RpcInfo>`）
- 跟踪最后收发时间（用于空闲检测）
- `RpcInfo` — 包含 ETTask<IResponse>，等待响应

#### NetServices（`Scripts/Core/Share/Network/NetServices.cs`）
管理所有 AService 实例的单例。

---

### 12. Actor 消息系统

#### ActorId（`Scripts/Core/Share/World/ActorId.cs`）
跨 Fiber/进程的 Actor 地址标识：`Process + FiberId + EntityInstanceId`。

#### MessageDispatcher（`Scripts/Model/Share/Actor/MessageDispatcher.cs`）
- 扫描所有 `[MessageHandlerAttribute]` 标记的 Handler
- 按消息类型 + SceneType 路由分发
- `Handle(Entity, Address, MessageObject)` — 执行对应处理器

#### ProcessInnerSender（`Scripts/Model/Share/Actor/ProcessInnerSender.cs`）
进程内跨 Fiber 消息发送器，通过 Mailbox 投递。

---

### 13. CoroutineLock（协程锁）

保证异步代码段的互斥执行。

#### 关键组件：
- `CoroutineLockComponent` — Scene 级别组件，管理所有锁队列
- `CoroutineLock` — 锁对象 Entity，Dispose 时自动释放锁并通知下一个等待者
- `CoroutineLockQueue` — 等待队列

**使用模式**：
```csharp
using (await scene.GetComponent<CoroutineLockComponent>().Wait(lockType, key))
{
    // 临界区代码
}
```

---

### 14. Timer 系统

#### TimerComponent（`Scripts/Model/Share/Timer/TimerComponent.cs`）
- `TimerClass.OnceTimer` — 一次性定时器（回调）
- `TimerClass.OnceWaitTimer` — 一次性等待定时器（await）
- `TimerClass.RepeatedTimer` — 重复定时器
- 使用 `MultiMap<long, long>` (time → timerId) 管理调度

---

### 15. IdGenerater（`Scripts/Core/Share/World/IdGenerater/IdGenerater.cs`）

分布式 ID 生成器。

**ID 结构（64bit）**：
```
[14bit 进程号] [30bit 2022年起秒数] [20bit 序号]
```

**InstanceId 结构（64bit）**：
```
[32bit 时间戳] [32bit 递增序号]
```

---

### 16. ObjectPool（`Scripts/Core/Share/World/ObjectPool/ObjectPool.cs`）

线程安全的无锁对象池。
- 每种类型最多缓存 1000 个对象
- 双层缓存：`FastItem`（单元素快速访问）+ `ConcurrentQueue`
- `Fetch<T>()` — 取出对象
- `Recycle(obj)` — 归还对象（自动置 null 引用）

---

## 实现原理与架构模式

### 数据/逻辑分离（Model/Hotfix）

ET 框架采用严格的代码分层：
- **Model**（`Scripts/Model/`）— 纯数据定义，Entity 字段，不含逻辑
- **Hotfix**（`Scripts/Hotfix/`）— 热更新代码，包含所有 System 逻辑实现
- **Core**（`Scripts/Core/`）— 框架底层，不热更新

这种分离使得热更新只需重载 Hotfix 程序集，而 Model 数据保持稳定。

### Entity System 注册流程

```
启动 → CodeTypes 扫描 [EntitySystemAttribute] 类
     → EntitySystemSingleton.Awake() 注册到 TypeSystems
     → 创建 Entity 时调用 Fiber.EntitySystem.RegisterSystem()
     → 每帧 EntitySystem.Publish(UpdateEvent) 触发所有实现 IUpdate 的 Entity
```

### 事件系统对比

```
Publish → 广播，SceneType 过滤，0..N 订阅者，不关心结果
Invoke  → 精确调用，必须存在，返回值，类似虚函数分发
```

### Fiber 并发模型

```
World
 └─ FiberManager
     ├─ MainThreadScheduler → [Fiber 1 (Main)]
     ├─ ThreadScheduler     → [Fiber 2] [Fiber 3]
     └─ ThreadPoolScheduler → [Fiber 4] [Fiber 5] ...
```

每个 Fiber 内部单线程执行（通过 `ThreadSynchronizationContext` 保证），Fiber 间通过 Mailbox 消息通信，避免直接共享状态。

---

## 关键流程

### Entity 创建流程
```
Entity.AddChild<T>() / AddComponent<T>()
  → ObjectPool.Fetch<T>()
  → 设置 Parent/IScene/InstanceId/Id
  → EntitySystemSingleton.Awake(entity)    // 触发 Awake System
  → Fiber.EntitySystem.RegisterSystem()    // 注册 Update 队列
```

### Entity 销毁流程
```
Entity.Dispose()
  → EntitySystemSingleton.Destroy(entity)  // 触发 Destroy System
  → 递归 Dispose Components
  → 递归 Dispose Children
  → ObjectPool.Recycle(entity)
  → 从 Parent.Components/Children 移除
```

### 消息发送流程（进程内）
```
ProcessInnerSender.Send(ActorId, message)
  → FiberManager.Get(fiberId)
  → fiber.Mailboxes.Enqueue(message)       // 投递到目标 Fiber 邮箱
  → 目标 Fiber LateUpdate 处理邮箱消息
  → MessageDispatcher.Handle(entity, message)
```

### RPC 调用流程
```
Session.Call(request)
  → 分配 RpcId
  → 存入 requestCallbacks[rpcId]
  → Session.Send(request)                  // 序列化发送
  → 等待 await tcs.Wait()
  → 收到响应包 → 反序列化 → tcs.SetResult()
```

---

## 依赖关系

`cn.etetet.core` 是**零依赖**的基础包（`relatedPackages: {}`）。

所有其他 ET 包都依赖此包，它是整个系统的根基。

**外部依赖**（通过 NuGet/Unity Package）：
- `MemoryPack` — 高性能二进制序列化
- `MongoDB.Bson` — BSON 序列化（持久化）
- KCP 协议 C# 实现（内嵌）

---

## 编辑器工具（Editor/）

`ComponentViewEditor` — Unity 编辑器下的 Entity 调试工具：
- `IEntityDrawer` — Entity 绘制接口
- `ITypeDrawer` — 字段类型绘制接口
- 支持所有常见 Unity/C# 类型的调试显示（Vector3, Color, Dictionary 等）
- `EntityRefTypeDrawer` — EntityRef 类型的专用绘制器

---

## 重要注意事项

1. **不能在非 Fiber 线程直接操作 Entity** — 所有 Entity 操作需通过 `ThreadSynchronizationContext.Post()` 投递
2. **ETTask 对象池使用陷阱** — await 后不能再引用同一 ETTask 实例
3. **Fiber 间不能直接引用** — `FiberManager.Get()` 为 internal，只能通过消息
4. **Id vs InstanceId** — Id 用于持久化，InstanceId 仅运行时有效，重启后变化
5. **SceneType 过滤** — Event 和 MessageHandler 均按 SceneType 过滤，注册时需指定正确的 SceneType

---

## Round 2 补充：遗漏类、修正与代码示例

### EntityRef<T> 与 EntityWeakRef<T>（`Scripts/Core/Share/Entity/EntityRef.cs`）

ET 框架的**安全 Entity 引用**机制，防止持有已销毁 Entity 的悬空引用。

```csharp
public struct EntityRef<T>: IEquatable<EntityRef<T>> where T: Entity
```

**设计原理**：
- 内部保存 `instanceId`（long）+ 直接引用 `entity`
- 访问 `.Entity` 时，检查 `entity.InstanceId == instanceId`，不一致则返回 null（说明已销毁并被池回收）
- 支持隐式转换：`T → EntityRef<T>` 和 `EntityRef<T> → T`，使用透明

```csharp
// 存储 Actor 引用（安全，不阻止 GC）
EntityRef<Player> playerRef = player;
// 后续访问：entity.InstanceId 变化则自动返回 null
Player p = playerRef;
if (p == null) { /* 已销毁 */ }
```

**EntityWeakRef<T>**：
- 使用 `WeakReference<T>` 包装，真正不阻止 GC
- 适合缓存场景（如 AOI 邻居列表），避免内存泄漏
- 访问时先 `TryGetTarget`，再校验 InstanceId

**与 EntityRef<T> 的区别**：

| 类型 | GC 影响 | 适用场景 |
|------|---------|---------|
| `EntityRef<T>` | 阻止 GC（有强引用） | 跨帧安全持有，Entity 不会被 GC |
| `EntityWeakRef<T>` | 不阻止 GC | 弱缓存，不影响对象生命周期 |

---

### ChildrenCollection / ComponentsCollection（`Scripts/Core/Share/Entity/`）

Entity 子节点和组件的存储容器，均为 **对象池化的 SortedDictionary**：

```csharp
public class ChildrenCollection : SortedDictionary<long, Entity>, IPool
public class ComponentsCollection : SortedDictionary<long, Entity>, IPool
```

- Key 为 Entity 的 `InstanceId`（long），按 ID 排序
- 通过 `ObjectPool.Fetch<T>()` 分配，`Dispose()` 时 `Clear()` 并归还池
- `SortedDictionary` 保证遍历顺序确定（调试友好）
- 各有对应的 `DebuggerTypeProxy`（`ChildrenCollectionDebugView`），Unity 编辑器中展示为 Entity[] 数组

---

### ETTask Context 传播系统

Round 1 只提到了 TaskType 枚举，这里补充完整的 Context 传播机制：

**TaskType 语义**：

| 枚举值 | 含义 |
|--------|------|
| `Common` | 普通 ETTask，不携带 Context |
| `WithContext` | 携带 Context，传递给链式 await |
| `ContextTask` | 消费 Context 的特殊 Task（用于 GetContextAsync） |

**Context API**：
```csharp
// 发起异步操作时传入上下文（如 ETCancellationToken）
task.NoContext();          // 不携带 context 运行
task.WithContext(ctx);     // 携带 context 运行
await task.NewContext(ctx);// await 的同时替换 context

// 在异步方法内获取当前 context
ETCancellationToken token = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
```

**传播机制**：`SetContext()` 沿 Task 链向下传递，直到遇到 `WithContext` 或 `ContextTask` 类型 Task 为止。

---

### StateMachineWrap<T>（`Scripts/Core/Share/ETTask/StateMachineWrap.cs`）

async/await 编译器生成的状态机的**对象池包装器**：

```csharp
public class StateMachineWrap<T>: IStateMachineWrap where T: IAsyncStateMachine
```

- 每次 async 方法调用创建状态机时，从池中取出 `StateMachineWrap`，避免 GC
- `MoveNext` 属性返回一个固定的 `Action`（`Run`），减少委托分配
- 池容量上限 100，超出则直接丢弃（不归还）
- `ETAsyncTaskMethodBuilder` 内部使用此类管理状态机生命周期

**意义**：这是 ET 框架零 GC async/await 的核心实现细节之一。

---

### ClassEventSystem（`Scripts/Core/Share/Entity/IClassEventSystem.cs`）

Round 1 未涵盖的第三种事件模式：**类级别事件**（ClassEvent），用于 Entity 收到特定结构体消息时触发。

```csharp
public interface IClassEvent<T> { }

public abstract class ClassEventSystem<E, T>: SystemObject, AClassEventSystem<T>
    where E: Entity, IClassEvent<T>
    where T: struct
```

- Entity 实现 `IClassEvent<T>` 标记接口，表示关注类型 T 的事件
- `ClassEventSystem<E,T>` 实现处理逻辑的 `Handle(Entity, T)` 方法
- 与 `Publish/Invoke` 的区别：ClassEvent 直接发给特定 Entity 实例（非广播）

**三种通信模式对比（修正 Round 1）**：

| 模式 | 目标 | 订阅方式 | 适用场景 |
|------|------|---------|---------|
| `Publish` | 广播 N 个订阅者 | `[EventAttribute]` | 跨模块通知 |
| `Invoke` | 精确单个 Handler | `[InvokeAttribute]` | 策略分发 |
| `ClassEvent` | 特定 Entity 实例 | `IClassEvent<T>` 接口 | Entity 内部事件响应 |

---

### ETCancellationToken 实现细节（修正）

Round 1 描述较简略，实际实现：

```csharp
public class ETCancellationToken
{
    private HashSet<Action> actions = new();
    public void Add(Action callback);     // callback 为 null 时抛异常（协程泄漏检测）
    public void Remove(Action callback);
    public bool IsDispose();              // actions == null 表示已取消
    public void Cancel();                 // 调用所有注册的 Action，然后置 null
}
```

- 取消后 `actions` 被设为 null（而非清空），`IsDispose()` 检测此状态
- 取消是**一次性**的：Cancel 后不能再添加回调
- 若 callback 为 null 则抛异常，用于检测协程泄漏

---

### ETTaskHelper 完整 API

Round 1 只提到 `WaitAll`，实际还有：

```csharp
// 等待所有任务完成
ETTaskHelper.WaitAll(ETTask[] tasks)
ETTaskHelper.WaitAll(List<ETTask> tasks)

// 等待任意一个任务完成（新增）
ETTaskHelper.WaitAny(ETTask[] tasks)
ETTaskHelper.WaitAny(List<ETTask> tasks)

// 获取当前异步链中的 Context（用于取消令牌传播）
T ctx = await ETTaskHelper.GetContextAsync<T>()

// 扩展方法：检查 token 是否已取消
bool canceled = token.IsCancel()
```

**CoroutineBlocker（WaitAll/WaitAny 内部实现）**：
- 私有类，计数器模式
- `WaitAll`：count = tasks.Length，每个子任务完成 count--，归零时 SetResult
- `WaitAny`：count = 1，第一个子任务完成即 SetResult

---

### Mailboxes 实现修正

Round 1 描述 Mailboxes 为"邮箱系统"，实际实现更精确：

```csharp
public class Mailboxes
{
    // Key: Entity (即 MailboxComponent 所在 Entity) 的 InstanceId
    private readonly Dictionary<long, EntityRef<Entity>> mailboxes = new();

    public void Add(Entity mailBox);        // key = mailBox.Parent.InstanceId
    public void Remove(long instanceId);
    public EntityRef<Entity> Get(long instanceId);
}
```

- 存储 `EntityRef<Entity>`（安全引用，不阻止销毁后的 GC）
- Key 是 **MailboxComponent 父 Entity 的 InstanceId**（而非 MailboxComponent 自身）
- Fiber 通过 `Mailboxes.Get(entityInstanceId)` 找到邮箱，投递跨 Fiber 消息

---

### 代码示例汇总

#### 创建带取消的异步操作
```csharp
public static async ETTask DoSomethingAsync(Scene scene, ETCancellationToken token)
{
    TimerComponent timer = scene.GetComponent<TimerComponent>();
    await timer.WaitAsync(1000, token);
    if (token.IsCancel()) return;
    // 继续执行...
}

// 调用方
ETCancellationToken cts = new ETCancellationToken();
DoSomethingAsync(scene, cts).WithContext(cts).NoContext();
// 取消：
cts.Cancel();
```

#### Entity 安全引用
```csharp
// 存储引用
EntityRef<Player> playerRef = player;
// 稍后验证
Player p = playerRef;
if (p != null && !p.IsDisposed)
{
    p.GetComponent<HealthComponent>().HP -= 10;
}
```

#### 并发等待多个异步操作
```csharp
ETTask[] tasks = {
    LoadConfigAsync(scene),
    ConnectServerAsync(scene),
    InitUIAsync(scene),
};
await ETTaskHelper.WaitAll(tasks);
// 或等待最先完成的一个：
await ETTaskHelper.WaitAny(tasks);
```

#### 协程锁保护临界区
```csharp
const int LockType = 1001;
long lockKey = playerId;
using (await scene.GetComponent<CoroutineLockComponent>().Wait(LockType, lockKey))
{
    // 同一 lockKey 只有一个协程能进入此区域
    await ProcessPlayerDataAsync(scene, playerId);
}
```

---

## Round 3 补充：跨包交互分析、边界情况与异常处理

### 跨包交互接口

#### ITransfer（`Scripts/Core/Share/Entity/ITransfer.cs`）

```csharp
public interface ITransfer { }
```

标记接口，无方法体。标记了此接口的 Unit 组件**需要在传送时跨进程/区服迁移**。`cn.etetet.actorlocation` 包在处理 Unit 传送时，会扫描 Unit 的所有组件，只将实现了 `ITransfer` 的组件序列化并发送到目标进程，从而实现精细化的传送数据筛选。

**使用模式**：
```csharp
// 在需要传送的组件上标记
[MemoryPackable]
public partial class MoveComponent : Entity, IAwake, ITransfer
{
    // 位置、路径等需要传送的数据
}
```

---

#### IGetComponentSys（`Scripts/Core/Share/Entity/IGetComponentSysSystem.cs`）

**用途**：优化增量保存和传送。Entity 实现此接口后，每次调用 `GetComponent<T>()` 时，系统会回调 `GetComponentSysSystem<T>.GetComponentSys(entity, type)`，业务层可以借此记录"哪些组件被访问过"——只有访问过（变化过）的组件才需要保存或传送，大幅减少 I/O 和网络开销。

```csharp
// 伪代码示例：Unit 追踪变化组件
[EntitySystem]
public class UnitGetComponentSysSystem : GetComponentSysSystem<Unit>
{
    protected override void GetComponentSys(Unit self, Type type)
    {
        self.DirtyComponents.Add(type); // 记录被访问的组件类型
    }
}
```

**设计意图**（源码注释原文）：
> "GetComponentSystem有巨大作用，比如每次保存Unit的数据不需要所有组件都保存，只需要保存Unit变化过的组件"

---

#### ISerializeToEntity（`Scripts/Core/Share/Entity/ISerializeToEntity.cs`）

标记接口，表示该对象可以被序列化还原成 Entity。用于序列化系统判断是否需要将嵌套对象反序列化为独立 Entity（而非普通 POCO 对象）。

---

### 消息包格式（MessageSerializeHelper 详解）

`MessageSerializeHelper.ToMemoryBuffer()` 根据 `ServiceType` 组装不同格式的数据包：

| ServiceType | 包头格式 | 说明 |
|-------------|---------|------|
| `Outer`（面向客户端） | `[2字节 opcode][payload]` | 不含 ActorId |
| `Inner`（服务器间） | `[16字节 ActorId][2字节 opcode][payload]` | ActorId = Process(4) + FiberId(4) + InstanceId(8) |

**反序列化**（`ToMessage()`）：
- Outer：从偏移 0 读取 opcode，然后反序列化 payload
- Inner：从偏移 0 读取 ActorId，偏移 16 读取 opcode，然后反序列化 payload
- 消息对象从 `ObjectPool.Fetch(type)` 取出（复用池化对象），而非 `new`

---

### KChannel 边界情况与异常处理

#### 连接阶段
- **重试间隔**：300ms 一次，通过 `AddToUpdate(300, id)` 调度
- **连接超时**：10 秒（`KService.ConnectTimeoutTime`），超时触发 `ERR_KcpConnectTimeout` → `OnError()`
- **防重复连接**：`IsConnected` 标志，连接成功后立即清空 `waitSendMessages` 队列（补发等待消息）

#### 消息分片（大消息处理）
- **最大单包**：10000 字节（`MaxKcpMessageSize`）
- 超过限制时，先发 8 字节分片头 `[int 0][int totalLen]`，再按 10000 字节分块发送
- 接收端通过 `needReadSplitCount` 状态机重组分片

#### 发送队列保护
```
Inner 最大等待发送数: Kcp.InnerMaxWaitSize
Outer 最大等待发送数: Kcp.OuterMaxWaitSize
超出时: OnError(ERR_KcpWaitSendSizeTooLarge)，主动断开连接
```

#### KCP 参数差异

| 参数 | Inner（服务器间） | Outer（面向客户端） |
|------|-----------------|-------------------|
| 窗口大小 | 1024/1024 | 256/256 |
| MTU | 1400 字节 | 470 字节 |
| minRTO | 30ms | 30ms |

Inner 使用更大的窗口和 MTU，适合内网高吞吐；Outer 使用更小的 MTU 应对公网 NAT/路由器限制。

#### OnError 机制
```csharp
public void OnError(int error)
{
    long channelId = this.Id;
    this.Service.Remove(channelId, error);   // 1. 从 Service 移除 channel
    this.Service.ErrorCallback(channelId, error); // 2. 触发上层错误回调（断开 Session）
}
```
`Remove` 与 `ErrorCallback` 始终成对调用，且先 Remove 再 Callback，避免回调中再次触发 Remove。

---

### AService 内存池边界

```csharp
// Fetch:
// - size > 1024: 直接 new，不从池取
// - size <= 1024: 统一扩展到 1024，从池取（空则 new）

// Recycle:
// - capacity > 1024: 丢弃（不入池）
// - pool.Count > 10: 丢弃（池满）
// 有效池大小：max 10 个 1024 字节 MemoryBuffer
```

**设计说明**（源码注释）：
> "这里不需要太大，其实Kcp跟Tcp,这里1就足够了"

---

### Session 异常处理

#### Destroy 时的 RPC 清理
```csharp
private static void Destroy(this Session self)
{
    self.AService.Remove(self.Id, self.Error);

    // 所有等待中的 RPC 都收到异常，解除 await 阻塞
    foreach (RpcInfo responseCallback in self.requestCallbacks.Values.ToArray())
    {
        responseCallback.SetException(new RpcException(self.Error,
            $"session dispose: {self.Id} {self.RemoteAddress}"));
    }
    self.requestCallbacks.Clear();
}
```

**关键**：Session 销毁时，所有挂起的 `await session.Call(request)` 都会抛出 `RpcException`，调用方应捕获此异常。

#### Call 的取消令牌集成
```csharp
ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
// 注册取消回调：token 取消时，构造 Cancel 响应并 SetResult
cancellationToken?.Add(CancelAction);
ret = await rpcInfo.Wait();
// finally 确保移除回调，防止内存泄漏
cancellationToken?.Remove(CancelAction);
```

#### Call 超时版本
```csharp
// session.Call(request, timeout) 内部实现：
return await self.Call(request).TimeoutAsync(timeout);
```
`TimeoutAsync` 是 ETTaskHelper 的扩展方法，超时后触发取消令牌。

---

### ProcessInnerSender 边界与异常处理

#### 进程边界检查
```csharp
// SendInner 和 Call 都会强制检查 Process：
if (actorId.Process != fiber.Process)
    throw new Exception($"actor inner process diff: ...");
```
`ProcessInnerSender` **只能处理同进程内的 Fiber 间通信**，跨进程消息必须走网络通道。

#### 每帧处理上限
```csharp
MessageQueue.Instance.Fetch(fiber.Id, 1000, self.list); // 每帧最多处理 1000 条消息
```
防止单帧处理消息过多导致卡顿。

#### Actor RPC 超时机制
```csharp
// Call 的超时实现（fire-and-forget 独立协程）：
async ETTask Timeout()
{
    await timer.WaitAsync(ProcessInnerSender.TIMEOUT_TIME);
    if (self.requestCallback.Remove(rpcId, out action))
    {
        // needException=true: 抛 Exception
        // needException=false: 返回 ERR_Timeout 响应
        action.SetException(...) / action.SetResult(timeout_response);
    }
}
Timeout().NoContext(); // 不等待，独立运行
```

#### 慢响应警告
```csharp
if (costTime > 200) Log.Warning($"actor rpc time > 200: {costTime} ...");
```
RPC 耗时超过 200ms 打 Warning，便于发现死锁或性能问题。

#### 找不到 Actor 时
- Send：打 Warning，若是 Request 则自动回复 `ERR_NotFoundActor`
- Call：`SendInner` 返回 false → 直接返回 `ERR_NotFoundActor` 响应，不等待超时

---

### CoroutineLock 边界情况

#### 下一帧执行设计（防栈溢出）
```csharp
// 不在当帧直接 Notify 下一个等待者，而是放入 nextFrameRun 队列
public static void RunNextCoroutine(self, long type, long key, int level)
{
    if (level == 100) Log.Warning($"too much coroutine level: ...");
    self.nextFrameRun.Enqueue((type, key, level));
}

// Update 中统一处理（同一帧可能继续加入队列）
public static void Update(self)
{
    while (self.nextFrameRun.Count > 0)
    {
        var (type, key, count) = self.nextFrameRun.Dequeue();
        self.Notify(type, key, count);
    }
}
```

**关键设计**：锁释放后**不立即**唤醒下一个等待者，而是在 **下一帧 Update** 中处理。这样：
1. 防止同帧内深层递归调用栈溢出
2. 允许同帧内多个锁释放后批量处理
3. `level` 计数：每帧连续处理超过 100 个 → Warning，提示可能存在死锁排查

---

### ObjectWait 系统（被忽略的实用组件）

`ObjectWait`（`Scripts/Model/Share/` 下的 Entity 组件）是一个通用的**结构体事件等待器**，允许异步等待任意 `IWaitType` 结构体信号：

```csharp
// 等待某类型信号
WaitSceneChangeFinish result = await entity.GetComponent<ObjectWait>().Wait<WaitSceneChangeFinish>();

// 发出信号
entity.GetComponent<ObjectWait>().Notify(new WaitSceneChangeFinish { Error = 0 });
```

**与取消令牌集成**：
```csharp
// Wait 内部自动注册取消回调：
void CancelAction() => self.Notify(new T { Error = WaitTypeError.Cancel });
cancellationToken?.Add(CancelAction);
```

**Destroy 时自动清理**：Destroy 时对所有挂起的等待者调用 `SetResult()`（返回默认值），不会泄漏。

**与 CoroutineLock 的区别**：

| 机制 | CoroutineLock | ObjectWait |
|------|--------------|------------|
| 目的 | 互斥访问临界区 | 等待特定业务事件 |
| 触发 | FIFO 队列，自动推进 | 外部显式 Notify |
| 取消 | 超时参数 | CancellationToken |

---

### ErrorCore 错误码体系

```
100205-100220: KCP/TCP/WebSocket 网络层错误
100230-100232: KCP 分片/读取错误
110000       : 自定义起始错误码 (ERR_MyErrorCode)
110005       : 数据包解析错误
110304       : WebSocket 连接错误
```

**注意**：`ErrorCore.cs` 使用 `[UniqueId]` 属性标记为偏特化类（`partial`），其他包可以继续添加错误码段（只要不与 SocketError 系统码冲突）。错误码 110000 以上专门留给业务层扩展。

---

### EntityHelper 扩展方法

```csharp
entity.Zone()    // 快速访问所在 Fiber 的区服号
entity.Scene()   // 转型访问 IScene 为 Scene
entity.Scene<T>()// 转型访问 IScene 为 T（自定义 Scene 子类）
entity.Root()    // 访问 Fiber.Root（根 Scene）
entity.Fiber()   // 访问 IScene.Fiber
```

**实现链路**：`Entity.IScene → IScene.Fiber → Fiber.Zone/Root`，所有路径均不分配内存。

---

### EntitySceneFactory 与 Scene 创建

```csharp
public static Scene CreateScene(Entity parent, long id, long instanceId, int sceneType, string name)
{
    Scene scene = new(parent.Fiber(), id, instanceId, sceneType, name);
    parent?.AddChild(scene);
    return scene;
}
```

**关键**：Scene 构造时直接传入 `Fiber` 引用（而非从 parent 推导），因为 Scene 本身就是 Fiber 的根或子根节点。`id` 和 `instanceId` 均由调用方指定（通常由 `IdGenerater` 生成），不使用 Entity 内部的自动赋值。
