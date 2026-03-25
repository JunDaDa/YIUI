# cn.etetet.ai — AI行为机模块

## 概述

**版本**：3.0.0
**描述**：行为机模块，用来写AI非常方便
**作者**：tanghai (ET框架)
**依赖**：无外部依赖（依赖框架内置的 TimerComponent、Entity系统等）

该模块实现了一个轻量级的**优先级行为选择AI系统**，基于配置驱动（Excel/AIConfig），通过定时器轮询方式每秒检查并切换AI行为。适用于单位（Unit）的服务端AI，也可用于客户端场景（ClientScene）。

---

## 目录结构

```
cn.etetet.ai/
├── Excel/
│   └── AIConfig.xlsx               # AI配置表
├── Scripts/
│   ├── Model/Share/
│   │   ├── AAIHandler.cs           # AI处理器抽象基类
│   │   ├── AIComponent.cs          # AI组件（挂载在Scene/Unit上）
│   │   ├── AIConfig.cs             # AI配置数据类（Excel生成）
│   │   ├── AIDispatcherComponent.cs # AI分发器（单例，管理所有Handler）
│   │   ├── PackageType.cs          # 包类型常量（AI = 31）
│   │   ├── TimerInvokeType.cs      # 定时器类型常量（AITimer）
│   │   └── AssemblyReference.asmref # 程序集引用（Model层）
│   └── Hotfix/Share/
│       ├── AIComponentSystem.cs    # AI组件系统（行为逻辑核心）
│       └── AssemblyReference.asmref # 程序集引用（Hotfix层）
└── package.json
```

> **注意**：Model 和 Hotfix 分别使用 `AssemblyReference.asmref` 引用主程序集，遵循 ET 框架的 Model/Hotfix 热更分层架构。

---

## 核心类说明

### `AAIHandler`（抽象基类）
```csharp
[AIHandler]
public abstract class AAIHandler: HandlerObject
{
    public abstract int Check(AIComponent aiComponent, AIConfig aiConfig);
    public abstract ETTask Execute(AIComponent aiComponent, AIConfig aiConfig);
}
```
- 继承自 `HandlerObject`（ET框架基础处理器对象）
- 标注 `[AIHandler]` 特性，由 `AIDispatcherComponent` 自动反射注册
- `Check()`：条件检查，返回 **0 表示满足条件**（可执行），非0跳过
- `Execute()`：异步执行AI行为，**必须支持取消**（通过 `ETCancellationToken`）
- **注册键为类名**（`type.Name`），AIConfig.Name 字段需与类名完全一致

### `AIComponent`
```csharp
[ComponentOf(typeof(Scene))]
public class AIComponent: Entity, IAwake<int>, IDestroy
{
    public int AIConfigId;          // AI配置ID（对应AIConfigCategory中的一组配置）
    public ETCancellationToken CancellationToken;  // 当前行为取消令牌
    public long Timer;              // 定时器ID（用于移除定时器）
    public int Current;             // 当前执行中的行为配置ID（AIConfig.Id）
}
```
- `[ComponentOf(typeof(Scene))]` 表示该组件通常挂在 Scene 衍生类上
- 实际使用：客户端 → `ClientScene`，服务端 → `Unit`（Unit 是 Scene 子类）
- `Current` 用于行为去重：若检查到的行为 Id 与 Current 相同，则不重新启动，保持当前执行状态

### `AIDispatcherComponent`（单例）
```csharp
[CodeProcess]
public class AIDispatcherComponent: Singleton<AIDispatcherComponent>, ISingletonAwake
```
- `[CodeProcess]` 标记：在代码进程层初始化的单例（非 Fiber 级）
- 启动时通过 `CodeTypes.Instance.GetTypes(typeof(AIHandlerAttribute))` 扫描所有 Handler
- 字典 `aiHandlers: Dictionary<string, AAIHandler>` 以类名为键存储实例
- 所有 Handler 在启动时 **一次性实例化**，运行时直接复用（Handler 应设计为无状态或状态在 AIComponent 中维护）

### `AIConfigCategory`（配置分类）
- 从 Excel 生成的分部类，持有所有 `AIConfig` 数据
- `EndInit()` 方法在配置加载完成后调用，执行分组和排序：
  - 按 `AIConfigId` 分组到 `AIConfigs: Dictionary<int, List<AIConfig>>`
  - 每组按 `AIConfigId` 字段值升序排列（即**优先级升序，数值小的优先执行**）
- 提供 `GetAI(int aiConfigId)` 获取一组AI行为列表

### `AIComponentSystem`（组件系统）
- 静态分部类，通过扩展方法实现 AIComponent 的生命周期
- 内嵌 `AITimer` 类实现 `ATimer<AIComponent>` 接口，响应 `TimerInvokeType.AITimer` 事件
- 核心私有方法 `Check()` 和 `Cancel()` 为内部实现，不对外暴露

---

## 实现原理

### 行为选择流程（每秒触发）

```
AITimer触发 (TimerInvokeType.AITimer = 31001)
    │
    ▼
AIComponentSystem.Check()
    │
    ├── 检查 self.Parent == null → 若已销毁，移除定时器并返回
    │
    ├── 获取 AIConfigs[self.AIConfigId]（按优先级排序的行为列表）
    │
    └── 遍历每个 AIConfig（优先级从高到低）：
            │
            ├── 通过 AIDispatcherComponent.Get(config.Name) 获取 Handler
            ├── 调用 handler.Check() 检查条件
            │       ├── 返回非0 → 条件不满足，continue 检查下一个
            │       └── 返回0  → 条件满足，进入行为处理
            │
            ├── 若 self.Current == aiConfig.Id → 当前行为未变，break（保持执行中的异步任务）
            │
            └── 行为切换（新行为）：
                    ├── self.Cancel()  → CancellationToken.Cancel()，Current=0，Token=null
                    ├── 创建新 ETCancellationToken
                    ├── self.Current = aiConfig.Id（记录新行为ID）
                    └── handler.Execute(self, config).WithContext(token)（启动新异步任务）
```

### 行为取消机制
- 切换到新行为前调用 `Cancel()`，触发 `CancellationToken.Cancel()`
- 正在执行的异步 `Execute()` 协程需**主动检查取消令牌**（ET框架协程取消是协作式的）
- `Current` 置0，`CancellationToken` 置 null，防止资源泄漏
- 组件销毁时也会调用取消，确保不残留异步任务

### 优先级设计
- AIConfig 中**同一 AIConfigId 的多条记录**代表同一 AI 的不同行为选项
- 按 `Id` 或优先级字段排序后，**排在前面的优先级更高**
- 每次检查只执行第一个满足条件的行为，确保高优先级行为能抢占低优先级行为

---

## 关键流程

### 1. 初始化
```csharp
// 服务端示例：给Unit挂载AI
unit.AddComponent<AIComponent, int>(aiConfigId);

// AIComponent.Awake 自动启动每秒定时器
self.AIConfigId = aiConfigId;
self.Timer = self.Root().GetComponent<TimerComponent>().NewRepeatedTimer(1000, TimerInvokeType.AITimer, self);
```

### 2. 自定义AI行为（完整示例）
```csharp
[AIHandler]
public class ChaseAIHandler : AAIHandler
{
    // Check 应是纯检查，不应有副作用
    public override int Check(AIComponent ai, AIConfig config)
    {
        // 检查是否有可追击的目标
        Unit unit = ai.GetParent<Unit>();
        if (unit == null) return 1;

        // 返回0表示满足条件（可执行此行为）
        return unit.GetComponent<TargetComponent>()?.Target != null ? 0 : 1;
    }

    public override async ETTask Execute(AIComponent ai, AIConfig config)
    {
        // ETCancellationToken 通过 WithContext 注入到协程上下文
        // 使用 ai.CancellationToken 检查取消
        Unit unit = ai.GetParent<Unit>();
        while (true)
        {
            // 必须检查取消令牌，否则切换行为时此协程无法停止
            if (ai.CancellationToken.IsCancel()) break;

            // 执行追击逻辑
            await unit.MoveToAsync(target.Position, ai.CancellationToken);
        }
    }
}
```

### 3. 边界情况处理
- **Handler 未找到**：`AIDispatcherComponent.Get()` 返回 null 时，`Log.Error` 记录错误并 continue
- **Parent 为 null**：组件被提前销毁时，`Check()` 直接移除定时器并返回，防止空引用
- **同一行为重复触发**：`self.Current == aiConfig.Id` 判断防止重复启动相同行为
- **定时器竞态**：`Destroy` 中同时移除定时器和取消令牌，确保清理顺序正确

### 4. 销毁
```csharp
// AIComponent.Destroy 自动清理
self.Root().GetComponent<TimerComponent>()?.Remove(ref self.Timer);  // 停止定时器
self.CancellationToken?.Cancel();  // 取消当前行为
self.CancellationToken = null;
self.Current = 0;
```

---

## 配置系统

### AIConfig（Excel配置字段）
| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 配置行唯一ID（用于 `Current` 去重判断） |
| AIConfigId | int | AI配置组ID（一个Unit对应一组行为） |
| Name | string | 对应的 AIHandler **类名**（必须完全匹配） |
| ... | ... | 其他行为参数（由各 Handler 自行解析） |

- 同一 `AIConfigId` 的多行按优先级排序
- 系统按顺序检查，**第一个满足条件的行为被执行**
- AIConfig.xlsx 位于包内 Excel 目录，由 Luban 或类似工具生成代码

---

## 依赖关系

```
cn.etetet.ai
    ├── (框架内置) TimerComponent       — 定时驱动AI检查（每秒）
    ├── (框架内置) Entity/Component     — 组件生命周期管理（IAwake/IDestroy）
    ├── (框架内置) ETCancellationToken  — 异步任务取消（协作式）
    ├── (框架内置) CodeTypes            — 反射扫描[AIHandler]标记的类
    ├── (框架内置) Singleton<T>         — AIDispatcherComponent单例
    └── (Excel生成) AIConfigCategory    — AI行为配置数据（cn.etetet.excel生成）

被以下包扩展（具体 Handler 实现）：
    ├── cn.etetet.move      — 可能实现移动相关AI Handler
    └── (其他业务包)         — 通过继承 AAIHandler 实现自定义行为
```

---

## 架构模式

- **策略模式（Strategy）**：`AAIHandler` 定义行为接口，具体类实现不同行为策略
- **优先级行为选择**：遍历有序配置列表，选第一个满足条件的行为（等价于行为树的**优先级选择节点**）
- **配置驱动**：AI逻辑通过Excel配置定义，无需修改代码即可调整AI行为优先级和参数
- **定时器驱动（Timer-based）**：使用 ET 框架的 `TimerComponent` 每秒触发检查，而非事件驱动
- **异步可取消（Coroutine + Cooperative Cancellation）**：Execute 使用 `ETTask` + `ETCancellationToken`，支持中途切换行为
- **反射自动注册**：Handler 无需手动注册，`[AIHandler]` 特性 + `CodeTypes` 反射自动完成

---

## 常量定义

| 常量 | 值 | 用途 |
|------|----|------|
| `PackageType.AI` | 31 | 包类型标识（用于计算TimerInvokeType） |
| `TimerInvokeType.AITimer` | 31001 | AI定时器事件类型（31 * 1000 + 1） |

---

## Round 2 补充说明

### 程序集分层
- `Scripts/Model/Share/AssemblyReference.asmref`：Model层程序集引用，包含数据结构定义
- `Scripts/Hotfix/Share/AssemblyReference.asmref`：Hotfix层程序集引用，包含热更逻辑
- `Ignore.ET.AI.asmdef`：包自身的程序集定义（标注为Ignore，可能用于Editor工具排除）
- Share 目录表示**客户端和服务端共用**的代码，无平台特定逻辑

### Handler 无状态设计
`AIDispatcherComponent` 在启动时创建所有 Handler 的**单个实例**并复用。这意味着：
- Handler 类本身不应存储与特定 AI 单位相关的状态
- 所有状态（当前行为、取消令牌）存储在 `AIComponent` 中
- Handler 的方法是**无状态函数**，通过参数接收上下文

### Check/Execute 返回值约定
- `Check()` 返回 **int** 而非 bool：0 = 满足（可执行），非0 = 不满足（跳过）
  - 设计为 int 可扩展为多种失败原因码，便于调试
- `Execute()` 返回 `ETTask`（void 异步）：行为执行完成或被取消时协程自然结束
