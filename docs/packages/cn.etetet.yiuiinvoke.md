# cn.etetet.yiuiinvoke

**版本**: 4.0.3
**分类**: UI/YIUI | Extend/Event
**依赖**: cn.etetet.core (1.0.0)
**描述**: 独立功能不依赖YIUI框架，提供基于字符串类型键的泛型Invoke调用系统，支持主调用者+多监听者模式、同步/异步、有无返回值等多种调用方式。

> **Round 2 更新**：补充遗漏内容、修正理解偏差、增加代码细节。

---

## 目录结构

```
cn.etetet.yiuiinvoke/
├── Scripts/Core/Share/
│   ├── Attribute/                  # 特性标记
│   │   ├── YIUIInvokeAttribute.cs          # 标记调用处理方法/类
│   │   ├── YIUIInvokeSystemAttribute.cs    # 标记系统注册类
│   │   ├── YIUIListenerInvokeAttribute.cs  # 标记监听处理方法/类
│   │   └── YIUIListenerInvokeSystemAttribute.cs # 标记监听系统注册类
│   ├── EventSystem/                # EventSystem 扩展
│   │   ├── EYIUIInvokeType.cs               # 预置invoke类型常量
│   │   ├── EventSystem_Check_Invoke.cs      # ET10兼容: 检查invoker是否存在
│   │   ├── EventSystem_Check_Invoke_Entity.cs
│   │   ├── EventSystem_Invoke_Entity.cs     # ET10兼容: Entity版invoke
│   │   ├── EventSystem_Invoke_Entity_Extend.cs  # Sync/Async便捷扩展
│   │   ├── EventSystem_Invoke_Entity_Safety_Extend.cs
│   │   ├── EventSystem_Invoke_Extend.cs
│   │   └── IInvokeEntity.cs                 # AInvokeEntityHandler 抽象基类
│   ├── Handler/                    # 处理器接口与基类
│   │   ├── YIUIInvokeHandler.cs         # 无返回值处理器接口/基类 (0~5参数)
│   │   ├── YIUIInvokeReturnHandler.cs   # 有返回值处理器接口/基类 (0~5参数)
│   │   └── YIUIInvokeCommonHandler.cs   # 通用处理器基类 (支持所有调用形式)
│   └── System/                     # 核心单例系统
│       ├── YIUIInvokeSystem.cs              # 主invoke系统 (单例, 注册+调用)
│       ├── YIUIInvokeSystem_Void.cs         # 同步无返回值 + 异步 Invoke 方法
│       ├── YIUIInvokeSystem_Return.cs       # 同步有返回值 InvokeReturn 方法
│       ├── YIUIInvokeSystem_Safety_Void.cs  # 安全版 SafetyInvoke (先检查再调用)
│       ├── YIUIInvokeSystem_Safety_Return.cs
│       ├── YIUIInvokeSystem_Check_Void.cs   # CheckInvoke 方法
│       ├── YIUIInvokeSystem_Check_Return.cs
│       └── YIUIListenerInvokeSystem.cs      # 监听系统 (单例, 注册+优先级排序)
├── YIUI.InvokeSourceGenerator.dll   # Source Generator，自动生成注册代码
└── package.json
```

---

## 核心类与接口

### Attribute 特性

| 特性类 | 应用目标 | 作用 |
|--------|---------|------|
| `YIUIInvokeAttribute(string invokeType)` | Class/Method | 标记为主调用处理器，SG自动生成对应 `YIUIInvokeSystemAttribute` 注册代码 |
| `YIUIInvokeAttribute()` | Method | 无参形式，SG使用方法名作为 invokeType |
| `YIUIInvokeSystemAttribute(string invokeType)` | Class | SG生成的注册特性，由 `YIUIInvokeSystem.Awake()` 扫描 |
| `YIUIListenerInvokeAttribute(string invokeType, int priority=0)` | Class/Method | 标记为监听器，priority<0在主调用前执行，priority≥0在主调用后执行 |
| `YIUIListenerInvokeSystemAttribute(string invokeType, int priority=0)` | Class | SG生成的监听注册特性，由 `YIUIListenerInvokeSystem.Awake()` 扫描 |

### Handler 处理器基类

#### 无返回值（IYIUIInvokeHandler）

```csharp
// 接口定义（0~5个泛型参数）
public interface IYIUIInvokeHandler : IYIUIInvokeBaseHandler
{
    void Invoke(Entity self);
}
public interface IYIUIInvokeHandler<in P1> : IYIUIInvokeBaseHandler
{
    void Invoke(Entity self, P1 p1);
}
// ...IYIUIInvokeHandler<P1,P2,...P5>

// 抽象基类（继承此类实现具体逻辑）
public abstract class YIUIInvokeHandler<T> : SystemObject, IYIUIInvokeHandler where T : Entity
{
    public string InvokeType { get; set; }
    public void Invoke(Entity self) => Invoke((T)self);
    protected abstract void Invoke(T self);
}
// 同理有 YIUIInvokeHandler<T,P1>...<T,P1,...P5>
```

#### 有返回值（IYIUIInvokeReturnHandler）

```csharp
public interface IYIUIInvokeReturnHandler<out R> : IYIUIInvokeBaseHandler
{
    R Invoke(Entity self);
}
// IYIUIInvokeReturnHandler<P1,R>...<P1,...P5,R>

public abstract class YIUIInvokeReturnHandler<T, R> : SystemObject, IYIUIInvokeReturnHandler<R>
    where T : Entity
{
    protected abstract R Invoke(T self);
}
```

#### Entity 版处理器（AInvokeEntityHandler）

用于需要将 Entity 实例传递给处理器的场景（与 ET EventSystem 集成）：

```csharp
public abstract class AInvokeEntityHandler<A> : HandlerObject, IInvoke where A : struct
{
    public abstract void Handle(Entity entity, A args);
}
public abstract class AInvokeEntityHandler<A, T> : HandlerObject, IInvoke where A : struct
{
    public abstract T Handle(Entity entity, A args);
}
```

#### 通用处理器（YIUIInvokeCommonHandler）

一个处理器同时实现所有 0~5 参数的有/无返回值接口，适用于需要处理不同参数形式的场景：

```csharp
public abstract class YIUIInvokeCommonHandler<T, P1, P2, P3, P4, P5, R> :
    IYIUIInvokeHandler, IYIUIInvokeHandler<P1>, ..., IYIUIInvokeReturnHandler<R>, ...
    where T : Entity
{
    protected abstract void InvokeParams(Entity self, params object[] paramVo);
    protected abstract R InvokeReturnParams(Entity self, params object[] paramVo);
}
```

### 核心系统单例

#### YIUIInvokeSystem（主调用系统）

```csharp
[CodeProcess]
public partial class YIUIInvokeSystem : Singleton<YIUIInvokeSystem>, ISingletonAwake
{
    // 内部存储: invokeType(string) -> 唯一处理器
    private readonly Dictionary<string, IYIUIInvokeBaseHandler> m_AllInvokers = new();

    public void Awake(); // 扫描 YIUIInvokeSystemAttribute 注册所有处理器

    // === 无返回值 ===
    public void Invoke<T>(T self, string invokeType) where T : Entity;
    public void Invoke<T, T1>(T self, string invokeType, T1 arg1) where T : Entity;
    // ...最多5个参数

    // === 有返回值 ===
    public R InvokeReturn<T, R>(T self, string invokeType, List<R> returnList=null) where T : Entity;
    public R InvokeReturn<T, T1, R>(T self, string invokeType, T1 arg1, ...) where T : Entity;
    // ...最多5个参数

    // === 异步 ===
    public async ETTask InvokeTask<T>(T self, string invokeType) where T : Entity;
    public async ETTask InvokeTask<T, T1>(T self, string invokeType, T1 arg1) where T : Entity;
    // ...最多5个参数

    // === 安全版（先检查是否存在再调用，不报错）===
    public void SafetyInvoke<T>(T self, string invokeType) where T : Entity;
    // ...全系列

    // === 检查是否存在 ===
    public bool CheckInvoke(string invokeType);
    public bool CheckInvoke<T1>(string invokeType);
    // ...全系列
}
```

**关键特性**：每个 invokeType 只允许注册一个主处理器（重复注册报错）。

#### YIUIListenerInvokeSystem（监听系统）

```csharp
[CodeProcess]
public partial class YIUIListenerInvokeSystem : Singleton<YIUIListenerInvokeSystem>, ISingletonAwake
{
    // 按 invokeType 分组，存储排序后的监听器列表
    private readonly Dictionary<string, List<(int priority, IYIUIInvokeBaseHandler)>> m_AllListenerInvokersBefore;
    private readonly Dictionary<string, List<(int priority, IYIUIInvokeBaseHandler)>> m_AllListenerInvokersAfter;

    public void Awake(); // 扫描 YIUIListenerInvokeSystemAttribute，按优先级排序

    // 运行时动态添加/删除监听器
    public IYIUIInvokeBaseHandler AddListenerInvoker<T>(string invokeType, int priority);
    public void RemoveListenerInvoker(IYIUIInvokeBaseHandler handler);

    public bool GetListenerInvokerBefore<T>(string invokeType, List<T> list);
    public bool GetListenerInvokerAfter<T>(string invokeType, List<T> list);
}
```

### EYIUIInvokeType（预置类型常量）

```csharp
[UniqueId]
public static partial class EYIUIInvokeType
{
    // 同步类型
    public const long Sync          = 1000;
    public const long SyncHandler_1 = 1001; // 有返回值，SyncHandler_X 区分不同返回类型
    // ...SyncHandler_1 ~ SyncHandler_10

    // 异步类型
    public const long Async          = 2000;
    public const long AsyncHandler_1 = 2001;
    // ...AsyncHandler_1 ~ AsyncHandler_10
}
```

---

## 实现原理

### 架构模式

该 package 实现了一种**策略+观察者混合模式**：

- **主处理器**（Strategy）：每个 invokeType 唯一对应一个处理器，执行核心逻辑
- **监听器**（Observer）：可以有多个，按优先级分为"前置监听"和"后置监听"

### Source Generator 机制

包含 `YIUI.InvokeSourceGenerator.dll`，在编译时：
1. 扫描标有 `[YIUIInvoke]` 的类/方法
2. 自动生成对应的 `[YIUIInvokeSystem(invokeType)]` 注册类
3. 扫描标有 `[YIUIListenerInvoke]` 的类/方法
4. 自动生成对应的 `[YIUIListenerInvokeSystem(invokeType, priority)]` 注册类

开发者无需手写注册代码，只需标注特性即可。

### ET10 兼容层

代码通过 `#if ET10 / #else` 预编译指令支持两个版本：
- **ET10**：将方法实现为 `EventSystem` 的静态扩展方法
- **非ET10（ET9等）**：将方法实现为 `EventSystem` 的 `partial class` 方法

---

## 关键流程

### Invoke 调用流程

```
调用者: YIUIInvokeSystem.Instance.Invoke(entity, "MyInvokeType", arg1)
    │
    ├─► 1. 检查 entity != null && !IsDisposed
    │
    ├─► 2. 获取主处理器 invoker = GetInvoker<IYIUIInvokeHandler<T1>>("MyInvokeType")
    │
    ├─► 3. 获取前置监听列表 GetListenerInvokerBefore("MyInvokeType", list)
    │       └─► 按 priority 升序执行所有前置监听器 listener.Invoke(entity, arg1)
    │
    ├─► 4. 执行主处理器 invoker.Invoke(entity, arg1)
    │
    └─► 5. 获取后置监听列表 GetListenerInvokerAfter("MyInvokeType", list)
            └─► 按 priority 升序执行所有后置监听器 listener.Invoke(entity, arg1)
```

### 系统初始化流程

```
游戏启动
    │
    ├─► YIUIInvokeSystem.Awake()
    │       扫描所有带 [YIUIInvokeSystemAttribute] 的类
    │       Activator.CreateInstance(type) → 注册到 m_AllInvokers
    │
    └─► YIUIListenerInvokeSystem.Awake()
            扫描所有带 [YIUIListenerInvokeSystemAttribute] 的类
            按 priority < 0 → Before; priority >= 0 → After
            各自按 priority 排序
```

### InvokeReturn 与 returnList

`InvokeReturn` 方法可选传入 `List<R> returnList`，若传入则会收集**所有监听器和主处理器**的返回值。主处理器的返回值作为方法最终返回值。

---

## 使用示例

### 定义处理器（通过 SG 自动生成注册）

```csharp
// 无参数无返回值
[YIUIInvoke("MyPanel_Open")]
public class MyPanelOpenSystem : YIUIInvokeHandler<MyPanelComponent>
{
    protected override void Invoke(MyPanelComponent self)
    {
        // 处理逻辑
    }
}

// 带参数有返回值
[YIUIInvoke("MyPanel_GetData")]
public class MyPanelGetDataSystem : YIUIInvokeReturnHandler<MyPanelComponent, string, bool>
{
    protected override bool Invoke(MyPanelComponent self, string param)
    {
        return true;
    }
}

// 异步调用（返回 ETTask）
[YIUIInvoke("MyPanel_LoadAsync")]
public class MyPanelLoadAsyncSystem : YIUIInvokeReturnHandler<MyPanelComponent, ETTask>
{
    protected override async ETTask Invoke(MyPanelComponent self)
    {
        await ETTask.CompletedTask;
    }
}
```

### 定义监听器

```csharp
// 优先级 -1：在主调用之前执行
[YIUIListenerInvoke("MyPanel_Open", -1)]
public class MyPanelOpenBeforeListener : YIUIInvokeHandler<OtherComponent>
{
    protected override void Invoke(OtherComponent self)
    {
        // 在 Open 之前执行
    }
}

// 优先级 10：在主调用之后执行（越小越先）
[YIUIListenerInvoke("MyPanel_Open", 10)]
public class MyPanelOpenAfterListener : YIUIInvokeHandler<OtherComponent>
{
    protected override void Invoke(OtherComponent self)
    {
        // 在 Open 之后执行
    }
}
```

### 调用

```csharp
// 无返回值
YIUIInvokeSystem.Instance.Invoke(myPanel, "MyPanel_Open");

// 有返回值
bool result = YIUIInvokeSystem.Instance.InvokeReturn<MyPanelComponent, string, bool>(
    myPanel, "MyPanel_GetData", "paramValue");

// 异步
await YIUIInvokeSystem.Instance.InvokeTask(myPanel, "MyPanel_LoadAsync");

// 安全调用（不存在时静默忽略）
YIUIInvokeSystem.Instance.SafetyInvoke(myPanel, "Optional_Feature");

// 提前检查
if (YIUIInvokeSystem.Instance.CheckInvoke("MyPanel_Open"))
{
    YIUIInvokeSystem.Instance.Invoke(myPanel, "MyPanel_Open");
}
```

### Entity 版 Invoke（集成 ET EventSystem）

```csharp
// 使用 EYIUIInvokeType 预置常量
EventSystem.Instance.YIUIInvokeEntitySync<MyArgs>(entity, args);
EventSystem.Instance.YIUIInvokeEntityAsync<MyArgs>(entity, args);

// 通用形式
EventSystem.Instance.YIUIInvokeEntity(entity, EYIUIInvokeType.Sync, args);
EventSystem.Instance.YIUIInvokeEntity<MyArgs, bool>(entity, EYIUIInvokeType.SyncHandler_1, args);
```

---

## 依赖关系

```
cn.etetet.yiuiinvoke
    └── cn.etetet.core (1.0.0)
         ├── ET框架核心：Entity、EventSystem、Singleton、SystemObject
         ├── CodeTypes (反射扫描)
         ├── Log (日志)
         └── ListComponent<T> (对象池List)
```

### 被其他 package 依赖

`cn.etetet.yiuiinvoke` 是 YIUI 框架的核心通信机制，被以下 package 广泛使用：
- `cn.etetet.yiuiframework` — 框架核心，通过 YIUI Invoke 驱动 Panel/UI 的生命周期
- `cn.etetet.yiui` — 具体 UI 功能实现
- 所有带 `cn.etetet.yiui*` 前缀的 package（效果、红点、Tips 等）

---

## 与 ET 原生 Invoke 的区别

| 特性 | ET 原生 Invoke | YIUIInvoke |
|------|-------------|------------|
| 类型键 | `Type` (结构体) + `long` | `string` |
| 多处理器 | 不支持（唯一） | 通过 ListenerInvoke 支持多监听 |
| 优先级 | 无 | 支持，负数在前正数在后 |
| 参数形式 | struct 作为 args | 最多5个泛型参数 |
| Source Generator | 需手动注册 | SG 自动生成 |
| 适用场景 | 全局事件系统 | YIUI 内部 UI 生命周期驱动 |

---

## 实现细节补充（Round 2）

### 异步调用的本质

`InvokeTask` 并不使用特殊的异步接口，而是复用 `IYIUIInvokeReturnHandler<ETTask>`——即返回值为 `ETTask` 的有返回值处理器。调用时 `await` 发生在调用侧：

```csharp
// InvokeTask 内部实现
var invoker = GetInvoker<IYIUIInvokeReturnHandler<ETTask>>(invokeType);
await invoker.Invoke(self);
```

因此异步处理器继承 `YIUIInvokeReturnHandler<T, ETTask>` 而非特殊的异步基类。

### CheckInvoke 的差异行为

- **无参版 `CheckInvoke(string invokeType)`**：内部调用 `GetInvoker<IYIUIInvokeHandler>(..., isThrowError: true)`，若找不到会输出 Error 日志。
- **带参版 `CheckInvoke<T1>(...)`**：传入 `isThrowError: false`，找不到时静默返回 `null`。
- **结论**：只用无参 `CheckInvoke` 做"是否存在"检测时要注意副作用，正确做法是用带类型参数的重载。

### SafetyInvoke 的两次查找

`SafetyInvoke` 实现为先 `CheckInvoke` 再 `Invoke`，即对字典进行两次查找，存在轻微性能开销：

```csharp
public void SafetyInvoke<T>(T self, string invokeType) where T : Entity
{
    if (!CheckInvoke(invokeType)) return;
    Invoke(self, invokeType);  // 内部再查一次
}
```

### AddListenerInvoker 的插入边界问题

`AddListenerInvoker` 使用线性插入排序，当新 priority 大于所有现有元素时，for 循环不会触发 `list.Insert`，导致**新元素未被添加到列表末尾**（可能是设计缺陷）。调用时需确保 priority 小于或等于至少一个现有元素的 priority，或在空列表时使用。

### ListComponent 对象池

所有 `Invoke*` 方法内部通过 `using var list = ListComponent<T>.Create()` 创建临时列表，`using` 语句确保执行后归还对象池，避免 GC 压力：

```csharp
using var list = ListComponent<IYIUIInvokeHandler>.Create();
YIUIListenerInvokeSystem.Instance.GetListenerInvokerBefore(invokeType, list);
// ... 使用后自动 Dispose → 归还池
```

### GetListenerInvoker 的类型不匹配处理

当监听器类型与期望接口不匹配时，`GetListenerInvoker` 不抛出异常，而是输出 Error 并跳过该监听器，继续处理后续的：

```csharp
foreach (var invoke in invokerList)
{
    if (handler is T tListenerInvoker) list.Add(tListenerInvoker);
    else Log.Error($"类型不一致...");  // 跳过，不中断
}
```

### InvokeEntity 与 YIUIInvoke 的错误处理差异

| 机制 | 未找到处理器 | 类型不匹配 |
|------|------------|-----------|
| `YIUIInvokeSystem.Invoke*` | `Log.Error` + return | `Log.Error` + return |
| `EventSystem.InvokeEntity` | `throw Exception` | `throw Exception` |
| `YIUIListenerInvokeSystem.GetListenerInvoker` | 跳过 | `Log.Error` + 跳过 |

**InvokeEntity 会抛出异常**，调用方需自行 try-catch。

### AInvokeEntityHandler 详细实现

```csharp
// 继承 HandlerObject（ET对象池管理），实现 IInvoke（ET调用接口）
// Type 属性返回 typeof(A)，供 ET EventSystem 按类型查找
public abstract class AInvokeEntityHandler<A> : HandlerObject, IInvoke where A : struct
{
    public Type Type => typeof(A);
    public abstract void Handle(Entity entity, A args);
}
```

注意：`A` 必须是 `struct`，用结构体传递调用参数（避免装箱是设计目标，但实际通过 `params object[]` 的 CommonHandler 仍会装箱）。

### YIUIInvokeEntitySync/Async 快捷方法

`EventSystem_InvokeEntity_YIUIExtension` 扩展类提供了快捷方法，映射到固定的 `EYIUIInvokeType` 常量：

```csharp
// YIUIInvokeEntitySync → EYIUIInvokeType.Sync (1000)
EventSystem.Instance.YIUIInvokeEntitySync<MyArgs>(entity, args);
// 等价于
EventSystem.Instance.InvokeEntity(entity, 1000L, args);

// YIUIInvokeEntityAsync → EYIUIInvokeType.Async (2000)
EventSystem.Instance.YIUIInvokeEntityAsync<MyArgs>(entity, args);
```

### YIUIInvokeCommonHandler 不继承 SystemObject

与 `YIUIInvokeHandler<T>`（继承 `SystemObject`）不同，`YIUIInvokeCommonHandler` 直接实现所有接口，没有基类：

```csharp
public abstract class YIUIInvokeCommonHandler<T, P1, P2, P3, P4, P5, R> :
    IYIUIInvokeHandler, IYIUIInvokeHandler<P1>, ...
    // 无 SystemObject 基类
```

子类需要自行管理生命周期（如需要的话）。所有参数通过 `params object[]` 转发，存在装箱开销。

---

## 注意事项

1. **一对一约束**：每个 invokeType 只允许一个主处理器，重复注册会报错。若需要多个响应者，应使用 `[YIUIListenerInvoke]`。
2. **invokeType 字符串唯一性**：建议使用 `组件名_方法名` 命名规范，避免冲突。
3. **Safety vs 普通版本**：`SafetyInvoke` 不会报错（静默忽略），适用于可选功能；普通 `Invoke` 找不到处理器时会输出 Error 日志。
4. **ListenerInvoke 排序**：相同优先级的监听器按注册顺序执行，不保证稳定顺序。
5. **ET10 兼容**：代码通过 `#if ET10` 区分，两套实现功能等价。
6. **动态注册**：`YIUIListenerInvokeSystem.AddListenerInvoker<T>()` 支持运行时动态添加监听器，`RemoveListenerInvoker()` 支持动态移除。注意 `AddListenerInvoker` 当 priority 大于所有现有元素时可能不会正确插入。
7. **InvokeEntity 会抛异常**：与 `YIUIInvokeSystem` 不同，`EventSystem.InvokeEntity` 在未找到处理器时抛出 `Exception`，务必在调用侧处理。
8. **InvokeTask 即 ETTask 返回**：异步处理器使用 `YIUIInvokeReturnHandler<T, ETTask>` 基类，不是独立的异步基类。

---

## 深化跨 Package 交互分析（Round 3）

### 与 cn.etetet.yiuiframework 的交互

`cn.etetet.yiuiframework` 是 `cn.etetet.yiuiinvoke` 最主要的消费者。框架通过 YIUIInvoke 驱动所有 UI 组件生命周期：

- **UITaskEventHandle（P0~P5）**：框架的 UIBind 系统中，`UITaskEventHandleP0~P5` 和 `UIEventHandleP0~P5` 均通过 `YIUIInvokeSystem.Instance.InvokeTask/Invoke` 调用注册的处理器。
- **Panel 生命周期**：Panel 的 Open/Close/Init/Dispose 等操作本质上是调用对应 invokeType 的处理器（如 `PanelName_Open`）。
- **参数化调用**：UITaskEventHandleP1~P5 对应1~5个参数的 `InvokeTask<T,T1,...>` 重载，实现带参数的异步 UI 事件。

```
yiuiframework (UITaskEventHandleP1.cs)
    │
    └─► YIUIInvokeSystem.Instance.InvokeTask<TPanel, TArg1>(panel, invokeType, arg1)
             │
             └─► IYIUIInvokeReturnHandler<TArg1, ETTask>.Invoke(panel, arg1)
```

### 与 cn.etetet.yiui 等 UI Package 的交互

所有 `cn.etetet.yiui*` Package 的具体 UI 功能均通过以下模式接入 YIUIInvoke：

1. 在各自 Package 内定义 `[YIUIInvoke("XXX_YYY")]` 处理器类
2. Source Generator 在编译期生成 `[YIUIInvokeSystem("XXX_YYY")]` 注册类
3. 游戏启动时 `YIUIInvokeSystem.Awake()` 统一扫描注册

这意味着 **yiuiinvoke 是整个 UI 系统的"消息总线"**，各 Package 通过 invokeType 字符串解耦，无需直接引用。

### 异常边界分析（源码确认）

通过阅读源码，确认各方法的实际异常处理行为：

| 方法 | Entity null/disposed | 找不到处理器 | 执行中抛异常 |
|------|---------------------|-------------|-------------|
| `Invoke*` | `Log.Error` + return | `Log.Error` + return | `Log.Error` (catch) |
| `InvokeReturn*` | `Log.Error` + return default | `Log.Error` + return default | `Log.Error` (catch) |
| `InvokeTask*` | `Log.Error` + return | `Log.Error` + return | `Log.Error` (catch) |
| `SafetyInvoke*` | 先Check后Invoke（Check静默，Invoke有Error） | 静默 return | `Log.Error` (catch，来自内部Invoke) |
| `CheckInvoke()` (无参) | N/A | `Log.Error` + return false | N/A |
| `CheckInvoke<T1>()` | N/A | 静默 return false | N/A |
| `EventSystem.InvokeEntity*` | N/A | **throw Exception** | 调用方处理 |

**关键结论**：
- `YIUIInvokeSystem` 系列**永远不抛异常**，所有错误通过 `Log.Error` 报告后安全返回
- `EventSystem.InvokeEntity` 系列**一定抛出异常**，务必 try-catch 包裹

### SafetyInvoke 的实际行为（源码确认）

从源码可见，`SafetyInvoke` 的无参版本 `CheckInvoke(invokeType)` 使用 `isThrowError: true`（默认值），**会输出 Error 日志**（若未找到）。带参版本 `CheckInvoke<T1>()` 使用 `isThrowError: false`，静默检查。

```csharp
// SafetyInvoke<T>(self, invokeType) 的检查：
public bool CheckInvoke(string invokeType)  // 无参版
{
    var invoker = GetInvoker<IYIUIInvokeHandler>(invokeType);  // isThrowError=true（默认）
    return invoker != null;
}

// SafetyInvoke<T, T1>(self, invokeType, arg1) 的检查：
public bool CheckInvoke<T1>(string invokeType)  // 带参版
{
    var invoker = GetInvoker<IYIUIInvokeHandler<T1>>(invokeType, false);  // isThrowError=false
    return invoker != null;
}
```

**实践建议**：无参的 `SafetyInvoke` 在处理器未注册时仍会产生 Error 日志，并非"完全静默"。真正静默应使用带参版本。

### AddListenerInvoker 插入 Bug 确认（源码）

```csharp
// 源码（YIUIListenerInvokeSystem.cs:126-133）
for (int i = 0; i < list.Count; i++)
{
    if (list[i].Item1 > priority)
    {
        list.Insert(i, (priority, handler));
        break;  // ← 找到插入点后 break
    }
    // ← 若所有元素 priority <= 新元素的 priority，循环结束，没有 Insert
}
```

**确认**：当新 priority ≥ 所有现有元素时，for 循环正常结束但 `list.Insert` 不被调用，**handler 丢失**。

修复场景的建议处理方式：
```csharp
// 调用方需在外部做判断或使用更小的 priority
// 当不确定排序时，使用 int.MaxValue-1 而非 int.MaxValue 来避免此问题
```

### GetListenerInvoker 并发安全性

`m_AllListenerInvokersBefore/After` 是普通 `Dictionary`，不是线程安全的。`AddListenerInvoker`/`RemoveListenerInvoker` 也直接修改 List，**没有任何锁机制**。

该系统假设在单线程（Unity 主线程）环境下使用。ET 框架本身基于单线程协程，因此在正常使用下没有并发问题，但若在异步上下文中动态注册/注销监听器需额外谨慎。

### InvokeReturn 的 returnList 语义（源码确认）

```csharp
// Before 监听器的返回值也被加入 returnList
foreach (var listener in list)  // Before
{
    var beforeResult = listener.Invoke(self);
    if (returnList != null) returnList.Add(beforeResult);  // ← Before 结果也收集
}
var result = invoker.Invoke(self);
if (returnList != null) returnList.Add(result);            // ← 主处理器结果

// After 监听器的返回值也被收集
```

**结论**：`returnList` 包含按顺序排列的 [所有Before监听返回值, 主处理器返回值, 所有After监听返回值]。方法本身只返回**主处理器**的结果，returnList 用于需要聚合所有结果的场景。

### 初始化顺序依赖

`YIUIInvokeSystem` 和 `YIUIListenerInvokeSystem` 都是 `Singleton<T>` 且标注了 `[CodeProcess]`，需要在游戏初始化阶段由框架统一 Awake。`Invoke` 调用必须在两个系统都完成 Awake 后才能正确运行。若在 Awake 之前调用，会因 `Singleton.Instance` 未初始化而异常。

### 调用链中的异常不中断其他监听器

由于整个 `Invoke*` 方法被 `try-catch` 包裹，**一旦任何处理器（含监听器）抛出异常，整个调用链立即中止**，后续监听器不再执行：

```csharp
try
{
    foreach (var listener in list)  // Before 监听
        listener.Invoke(self);      // ← 若此处抛异常 →
    invoker.Invoke(self);           //   ← 这里不会执行
    // After 监听也不会执行
}
catch (Exception e)
{
    Log.Error(...);  // 记录错误
}
// 调用方不感知异常
```

这是一个潜在的设计风险：**前置监听器异常会导致主处理器和后置监听器全部跳过**。
