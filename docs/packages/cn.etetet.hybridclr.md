# cn.etetet.hybridclr

## 概述

**版本**：7.8.1
**作者**：Code Philosophy
**描述**：HybridCLR 是 Unity 全平台原生 C# 热更新方案，零成本、高性能、低内存，支持 IL2CPP 后端下的动态加载和运行热更新 DLL。

HybridCLR 通过修改 Unity 的 il2cpp 后端，在原生 AOT 编译代码中嵌入 IL 解释器，使得热更新程序集可以在 IL2CPP 平台上直接运行，无需任何 AOT 预编译。

---

## 目录结构

```
cn.etetet.hybridclr/
├── Runtime/                        # 运行时 API（5个文件）
│   ├── RuntimeApi.cs               # 核心运行时接口（加载元数据、PreJIT 等）
│   ├── HomologousImageMode.cs      # 同源镜像模式枚举
│   ├── LoadImageErrorCode.cs       # 加载错误码枚举
│   ├── RuntimeOptionId.cs          # 运行时选项 ID 枚举
│   └── ReversePInvokeWrapperGenerationAttribute.cs  # 反向 P/Invoke 生成特性
├── Editor/                         # 编辑器工具
│   ├── HybridCLREditor.cs          # ET 框架集成入口（菜单命令）
│   ├── HashUtil.cs                 # 哈希工具
│   ├── Commands/                   # 编辑器命令
│   │   ├── PrebuildCommand.cs      # 打包前一键生成命令
│   │   ├── CompileDllCommand.cs    # 编译热更 DLL
│   │   ├── Il2CppDefGeneratorCommand.cs  # 生成 Il2CppDef
│   │   ├── LinkGeneratorCommand.cs       # 生成 link.xml
│   │   ├── MethodBridgeGeneratorCommand.cs  # 生成方法桥接代码
│   │   ├── AOTReferenceGeneratorCommand.cs  # 生成 AOT 泛型引用
│   │   └── StripAOTDllCommand.cs         # 裁剪 AOT DLL
│   ├── BuildProcessors/            # 构建处理器（自动化构建流程）
│   │   ├── CheckSettings.cs        # 构建前检查配置（callbackOrder=0）
│   │   ├── FilterHotFixAssemblies.cs
│   │   ├── CopyStrippedAOTAssemblies.cs
│   │   ├── PatchScriptingAssemblyList.cs
│   │   ├── ScriptingAssembliesJsonPatcher.cs
│   │   ├── MsvcStdextWorkaround.cs
│   │   └── AddLil2cppSourceCodeToXcodeproj*.cs  # iOS Xcode 集成（2019/2020-2021/2022/2023+版本）
│   ├── Installer/                  # 安装控制器
│   │   ├── InstallerController.cs  # 安装/更新 HybridCLR 到本地 il2cpp
│   │   ├── InstallerWindow.cs      # 安装器 UI 窗口
│   │   └── BashUtil.cs             # Shell 命令工具
│   ├── AOT/                        # AOT 泛型分析
│   │   ├── Analyzer.cs             # 泛型类型/方法分析器（迭代扫描）
│   │   ├── ConstraintContext.cs
│   │   ├── GenericReferenceWriter.cs
│   │   └── AOTAssemblyMetadataStripper.cs
│   ├── MethodBridge/               # 方法桥接代码生成
│   │   ├── Analyzer.cs
│   │   ├── CalliAnalyzer.cs
│   │   └── Generator.cs
│   ├── ABI/                        # ABI 计算（参数/返回值类型布局）
│   │   ├── ABIUtil.cs
│   │   ├── MethodDesc.cs
│   │   ├── ParamInfo.cs / ParamOrReturnType.cs
│   │   ├── PlatformABI.cs
│   │   ├── TypeCreator.cs / TypeInfo.cs
│   │   └── ValueTypeSizeAligmentCalculator.cs
│   ├── Meta/                       # 程序集元数据解析
│   │   ├── AssemblyCache.cs / AssemblyCacheBase.cs
│   │   ├── AssemblyReferenceDeepCollector.cs
│   │   ├── AssemblyResolverBase.cs / CombinedAssemblyResolver.cs
│   │   ├── GenericClass.cs / GenericMethod.cs / GenericArgumentContext.cs
│   │   ├── MetaUtil.cs
│   │   └── MethodReferenceAnalyzer.cs
│   ├── Link/                       # link.xml 生成
│   │   ├── Analyzer.cs
│   │   └── LinkXmlWriter.cs
│   ├── Il2CppDef/                  # IL2CPP 宏定义生成
│   │   └── Il2CppDefGenerator.cs
│   ├── HotUpdate/
│   │   └── MissingMetadataChecker.cs  # 缺失元数据检查
│   └── 3rds/                       # 第三方库
│       ├── 7zip/                   # LZMA 压缩（LzmaEncoder/Decoder）
│       ├── UnityFS/                # Unity AssetBundle 解析/修补
│       └── UnityHook/              # Unity 方法 Hook 机制（用于编辑器构建拦截）
├── Data~/                          # 非导入数据（~后缀不被 Unity 导入）
│   ├── ModifiedUnityAssemblies/    # 修改过的 IL2CPP DLL（Unity 2019）
│   ├── NetStandard/                # netstandard2.0/2.1 DLL
│   ├── Templates/                  # 代码生成模板（AssemblyManifest.cpp/MethodBridge.cpp/UnityVersion.h）
│   └── hybridclr_version.json      # 版本清单（各 Unity 版本对应的 hybridclr/il2cpp_plus 分支）
└── HybridCLR/
    └── AssemblyReferenceToLoader.asmref  # 程序集引用配置
```

---

## 核心类/接口

### Runtime 层

#### `RuntimeApi`（`HybridCLR` 命名空间）
`[Preserve]` 标记的静态类，防止 IL2CPP 代码裁剪。核心模式：**编辑器下有桩实现，IL2CPP 平台下使用 `[MethodImpl(MethodImplOptions.InternalCall)]` 调用原生方法**。

| 方法 | 说明 | Editor 行为 |
|------|------|------------|
| `LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode)` | 加载 AOT 程序集的补充元数据，解决泛型共享问题 | 返回 `OK`（无操作） |
| `PreJitMethod(MethodInfo method)` | 预 JIT 单个方法，避免首次调用耗时 | 返回 `false` |
| `PreJitClass(Type type)` | 预 JIT 一个类的所有方法 | 返回 `false` |
| `SetRuntimeOption(RuntimeOptionId, int)` | 设置运行时选项（栈大小、帧数等） | 存入 `Dictionary` |
| `GetRuntimeOption(RuntimeOptionId)` | 获取运行时选项 | 从 `Dictionary` 读取，默认 0 |
| `SetInterpreterThreadObjectStackSize(int)` | 封装 `SetRuntimeOption(InterpreterThreadObjectStackSize, ...)` | 同上 |
| `SetInterpreterThreadFrameStackSize(int)` | 封装 `SetRuntimeOption(InterpreterThreadFrameStackSize, ...)` | 同上 |
| `GetInterpreterThreadObjectStackSize()` | 封装 `GetRuntimeOption(...)` | 同上 |
| `GetInterpreterThreadFrameStackSize()` | 封装 `GetRuntimeOption(...)` | 同上 |

> **重要**：`LoadMetadataForAOTAssembly` 在编辑器下是空实现，Editor Play Mode 测试时不会真正使用 HybridCLR 解释器。

#### `HomologousImageMode` 枚举
```csharp
Consistent  // 严格一致模式：热更 DLL 与 AOT DLL 完全一致
SuperSet    // 超集模式：热更 DLL 是 AOT DLL 的超集（可新增类型）
```
实际使用中，**`SuperSet` 最为常用**，允许热更 DLL 在 AOT 版本基础上新增类型和方法。

#### `LoadImageErrorCode` 枚举
| 值 | 含义 |
|----|------|
| `OK` | 成功 |
| `BAD_IMAGE` | 无效的 DLL 文件 |
| `NOT_IMPLEMENT` | 未实现的功能 |
| `AOT_ASSEMBLY_NOT_FIND` | 找不到对应的 AOT 程序集 |
| `HOMOLOGOUS_ONLY_SUPPORT_AOT_ASSEMBLY` | 只支持 AOT 程序集的补充元数据 |
| `HOMOLOGOUS_ASSEMBLY_HAS_LOADED` | 重复加载 |
| `INVALID_HOMOLOGOUS_MODE` | 无效的同源模式 |
| `PDB_BAD_FILE` | 无效的 PDB 文件 |

#### `RuntimeOptionId` 枚举
| ID | 含义 |
|----|------|
| `InterpreterThreadObjectStackSize` (1) | 解释器线程对象栈最大数量（×8 = 字节数） |
| `InterpreterThreadFrameStackSize` (2) | 解释器线程帧栈数量 |
| `ThreadExceptionFlowSize` (3) | 异常流大小 |
| `MaxMethodBodyCacheSize` (4) | 方法体缓存最大大小 |
| `MaxMethodInlineDepth` (5) | 最大内联深度 |
| `MaxInlineableMethodBodySize` (6) | 可内联方法体最大大小 |

#### `ReversePInvokeWrapperGenerationAttribute`
```csharp
[AttributeUsage(AttributeTargets.Method)]
public class ReversePInvokeWrapperGenerationAttribute : Attribute
{
    public int ReserveWrapperCount { get; }
}
```
标记需要生成反向 P/Invoke 包装器的方法，用于从原生代码回调到热更新 C# 代码（如 delegate 传给原生插件）。

---

### Editor 层

#### `HybridCLREditor`（`ET` 命名空间）
ET 框架专用的编辑器入口，提供两个菜单命令：

**`CopyAotDll()`：**
```csharp
// 从 strippedAOTDllOutputRootDir/{activeBuildTarget}/ 读取
// 复制到 Packages/cn.etetet.loader/Bundles/AotDlls/
// 每个 DLL 以 "{aotDll}.bytes" 格式存储（便于 AssetBundle 加载）
foreach (string aotDll in HybridCLRSettings.Instance.patchAOTAssemblies)
{
    File.Copy(Path.Combine(fromDir, aotDll), Path.Combine(toDir, $"{aotDll}.bytes"), true);
}
```

**`Init()`：**
```csharp
// 从 Package 复制 AssemblyReferenceToLoader.asmref 到 Assets/HybridCLR/
// 该文件将热更新程序集关联到 cn.etetet.loader 的程序集定义
```

#### `PrebuildCommand`（`HybridCLR.Editor.Commands`）
打包前必须执行的完整生成流程（`HybridCLR/Generate/All`）：

```csharp
// 首先检查 HybridCLR 是否已安装
if (!installer.HasInstalledHybridCLR())
    throw new BuildFailedException("...");

// 按顺序执行：
CompileDllCommand.CompileDll(target, development);          // 1. 编译热更 DLL
Il2CppDefGeneratorCommand.GenerateIl2CppDef();              // 2. 生成 il2cpp 宏定义
LinkGeneratorCommand.GenerateLinkXml(target);               // 3. 生成 link.xml
StripAOTDllCommand.GenerateStripedAOTDlls(target);          // 4. 裁剪 AOT DLL（需要完整 Build）
MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper(target); // 5. 桥接代码（依赖 AOT DLL）
AOTReferenceGeneratorCommand.GenerateAOTGenericReference(target); // 6. AOT 泛型引用
```

#### `CheckSettings`（`IPreprocessBuildWithReport`，`callbackOrder = 0`）
构建前自动检查，静态属性 `DisableMethodBridgeDevelopmentFlagChecking` 允许测试时跳过开发标志检查：

1. 根据 `enable` 和 `useGlobalIl2cpp` 标志设置/清除 `UNITY_IL2CPP_PATH` 环境变量
2. 强制切换 Scripting Backend 为 IL2CPP（自动修复，记录错误日志）
3. 检查 HybridCLR 是否已安装
4. 检查 `PackageVersion == InstalledLibil2cppVersion`
5. 检查 `hotUpdateAssemblies` + `hotUpdateAssemblyDefinitions` 不为空（仅警告）
6. 解析 `MethodBridge.cpp` 中的 `// DEVELOPMENT=N` 注释，验证与当前构建模式一致

#### `InstallerController`（`HybridCLR.Editor.Installer`）
负责将 HybridCLR 安装到本地 Unity il2cpp 目录：

**关键内部类型：**
```csharp
class HybridclrVersionInfo {
    public string unity_version;  // e.g. "2022" 或 "2022-tuanjie"
    public VersionDesc hybridclr;  // { branch }
    public VersionDesc il2cpp_plus; // { branch }
}
```

**关键属性：**
- `PackageVersion`：从 `package.json` 读取的包版本
- `InstalledLibil2cppVersion`：从本地安装记录读取的已安装版本
- `MajorVersion`：当前 Unity 版本主号（支持 2019/2020/2021/2022/2023/6000）
- 支持 **团结引擎（Tuanjie Engine）** 通过 `isTuanjieEngine` 标志

#### `AOT/Analyzer`（`HybridCLR.Editor.AOT`）
AOT 泛型分析器，使用**迭代扩散**方式：

```csharp
public class Options {
    AssemblyReferenceDeepCollector Collector;  // 深度依赖收集器
    int MaxIterationCount;                     // 最大迭代轮数
    bool ComputeAotAssembly;                   // 是否分析 AOT 程序集
}

// 分析结果
IReadOnlyCollection<GenericClass> GenericTypes;   // 所有泛型类型实例
IReadOnlyCollection<GenericMethod> GenericMethods; // 所有泛型方法实例
List<GenericClass> AotGenericTypes;               // 仅 AOT 泛型类型
List<GenericMethod> AotGenericMethods;            // 仅 AOT 泛型方法
ConstraintContext ConstraintContext;              // 约束上下文
```

**分析过程**：
1. 通过 `MethodReferenceAnalyzer` 扫描所有方法体中的方法调用
2. 发现新泛型方法时加入队列 (`_newMethods`)
3. 迭代直到无新发现或达到 `MaxIterationCount`
4. 区分热更新程序集（通过 `_hotUpdateAssemblyFiles`）和 AOT 程序集

---

## 实现原理

### HybridCLR 热更新机制

```
┌─────────────────────────────────────────────────────────┐
│                    Unity IL2CPP Build                    │
│  ┌─────────────────────┐    ┌──────────────────────────┐ │
│  │   AOT 代码（原生）    │    │  HybridCLR IL 解释器     │ │
│  │  MethodBridge.cpp   │◄───│  (嵌入 libil2cpp 中)     │ │
│  │  AOT Assemblies     │    └──────────────────────────┘ │
│  └─────────────────────┘                ▲                │
│                                         │                │
│  运行时：                                │ 加载元数据      │
│  RuntimeApi.LoadMetadataForAOTAssembly()│                │
│                                         │                │
│  ┌──────────────────────────────────────┴─────────────┐ │
│  │            热更新 DLL（从资源服务器下载）              │ │
│  │  HotUpdate Assembly 1                               │ │
│  │  HotUpdate Assembly 2 ...                           │ │
│  └─────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 泛型共享补充元数据

IL2CPP 使用泛型共享（Generic Sharing）机制，某些泛型实例化在 AOT 代码中可能没有具体实现。HybridCLR 通过**加载裁剪后的 AOT DLL** 作为补充元数据，使解释器能够正确执行这些泛型方法：

```csharp
// 运行时加载补充元数据（在热更新代码执行前调用）
byte[] dllBytes = await LoadFromAssetBundle("mscorlib.dll.bytes");
var errorCode = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
if (errorCode != LoadImageErrorCode.OK)
{
    Debug.LogError($"加载补充元数据失败: {errorCode}");
}
```

### 方法桥接（Method Bridge）

热更新代码调用 AOT 代码，或 AOT 代码回调热更新代码时，需要通过**方法桥接**。工具链在构建前分析所有可能的调用并生成 `MethodBridge.cpp` 和 `ReversePInvokeWrapper.cpp`。

`MethodBridge.cpp` 文件中包含 `// DEVELOPMENT=1` 或 `// DEVELOPMENT=0` 注释，`CheckSettings` 在构建时会验证此标志与当前构建模式一致。

### UnityHook 机制（Editor）

`3rds/UnityHook` 实现了一个**方法注入**机制，在运行时修改 Unity 编辑器内部方法的行为：
- `MethodHook`：保存原始方法字节，注入跳转指令
- `HookPool`：管理所有 Hook 的生命周期
- `HybridCLRHooks/`：具体的 Hook 实现（`CopyStrippedAOTAssembliesHook`、`PatchScriptingAssembliesJsonHook`、`GetIl2CppFolderHook`）

---

## 关键流程

### 1. 安装流程
```
开发者操作: HybridCLR/Installer
→ InstallerController.InstallDefaultHybridCLR()
→ 从 GitHub 克隆 hybridclr + il2cpp_plus 源码（按 hybridclr_version.json 中对应版本的 branch）
→ 合并 hybridclr 到 il2cpp_plus/libil2cpp/hybridclr
→ 替换 Unity 编辑器 libil2cpp 目录（或设置 UNITY_IL2CPP_PATH 环境变量）
→ 记录已安装版本到 LocalVersionFile
```

### 2. 打包前生成流程（严格顺序）
```
HybridCLR/Generate/All (PrebuildCommand.GenerateAll)
    │
    ├── 1. CompileDll             → 编译热更新 DLL 到输出目录
    ├── 2. GenerateIl2CppDef      → 生成 il2cpp_plus 所需宏定义
    ├── 3. GenerateLinkXml        → 生成 link.xml 防止代码被裁剪
    ├── 4. GenerateStripedAOTDlls → 触发构建获取裁剪后的 AOT DLL（最耗时）
    ├── 5. GenerateMethodBridge   → 生成 MethodBridge.cpp + ReversePInvokeWrapper.cpp
    │                               （必须在步骤4后，因为依赖 AOT DLL）
    └── 6. GenerateAOTGenericReference → 生成 AOTGenericReferences.cs（元数据加载代码）
```

### 3. 运行时热更新加载流程
```csharp
// 步骤1: 下载/加载 AOT DLL 字节数据
// 步骤2: 加载补充元数据（每个需要补充的 AOT DLL 都要加载）
foreach (var dllName in patchAOTAssemblies)
{
    byte[] dllBytes = await LoadBytes($"{dllName}.bytes");
    RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
}
// 步骤3: 加载热更新程序集
Assembly hotUpdateAssembly = Assembly.Load(hotUpdateDllBytes);
// 步骤4: 反射获取入口类型并调用启动逻辑
Type entryType = hotUpdateAssembly.GetType("HotUpdateEntry");
entryType.GetMethod("Start").Invoke(null, null);
```

### 4. ET 框架集成
```
ET/HybridCLR/Init → 创建 Assets/HybridCLR/AssemblyReferenceToLoader.asmref
                   → 将热更新程序集关联到 Loader 程序集定义

ET/HybridCLR/CopyAotDlls → 读取 HybridCLRSettings.patchAOTAssemblies 列表
                          → 从 strippedAOTDllOutputRootDir/{target}/ 复制
                          → 目标: Packages/cn.etetet.loader/Bundles/AotDlls/{dllName}.bytes
```

---

## 依赖关系

### 对外依赖
- **Unity IL2CPP**：必须使用 IL2CPP 构建后端
- **dnlib**：Editor 程序集元数据解析（`Editor/Meta/` 等大量使用，通过 Mono.Cecil 风格的 API）
- **`cn.etetet.loader`**：AOT DLL 被复制到 loader 包，由 loader 加载

### 被依赖
- **`cn.etetet.loader`**：加载 AOT DLL 后调用 `RuntimeApi.LoadMetadataForAOTAssembly()`
- 所有热更新程序集：通过 `AssemblyReferenceToLoader.asmref` 关联

---

## 依赖关系图

```
cn.etetet.hybridclr
    │
    ├──(Editor/安装)──→ Unity 编辑器 libil2cpp（修改替换）
    ├──(Editor/安装)──→ GitHub Repos（hybridclr + il2cpp_plus，按版本 branch）
    ├──(Editor/CopyAotDlls)──→ cn.etetet.loader/Bundles/AotDlls/
    │
    └──(Runtime API 被使用)
           └──→ cn.etetet.loader（调用 LoadMetadataForAOTAssembly）
```

---

## 关键注意事项

1. **必须在打包前运行 `HybridCLR/Generate/All`**：生成的 `MethodBridge.cpp` 等文件与构建目标、开发/发布模式绑定。`CheckSettings` 会在构建时自动验证这一点。

2. **版本一致性**：`PackageVersion` 必须与 `InstalledLibil2cppVersion` 保持一致，升级 Package 后必须重新安装。

3. **AOT DLL 补充元数据**：`patchAOTAssemblies` 列表中的每个程序集都需要在运行时调用 `LoadMetadataForAOTAssembly`，**必须在热更新代码执行前完成**，否则泛型方法会找不到元数据而崩溃。

4. **Editor 下行为**：`RuntimeApi` 的所有 `InternalCall` 方法在 Editor 下有空实现（桩），不会真正执行热更逻辑。Editor Play Mode 测试的是 AOT 版本的代码。

5. **平台支持**：支持 iOS、Android、Windows、MacOS、Linux、WebGL 等所有 Unity 支持的 IL2CPP 平台，以及团结引擎（Tuanjie Engine）。

6. **`useGlobalIl2cpp` 选项**：当 `HybridCLRSettings.useGlobalIl2cpp = true` 时，使用全局 il2cpp 而非本地安装的 HybridCLR il2cpp，此时 `CheckSettings` 会清除 `UNITY_IL2CPP_PATH`。

7. **UnityFS 模块的作用**：`Editor/3rds/UnityFS/` 负责解析和修补 Unity AssetBundle 文件（UnityFS 格式），主要用于修补 `scripting_assemblies.json` 文件，确保热更新程序集被正确注册。

---

*（Round 2 - 补充了 RuntimeApi 编辑器桩实现细节、CheckSettings 的 DisableMethodBridgeDevelopmentFlagChecking 标志、InstallerController 内部数据结构、AOT/Analyzer 的迭代扩散机制、UnityHook 机制说明、代码示例）*
