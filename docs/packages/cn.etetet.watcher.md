# cn.etetet.watcher

> 版本：3.0.0 | 分析轮次：Round 2

---

## 概述

`cn.etetet.watcher` 是 ET 框架的**进程守护/监视包**，负责在 Watcher 服务器进程启动时根据 `StartProcessConfig` 配置拉起所有属于本机的子进程，并持有进程引用以备后续管理。

- **描述**：用于监视进程，拉起进程，防止进程挂掉
- **作用域**：仅服务端（Server）
- **命名空间**：`ET.Server`
- **作者**：tanghai (ET框架 egametang/ET)

---

## 目录结构

```
cn.etetet.watcher/
├── Scripts/
│   ├── Model/Server/
│   │   ├── WatcherComponent.cs           # 组件数据：持有进程字典（字段内联初始化）
│   │   └── AssemblyReference.asmref      # 指向 Model 程序集
│   └── Hotfix/Server/
│       ├── WatcherComponentSystem.cs     # 启动逻辑：Awake时读配置、拉起本机进程
│       ├── WatcherHelper.cs              # 工具：机器IP匹配、进程启动、机器配置查找
│       └── AssemblyReference.asmref      # 指向 Hotfix 程序集
├── Ignore.ET.Watcher.asmdef              # Assembly 定义
└── package.json                          # 版本信息 (无显式包依赖声明)
```

---

## 核心类

### WatcherComponent（Model 层）

```csharp
namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public class WatcherComponent : Entity, IAwake
    {
        // 字典在字段声明时内联初始化（非 Awake 中初始化）
        public readonly Dictionary<int, System.Diagnostics.Process> Processes
            = new Dictionary<int, System.Diagnostics.Process>();
    }
}
```

**设计细节**：
- `readonly` 保证字典引用不可替换，但字典内容可修改
- `Dictionary<int, Process>` — key 为 `StartProcessConfig.Id`（整数进程配置ID），value 为操作系统进程对象
- `[ComponentOf(typeof(Scene))]` — 挂载于 Scene 实体，每个 Watcher Scene 对应一个守护实例

---

### WatcherComponentSystem（Hotfix 层）

```csharp
namespace ET.Server
{
    [EntitySystemOf(typeof(WatcherComponent))]
    public static partial class WatcherComponentSystem
    {
        [EntitySystem]
        public static void Awake(this WatcherComponent self)
        {
            string[] localIP = NetworkHelper.GetAddressIPs();
            var processConfigs = StartProcessConfigCategory.Instance.GetAll();
            foreach (StartProcessConfig startProcessConfig in processConfigs.Values)
            {
                if (!WatcherHelper.IsThisMachine(startProcessConfig.InnerIP, localIP))
                {
                    continue;
                }
                System.Diagnostics.Process process = WatcherHelper.StartProcess(startProcessConfig.Id);
                self.Processes.Add(startProcessConfig.Id, process);
            }
        }
    }
}
```

**Awake 流程**：
1. 调用 `NetworkHelper.GetAddressIPs()` 获取本机所有 IP（来自 `cn.etetet.core`）
2. 通过 `StartProcessConfigCategory.Instance.GetAll()` 获取全部进程配置（来自 `cn.etetet.startconfig`）
3. 逐条过滤：只启动 `InnerIP` 属于本机的进程配置
4. 调用 `WatcherHelper.StartProcess(id)` 拉起子进程
5. 进程对象存入 `Processes` 字典，key = `startProcessConfig.Id`

---

### WatcherHelper（Hotfix 层）

```csharp
namespace ET.Server
{
    public static partial class WatcherHelper
    {
        // 查找本机对应的 StartMachineConfig（用于外部调用，Awake内部不调用此方法）
        public static StartMachineConfig GetThisMachineConfig() { ... }

        // 判断一个 IP 字符串是否属于本机
        public static bool IsThisMachine(string ip, string[] localIPs) { ... }

        // 用 dotnet 命令启动指定 processId 的子进程
        public static System.Diagnostics.Process StartProcess(int processId, int createScenes = 0) { ... }
    }
}
```

#### GetThisMachineConfig()

```csharp
public static StartMachineConfig GetThisMachineConfig()
{
    string[] localIP = NetworkHelper.GetAddressIPs();
    StartMachineConfig startMachineConfig = null;
    foreach (StartMachineConfig config in StartMachineConfigCategory.Instance.GetAll().Values)
    {
        if (!WatcherHelper.IsThisMachine(config.InnerIP, localIP))
            continue;
        startMachineConfig = config;
        break; // 只取第一个匹配的机器配置
    }
    if (startMachineConfig == null)
        throw new Exception("not found this machine ip config!");
    return startMachineConfig;
}
```

> **注意**：此方法在包内 `WatcherComponentSystem.Awake()` 中**未被调用**，是供外部代码（如 `cn.etetet.startconfig` 相关启动流程）使用的公共工具方法。

#### IsThisMachine()

```csharp
public static bool IsThisMachine(string ip, string[] localIPs)
{
    // 以下三种情况均视为本机：
    // 1. ip == "127.0.0.1"  (loopback)
    // 2. ip == "0.0.0.0"    (全地址绑定)
    // 3. ip 在 localIPs 数组中（GetAddressIPs 返回的实际网卡IP）
    if (ip != "127.0.0.1" && ip != "0.0.0.0" && !((IList) localIPs).Contains(ip))
        return false;
    return true;
}
```

**实现细节**：使用 `((IList) localIPs).Contains(ip)` 进行字符串查找，而非 LINQ `Contains`，避免引入不必要的依赖。

#### StartProcess()

```csharp
public static System.Diagnostics.Process StartProcess(int processId, int createScenes = 0)
{
    StartProcessConfig startProcessConfig = StartProcessConfigCategory.Instance.Get(processId);
    const string exe = "dotnet";
    string arguments = $"App.dll"
        + $" --Process={startProcessConfig.Id}"
        + $" --SceneName=Server"
        + $" --StartConfig={Options.Instance.StartConfig}"
        + $" --Develop={Options.Instance.Develop}"
        + $" --LogLevel={Options.Instance.LogLevel}"
        + $" --Console={Options.Instance.Console}";
    Log.Debug($"{exe} {arguments}");
    System.Diagnostics.Process process = ProcessHelper.Run(exe, arguments);
    return process;
}
```

**Round 2 补充**：
- `createScenes` 参数虽然声明，但在函数体内**从未被使用**（不出现在命令行参数中），疑为预留的扩展点，供未来支持"创建特定场景"时使用
- `SceneName` 固定为 `"Server"` — 所有子进程均以 Server 场景类型启动
- 继承父进程的 `Options`（启动配置路径、开发模式、日志级别、控制台开关）传递给子进程

---

## 关键流程

### 服务器进程拉起流程

```
[Watcher进程 启动]
    │
    └─ WatcherComponent.Awake()
           │
           ├─ NetworkHelper.GetAddressIPs()
           │       └─ 返回 string[] { "192.168.1.10", "127.0.0.1", ... }
           │
           ├─ StartProcessConfigCategory.Instance.GetAll()
           │       └─ 返回全部进程配置（processId -> config）
           │
           └─ foreach config in processConfigs.Values:
                  │
                  ├─ IsThisMachine(config.InnerIP, localIP)?
                  │       ├─ false → 跳过（属于其他物理机器）
                  │       └─ true ↓
                  │
                  └─ WatcherHelper.StartProcess(config.Id)
                         │
                         ├─ StartProcessConfigCategory.Instance.Get(processId)
                         │       └─ 获取该进程配置
                         │
                         ├─ 构造命令：dotnet App.dll --Process=X --SceneName=Server ...
                         │
                         └─ ProcessHelper.Run("dotnet", arguments)
                                └─ 返回 Process 对象 → 存入 self.Processes[config.Id]
```

### 多机部署场景

```
物理机 A (192.168.1.10):
    WatcherComponent.Awake()
        → 仅启动 InnerIP == "192.168.1.10" 或 "127.0.0.1" 的进程
        → Gate进程(192.168.1.10:10002), Login进程(127.0.0.1:10001)

物理机 B (192.168.1.20):
    WatcherComponent.Awake()
        → 仅启动 InnerIP == "192.168.1.20" 的进程
        → Game进程(192.168.1.20:20001)

两台机器各自只拉起自己的进程，无需中心协调
```

---

## 依赖关系

### 外部依赖

| 依赖 | 用途 |
|------|------|
| `cn.etetet.core` | `Entity`/`EntitySystem`基础、`NetworkHelper.GetAddressIPs()`、`ProcessHelper.Run()`、`Options.Instance`、`Log.Debug()` |
| `cn.etetet.startconfig` | `StartProcessConfig`/`StartProcessConfigCategory`、`StartMachineConfig`/`StartMachineConfigCategory` |

### package.json 中的依赖声明

`package.json` 的 `relatedPackages` 为空对象 `{}`，即**未在清单中声明依赖**，实际依赖通过 asmdef 引用隐式满足。

### 被哪些 package 使用

`WatcherComponent` 由服务端 Watcher Scene 启动时挂载（ET框架服务端入口）。`WatcherHelper.GetThisMachineConfig()` 供其他服务端包查询本机配置使用。

---

## 设计特点与注意事项

| 特点 | 说明 |
|------|------|
| **单次启动** | 仅在 `Awake` 时拉起进程，没有 `Update` 轮询检查进程存活 |
| **无重启逻辑** | `Processes` 字典保存引用，但尚未实现健康检查/崩溃重启 |
| **IP判断简单** | 基于字符串匹配，不考虑 DNS/FQDN，对多网卡环境需配置正确 IP |
| **预留扩展** | `StartProcess` 的 `createScenes` 参数未使用，为后续扩展预留 |
| **命令行传递** | 子进程继承父进程（Watcher进程）的启动配置，确保所有进程用同一套配置 |

---

## 与整体架构的关系

在 ET 框架分布式架构中，`cn.etetet.watcher` 处于**运维/部署层**：

```
[物理机 启动]
    │
    └─ dotnet App.dll --Process=1 --SceneName=Watcher  ← 手动启动 Watcher 进程
           │
           └─ WatcherComponent.Awake()
                  ├─→ 启动 [Gate进程]    dotnet App.dll --Process=2 ...
                  ├─→ 启动 [Game进程]    dotnet App.dll --Process=3 ...
                  ├─→ 启动 [Login进程]   dotnet App.dll --Process=4 ...
                  └─→ 启动 [其他进程]    dotnet App.dll --Process=N ...

每个子进程 = 独立 dotnet 进程，通过 StartConfig 约定端口/场景角色/内网地址
```

---

*文档生成：Round 2 — 补充了 `createScenes` 未使用参数说明、`GetThisMachineConfig()` 调用范围说明、IP判断实现细节及多机部署示意图*
