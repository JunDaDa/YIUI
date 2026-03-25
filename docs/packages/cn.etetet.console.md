# cn.etetet.console

## 概述

ET.Console 是服务器端控制台命令解析与分发系统，版本 3.0.0。提供交互式命令行界面，支持模式化命令处理（输入模式切换），通过属性标记自动注册命令处理器，实现热重载配置和 DLL 等运维操作。

**描述**：实现了控制台解析分发
**作者**：tanghai (ET Framework)
**适用端**：Server 端

---

## 目录结构

```
cn.etetet.console/
├── package.json
├── Scripts/
│   ├── Model/Server/
│   │   ├── ConsoleComponent.cs        # 控制台组件（Entity）
│   │   ├── ConsoleDispatcher.cs       # 命令分发器（Singleton）
│   │   ├── ConsoleHandlerAttribute.cs # 命令处理器标记属性
│   │   ├── IConsoleHandler.cs         # 命令处理器接口
│   │   └── ModeContex.cs              # 当前模式上下文（Entity）+ ModeContexSystem
│   └── Hotfix/Server/
│       ├── ConsoleComponentSystem.cs      # ConsoleComponent 生命周期系统
│       ├── ReloadConfigConsoleHandler.cs  # 热重载配置命令处理器
│       └── ReloadDllConsoleHandler.cs     # 热重载 DLL 命令处理器
```

---

## 核心类与接口

### ConsoleComponent（Model）
- **继承**：`Entity`, `IAwake`
- **挂载**：`[ComponentOf(typeof(Scene))]` — 挂载在 Scene 上
- **字段**：
  - `CancellationTokenSource CancellationTokenSource` — 用于取消控制台读取任务
- **功能**：作为控制台功能的根组件，Awake 时启动异步读取循环

### ConsoleMode（Model）
- **类型**：`static partial class`（常量集合，**partial** 允许其他 package 在同命名空间扩展新模式）
- **定义的模式常量**：
  | 常量 | 值 | 说明 |
  |------|-----|------|
  | `ReloadDll` | `"R"` | 热重载 DLL |
  | `ReloadConfig` | `"C"` | 热重载配置 |
  | `ShowMemory` | `"M"` | 显示内存（处理器由其他 package 提供） |
  | `Repl` | `"Repl"` | REPL 模式（处理器由其他 package 提供） |
  | `Debugger` | `"Debugger"` | 调试模式（处理器由其他 package 提供） |
  | `CreateRobot` | `"CreateRobot"` | 创建机器人（处理器由其他 package 提供） |
  | `Robot` | `"Robot"` | 机器人模式（处理器由其他 package 提供） |

### ConsoleDispatcher（Model）
- **继承**：`Singleton<ConsoleDispatcher>`, `ISingletonAwake`
- **标记**：`[CodeProcess]` — 表示此单例在代码加载阶段初始化
- **字段**：`Dictionary<string, IConsoleHandler> handlers` — 模式名 → 处理器实例
- **方法**：
  - `Awake()` — 扫描所有标记了 `[ConsoleHandler]` 的类型，反射实例化并注册到 handlers 字典
  - `Get(string key)` — 按模式名称查找对应的处理器（未找到会抛出 KeyNotFoundException）
- **功能**：单例分发器，启动时通过反射自动发现并注册所有 `IConsoleHandler` 实现

### ConsoleHandlerAttribute（Model）
- **继承**：`BaseAttribute`（`ET` 命名空间，非 `ET.Server`）
- **属性**：`string Mode` — 该处理器对应的模式名称
- **用途**：标记类为控制台命令处理器，指定其处理的模式

### IConsoleHandler（Model）
- **接口方法**：`ETTask Run(Fiber fiber, ModeContex contex, string content)`
- **参数**：
  - `fiber` — 当前运行的 Fiber（通过 `self.Fiber()` 获取）
  - `contex` — 当前模式上下文（ModeContex 实体）
  - `content` — 用户输入的完整原始命令行字符串

### ModeContex（Model）
- **继承**：`Entity`, `IAwake`, `IDestroy`
- **挂载**：`[ComponentOf(typeof(ConsoleComponent))]`
- **字段**：`string Mode` — 当前激活的模式名称（初始为空字符串）
- **系统**：`ModeContexSystem`（同文件）— Awake/Destroy 时重置 Mode 为 `""`
- **功能**：表示控制台当前所处的命令模式（类似 shell 的子模式），`"exit"` 命令会移除此组件退出模式

---

## 命令处理器（Hotfix）

### ConsoleComponentSystem
- **EntitySystemOf**：`ConsoleComponent`
- **FriendOf**：`ModeContex`（允许访问其字段）
- **核心逻辑**（`Start` 异步循环）：
  1. 创建 `CancellationTokenSource`
  2. 无限循环：在线程池异步读取 `Console.In.ReadLine()`（防止阻塞主 Fiber）
  3. 显示提示符格式：`{currentMode}> `（无模式时显示 `> `）
  4. 处理特殊命令：
     - 空行：跳过
     - `"exit"`：`RemoveComponent<ModeContex>()`（退出当前模式，回到顶级）
     - 其他：按空格分割，取第一个词为模式名（无 ModeContex 时），从 `ConsoleDispatcher` 获取处理器并执行

### ReloadDllConsoleHandler
- **模式**：`ConsoleMode.ReloadDll`（`"R"`）
- **功能**：调用 `CodeLoader.Instance.Reload()` 热重载代码 DLL
- **注意**：直接调用后立即返回，无参数处理逻辑

### ReloadConfigConsoleHandler
- **模式**：`ConsoleMode.ReloadConfig`（`"C"`）
- **功能**：解析配置名，找到对应的 `{ConfigName}Category` 类型，调用 `ConfigLoader.Instance.Reload(type)` 热重载单个配置
- **错误处理**：
  - 仅输入 `"C"`（无配置名）→ 移除 ModeContex，提示错误信息
  - 找不到 Category 类型 → 打印错误日志，不重载

---

## 架构模式

本 package 采用以下模式：

1. **Entity-Component 模式**：`ConsoleComponent` 挂载在 Scene 上，`ModeContex` 作为子组件表示状态
2. **Singleton 分发器**：`ConsoleDispatcher` 以单例形式管理所有命令处理器
3. **属性驱动注册**：通过 `[ConsoleHandler(mode)]` 属性自动发现处理器，无需手动注册
4. **模式化命令**：类似 vim/gdb 的模式切换，进入某个模式后后续输入都由该模式处理器处理，直到 `exit`
5. **异步 I/O**：使用 `Task.Factory.StartNew` 将阻塞的 `Console.ReadLine` 放到线程池，避免阻塞 ET 主循环
6. **partial class 扩展**：`ConsoleMode` 是 partial 类，其他 package 可在同命名空间声明新的 `ConsoleMode` partial 部分来添加新模式常量

---

## 关键流程

### 命令处理流程

```
用户输入
    │
    ▼
Task.Factory.StartNew(Console.In.ReadLine) ← 线程池异步读取，避免阻塞主 Fiber
    │
    ▼
ConsoleComponent.Start() 循环（在 Fiber 主线程中）
    │
    ├─ 空行 → 跳过
    ├─ "exit" → RemoveComponent<ModeContex>（退出当前模式）
    └─ 其他命令
           │
           ├─ 有 ModeContex → mode = modeContex.Mode（使用当前模式）
           └─ 无 ModeContex → mode = lines[0]，新建 ModeContex，设置 mode
                   │
                   ▼
           ConsoleDispatcher.Get(mode) → IConsoleHandler
                   │
                   ▼
           handler.Run(fiber, modeContex, line)（完整原始行传入）
```

### 处理器注册流程

```
程序启动 → ConsoleDispatcher.Awake()（[CodeProcess] 阶段）
    │
    ▼
CodeTypes.Instance.GetTypes(typeof(ConsoleHandlerAttribute))
    │  扫描所有标记了 [ConsoleHandler] 的类型
    ▼
foreach type
    │  读取 ConsoleHandlerAttribute.Mode
    │  Activator.CreateInstance(type)
    │  校验是否实现 IConsoleHandler（否则抛异常）
    ▼
handlers[mode] = handler（字典注册）
```

### 模式切换示意

```
初始状态：无 ModeContex
    │
    用户输入 "R"
    ├─ mode = "R"，创建 ModeContex（Mode="R"）
    └─ ReloadDllConsoleHandler.Run() 执行，重载 DLL

用户输入 "C UnitConfig"
    ├─ mode = "C"，创建 ModeContex（Mode="C"）
    └─ ReloadConfigConsoleHandler.Run() 执行
           ├─ 解析: configName="UnitConfig", category="UnitConfigCategory"
           └─ ConfigLoader.Instance.Reload(UnitConfigCategory)

【若已在 ModeContex="C" 模式】
用户继续输入 "UnitConfig"
    └─ mode = "C"（ModeContex 已存在），直接分发给 ReloadConfigConsoleHandler
           ├─ content = "UnitConfig"（非 "C"），走 default 分支
           └─ 尝试解析 ss[1]... 但此时 content 只有一段，会 IndexOutOfRange！

用户输入 "exit"
    └─ RemoveComponent<ModeContex>，回到无模式状态
```

---

## 代码示例

### 注册自定义控制台命令

```csharp
// 1. 在 ConsoleMode 中添加常量（可选，也可直接用字符串）
namespace ET.Server
{
    public static partial class ConsoleMode
    {
        public const string MyCommand = "MyCmd";
    }
}

// 2. 实现处理器
[ConsoleHandler(ConsoleMode.MyCommand)]  // 也可写 [ConsoleHandler("MyCmd")]
public class MyConsoleHandler : IConsoleHandler
{
    public async ETTask Run(Fiber fiber, ModeContex contex, string content)
    {
        // content 是用户输入的完整原始行，如 "MyCmd arg1 arg2"
        string[] parts = content.Split(' ');
        // 处理命令...
        Log.Console($"Received: {content}");

        // 若是单次命令，处理完后退出模式
        contex.Parent.RemoveComponent<ModeContex>();

        await ETTask.CompletedTask;
    }
}
```

### 查看 ReloadConfig 完整调用链

```csharp
// 用户输入: "C UnitConfig"
// 1. ConsoleComponent.Start() 解析:
//    lines = ["C", "UnitConfig"]
//    mode = "C" → 创建 ModeContex(Mode="C")
//    ConsoleDispatcher.Get("C") → ReloadConfigConsoleHandler

// 2. ReloadConfigConsoleHandler.Run():
//    content = "C UnitConfig"
//    ss = ["C", "UnitConfig"]
//    configName = "UnitConfig"
//    category = "UnitConfigCategory"
//    type = CodeTypes.Instance.GetType("ET.UnitConfigCategory")
//    await ConfigLoader.Instance.Reload(type)
//    Log.Console("reload config UnitConfig finish!")
```

---

## 依赖关系

### 依赖的 Package
- **cn.etetet.core**：`Entity`, `Singleton`, `ETTask`, `Fiber`, `CodeTypes`, `BaseAttribute`, `[EntitySystem]`, `[ComponentOf]`, `[CodeProcess]` 等核心机制
- **cn.etetet.loader**：`CodeLoader.Instance.Reload()`（热重载 DLL）、`ConfigLoader.Instance.Reload(type)`（热重载配置）

### 被依赖
- 该 package 是可选的运维工具包，其他 package 不依赖它
- 外部 package 可通过实现 `IConsoleHandler` 并标记 `[ConsoleHandler(mode)]` 扩展新命令

---

## 边界情况与潜在问题（Round 2 补充）

1. **模式内输入格式问题**：在 `ModeContex` 激活状态下，用户输入的 `content` 是完整原始行。`ReloadConfigConsoleHandler` 中 `ss[1]` 假设格式为 `"C ConfigName"`，若用户已在 "C" 模式下只输入 `"ConfigName"`（无前缀），则 `ss[1]` 会抛出 `IndexOutOfRangeException`

2. **未注册模式**：若用户输入一个未注册的模式名，`ConsoleDispatcher.Get(key)` 直接抛 `KeyNotFoundException`，被外层 `catch (Exception e)` 捕获并打印到控制台

3. **ModeContex 生命周期**：`ModeContex` 由 `ConsoleComponent.Start()` 动态添加/移除，每次进入新命令都会建立上下文，`exit` 时销毁。但注意：当前实现在每次非空命令时（无论是否已有 ModeContex）都会尝试获取处理器并可能覆盖 ModeContex 的 Mode

4. **线程安全**：`Console.ReadLine()` 在线程池执行，结果通过 `await` 返回到主 Fiber，ET 的命令处理逻辑都在 Fiber 主线程执行，因此线程安全

5. **`[FriendOf(typeof(ModeContex))]`**：`ConsoleComponentSystem` 使用了此标记，允许访问 `ModeContex.Mode` 字段（ET 框架的访问控制机制）

---

## 注意事项

- `ConsoleMode` 中的 `ShowMemory("M")`、`Repl`、`Debugger`、`CreateRobot`、`Robot` 等模式常量在本 package 中没有对应的处理器实现，需由其他 package 提供（如 robot 相关包）
- `ModeContex` 的 Mode 字段在 Destroy 时重置为空字符串，确保组件复用时状态清洁
- 控制台读取在线程池执行，但命令处理在 ET 主 Fiber 中执行，保证线程安全
- `ConsoleHandlerAttribute` 定义在 `ET` 命名空间（非 `ET.Server`），使其可在 Model 层通用
- `ConsoleDispatcher` 标记了 `[CodeProcess]`，意味着它在代码加载阶段（热重载时）会重新初始化，自动重新注册处理器
