# cn.etetet.loader — ET 框架加载器

> 版本：3.0.1 | Round 3 深化：跨包交互、边界情况、异常处理

---

## 概述

`cn.etetet.loader` 是 ET 框架的**启动引导层**，负责将游戏拆分为可热更新的多个程序集（Assembly），并在运行时动态加载它们。这是整个热更新架构的入口点。

**核心职责：**
- 作为 Unity 游戏的启动入口（`Init` MonoBehaviour）
- 从 YooAssets 或本地文件系统加载热更新 DLL
- 通过 HybridCLR 支持 IL2CPP 环境下的 AOT 元数据注入
- 初始化 `World`、`FiberManager`、`TimeInfo` 等核心单例
- 管理 Editor 下的 Assembly 热重载（F6 编译、F7 热重载）
- 提供 UnityLogger（客户端）和 NLogger（服务端）日志实现
- 提供日志重定向（双击 Console 日志跳转真实源文件）
- 通过 `CodeModeChangeHelper` 管理 asmref 文件驱动的代码模式切换

---

## 目录结构

```
cn.etetet.loader/
├── Bundles/
│   ├── AotDlls/          # IL2CPP AOT 元数据 DLL（mscorlib 等）
│   └── Code/             # 热更新 DLL bytes 文件
│       ├── ET.Model.dll.bytes
│       ├── ET.Model.pdb.bytes
│       ├── ET.ModelView.dll.bytes
│       ├── ET.ModelView.pdb.bytes
│       ├── ET.Hotfix.dll.bytes
│       ├── ET.Hotfix.pdb.bytes
│       ├── ET.HotfixView.dll.bytes
│       └── ET.HotfixView.pdb.bytes
├── Editor/
│   ├── Assembly/
│   │   └── AssemblyEditor.cs          # PlayMode 进入时清除 Library DLL
│   ├── AssetPostProcessor/
│   │   └── OnGenerateCSProjectProcessor.cs
│   ├── BuildEditor/
│   │   ├── BuildEditor.cs             # ET/Loader/Build Tool 窗口
│   │   └── BuildHelper.cs             # 实际构建逻辑 + ENABLE_VIEW 菜单
│   ├── GlobalConfigEditor/
│   │   └── GlobalConfigEditor.cs      # GlobalConfig 自定义 Inspector
│   ├── Helper/
│   │   ├── AssemblyTool.cs            # F6编译、F7热重载、CompileDlls流程
│   │   └── EditorLogHelper.cs         # 编辑器日志辅助
│   ├── InitEditor/
│   │   ├── LoaderEditor.cs            # ET/Loader/Init 菜单
│   │   ├── CodeModeChangeHelper.cs    # 切换 Client/Server/ClientServer 模式（asmref）
│   │   ├── DefineHelper.cs            # 管理编译宏
│   │   ├── InitScriptHelper.cs
│   │   ├── LinkSlnHelper.cs
│   │   ├── ReGenerateProjectFilesHelper.cs
│   │   ├── SceneNameSetHelper.cs
│   │   └── ScriptsReferencesHelper.cs
│   ├── LogRedirection/
│   │   └── LogRedirection.cs          # Console双击跳转真实源文件
│   └── ServerCommandLineEditor/
│       └── ServerCommandLineEditor.cs # 服务端进程启动工具
├── Scripts/
│   ├── HotfixView/Client/
│   │   └── GlobalComponentSystem.cs   # GlobalComponent Awake 实现
│   ├── Loader/
│   │   ├── Client/
│   │   │   ├── Init.cs                # Unity MonoBehaviour 入口
│   │   │   ├── CodeLoader.cs          # 客户端程序集加载器（Singleton）
│   │   │   ├── Define.cs              # 编译期常量/条件编译标志
│   │   │   ├── GlobalConfig.cs        # ScriptableObject 全局配置
│   │   │   ├── CoroutineHelper.cs     # AsyncOperation + HTTP 工具
│   │   │   └── UnityLogger.cs         # Unity 日志实现（含编辑器链接跳转）
│   │   ├── Server/
│   │   │   ├── Init.cs                # 服务端 .NET 入口 (#if DOTNET)
│   │   │   ├── CodeLoader.cs          # 服务端程序集加载器（AssemblyLoadContext）
│   │   │   └── NLogger.cs             # NLog 日志实现
│   │   └── Share/
│   │       └── HttpClientHelper.cs    # HTTP GET（WebGL/非WebGL双分支）
│   └── ModelView/Client/
│       └── GlobalComponent.cs         # 全局 Unity 场景引用组件
└── Resources/
    └── GlobalConfig.asset             # 默认全局配置
```

---

## 核心类/接口详解

### `Init` (MonoBehaviour) — 客户端启动入口
**文件**: `Scripts/Loader/Client/Init.cs`

Unity Scene 的起点，挂载在初始 GameObject 上。

```csharp
private void Start()
{
    this.StartAsync().NoContext(); // 异步启动，不等待
}

private async ETTask StartAsync()
{
    DontDestroyOnLoad(gameObject);

    // 全局未捕获异常处理
    AppDomain.CurrentDomain.UnhandledException += (sender, e) => Log.Error(e.ExceptionObject.ToString());

    // 注意：客户端命令行参数从空字符串解析（非真实命令行）
    string[] args = "".Split(" ");
    Parser.Default.ParseArguments<Options>(args)
        .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
        .WithParsed(o => World.Instance.AddSingleton(o));

    // 从 Resources 加载 ScriptableObject 配置
    GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
    Options.Instance.SceneName = globalConfig.SceneName;

    World.Instance.AddSingleton<Logger>().Log = new UnityLogger();
    ETTask.ExceptionHandler += Log.Error;

    World.Instance.AddSingleton<TimeInfo>();
    World.Instance.AddSingleton<FiberManager>();

    // 等待 YooAssets 初始化（第二参数 true = 默认包）
    await World.Instance.AddSingleton<ResourcesComponent>().CreatePackageAsync("DefaultPackage", true);

    // 异步加载热更新代码（fire-and-forget）
    World.Instance.AddSingleton<CodeLoader>().Start().NoContext();
}

void Update()      { TimeInfo.Instance.Update(); FiberManager.Instance.Update(); }
void LateUpdate()  { FiberManager.Instance.LateUpdate(); }
void OnApplicationQuit() { World.Instance.Dispose(); }
```

> **边界情况**：`WithNotParsed` 在解析失败时抛出异常，但客户端实际传入空字符串，因此这里是防御性代码，正常情况下不会触发。

---

### `Init` — 服务端入口
**文件**: `Scripts/Loader/Server/Init.cs` (`#if DOTNET`)

```csharp
public void Start()
{
    // 服务端读取真实命令行参数
    Parser.Default.ParseArguments<Options>(System.Environment.GetCommandLineArgs())
        .WithParsed(o => World.Instance.AddSingleton(o));

    // NLogger 需要 SceneName、Process 参数（用于 logger 命名）
    World.Instance.AddSingleton<Logger>().Log = new NLogger(
        Options.Instance.SceneName, Options.Instance.Process, 0);

    // 同步启动（无 async）
    World.Instance.AddSingleton<CodeLoader>().Start();
}
```

---

### `CodeLoader` (Singleton) — 客户端程序集加载器
**文件**: `Scripts/Loader/Client/CodeLoader.cs`

**注册到 CodeTypes 的 6 个程序集**：
```csharp
World.Instance.AddSingleton<CodeTypes, Assembly[]>(new[]
{
    typeof(World).Assembly,   // ET.Core
    typeof(Init).Assembly,    // ET.Loader
    this.modelAssembly,       // ET.Model      (热更新，bytes加载)
    this.modelViewAssembly,   // ET.ModelView  (热更新，bytes加载)
    hotfixAssembly,           // ET.Hotfix     (热更新，bytes加载)
    hotfixViewAssembly        // ET.HotfixView (热更新，bytes加载)
});
```

**DownloadAsync 细节**：
```csharp
// 通过 LoadAllAssetsAsync 加载同一 bundle 内所有 TextAsset
this.dlls = await ResourcesComponent.Instance
    .LoadAllAssetsAsync<TextAsset>("Packages/cn.etetet.loader/Bundles/Code/ET.Model.dll.bytes");
// 返回 Dictionary<string, TextAsset>，key 为不带路径的资源名
// 如 "ET.Model.dll", "ET.Model.pdb", "ET.Hotfix.dll" 等
```

**IL2CPP AOT 元数据注入**：
```csharp
foreach (var kv in this.aotDlls)
{
    RuntimeApi.LoadMetadataForAOTAssembly(kv.Value.bytes, HomologousImageMode.SuperSet);
}
```

**热重载流程（Reload）**：
```csharp
public void Reload()
{
    // 仅重载 Hotfix + HotfixView（Model 不重载）
    (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = LoadHotfix();

    CodeTypes codeTypes = World.Instance.AddSingleton<CodeTypes, Assembly[]>(/* 6个 */);
    codeTypes.CodeProcess(); // 重新扫描所有系统并注册

    Log.Info("reload dll finish!");
}
```

> **边界情况（Round 3）**：
> - `LoadAllAssetsAsync` 的参数是 bundle 内任意一个资源路径，实际返回的是整个 bundle 内所有资源的字典。这意味着 Model/ModelView/Hotfix/HotfixView 四套 DLL 都在同一个 bundle 里。
> - 客户端 `Reload()` 时保留 `modelAssembly` 和 `modelViewAssembly` 字段的旧引用；Model 层不热重载，只有 Hotfix 和 HotfixView 被替换。这是设计约束，不是 bug。

---

### `CodeLoader` — 服务端程序集加载器
**文件**: `Scripts/Loader/Server/CodeLoader.cs` (`#if DOTNET`)

只注册 **4 个程序集**（无 ModelView/HotfixView），从 `./Bin/ET.Hotfix.dll` 加载 Hotfix（Model 从 AppDomain 已有程序集查找）。

```csharp
public void Start()
{
    // Model 从已编译程序集中查找（不需要 bytes）
    foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
    {
        if (ass.GetName().Name == "ET.Model") { this.assembly = ass; break; }
    }

    Assembly hotfixAssembly = LoadHotfix();

    // 只有4个程序集：Core + Loader + Model + Hotfix
    World.Instance.AddSingleton<CodeTypes, Assembly[]>([
        typeof(World).Assembly, typeof(Init).Assembly,
        this.assembly, hotfixAssembly
    ]);

    new StaticMethod(this.assembly, "ET.Entry", "Start").Run();
}

private Assembly LoadHotfix()
{
    assemblyLoadContext?.Unload();   // 卸载旧版本
    GC.Collect();
    assemblyLoadContext = new AssemblyLoadContext("ET.Hotfix", isCollectible: true);

    byte[] dllBytes = File.ReadAllBytes("./Bin/ET.Hotfix.dll");
    byte[] pdbBytes = File.ReadAllBytes("./Bin/ET.Hotfix.pdb");
    return assemblyLoadContext.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
}
```

> **边界情况（Round 3）**：
> - `assemblyLoadContext.Unload()` 后必须 `GC.Collect()` 才能真正释放内存，否则旧 DLL 会持续占用直到 GC 回收。
> - 服务端 `Reload()` 记录的是 `Log.Debug`（而非客户端的 `Log.Info`），级别不同。
> - 如果 `./Bin/ET.Hotfix.dll` 不存在，`File.ReadAllBytes` 会直接抛 `FileNotFoundException`，无错误回退处理。

---

### `AssemblyTool` — 编译与热重载工具
**文件**: `Editor/Helper/AssemblyTool.cs`

**快捷键**：
- `F6` → `ET/Loader/Compile` → `DoCompile()`
- `F7` → `ET/Loader/Reload` → `CodeLoader.Instance?.Reload()`（仅 PlayMode）

**编译流程 `DoCompile()`**：
```
1. AssetDatabase.Refresh(ForceUpdate)   — 确保文件时间戳准确
2. CodeModeChangeHelper.ChangeToCodeMode(globalConfig.CodeMode)
3. CompileDlls()
   └── PlayerBuildInterface.CompilePlayerScripts(settings, "Temp/Bin/Debug")
       — 编译时额外注入 IS_COMPILING 宏
       — 编译中临时切换为 unitySynchronizationContext（PlayMode 下需要）
       — isCompileOk = result.assemblies.Count > 0
4. CopyHotUpdateDlls()
   └── FileHelper.CleanDirectory(CodeDir)
   └── 逐一复制 ET.Hotfix/ET.HotfixView/ET.Model/ET.ModelView
       从 Temp/Bin/Debug/*.dll → Bundles/Code/*.dll.bytes
       从 Temp/Bin/Debug/*.pdb → Bundles/Code/*.pdb.bytes
5. AssetDatabase.Refresh()
```

> **边界情况（Round 3）**：
> - `CompileDlls()` 失败时（`isCompileOk = false`）直接 `return`，不会执行 `CopyHotUpdateDlls()`，Code 目录保留旧版本。
> - `try/finally` 确保 PlayMode 下的同步上下文一定被还原，即使编译失败。
> - `result.assemblies.Count > 0` 是成功判断，但如果没有任何程序集被编译（比如所有文件都未变化），这里仍然返回 `false`，触发早退。

**DllNames 列表**（按编译顺序）：
```csharp
public static readonly string[] DllNames =
{
    "ET.Hotfix", "ET.HotfixView", "ET.Model", "ET.ModelView"
};
```

---

### `CodeModeChangeHelper` — asmref 文件管理器
**文件**: `Editor/InitEditor/CodeModeChangeHelper.cs`

这是跨包代码模式管理的核心机制。它通过写入/删除 `AssemblyReference.asmref` 文件，控制每个包的哪些脚本目录被编译到哪个程序集中。

**搜索路径矩阵**：
```
moduleDirs × scriptDirs × modelDirs × serverDirs
= { Packages, Library/PackageCache }
× { Scripts, CodeMode }
× { Model, Hotfix, ModelView, HotfixView, Core, Loader }
× { Server, Client, Share, ClientServer }
```

对每个存在的目录路径，根据 `codeMode` 决定是否创建 asmref：
```csharp
// 白名单路径（47条，以 codeMode 为前缀）
private static readonly HashSet<string> v = new()
{
    "Client/Scripts/Model/Client",
    "Client/Scripts/Model/Share",
    "Client/Scripts/Hotfix/Client",
    // ...
    "ClientServer/Scripts/Loader/Server",
    // ...
};
```

**asmref 内容格式**：
```json
{ "reference": "ET.Model" }
```

> **跨包影响（Round 3）**：
> `ChangeToCodeMode` 同时处理 `Packages/` 和 `Library/PackageCache/` 下所有匹配 `cn.etetet.*` 的包。这意味着切换 CodeMode 时会**同时修改所有 ET 包**的 asmref 文件，而不仅仅是 loader 包自身。这是全局操作。

---

### `LogRedirection` — Console 日志跳转
**文件**: `Editor/LogRedirection/LogRedirection.cs`

```csharp
[OnOpenAsset(0)]  // 双击Console日志时触发
private static bool OnOpenAsset(int instanceID, int line)
{
    if (line <= 0) return false;  // 快速退出：无行号

    Regex logFileRegex = new(@"((Log\.cs)|(UnityLogger\.cs)|(YooLogger\.cs))");
    string codePath = AssetDatabase.GetAssetPath(instanceID);
    if (logFileRegex.IsMatch(codePath))
    {
        var content = GetStackTrace();  // 反射获取 m_ActiveText
        // 优先尝试 <a href="..."> 格式
        var hrefMatch = Regex.Match(content, @"<a href=""(.*?)"" line=""(\w+)"">.*?</a>");
        if (hrefMatch.Success)
        {
            OpenIDE(hrefMatch.Groups[1].Value, int.Parse(hrefMatch.Groups[2].Value));
            return true;
        }
        // 回退到 (at path:line) 格式，跳过日志文件帧
        Match stackLineMatch = Regex.Match(content, @"\(at (.+):([0-9]+)\)");
        while (stackLineMatch.Success)
        {
            codePath = stackLineMatch.Groups[1].Value;
            if (!logFileRegex.IsMatch(codePath))
            {
                OpenIDE(codePath, int.Parse(stackLineMatch.Groups[2].Value));
                return true;
            }
            stackLineMatch = stackLineMatch.NextMatch();
        }
    }
    return false;
}
```

> **边界情况（Round 3）**：
> - `line <= 0` 的早期返回：当双击的是没有行号信息的日志时，直接放行给 Unity 默认处理。
> - `GetStackTrace()` 可能返回 `null`（ConsoleWindow 未聚焦时），此时 `Regex.Match(null, ...)` 会抛 `ArgumentNullException`。这是潜在 bug，但由于双击时 ConsoleWindow 通常已聚焦，实际很少触发。
> - 路径通过 `Path.IsPathFullyQualified` 检查，非绝对路径时 `Path.GetFullPath` 转换，保证在任何工作目录下都正确打开文件。

---

### `BuildEditor` 与 `BuildHelper` — 构建工具
**文件**: `Editor/BuildEditor/BuildEditor.cs` / `BuildHelper.cs`

**菜单优先级（`ETMenuItemPriority`）**：
```csharp
BuildTool    = 1001  // ET/Loader/Build Tool
ChangeDefine = 1002  // ET/Loader/Add|Remove ENABLE_VIEW
Compile      = 1003  // ET/Loader/Compile (F6)
Reload       = 1004  // ET/Loader/Reload (F7)
NavMesh      = 1005
ServerTools  = 1006  // ET/Loader/ServerTools
```

**PlatformType 枚举**：
```csharp
None, Android, IOS, Windows, MacOS, Linux, WebGL
```

**构建流程 `BuildHelper.Build()`**：
```
1. 验证 CodeMode == Client（强制，否则报错）
2. 跨平台时弹出确认对话框
3. BuildPipeline.BuildPlayer(
       scenes: ["Packages/cn.etetet.loader/Scenes/Init.unity"],
       output: "./Release/{exeName}",
       target: buildTarget,
       options: buildOptions)
4. 打开 ./Release 目录
```

**ENABLE_VIEW 宏菜单**（条件编译）：
- 已定义时显示 "Remove ENABLE_VIEW" 菜单
- 未定义时显示 "Add ENABLE_VIEW" 菜单

---

### `ServerCommandLineEditor` — 服务端进程启动工具
**文件**: `Editor/ServerCommandLineEditor/ServerCommandLineEditor.cs`

```csharp
public enum DevelopMode { 正式 = 0, 开发 = 1, 压测 = 2 }
```

**功能按钮**：
| 按钮 | 命令 |
|------|------|
| Start Server (Single Process) | `dotnet Bin/ET.App.dll --SceneName={SceneName} --Process=1 --StartConfig=StartConfig/{config} --Console=1` |
| Start Watcher | `dotnet Bin/ET.App.dll --SceneName=Watcher --StartConfig=StartConfig/{config} --Console=1` |
| Start Mongo | `mongod --dbpath=db`（在 `../Database/bin/` 目录） |

**StartConfig 来源**：读取 `Packages/cn.etetet.yiuilubangen/Assets/Config/Binary/Server/StartConfig` 目录下的子文件夹列表。

> **跨包依赖（Round 3）**：`ServerCommandLineEditor` 硬编码依赖 `cn.etetet.yiuilubangen` 包的输出路径，若该包路径或结构变化，服务端启动配置列表将为空。

---

### `UnityLogger` — Unity 日志实现
**文件**: `Scripts/Loader/Client/UnityLogger.cs`

```csharp
[Invoke]
public class LogInvoker_Unity: AInvokeHandler<LogInvoker, ILog>
{
    public override ILog Handle(LogInvoker args) => new UnityLogger();
}
```

**日志级别映射**：
| ILog 方法 | Unity API |
|-----------|-----------|
| Trace/Debug/Info | `Debug.Log` |
| Warning | `Debug.LogWarning` |
| Error(string) | `Debug.LogError`（Editor 下先链接化） |
| Error(Exception) | `Debug.LogException` |

> **Round 3 注意**：`[Invoke]` 注解使 `LogInvoker_Unity` 被 `CodeTypes` 扫描后注册，这意味着 loader 包的日志实现是通过 Invoke 系统动态绑定的，而非在 `Init.cs` 中直接 `new UnityLogger()`。`Init.cs` 里的 `new UnityLogger()` 是冷启动时的直接实例化，`LogInvoker_Unity` 是热更新后通过 Invoke 系统重新创建日志实例的通道。

---

### `HttpClientHelper` — HTTP 工具
**文件**: `Scripts/Loader/Share/HttpClientHelper.cs`

```csharp
public static partial class HttpClientHelper
{
    public static async ETTask<string> Get(string link)
    {
#if UNITY_WEBGL
        UnityEngine.Networking.UnityWebRequest req = UnityWebRequest.Get(link);
        await req.SendWebRequest();
        return req.downloadHandler.text;
#else
        using HttpClient httpClient = new();
        HttpResponseMessage response = await httpClient.GetAsync(link);
        return await response.Content.ReadAsStringAsync();
#endif
    }
}
```

**错误处理**：异常时仅显示 URL 问号之前的部分（隐藏查询参数中的敏感数据）。

---

### `GlobalConfig` (ScriptableObject) — 全局配置
**文件**: `Scripts/Loader/Client/GlobalConfig.cs`

```csharp
public class GlobalConfig: ScriptableObject
{
    public CodeMode CodeMode;   // Client / Server / ClientServer
    public string SceneName;    // 初始场景名
    public string Address;      // 服务器地址
}

public enum CodeMode { Client = 1, Server = 2, ClientServer = 3 }
public enum BuildType { Debug, Release }
```

存储为 `Resources/GlobalConfig.asset`，由 `BuildEditor` 和 `LoaderEditor` 读写。

---

### `GlobalComponent` — 全局 Unity 场景引用
**文件**: `Scripts/ModelView/Client/GlobalComponent.cs`

```csharp
[ComponentOf(typeof(Scene))]
public class GlobalComponent: Entity, IAwake
{
    public Transform Global;     // /Global
    public Transform Unit;       // /Global/Unit
    public Transform UI;         // /Global/UI
    public GlobalConfig GlobalConfig;
}
```

**系统 Awake 实现**（`Scripts/HotfixView/Client/GlobalComponentSystem.cs`）：
```csharp
[EntitySystemOf(typeof(GlobalComponent))]
public static partial class GlobalComponentSystem
{
    [EntitySystem]
    private static void Awake(this GlobalComponent self)
    {
        self.Global = GameObject.Find("/Global").transform;
        self.Unit = GameObject.Find("/Global/Unit").transform;
        self.UI = GameObject.Find("/Global/UI").transform;
        self.GlobalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
    }
}
```

> **边界情况（Round 3）**：若 Scene 中不存在 `/Global`、`/Global/Unit` 或 `/Global/UI` GameObject，`GameObject.Find()` 返回 null，后续 `.transform` 访问会抛 `NullReferenceException`。这是场景配置错误时的硬崩溃点。

---

### `Define` — 编译期常量
**文件**: `Scripts/Loader/Client/Define.cs`

```csharp
public static class Define
{
    public const string CodeDir = "Packages/cn.etetet.loader/Bundles/Code";
    public const string BuildOutputDir = "Temp/Bin/Debug";

    public static bool IsDebug;      // #if DEBUG
    public static bool IsAsync;      // 非 UNITY_EDITOR || ASYNC
    public static bool IsEditor;     // #if UNITY_EDITOR
    public static bool EnableView;   // #if ENABLE_VIEW
    public static bool EnableIL2CPP; // #if ENABLE_IL2CPP
}
```

---

## 实现原理

### 热更新 DLL 加载流程

```
编辑器模式:
  File.ReadAllBytes("Packages/cn.etetet.loader/Bundles/Code/ET.Model.dll.bytes")
  Assembly.Load(bytes, pdbBytes)

发布模式（Mono）:
  YooAssets LoadAllAssetsAsync → Dictionary<string, TextAsset>
  Assembly.Load(textAsset.bytes, pdbTextAsset.bytes)

发布模式（IL2CPP）:
  1. foreach aotDll: RuntimeApi.LoadMetadataForAOTAssembly(bytes, SuperSet)
  2. Assembly.Load(hotfixBytes, pdbBytes)   // 解释执行
```

### 程序集注册对比

| 环境 | 注册程序集数量 | 说明 |
|------|-------------|------|
| 客户端 | 6 | Core + Loader + Model + ModelView + Hotfix + HotfixView |
| 服务端 | 4 | Core + Loader + Model + Hotfix（无 View 层） |

### 编辑器编译流程（F6 触发）

```
F6 按下
  └── AssemblyTool.DoCompile()
        ├── AssetDatabase.Refresh(ForceUpdate)
        ├── CodeModeChangeHelper.ChangeToCodeMode(globalConfig.CodeMode)
        ├── CompileDlls(): try/finally 保护同步上下文
        │     PlayerBuildInterface.CompilePlayerScripts(settings, "Temp/Bin/Debug")
        │     — 注入 IS_COMPILING 宏
        │     — 失败时返回 false，中断后续步骤
        └── CopyHotUpdateDlls() [仅编译成功时]
              ├── FileHelper.CleanDirectory(CodeDir)  [先清空]
              └── ET.Model/ET.ModelView/ET.Hotfix/ET.HotfixView
                    Temp/Bin/Debug/*.dll → Bundles/Code/*.dll.bytes
                    Temp/Bin/Debug/*.pdb → Bundles/Code/*.pdb.bytes
```

---

## 跨包交互分析（Round 3 深化）

### loader → core 的初始化链

```
Init.StartAsync()
  ├── World.Instance                    [cn.etetet.core - World 单例]
  ├── World.AddSingleton<Logger>        [cn.etetet.core - Logger]
  ├── World.AddSingleton<TimeInfo>      [cn.etetet.core - TimeInfo]
  ├── World.AddSingleton<FiberManager>  [cn.etetet.core - FiberManager]
  └── CodeLoader.Start()
        └── World.AddSingleton<CodeTypes, Assembly[]>  [cn.etetet.core - CodeTypes]
              └── new StaticMethod(modelAssembly, "ET.Entry", "Start").Run()
                    [反射调用热更新代码的 Entry.Start()]
```

**关键接口约定**：loader 通过反射调用 `ET.Entry.Start()`，这是 loader 与热更新代码之间的**唯一契约接口**。热更新代码必须在 `ET.Model` 程序集中存在 `ET.Entry` 类和 `Start()` 静态方法，否则 `StaticMethod.Run()` 会抛出异常。

### loader → yooassets 的资源加载链

```
Init.StartAsync()
  └── World.AddSingleton<ResourcesComponent>()   [cn.etetet.yooassets 或 yiuiyooassets]
        └── CreatePackageAsync("DefaultPackage", true)
              [初始化 YooAssets 默认包，包含 DLL bytes bundle]

CodeLoader.DownloadAsync()
  └── ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>(path)
        [加载整个 Code bundle，返回 Dict<string, TextAsset>]
```

**YooAssets bundle 策略**：所有热更新 DLL（Model/ModelView/Hotfix/HotfixView）被打包到同一个 bundle，一次性加载。AOT DLL 是另一个 bundle（`Bundles/AotDlls/`）。

### loader → hybridclr 的 AOT 注入链

```
CodeLoader.Start() [仅 !IsEditor && EnableIL2CPP]
  └── foreach aotDll in aotDlls.Values:
        RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet)
        [必须在 Assembly.Load(hotfix) 之前调用]
  └── Assembly.Load(modelBytes)
  └── Assembly.Load(hotfixBytes)  [依赖上面的 AOT 元数据]
```

**顺序约束**：AOT 元数据注入必须先于热更新代码加载，否则泛型方法会因缺少 AOT 支持而运行时崩溃（这是 HybridCLR 的底层约束）。

### CodeModeChangeHelper 对全局包的影响

```
ChangeToCodeMode("Client")
  ├── 遍历 Packages/cn.etetet.*/Scripts/*/Client/ → 创建 asmref
  ├── 遍历 Packages/cn.etetet.*/Scripts/*/Server/ → 删除 asmref
  ├── 遍历 Library/PackageCache/cn.etetet.*/Scripts/*/Client/ → 创建 asmref
  └── 遍历 Library/PackageCache/cn.etetet.*/Scripts/*/Server/ → 删除 asmref
```

**影响范围**：该操作修改项目中所有 `cn.etetet.*` 包（不仅是 loader），包括 core、yiuiframework、login 等。这是一个**跨包的全局状态变更**。

### GlobalComponent 与 YIUI 框架的交互

```
Scene 初始化时（由 cn.etetet.core 驱动）
  └── GlobalComponent.Awake()
        ├── GameObject.Find("/Global")       [场景层级结构约定]
        ├── GameObject.Find("/Global/Unit")  [单元容器]
        ├── GameObject.Find("/Global/UI")    [UI 容器 → YIUI 框架使用]
        └── Resources.Load<GlobalConfig>()  [重复加载，与 Init 冗余]
```

**设计问题（Round 3 发现）**：`GlobalConfig` 在 `Init.StartAsync()` 中已经加载过一次，`GlobalComponent.Awake()` 又重新通过 `Resources.Load` 加载。两次加载不共享实例（Unity Resources 系统对同一路径可能返回不同或相同实例，取决于缓存），这是轻微的冗余。

---

## 关键流程

### 游戏启动流程（客户端）

```
1. Unity 启动 Init.Start()
2. StartAsync() 异步执行（.NoContext()，无需 await）
3. AppDomain.UnhandledException 注册
4. 解析空命令行参数 → World.AddSingleton<Options>
   （失败时抛异常，正常情况不触发）
5. 加载 GlobalConfig.asset → Options.SceneName
6. 创建: Logger(UnityLogger), TimeInfo, FiberManager
7. ETTask.ExceptionHandler = Log.Error
8. await ResourcesComponent.CreatePackageAsync("DefaultPackage", true)
   [阻塞等待 YooAssets 初始化完成]
9. CodeLoader.Start().NoContext()  ← fire-and-forget
   a. DownloadAsync():
      - 非Editor: YooAssets 加载整个 Code bundle → Dictionary
      - IL2CPP: 额外加载 AotDlls bundle → LoadMetadataForAOTAssembly (顺序关键)
   b. 加载 ET.Model + ET.ModelView (Assembly.Load)
   c. LoadHotfix(): 加载 ET.Hotfix + ET.HotfixView
   d. World.AddSingleton<CodeTypes>(6个程序集) → 扫描所有 ISystem
   e. StaticMethod("ET.Entry.Start").Run() → 游戏逻辑正式启动
10. Unity 主循环 Update → TimeInfo.Update() + FiberManager.Update()
```

### 日志点击跳转流程

```
1. 开发者在 Console 双击错误日志
2. LogRedirection.OnOpenAsset() 触发
3. line <= 0? → 直接返回 false（让 Unity 默认处理）
4. 检测：双击文件是否为 Log.cs / UnityLogger.cs / YooLogger.cs？
   - 否 → 返回 false（默认 Unity 行为）
   - 是 → 进入重定向流程
5. GetStackTrace() 反射读取 ConsoleWindow.m_ActiveText
   （ConsoleWindow 未聚焦时返回 null → 潜在 NPE）
6. 优先解析 <a href="path" line="N"> 链接（UnityLogger.Error 生成）
7. 否则 Regex 解析 "(at path:N)" 格式，跳过日志文件帧
8. Path.IsPathFullyQualified 检查 → 必要时转绝对路径
9. InternalEditorUtility.OpenFileAtLineExternal(path, line)
```

### 项目初始化流程（首次使用）

```
ET/Loader/Init 菜单:
1. SceneNameSetHelper.Run()        — 设置初始场景名
2. LinkSlnHelper.Run()             — 链接 .sln 文件
3. ScriptsReferencesHelper.Run()   — 刷新 asmdef 引用
4. CodeModeChangeHelper("ClientServer") — 切换到 ClientServer 模式
5. InitScriptHelper.Run()          — 创建初始化脚本
6. DefineHelper.EnableDefineSymbols("INITED", true) — 标记已初始化
```

---

## 依赖关系

### 运行时依赖
- **`cn.etetet.core`** — World, FiberManager, TimeInfo, Entity, Singleton, ETTask, ILog, CodeTypes, StaticMethod
- **`cn.etetet.yooassets`** (cn.etetet.yiuiyooassets 或直接) — ResourcesComponent（加载 DLL bytes bundle）
- **`cn.etetet.hybridclr`** — HybridCLR.RuntimeApi（AOT 元数据注入，IL2CPP 专用）
- **CommandLineParser** — 解析命令行参数
- **NLog** — 服务端日志

### 编辑器依赖
- **`cn.etetet.yooassets`** — YooConfig（BuildEditor 中读取）
- **`cn.etetet.yiuilubangen`** — StartConfig 目录（ServerCommandLineEditor 中硬编码路径依赖）

### 被依赖
- 所有其他包的热更新代码（Model/Hotfix等）都是被 loader 加载的目标
- loader 是整个项目的**启动引导包**，其他包不依赖 loader
- `CodeModeChangeHelper` 的 asmref 管理影响所有 `cn.etetet.*` 包

---

## 设计要点与注意事项

1. **双模式加载**：Editor 直接读文件，发布版从 YooAssets 加载，通过 `Define.IsEditor` 切换。

2. **IL2CPP 补充模式**：HybridCLR 的 SuperSet 模式下，AOT 元数据必须先于 hotfix 代码加载，顺序错误会导致泛型运行时崩溃。

3. **服务端可卸载上下文**：服务端使用 `AssemblyLoadContext(isCollectible: true)` 实现真正的 DLL 卸载和热重载，而客户端受限于 Unity 无法卸载程序集。卸载后必须 `GC.Collect()` 才能真正释放内存。

4. **代码模式隔离**：通过 `asmref` 文件机制，同一份代码可以根据 CodeMode 决定编译到哪个程序集，优雅地实现 Client/Server/ClientServer 三种部署模式。`ChangeToCodeMode` 是全局操作，影响所有 ET 包。

5. **Assembly 冲突避免**：Editor 进入 PlayMode 前删除 Library 中的同名 DLL，确保动态加载的 bytes 版本生效。

6. **客户端命令行参数**：客户端 Init 从空字符串 `"".Split(" ")` 解析（相当于不解析），服务端才读取真实命令行。`Options.SceneName` 由 `GlobalConfig.asset` 手动填充。

7. **F6/F7 快捷键**：`IS_COMPILING` 宏在编译期注入，可用于条件编译（区分运行态与编译态）。编译失败时不会更新 Code 目录，保留旧版本。

8. **日志封装穿透**：LogRedirection + UnityLogger 的 `<a href>` 标签机制共同实现双击日志直跳真实出错代码行。`GetStackTrace()` 对未聚焦的 ConsoleWindow 返回 null，是潜在的 NullReferenceException 点。

9. **WebGL 限制**：WebGL 平台下 HttpClientHelper 回退到 UnityWebRequest，因浏览器沙箱不允许直接使用 System.Net.Http.HttpClient。

10. **Scene 约定**：`GlobalComponentSystem.Awake` 依赖 Scene 中存在 `/Global`、`/Global/Unit`、`/Global/UI` 三个 GameObject，这是硬编码的场景结构约定，不满足时直接 NullReferenceException。

11. **ET.Entry.Start() 契约**：热更新代码与 loader 之间唯一的接口约定。Model 程序集必须包含 `ET` 命名空间下的 `Entry` 类及静态 `Start()` 方法。

12. **Bundle 共享策略**：所有热更新 DLL 在同一个 YooAssets bundle 中，一次性下载全部，简单但无法按需更新单个程序集。
