# cn.etetet.yooassets

**版本**: 2.3.18
**显示名**: ET.YooAssets
**作者**: TuYoo Games
**来源**: https://github.com/tuyoogame/YooAsset
**Unity最低版本**: 2022.3
**ET包类型编号**: PackageType.YooAssets = 7

---

## 概述

YooAssets 是 Unity 资源管理系统（Asset Bundle 热更新框架），由 TuYoo Games 开发。在 ET 框架中作为客户端资源加载与热更新的核心底层，由 `cn.etetet.yiuiyooassets` 上层封装调用。

本包提供：
- **多种运行模式**：编辑器模拟 / 离线 / 联机热更新 / WebGL / 自定义
- **完整的 AssetBundle 构建管线**：支持 BuiltinBuildPipeline、ScriptableBuildPipeline、RawFileBuildPipeline
- **文件系统抽象**：内置文件系统、缓存文件系统、Web 文件系统等
- **下载系统**：支持断点续传、并发控制
- **资源包管理**：多包并行、独立生命周期
- **Editor 工具链**：资源扫描器、艺术资源报告器、AssetBundle 构建窗口

---

## 目录结构

```
cn.etetet.yooassets/
├── Runtime/                    # 运行时核心
│   ├── YooAssets.cs            # 静态入口类
│   ├── YooAssetsDriver.cs      # MonoBehaviour 驱动
│   ├── YooAssetsExtension.cs   # 扩展方法（DefaultPackage 快捷方式）
│   ├── InitializeParameters.cs # 初始化参数定义（EPlayMode + 各模式参数类）
│   ├── ResourcePackage/        # 资源包裹（核心管理单元）
│   │   ├── ResourcePackage.cs      # 资源包裹主类
│   │   ├── PackageManifest.cs      # 包清单（资源列表/Bundle映射）
│   │   ├── PackageAsset.cs         # 单个资源条目
│   │   ├── PackageBundle.cs        # 单个Bundle条目
│   │   ├── AssetInfo.cs            # 资源地址信息
│   │   ├── BundleInfo.cs           # Bundle信息
│   │   ├── PlayMode/               # 运行模式实现
│   │   │   ├── PlayModeImpl.cs         # 运行模式统一实现
│   │   │   └── EditorSimulateModeHelper.cs
│   │   └── Operation/              # 异步操作（初始化/更新/下载/销毁）
│   ├── ResourceManager/        # 资源加载管理器
│   │   ├── ResourceManager.cs
│   │   ├── Handle/             # 句柄（AssetHandle/SceneHandle/等）
│   │   ├── Provider/           # 资源提供者
│   │   └── Operation/          # 加载异步操作
│   ├── FileSystem/             # 文件系统抽象层
│   │   ├── DefaultBuildinFileSystem/   # 内置（StreamingAssets）
│   │   ├── DefaultCacheFileSystem/     # 缓存（本地热更文件）
│   │   ├── DefaultEditorFileSystem/    # 编辑器模拟
│   │   ├── DefaultWebServerFileSystem/ # WebGL服务器端
│   │   ├── DefaultWebRemoteFileSystem/ # WebGL远端
│   │   └── DefaultUnpackFileSystem/    # 解包文件系统
│   ├── DownloadSystem/         # 下载系统
│   ├── OperationSystem/        # 异步操作调度系统
│   ├── DiagnosticSystem/       # 调试诊断系统
│   └── Services/               # 服务接口定义
│       ├── IRemoteServices.cs
│       ├── IDecryptionServices.cs
│       ├── IEncryptionServices.cs
│       ├── IManifestProcessServices.cs
│       ├── IManifestRestoreServices.cs
│       ├── ICopyLocalFileServices.cs
│       └── IWebDecryptionServices.cs
├── Scripts/                    # ET框架集成层
│   ├── Model/Share/
│   │   ├── PackageType.cs          # PackageType.YooAssets = 7
│   │   └── CoroutineLockType.cs    # CoroutineLockType.ResourcesLoader
│   ├── ModelView/Client/
│   │   └── ResourcesLoaderComponent.cs  # ET组件：资源加载器
│   └── HotfixView/Client/
│       └── ResourcesLoaderComponentSystem.cs  # ET系统：异步加载实现
└── Editor/                     # 编辑器工具
    ├── AssetBundleBuilder/     # 构建工具（四种Pipeline）
    ├── AssetArtScanner/        # 艺术资源规范扫描
    └── AssetArtReporter/       # 扫描报告生成器
```

---

## 核心类/接口列表

### 运行时入口

| 类/接口 | 类型 | 说明 |
|---------|------|------|
| `YooAssets` | 静态类 | 全局入口，管理所有 ResourcePackage |
| `YooAssetsDriver` | MonoBehaviour | 每帧驱动 OperationSystem.Update() |
| `ResourcePackage` | 类 | 资源包裹，独立生命周期的资源管理单元 |

### 初始化参数

| 类 | 对应运行模式 |
|----|------------|
| `EditorSimulateModeParameters` | 编辑器模拟模式 |
| `OfflinePlayModeParameters` | 离线模式（无热更） |
| `HostPlayModeParameters` | 联机热更模式（主流用法） |
| `WebPlayModeParameters` | WebGL模式 |
| `CustomPlayModeParameters` | 自定义多文件系统模式 |

### 异步操作（Operation）

| 类 | 说明 |
|----|------|
| `InitializationOperation` | 包裹初始化 |
| `RequestPackageVersionOperation` | 获取远端包版本号 |
| `UpdatePackageManifestOperation` | 更新包清单 |
| `PreDownloadContentOperation` | 预下载内容 |
| `DownloaderOperation` | 文件下载器 |
| `ClearCacheFilesOperation` | 清理缓存 |
| `DestroyOperation` | 销毁包裹 |

### 资源句柄（Handle）

| 类 | 说明 |
|----|------|
| `HandleBase` | 句柄基类，包含 Task/Status/Progress |
| `AssetHandle` | 单个资源加载句柄 |
| `AllAssetsHandle` | 目录下所有资源批量加载 |
| `SubAssetsHandle` | 子资源加载 |
| `RawFileHandle` | 原始文件加载 |
| `SceneHandle` | 场景加载句柄 |

### 服务接口（可自定义实现）

| 接口 | 说明 |
|------|------|
| `IRemoteServices` | 提供主/备用 CDN 地址 |
| `IDecryptionServices` | 资源包解密（同步/异步/后备） |
| `IEncryptionServices` | 资源包加密（构建时） |
| `IManifestProcessServices` | 清单处理自定义 |
| `IManifestRestoreServices` | 清单还原自定义 |
| `ICopyLocalFileServices` | 本地文件拷贝自定义 |
| `IWebDecryptionServices` | WebGL解密 |

### ET 集成组件

| 类 | 类型 | 说明 |
|----|------|------|
| `ResourcesLoaderComponent` | Entity组件 | 持有 ResourcePackage 引用和 Handler 字典，生命周期跟随父Entity |
| `ResourcesLoaderComponentSystem` | EntitySystem | 实现 Awake/Destroy 及异步加载方法 |

---

## 实现原理

### 1. 运行模式（EPlayMode）

```
EPlayMode
├── EditorSimulateMode  → 编辑器内模拟，不打Bundle直接读资产
├── OfflinePlayMode     → 离线，资源打包在 StreamingAssets 中
├── HostPlayMode        → 联机，StreamingAssets + 网络热更缓存
├── WebPlayMode         → WebGL平台（仅限WebGL）
└── CustomPlayMode      → 自定义文件系统列表
```

每种模式对应不同的 `FileSystemParameters` 组合，由 `PlayModeImpl` 统一处理。

### 2. 文件系统分层

```
IPlayMode (文件系统层)
├── DefaultBuildinFileSystem    → 读 StreamingAssets/
├── DefaultCacheFileSystem      → 读写持久化缓存目录
├── DefaultEditorFileSystem     → 读取 AssetDatabase
├── DefaultWebServerFileSystem  → WebGL 读取服务端文件
└── DefaultWebRemoteFileSystem  → WebGL 读取远端CDN
```

### 3. 资源加载流程

```
ResourcesLoaderComponent.LoadAssetAsync<T>(location)
  └── CoroutineLock（防并发重复加载）
      └── ResourcePackage.LoadAssetAsync<T>(location)
          └── ResourceManager → Provider（BundledAssetProvider/RawFileProvider）
              └── 文件系统读取 → AssetBundle.LoadAssetAsync
                  └── AssetHandle.AssetObject → 返回 T
```

### 4. 热更新流程（HostPlayMode）

```
1. InitializeAsync(HostPlayModeParameters)
2. RequestPackageVersionOperation    → 获取服务端版本号
3. UpdatePackageManifestOperation    → 下载/更新资源清单
4. PreDownloadContentOperation       → 计算需要下载的Bundle
5. DownloaderOperation               → 下载Bundle到缓存目录
6. 正常加载资源（缓存命中则直接读取）
```

### 5. OperationSystem（异步调度）

- 每帧 `Update()` 驱动所有 `AsyncOperationBase` 状态机推进
- `MaxTimeSlice`（默认无限制）控制每帧最大执行时间
- 支持按 PackageName 隔离，销毁包时只清理该包的 Operation

### 6. ET 资源生命周期管理

```
父Entity（如 Scene）
└── ResourcesLoaderComponent
    ├── package: ResourcePackage（通常是 "DefaultPackage"）
    └── handlers: Dictionary<string, HandleBase>
        ├── "Assets/Foo.prefab" → AssetHandle
        └── "Assets/Scenes/Main" → SceneHandle

当父Entity销毁时：
→ ResourcesLoaderComponentSystem.Destroy()
→ 遍历所有 handler，调用 Release() 或 UnloadAsync()
→ 资源引用计数归零 → 可被 YooAssets 卸载
```

---

## 关键流程图

### 资源热更新（联机模式）

```
游戏启动
    │
    ▼
YooAssets.Initialize()
    │
    ▼
ResourcePackage.InitializeAsync(HostPlayModeParameters)
    │ BuildinFileSystem + CacheFileSystem
    ▼
RequestPackageVersionOperation
    │ 请求: IRemoteServices.GetRemoteMainURL("PackageVersion.xxx")
    ▼
UpdatePackageManifestOperation
    │ 下载并解析 PackageManifest
    ▼
比对本地缓存 vs 远端清单
    │
    ├── 无需更新 → 直接进入游戏
    │
    └── 需要更新
            │
            ▼
       DownloaderOperation（并发下载，支持断点续传）
            │
            ▼
       完成热更 → 进入游戏
```

---

## 依赖关系

### 外部依赖（Unity官方包）

| 包 | 版本 | 用途 |
|----|------|------|
| `com.unity.scriptablebuildpipeline` | 1.21.25 | SBP构建Pipeline |
| `com.unity.modules.assetbundle` | 1.0.0 | AssetBundle核心 |
| `com.unity.modules.unitywebrequest` | 1.0.0 | 网络请求 |
| `com.unity.modules.unitywebrequestassetbundle` | 1.0.0 | 网络下载AssetBundle |

### 被哪些ET包使用

| 包 | 使用方式 |
|----|---------|
| `cn.etetet.yiuiyooassets` | 上层封装，对 YooAssets API 的 ET 风格包装 |
| `cn.etetet.loader` | 可能通过 ResourcesLoaderComponent 加载资源 |
| 各 UI/功能包 | 通过 ResourcesLoaderComponent.LoadAssetAsync 加载 Prefab/图集 |

### ET 框架集成

- `PackageType.YooAssets = 7`：在 ET 包管理体系中的编号
- `CoroutineLockType.ResourcesLoader`：用于防止同一资源并发重复加载
- `ResourcesLoaderComponent` 跟随实体生命周期自动释放资源，是 ET + YooAssets 集成的关键桥梁

---

## 枚举说明

| 枚举 | 值 | 说明 |
|------|----|------|
| `EPlayMode` | EditorSimulateMode/OfflinePlayMode/HostPlayMode/WebPlayMode/CustomPlayMode | 运行模式 |
| `EOperationStatus` | None/Processing/Succeed/Failed | 操作状态 |
| `EFileClearMode` | - | 缓存清理策略 |
| `EFileVerifyLevel` | - | 文件校验级别 |
| `EBuildBundleType` | - | Bundle构建类型 |
| `EFileNameStyle` | - | 文件命名风格 |

---

## 性能考量与最佳实践

1. **并发加载控制**：`BundleLoadingMaxConcurrency` 默认无限制，移动端建议设置为 4~8
2. **自动卸载**：`AutoUnloadBundleWhenUnused = true` 时引用计数为0自动卸载，谨慎使用（频繁加载卸载有开销）
3. **弱引用句柄**：`UseWeakReferenceHandle`（实验性）可减少内存泄漏风险
4. **CoroutineLock 防重**：ET 的 `ResourcesLoaderComponentSystem` 用 CoroutineLock 防止同一 location 被重复加载，Handler 字典作二级缓存
5. **生命周期绑定**：始终将 `ResourcesLoaderComponent` 挂载到与使用场景同生命周期的 Entity 上，避免资源泄漏
6. **DefaultPackage**：默认包名 "DefaultPackage"，无包名参数时 `ResourcesLoaderComponent.Awake()` 自动绑定此包

---

## Round 2 补充：代码示例与细节修正

### CoroutineLockType 精确值

```csharp
// Scripts/Model/Share/CoroutineLockType.cs
namespace ET
{
    public static partial class CoroutineLockType
    {
        // PackageType.YooAssets = 7
        public const int Resources = 7 * 1000 + 1;       // = 7001（保留，未在ET集成层使用）
        public const int ResourcesLoader = 7 * 1000 + 2; // = 7002（LoadAssetAsync 防并发锁）
    }
}
```

> 第一轮文档写的"CoroutineLockType.ResourcesLoader"是正确的，但数值是 7002（基于 PackageType × 1000 + 序号）。

---

### ResourcesLoaderComponentSystem 完整方法签名

```csharp
// 加载单个资源（带 CoroutineLock 防重 + Handler 缓存）
public static async ETTask<T> LoadAssetAsync<T>(this ResourcesLoaderComponent self, string location)
    where T : UnityEngine.Object

// 加载目录下所有资源，返回 name→T 字典
public static async ETTask<Dictionary<string, T>> LoadAllAssetsAsync<T>(this ResourcesLoaderComponent self, string location)
    where T : UnityEngine.Object

// 加载场景（同一场景重复调用直接return，不重复加载）
public static async ETTask LoadSceneAsync(this ResourcesLoaderComponent self, string location, LoadSceneMode loadSceneMode)
```

**注意**：`LoadAllAssetsAsync` 返回的字典以资源 `.name` 为键（而非 location 路径）。

---

### ET 资源加载代码示例

```csharp
// 获取或创建 ResourcesLoaderComponent（绑定到当前场景 Entity）
ResourcesLoaderComponent loader = scene.GetOrAddComponent<ResourcesLoaderComponent>();

// 加载 Prefab
GameObject prefab = await loader.LoadAssetAsync<GameObject>("Assets/Res/UI/LoginPanel.prefab");

// 加载图集下所有 Sprite（字典形式）
Dictionary<string, Sprite> sprites = await loader.LoadAllAssetsAsync<Sprite>("Assets/Res/Atlas/Common");

// 加载场景（Additive）
await loader.LoadSceneAsync("Assets/Res/Scenes/Map01.unity", LoadSceneMode.Additive);

// 资源随 scene Entity 销毁时自动 Release（无需手动调用）
scene.Dispose();
```

---

### 构建管线（Editor）架构

四种构建管线（`EBuildPipeline`）均采用 **Task 链** 模式，由 `BuildRunner.Run(List<IBuildTask>, BuildContext)` 顺序执行：

```
EBuildPipeline
├── EditorSimulateBuildPipeline (ESBP) — 编辑器模拟，不生成实际 Bundle
│   └── TaskPrepare → TaskGetBuildMap → TaskUpdateBundleInfo → TaskCreateManifest
│
├── BuiltinBuildPipeline (BBP) — 传统 Unity AssetBundle 构建
│   └── TaskPrepare → TaskGetBuildMap → TaskUpdateBundleInfo → TaskBuilding
│       → TaskVerifyBuildResult → TaskEncryption → TaskCreateManifest
│       → TaskCreateCatalog → TaskCreateReport → TaskCopyBuildinFiles
│
├── ScriptableBuildPipeline (SBP) — SBP（需 com.unity.scriptablebuildpipeline）
│   └── 同 BBP 结构，Task 后缀 _SBP
│
└── RawFileBuildPipeline (RFBP) — 原始文件打包（非 AssetBundle）
    └── TaskPrepare → TaskGetBuildMap → TaskUpdateBundleInfo → TaskBuilding(直接拷贝)
        → TaskEncryption → TaskCreateManifest → TaskCreateCatalog
        → TaskCreateReport → TaskCopyBuildinFiles
```

**IBuildTask 接口**：
```csharp
public interface IBuildTask
{
    void Run(BuildContext context);  // 同步执行，异常即终止整个 Pipeline
}
```

**BuildRunner 关键逻辑**：
- 遍历 Task 列表，每个 Task 用 `Stopwatch` 计时
- 任意 Task 抛出异常 → `BuildResult.Success = false`，记录 `FailedTask`、`ErrorInfo`、`ErrorStack`
- `BuildContext` 作为 Task 间共享数据容器（类似 Context 模式）

---

### BuildParameters 关键字段（第一轮遗漏）

| 字段 | 类型 | 说明 |
|------|------|------|
| `BuildOutputRoot` | string | 构建输出根目录 |
| `BuildinFileRoot` | string | 内置文件根目录（StreamingAssets路径） |
| `PackageName` | string | 资源包名称（如 "DefaultPackage"） |
| `PackageVersion` | string | 版本号（决定输出子目录名） |
| `ClearBuildCacheFiles` | bool | 是否清空构建缓存（默认false） |
| `UseAssetDependencyDB` | bool | 依赖缓存DB，开启后大幅提升收集速度 |
| `EnableSharePackRule` | bool | 共享资源打包规则（防冗余） |
| `SingleReferencedPackAlone` | bool | 单独引用的共享资源是否独立打包（默认true） |
| `VerifyBuildingResult` | bool | 构建后验证结果完整性 |
| `FileNameStyle` | EFileNameStyle | Bundle命名（默认 HashName） |
| `BuildinFileCopyOption` | EBuildinFileCopyOption | 内置文件拷贝策略 |
| `EncryptionServices` | IEncryptionServices | Bundle加密实现 |
| `ManifestProcessServices` | IManifestProcessServices | 清单处理自定义 |

**输出目录结构**（由 `BuildParameters` 方法生成）：
```
{BuildOutputRoot}/{BuildTarget}/{PackageName}/
├── {OutputFolderName}/    ← GetPipelineOutputDirectory()
└── {PackageVersion}/      ← GetPackageOutputDirectory()（热更包）
```

---

## Round 3 深化：跨包交互、边界情况与异常处理

### 一、跨包调用链全景

#### 1. cn.etetet.loader → cn.etetet.yooassets（代码热更加载）

`loader` 包的 `CodeLoader` 通过 `ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>()` 使用 YooAssets 加载 DLL 字节：

```
CodeLoader.Start()
  └── DownloadAsync()
        └── ResourcesComponent.LoadAllAssetsAsync<TextAsset>("Packages/cn.etetet.loader/Bundles/Code/ET.Model.dll.bytes")
              └── YooAssets（DefaultPackage）
                    ├── 非编辑器：从 AssetBundle 中读取 TextAsset（DLL 字节）
                    └── 编辑器：File.ReadAllBytes() 直接读取磁盘
  └── Assembly.Load(bytes) → 热更 DLL 注入运行时
```

**关键意义**：YooAssets 不仅加载游戏资产，还是整个热更代码（ET.Model.dll / ET.Hotfix.dll）的传输载体。如果 YooAssets 未正确初始化，整个代码热更新流程将失败。

#### 2. cn.etetet.yiuiyooassets → cn.etetet.yooassets（YIUI 资源桥接）

两条调用路径并存，行为有所不同：

**路径 A：YIUIYooAssetsLoadComponent（通用资源加载）**
```
YIUILoadDI.LoadAssetAsyncFunc → YIUIYooAssetsLoadComponent.LoadAssetAsyncFunc
  └── self.m_Package.LoadAssetAsync(location, type)   ← 使用绑定的 ResourcePackage
        └── handle.Task（await）→ LoadAssetHandle()
              ├── 成功：m_AllHandle[hashCode] = handle，返回 (AssetObject, hashCode)
              └── 失败（AssetObject == null）：handle.Release()，返回 (null, 0)
```

**路径 B：YIUIYooAssetsSpriteComponent（图集加载）**
```
YIUIYooAssetsSpriteComponentSystem.LoadAtlasAsync()
  └── YooAssets.LoadAssetAsync<YIUIAtlasData>(YIUIConstAsset.AtlasDataName)
        ↑ 注意：直接调用静态入口（YooAssets.xxx），固定使用 DefaultPackage
        └── atlasData.Infos → 构建 spriteName → atlasPath 映射表
              └── handle.Release()（图集数据加载后立即释放句柄）
```

**静态 API vs 包实例 API 的差异**：

| 调用方式 | 使用包 | 适用场景 |
|---------|--------|---------|
| `package.LoadAssetAsync()` | 指定 ResourcePackage | 支持多包场景，由调用者决定 |
| `YooAssets.LoadAssetAsync()` | DefaultPackage（固定） | 单包游戏，或只从默认包加载 |

`YIUIAtlasData` 始终从 DefaultPackage 加载，即 Atlas 索引文件只能存在于默认包中。

#### 3. YIUIInvoke 连接模式（跨框架调用）

```
YIUI框架 → ET Invoke系统 → YooAssets
    │
    ├── YIUIInvokeEntity_LoadInitialize
    │     └── YIUIInvokeYooAssetsHandler.Handle()
    │           └── entity.AddComponent<YIUIYooAssetsLoadComponent>().Initialize()
    │                 └── YIUILoadDI.LoadAssetAsyncFunc = ...（注入加载委托）
    │                       └── EventSystem.PublishAsync(YIUIEvent_YooAssetsLoad_Completed)
    │
    ├── YIUIInvokeEntity_GetAssetInfo [Sync]
    │     └── YIUIInvokeGetAssetsInfoSyncHandler
    │           └── package.GetAssetInfo(location, type)
    │
    └── YIUIInvokeEntity_GetAssetInfoByGUID [Sync]
          └── YIUIInvokeGetAssetInfoByGUIDSyncHandler
                └── package.GetAssetInfoByGUID(assetGUID, type)
```

---

### 二、边界情况与异常处理

#### 2.1 资源加载失败路径

**ResourcesLoaderComponent.LoadAssetAsync**（ET 基础层）：
```csharp
// 无失败检测！直接强转 AssetObject
return (T)((AssetHandle)handler).AssetObject;
```
- 若 `AssetObject == null`（资源不存在/加载失败），会返回 `null` 而不抛出异常
- 但若 handler 非 AssetHandle 类型（如 AllAssetsHandle），强转会抛 `InvalidCastException`
- **风险**：调用方需自行判断返回值是否为 null

**YIUIYooAssetsLoadComponent.LoadAssetHandle**（YIUI 层）：
```csharp
if (handle.AssetObject != null)
{
    // 注册句柄，返回资源
}
else
{
    handle.Release();  // 立即释放，防止句柄泄漏
    return (null, 0);  // hashCode 0 为哨兵值，表示加载失败
}
```
- YIUI 层有更好的防御性：失败时会释放句柄，避免泄漏
- 调用方通过检查 hashCode == 0 或 Object == null 判断是否成功

#### 2.2 包不存在的边界情况

```csharp
// YIUIYooAssetsLoadComponent.Initialize
self.m_Package = YooAssets.GetPackage(packageName);
if (self.m_Package == null)
{
    Log.Error($"YooAsset 加载资源包失败: {packageName}");
    return false;  // 返回 false 而非抛出异常
}
```

对比 `ResourcesLoaderComponent.Awake`：
```csharp
self.package = YooAssets.GetPackage("DefaultPackage");
// 无空检查！若 DefaultPackage 未初始化，package 为 null
// 后续 LoadAssetAsync 调用 package.LoadAssetAsync 会 NullReferenceException
```

**隐患**：若在 YooAssets 初始化完成前创建 `ResourcesLoaderComponent`，会产生运行时异常。

#### 2.3 异步操作中的 Entity 失效（EntityRef 模式）

```csharp
// YIUIYooAssetsLoadComponent.LoadAssetAsyncFunc
EntityRef<YIUIYooAssetsLoadComponent> selfRef = self;  // 保存弱引用
var handle = self.m_Package.LoadAssetAsync(arg2, arg3);
await handle.Task;                                       // 此处 self 可能已被销毁
self = selfRef;                                          // 重新获取（若已销毁则为 null）
return self.LoadAssetHandle(handle);                     // self 为 null 时抛异常！
```

`YIUIYooAssetsSpriteComponentSystem.GetSpriteAsync` 采用相同模式但更安全：
```csharp
EntityRef<YIUIYooAssetsSpriteComponent> selfRef = self;
var spriteAtlas = await self.YIUILoad.LoadAssetAsync<SpriteAtlas>(atlasPath);
...
self = selfRef;
if (!self.m_LoadedSprites.TryAdd(...))  // 若 self 已销毁，selfRef 访问为 null，需 null 检查
```

**结论**：所有跨 await 的 Entity 方法都需使用 EntityRef 模式；YIUIYooAssetsLoadComponent 在 await 后未做 null 检查，是潜在的崩溃点（正常游戏流程中 Component 不会在资源加载期间被销毁）。

#### 2.4 下载中断的边界处理

```csharp
// UnityWebFileRequestOperation.CreateWebRequest
DownloadHandlerFile handler = new DownloadHandlerFile(_fileSavePath);
handler.removeFileOnAbort = true;  // 关键：下载中断时自动删除不完整文件
_webRequest.timeout = _timeout;    // 超时设置（0 = 不超时）
```

- `removeFileOnAbort = true`：防止残留损坏的部分文件被误当完整文件使用
- 超时设置为 0 时永不超时，在弱网环境下可能导致下载请求永久挂起
- `DisposeRequest()` 在每次完成（成功或失败）后均调用，避免 UnityWebRequest 资源泄漏

#### 2.5 场景重复加载保护

```csharp
// LoadSceneAsync：已加载则直接 return（非错误）
if (self.handlers.TryGetValue(location, out handler))
{
    return;  // 静默跳过，不重新加载也不报错
}
```

注意：`LoadSceneMode` 参数被传入但 YooAssets 的 `package.LoadSceneAsync(location)` 调用未传入该参数，实际加载模式由 YooAssets 内部决定（默认 Single）。这是一个潜在的 Bug：Additive 加载场景时 loadSceneMode 参数被忽略。

#### 2.6 CoroutineLock 哈希冲突

```csharp
await self.Root().GetComponent<CoroutineLockComponent>().Wait(
    CoroutineLockType.ResourcesLoader,
    location.GetHashCode()  // 字符串哈希冲突概率低但非零
);
```

两个不同的 location 字符串若哈希值相同，会在同一个 CoroutineLock 下等待，导致本不相关的加载请求串行化（性能下降但不会崩溃）。

---

### 三、初始化顺序约束（跨包依赖顺序）

```
YooAssets.Initialize()                     ← 必须最先执行（创建 Driver，初始化 OperationSystem）
    │
    ▼
ResourcePackage.InitializeAsync(params)    ← 包初始化（根据模式可能需要网络）
    │
    ▼
（可选）UpdatePackageManifest + Download   ← 热更新流程
    │
    ▼
ResourcesComponent 创建（核心层可用）      ← 此时才能创建 ResourcesLoaderComponent
    │
    ▼
YIUIYooAssetsLoadComponent.Initialize()   ← 注入 YIUILoadDI 委托
    │
    ▼
YIUIEvent_YooAssetsLoad_Completed 事件     ← YIUI UI 系统可以开始加载资源
    │
    ▼
CodeLoader.DownloadAsync()                 ← 加载热更 DLL（使用 ResourcesComponent）
    │
    ▼
Assembly.Load() → ET.Entry.Start()         ← 游戏逻辑正式启动
```

任意步骤失败，均不得继续后续步骤，否则会产生空引用或逻辑错误。

---

### 四、YIUIAtlasData 的特殊生命周期

```csharp
// LoadAtlasAsync：加载后立即释放句柄
var handle = YooAssets.LoadAssetAsync<YIUIAtlasData>(YIUIConstAsset.AtlasDataName);
await handle.Task;
var atlasData = handle.AssetObject as YIUIAtlasData;
// ... 读取 atlasData.Infos 构建映射 ...
handle.Release();  ← 立即释放！atlasData 对象可能被 GC
```

**潜在问题**：在 Unity 中，`handle.Release()` 后如果引用计数为零，AssetBundle 可能被卸载，`atlasData` 对象会变为 null 或损坏。此处安全的原因是：映射表（`m_SpritePathMap`）在 `handle.Release()` 前已完全构建完毕，之后不再访问 `atlasData`。

但若 `YIUIAtlasData` 的 `Infos` 数组很大，`foreach` 遍历期间不能提前释放 `handle`。当前代码是正确的（Release 在 foreach 之后），但顺序不能改变。
