# cn.etetet.yiuiyooassets

## 概述

**ET.YIUI.YooAssets** 是 YIUI 框架与 YooAsset 资源管理器之间的桥接层（Adapter）。它将 YooAsset 的资源加载能力注入到 YIUI 的依赖注入接口（`YIUILoadDI`）中，使 YIUI UI 框架可以透明地使用 YooAsset 进行资源的异步/同步加载、释放和合法性验证。同时提供图集（SpriteAtlas）的运行时管理，支持按名称加载 Sprite，自动识别是否存在图集并优先从图集中读取。

- **版本**: 3.1.0
- **Unity 版本**: 2022.3+
- **分类**: UI/YIUI
- **依赖**: `cn.etetet.core >= 1.0.0`

---

## 目录结构

```
cn.etetet.yiuiyooassets/
├── Runtime/
│   └── Atlas/
│       ├── YIUIAtlasData.cs          # 图集数据 ScriptableObject
│       └── YIUIConstAsset_Atlas.cs   # 图集常量（资源名、路径）
├── Scripts/
│   ├── ModelView/Client/
│   │   ├── Component/
│   │   │   ├── YIUIYooAssetsLoadComponent.cs    # 资源加载组件（Model）
│   │   │   └── YIUIYooAssetsSpriteComponent.cs  # 图集/Sprite 组件（Model）
│   │   └── Event/
│   │       └── YIUIYooAssetsEvent.cs             # 加载完成事件定义
│   └── HotfixView/Client/
│       ├── System/
│       │   ├── YIUIYooAssetsLoadComponentSystem.cs      # 资源加载逻辑（System）
│       │   ├── YIUIYooAssetsSpriteComponentSystem.cs    # Sprite/图集逻辑（System）
│       │   └── YIUILoadComponentSystem_YooAsset.cs      # YooAsset 扩展（无需 pkgName）
│       ├── Event/
│       │   ├── YIUIInvokeYooAssetsHandler.cs            # Invoke 处理：初始化、资源信息查询
│       │   ├── YIUIInvokeLoadHandler.cs                 # Invoke 处理：加载 Sprite（同步/异步）
│       │   ├── YIUIInvokeReleaseHandler.cs              # Invoke 处理：释放 Sprite
│       │   └── On_YIUIEvent_YooAssetsLoad_Completed_SpriteHandler.cs  # 事件：初始化完成后加载图集
│       └── Helper/
│           └── YIUIFactory_YooAsset.cs           # YIUIFactory 扩展（实例化 GameObject）
└── Editor/
    ├── YIUIEditor/
    │   └── YIUIAtlasModule.cs         # 编辑器：刷新/生成图集数据
    └── YooAssetExtension/
        └── YIUIYooAssetExtension.cs   # 编辑器：YooAsset 自定义过滤规则
```

---

## 核心类/接口列表

### Runtime

#### `YIUIAtlasData` (ScriptableObject)
- **命名空间**: `YIUIFramework`
- **描述**: 存储所有图集的元数据，在编辑器中自动生成，运行时加载
- **字段**: `YIUIAtlasInfo[] Infos` — 图集信息数组

#### `YIUIAtlasInfo`
- **命名空间**: `YIUIFramework`
- **描述**: 单个图集的描述
- **字段**:
  - `string AtlasName` — 图集名称（YooAsset 资源定位地址）
  - `string[] SpriteNames` — 该图集包含的所有 Sprite 名称

#### `YIUIConstAsset` (partial)
- **命名空间**: `YIUIFramework`
- **常量**:
  - `AtlasDataName = "YIUIAtlasData"` — YooAsset 加载图集数据时使用的资源名
  - `AtlasDataPath = "Assets/GameRes/YIUI/YIUISettings/YIUIAtlasData.asset"` — 编辑器保存路径

---

### Model（Component 定义）

#### `YIUIYooAssetsLoadComponent`
```csharp
[ComponentOf(typeof(YIUILoadComponent))]
public class YIUIYooAssetsLoadComponent : Entity, IAwake, IDestroy
{
    public Dictionary<int, AssetHandle> m_AllHandle = new();
    public ResourcePackage m_Package;
}
```
- **命名空间**: `ET.Client`
- **所属**: `[ComponentOf(typeof(YIUILoadComponent))]`
- **字段**:
  - `Dictionary<int, AssetHandle> m_AllHandle` — 已加载资源句柄表（HashCode → Handle）
  - `ResourcePackage m_Package` — YooAsset 资源包引用
- **生命周期**: `IAwake`（空实现）, `IDestroy`（自动 `ReleaseAllAction()`）

#### `YIUIYooAssetsSpriteComponent`
```csharp
[ComponentOf(typeof(YIUILoadComponent))]
public class YIUIYooAssetsSpriteComponent : Entity, IAwake
{
    public EntityRef<YIUILoadComponent> m_YIUILoadRef;
    public YIUILoadComponent YIUILoad => m_YIUILoadRef;  // 属性访问器
    public readonly Dictionary<string, string> m_SpritePathMap = new();
    public readonly Dictionary<Sprite, SpriteAtlas> m_LoadedSprites = new();
}
```
- **命名空间**: `ET.Client`
- **关键属性**: `YIUILoad` — 直接访问父级 `YIUILoadComponent`（通过 `EntityRef` 安全引用）
- **字段**:
  - `m_SpritePathMap` — SpriteName → AtlasName 映射（从 YIUIAtlasData 构建）
  - `m_LoadedSprites` — 已加载 Sprite → SpriteAtlas 映射（用于图集释放）
- **注意**: `m_SpritePathMap` 和 `m_LoadedSprites` 声明为 `readonly`，防止整体替换

#### `YIUIEvent_YooAssetsLoad_Completed`
```csharp
public struct YIUIEvent_YooAssetsLoad_Completed
{
    public EntityRef<YIUIYooAssetsLoadComponent> YooAssetsLoadRef;
}
```
- **命名空间**: `ET.Client`
- **类型**: struct（异步发布事件）
- **描述**: YooAssets 组件初始化完成后发布，触发图集数据加载

---

### Hotfix（System 逻辑）

#### `YIUIYooAssetsLoadComponentSystem`
- **命名空间**: `ET.Client`
- **修饰**: `[FriendOf(typeof(YIUIYooAssetsLoadComponent))]`, `[EntitySystemOf(...)]`

| 方法 | 可见性 | 说明 |
|------|--------|------|
| `Initialize(packageName)` | public | 初始化入口，注入5个 DI 委托，PublishAsync 完成事件 |
| `LoadAssetAsyncFunc(arg1, arg2, arg3)` | private | 异步加载实现，忽略 arg1(包名)，使用 arg2(resName) |
| `LoadAssetFunc(arg1, arg2, arg3)` | private | 同步加载实现（宏 `!YIUIMACRO_SYNCLOAD_CLOSE`） |
| `LoadAssetHandle(handle)` | private | 加载结果封装：成功存入字典返回(obj, hashCode)，失败立即 Release 返回(null,0) |
| `ReleaseAction(hashCode)` | private | 按 hashCode 释放单个句柄 |
| `ReleaseAllAction()` | private | 释放所有句柄，清空字典 |
| `VerifyAssetValidityFunc(arg1, arg2)` | private | 调用 `m_Package.CheckLocationValid(arg2)` |
| `GetPackage()` | public | 返回当前 ResourcePackage |
| `GetAssetInfo(location, type)` | public | 查询资源信息（null 安全） |
| `GetAssetInfoByGUID(assetGUID, type)` | public | 按 GUID 查询资源信息（null 安全） |

**重要细节**：`LoadAssetAsyncFunc` 在 await 前后做了安全检查：
```csharp
EntityRef<YIUIYooAssetsLoadComponent> selfRef = self;
var handle = self.m_Package.LoadAssetAsync(arg2, arg3);
await handle.Task;
self = selfRef;  // await 后重新解引用，防止 Entity 销毁
return self.LoadAssetHandle(handle);
```

#### `YIUIYooAssetsSpriteComponentSystem`
- **命名空间**: `ET.Client`
- **修饰**: `[FriendOf(typeof(YIUIYooAssetsSpriteComponent))]`, `[EntitySystemOf(...)]`

| 方法 | 可见性 | 说明 |
|------|--------|------|
| `Awake()` | [EntitySystem] | 从父级获取 `YIUILoadComponent` 引用 |
| `LoadAtlasAsync()` | public | 加载 YIUIAtlasData，构建 m_SpritePathMap，加载完后 Release handle |
| `GetSpriteAtlasPath(spriteName)` | public | 查询 Sprite 所属图集路径（不存在返回 null） |
| `GetSprite(spriteName)` | public | 同步加载（宏保护） |
| `GetSpriteAsync(spriteName)` | public | 异步加载 Sprite |
| `ReleaseSprite(sprite)` | public | 智能释放：图集 Sprite 释放图集，散图 Sprite 直接释放 |

**GetSpriteAsync 核心逻辑**（修正第一轮）：
```csharp
// 无图集路径 → 直接加载散图
sprite = await self.YIUILoad.LoadAssetAsync<Sprite>(spriteName);

// 有图集路径 → 加载图集后取子 Sprite，并记录映射
EntityRef<YIUIYooAssetsSpriteComponent> selfRef = self;
var spriteAtlas = await self.YIUILoad.LoadAssetAsync<SpriteAtlas>(atlasPath);
sprite = spriteAtlas?.GetSprite(spriteName);
self = selfRef;  // await 后重新解引用
if (!self.m_LoadedSprites.TryAdd(sprite, spriteAtlas))
    Log.Error($"重复添加Sprite：{spriteName}");
```

#### `YIUILoadComponentSystem` (partial 扩展 — YooAsset 特化版)
- **描述**: 为 `YIUILoadComponent` 添加无需 pkgName 的快捷调用，统一传空字符串作为包名
- **可见性**: 所有方法均为 `internal`

```csharp
// 以下所有方法均将 pkgName 固定为 "" 转发给标准接口
internal static T LoadAsset<T>(this YIUILoadComponent self, string resName) where T : UnityObject
    => self.LoadAsset<T>("", resName);

internal static async ETTask<T> LoadAssetAsync<T>(this YIUILoadComponent self, string resName) where T : UnityObject
    => await self.LoadAssetAsync<T>("", resName);

internal static void LoadAssetAsync<T>(this YIUILoadComponent self, string resName, Action<T> action) where T : UnityObject
    => self.LoadAssetAsync<T>("", resName, action);

internal static bool VerifyAssetValidity(this YIUILoadComponent self, string resName)
    => self.VerifyAssetValidity("", resName);

// 非泛型重载
internal static UnityObject LoadAsset(this YIUILoadComponent self, string resName, Type assetType) ...
internal static async ETTask<UnityObject> LoadAssetAsync(this YIUILoadComponent self, string resName, Type assetType) ...
internal static void LoadAssetAsync(this YIUILoadComponent self, string resName, Type assetType, Action<UnityObject> action) ...
```

---

### Invoke 处理器

| 类名 | Invoke 类型 | Invoke 消息 | 说明 |
|------|------------|------------|------|
| `YIUIInvokeYooAssetsHandler` | Default（Async） | `YIUIInvokeEntity_LoadInitialize` → `ETTask<bool>` | 添加 `YIUIYooAssetsLoadComponent` 并调用 `Initialize()` |
| `YIUIInvokeGetAssetsInfoSyncHandler` | Sync | `YIUIInvokeEntity_GetAssetInfo` → `AssetInfo` | 查询资源信息 |
| `YIUIInvokeGetAssetInfoByGUIDSyncHandler` | Sync | `YIUIInvokeEntity_GetAssetInfoByGUID` → `AssetInfo` | 按 GUID 查询资源信息 |
| `YIUIInvokeLoadSpriteSyncHandler` | Sync（宏保护） | `YIUIInvokeEntity_LoadSprite` → `Sprite` | 同步加载 Sprite |
| `YIUIInvokeLoadSpriteAsyncHandler` | Async | `YIUIInvokeEntity_LoadSprite` → `ETTask<Sprite>` | 异步加载 Sprite |
| `YIUIInvokeReleaseSpriteSyncHandler` | Sync | `YIUIInvokeEntity_ReleaseSprite` → `void` | 释放 Sprite |

**修正第一轮**：`YIUIInvokeYooAssetsHandler` 继承自 `AInvokeEntityHandler<..., ETTask<bool>>`，属于异步 Invoke，不是"Default"类型。

---

### Helper

#### `YIUIFactory` (partial 扩展 — YooAsset 特化版)
```csharp
public static async ETTask<GameObject> InstantiateGameObjectAsync(Scene scene, string resName)
    => await InstantiateGameObjectAsync(scene, "", resName);
```
- **描述**: 提供不需要包名的 GameObject 实例化快捷方法

---

### Editor 工具

#### `YIUIAtlasModule.RefreshAtlasData()`
- **命名空间**: `YIUIFramework.Editor`
- **描述**: 打包所有图集（PackAllAtlases），扫描 `*.spriteatlasv2` 文件，生成 `YIUIAtlasData.asset`

#### YooAsset 自定义过滤规则（`YIUIYooAssetExtension.cs`）

| 类名 | 规则说明 |
|------|--------|
| `YIUIFilterRule` | 收集 Prefabs/ 下预制体 + Sprites/ 下所有图片 |
| `YIUIFilterRule_Root` | 只收集根目录文件（不含子目录） |
| `YIUIFilterRule_Prefab` | 只收集 Prefabs/ 下预制体 |
| `YIUIFilterRule_Sprite` | 只收集 Sprites/ 下图片 |
| `YIUIFilterRule_Atlas` | 只收集 Atlas/ 下图集 |
| `YIUIFilterRule_NoAtlas_Sprite` | 只收集没有对应图集的散图（AtlasIgnore 或无对应图集文件夹） |

---

## 实现原理

### 依赖注入（DI）模式

YIUI 框架通过 `YIUILoadDI`（静态委托容器）抽象资源加载接口，本包在 `Initialize()` 中将具体的 YooAsset 实现注入：

```csharp
// YIUIYooAssetsLoadComponentSystem.Initialize()
self.m_Package = YooAssets.GetPackage(packageName);

#if !YIUIMACRO_SYNCLOAD_CLOSE
YIUILoadDI.LoadAssetFunc = self.LoadAssetFunc;        // 同步加载
#endif
YIUILoadDI.LoadAssetAsyncFunc = self.LoadAssetAsyncFunc;  // 异步加载
YIUILoadDI.ReleaseAction = self.ReleaseAction;            // 释放
YIUILoadDI.VerifyAssetValidityFunc = self.VerifyAssetValidityFunc;  // 合法性校验
YIUILoadDI.ReleaseAllAction = self.ReleaseAllAction;      // 批量释放

await EventSystem.Instance.PublishAsync(self.Scene(), new YIUIEvent_YooAssetsLoad_Completed { YooAssetsLoadRef = self });
```

这实现了策略模式，YIUI 框架本身不依赖 YooAsset，可以替换为其他资源管理器。

### 资源句柄管理

加载时通过 `handle.GetHashCode()` 作为唯一 ID 存入 `m_AllHandle`，调用方持有该 ID 用于后续释放：

```csharp
private static (UnityEngine.Object, int) LoadAssetHandle(this YIUIYooAssetsLoadComponent self, AssetHandle handle)
{
    if (handle.AssetObject != null)
    {
        var hashCode = handle.GetHashCode();
        self.m_AllHandle.Add(hashCode, handle);
        return (handle.AssetObject, hashCode);
    }
    else
    {
        handle.Release();      // 加载失败立即释放，不缓存
        return (null, 0);
    }
}
```

### Entity 安全引用模式

在 `async ETTask` 方法中，`await` 可能导致 Entity 被销毁。本包采用标准 ET 安全模式：

```csharp
EntityRef<YIUIYooAssetsLoadComponent> selfRef = self;  // 保存弱引用
var handle = self.m_Package.LoadAssetAsync(arg2, arg3);
await handle.Task;                                       // 异步等待，期间 Entity 可能销毁
self = selfRef;                                          // 重新解引用（若已销毁则返回 null）
return self.LoadAssetHandle(handle);
```

### 图集加载后手动 Release

```csharp
// LoadAtlasAsync() 加载完 YIUIAtlasData 后立即释放 handle
// 因为只需要构建 m_SpritePathMap，不需要持续持有 YIUIAtlasData
handle.Release();
```

---

## 关键流程

### 初始化流程

```
YIUIInvokeYooAssetsHandler.Handle(entity)
  └─ YIUILoadComponent.AddComponent<YIUIYooAssetsLoadComponent>()
       └─ YIUIYooAssetsLoadComponentSystem.Initialize("DefaultPackage")
            ├─ YooAssets.GetPackage("DefaultPackage")  → m_Package
            ├─ 注入 YIUILoadDI 委托（4~5 个函数）
            └─ EventSystem.PublishAsync(YIUIEvent_YooAssetsLoad_Completed)
                 └─ On_YIUIEvent_YooAssetsLoad_Completed_SpriteHandler.Run()
                      └─ YIUILoadComponent.AddComponent<YIUIYooAssetsSpriteComponent>()
                           └─ Awake: m_YIUILoadRef = parent<YIUILoadComponent>
                           └─ LoadAtlasAsync()
                                ├─ YooAssets.LoadAssetAsync<YIUIAtlasData>("YIUIAtlasData")
                                ├─ 遍历 atlasData.Infos，构建 m_SpritePathMap
                                └─ handle.Release()  ← 释放 atlasData 的 handle
```

### Sprite 异步加载流程

```
UI 请求: YIUIInvokeLoadSpriteAsyncHandler.Handle(entity, {ResName="icon_attack"})
  └─ entity.YIUILoad().GetComponent<YIUIYooAssetsSpriteComponent>().GetSpriteAsync("icon_attack")
       ├─ [有图集] m_SpritePathMap["icon_attack"] = "Atlas_Battle"
       │   ├─ await YIUILoad.LoadAssetAsync<SpriteAtlas>("Atlas_Battle")
       │   ├─ spriteAtlas.GetSprite("icon_attack")
       │   └─ m_LoadedSprites[sprite] = spriteAtlas  ← 记录图集引用（用于释放）
       └─ [无图集] await YIUILoad.LoadAssetAsync<Sprite>("icon_attack")
```

### Sprite 释放流程

```
ReleaseSprite(sprite)
  ├─ [来自图集] m_LoadedSprites.ContainsKey(sprite) == true
  │   ├─ spriteAtlas = m_LoadedSprites[sprite]
  │   ├─ m_LoadedSprites.Remove(sprite)
  │   └─ YIUILoad.Release(spriteAtlas)   ← 释放图集（间接释放子 Sprite）
  └─ [散图] YIUILoad.Release(sprite)    ← 直接释放 Sprite
```

---

## 依赖关系

### 依赖的 Package
- `cn.etetet.core` — ET 框架核心（Entity、Scene、EventSystem 等）
- `cn.etetet.yiuiframework`（间接）— `YIUILoadComponent`、`YIUILoadDI`、`YIUIFramework` 命名空间

### 依赖的外部库
- **YooAsset** — `ResourcePackage`, `AssetHandle`, `AssetInfo`, `YooAssets`
- **Unity.U2D** — `SpriteAtlas`
- **Sirenix.OdinInspector**（Runtime，用于 Inspector 显示）

### 被依赖关系
- 是 YIUI UI 框架使用 YooAsset 的唯一桥接实现
- 所有需要加载 UI 资源的业务逻辑都通过 `YIUILoadComponent` → `YIUILoadDI` → 本包委托
- `YIUIFactory_YooAsset` 供所有需要实例化 UI GameObject 的地方调用

---

## 编译宏

| 宏 | 作用 |
|----|------|
| `YIUIMACRO_SYNCLOAD_CLOSE` | 关闭所有同步加载逻辑（保留异步接口），影响：`LoadAssetFunc`、`LoadAsset<T>()`、`YIUIInvokeLoadSpriteSyncHandler`、`GetSprite()` |
| `YOO_ASSET_2_3_OR_NEWER` | 适配 YooAsset 2.3+ 版本的 API 差异 |
| `UNITY_EDITOR` | 编辑器下加载前调用 `VerifyAssetValidity()` 打印详细错误日志 |

---

## 边界情况与异常处理

1. **包不存在**: `Initialize()` 中若 `YooAssets.GetPackage()` 返回 null，记录错误并返回 `false`，不注入 DI
2. **图集数据缺失**: `LoadAtlasAsync()` 中若 `YIUIAtlasData` 为 null，直接返回（不崩溃，m_SpritePathMap 为空，所有 Sprite 走散图路径）
3. **handle 加载失败**: `LoadAssetHandle()` 中 `handle.AssetObject == null` 时立即 Release，返回 `(null, 0)`，不缓存失败句柄
4. **Sprite 不在图集中**: `spriteAtlas.GetSprite()` 返回 null 时记录错误并返回 null
5. **重复添加 Sprite**: `m_LoadedSprites.TryAdd()` 失败时记录错误（不抛异常）
6. **Entity 异步安全**: 所有异步方法均在 await 后重新解 `EntityRef`，防止 await 期间 Entity 被销毁导致空引用
7. **同一 Atlas 多 Sprite**: 每次 `GetSpriteAsync` 请求同图集的不同 Sprite，都会独立加载一次 SpriteAtlas handle（不复用），`m_LoadedSprites` 中记录的是 `Sprite→SpriteAtlas`，释放时调用 `YIUILoad.Release(spriteAtlas)`（通过 hashCode 匹配到各自的 handle），引用计数正确
8. **LoadAtlasAsync 直连 YooAssets 全局 API**: `YooAssets.LoadAssetAsync<YIUIAtlasData>()` 绕过了 `m_AllHandle` 字典，handle 在读取数据后立即 `Release()`，不会造成句柄泄漏

---

## 跨 Package 交互深度分析（Round 3）

### 与 cn.etetet.yiuiframework 的交互

| 交互点 | 本包调用 | yiuiframework 提供 | 说明 |
|--------|---------|-------------------|------|
| DI 注入 | `YIUILoadDI.LoadAssetAsyncFunc = ...` | `YIUILoadDI`（静态委托容器） | 本包覆写 4~5 个委托，完成资源加载能力接管 |
| 组件注册 | `[ComponentOf(typeof(YIUILoadComponent))]` | `YIUILoadComponent`（父实体） | 两个 Component 均依附于 `YIUILoadComponent` |
| 快捷扩展 | `YIUILoadComponent.LoadAsset<T>(resName)` | 接受扩展方法 | 本包以 partial 静态类形式扩展 `YIUILoadComponentSystem` |
| 事件触发 | `EventSystem.Instance.PublishAsync(YIUIEvent_YooAssetsLoad_Completed)` | `EventSystem`（ET 核心） | 初始化完成后发布事件，触发 SpriteComponent 的创建 |
| Entity Invoke | `entity.YIUILoad()` | `YIUILoad()` 扩展方法（yiuiframework 提供） | Invoke 处理器通过此方法定位 `YIUILoadComponent` |

### 与 cn.etetet.core（ET 框架）的交互

| 交互点 | 说明 |
|--------|------|
| `EntityRef<T>` | 弱引用模式，防止 async/await 期间 Entity 销毁导致悬空引用 |
| `[FriendOf]` / `[EntitySystemOf]` | Source Generator 宏，生成系统注册代码 |
| `ETTask` / `ETTask<T>` | ET 自定义协程任务，替代 C# `Task`，避免 GC 压力 |
| `[Event(SceneType.All)]` | `On_YIUIEvent_YooAssetsLoad_Completed_SpriteHandler` 注册到所有场景类型，确保不论 Scene 如何切换都能收到图集初始化事件 |
| `[Invoke]` 注册 | `YIUIInvokeYooAssetsHandler` 继承 `AInvokeEntityHandler<..., ETTask<bool>>`，属于异步 Invoke（不是 Sync） |

### YooAsset 直连 vs 通过 YIUILoad 的差异

```
LoadAtlasAsync():
  YooAssets.LoadAssetAsync<YIUIAtlasData>(AtlasDataName)  ← 直连 YooAssets 全局 API
    ✓ 不经过 YIUILoadDI 委托
    ✓ Handle 不存入 m_AllHandle
    ✓ 数据读取后立即 handle.Release()
    目的：一次性加载图集元数据，不需要长期持有引用

GetSpriteAsync() / GetSprite():
  self.YIUILoad.LoadAssetAsync<SpriteAtlas>(atlasPath)  ← 经过 YIUILoadDI 委托
    → YIUILoadDI.LoadAssetAsyncFunc → LoadAssetAsyncFunc → m_Package.LoadAssetAsync
    → 存入 m_AllHandle（hashCode 由 YIUILoadComponent 管理）
    → 长期持有，直到 ReleaseSprite() 或组件销毁
```

### Invoke 调用链（从 UI 代码到 YooAsset）

```
UI 业务代码
  └─ entity.Invoke<YIUIInvokeEntity_LoadSprite, ETTask<Sprite>>(args)
       └─ ET Invoke 系统 → YIUIInvokeLoadSpriteAsyncHandler.Handle(entity, args)
            └─ entity.YIUILoad()  ← yiuiframework 扩展方法，找 YIUILoadComponent
                 └─ .GetComponent<YIUIYooAssetsSpriteComponent>().GetSpriteAsync(resName)
                      ├─ [有图集路径] self.YIUILoad.LoadAssetAsync<SpriteAtlas>(atlasPath)
                      │     └─ YIUILoadDI.LoadAssetAsyncFunc("", atlasPath, typeof(SpriteAtlas))
                      │           └─ m_Package.LoadAssetAsync(atlasPath, typeof(SpriteAtlas))
                      │                 └─ YooAsset 运行时
                      └─ [无图集路径] self.YIUILoad.LoadAssetAsync<Sprite>(resName)
                            └─ YIUILoadDI.LoadAssetAsyncFunc("", resName, typeof(Sprite))
```

### Entity 直接类型转换的潜在问题

`YIUIInvokeYooAssetsHandler` 中使用了直接转换：
```csharp
EntityRef<YIUILoadComponent> loadComponentRef = (YIUILoadComponent)entity;
```
这要求调用方传入的 `entity` 必须是 `YIUILoadComponent` 类型（ET 框架的隐式 Entity 转换）。若传入其他 Entity 类型，`loadComponentRef.Entity` 会为 null，通过首行 null 检查安全退出。

---

## 最佳实践

1. **初始化时机**: 必须在 YooAsset 的 `ResourcePackage` 初始化（`InitializeAsync`）完成后，才能调用本包的 `YIUIInvokeYooAssetsHandler`
2. **图集优先**: 编辑器端通过 `YIUIAtlasModule.RefreshAtlasData()` 生成 `YIUIAtlasData.asset` 后，运行时会自动优先从图集加载 Sprite，减少 Draw Call
3. **同步加载**: 生产环境建议开启 `YIUIMACRO_SYNCLOAD_CLOSE` 禁用同步加载，避免主线程卡顿
4. **资源释放**: UI 关闭时须调用 `ReleaseSprite()` 释放图集引用，防止内存泄漏；销毁 `YIUIYooAssetsLoadComponent` 时会自动释放所有句柄
5. **扩展性**: 若项目不使用 YooAsset，可实现同等 DI 注入的适配层替换本包，YIUI 框架无需修改
6. **图集句柄复用**: 同一图集的多个 Sprite 各自持有一个 handle（通过 m_AllHandle），YooAsset 内部通过引用计数管理真实内存。频繁加载同图集时，可考虑在业务层缓存 SpriteAtlas 对象减少 handle 开销
