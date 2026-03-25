# cn.etetet.yiuistatesync

## 概述

**版本**: 3.0.2
**分类**: Demo/YIUI | UI/YIUI
**描述**: YIUI Demo 演示包，基于 ET9 StateSync 场景，展示如何将 YIUI 框架集成到 ET 框架中，实现登录、大厅、主场景三个典型 UI 面板的完整流程。

该包是一个**演示/示例包（Demo Package）**，而非通用功能库。它的主要价值在于：
1. 提供 YIUI 框架与 ET StateSync 的集成范例
2. 提供编辑器工具辅助开发者切换 YIUI Demo / ET Demo 两种模式
3. 展示三层 UI 面板（登录→大厅→游戏主界面）的标准实现模式

---

## 目录结构

```
cn.etetet.yiuistatesync/
├── package.json                          # 包描述与依赖
├── ET.sln                                # YIUI Demo 专用 ET 工程解决方案文件
├── README.md                             # 包索引说明
├── Scenes/
│   └── Init.unity                        # Demo 专属启动场景
├── Assets/GameRes/YIUI/                  # UI 资源目录
│   ├── Common/                           # 公共资源（YIUIRoot.prefab）
│   ├── Login/                            # 登录面板资源（LoginPanel.prefab）
│   ├── Lobby/                            # 大厅面板资源（LobbyPanel.prefab）
│   └── Main/                             # 主界面面板资源（MainPanel.prefab）
├── Scripts/
│   ├── ModelView/Client/
│   │   ├── YIUIComponent/                # Panel 数据组件手写定义（空壳 partial）
│   │   │   ├── Login/LoginPanelComponent.cs
│   │   │   ├── Lobby/LobbyPanelComponent.cs
│   │   │   └── Main/MainPanelComponent.cs
│   │   └── YIUIGen/                      # 工具自动生成的 Panel 数据代码
│   │       ├── Login/LoginPanelComponentGen.cs
│   │       ├── Lobby/LobbyPanelComponentGen.cs
│   │       └── Main/MainPanelComponentGen.cs
│   └── HotfixView/Client/
│       ├── AssemblyReference.asmref      # 热更程序集引用
│       ├── EntryEvent3_InitClient.cs     # 客户端初始化入口事件
│       ├── UI/
│       │   ├── UILogin/                  # 登录 UI 事件处理（创建/关闭 LoginPanel）
│       │   ├── UILobby/                  # 大厅 UI 事件处理（创建 LobbyPanel）
│       │   └── UIHelp/                   # 场景切换 UI 事件处理（创建 MainPanel）
│       ├── YIUIGen/                      # 工具自动生成的 System 绑定代码
│       │   ├── Login/LoginPanelComponentSystemGen.cs
│       │   ├── Lobby/LobbyPanelComponentSystemGen.cs
│       │   └── Main/MainPanelComponentSystemGen.cs
│       └── YIUISystem/                   # 手写逻辑 System
│           ├── Login/LoginPanelComponentSystem.cs
│           ├── Lobby/LobbyPanelComponentSystem.cs
│           └── Main/MainPanelComponentSystem.cs
└── Editor/
    ├── YIUIDemoWindow.cs                 # 核心编辑器 Demo 切换工具窗口
    ├── YIUIYooAssetSetting.cs            # YooAsset 设置配置（ScriptableObject）
    ├── ScriptsReferencesHelper.cs        # 脚本引用修复工具
    ├── FileHelper.cs                     # 文件操作辅助
    └── PackageGit.cs                     # Git 版本工具
```

---

## 核心类与接口说明

### 1. EntryEvent3_InitClient
**位置**: `Scripts/HotfixView/Client/EntryEvent3_InitClient.cs`
**命名空间**: `ET.Client`
**特性**: `[Event(SceneType.StateSync)]`

客户端初始化入口。在 `EntryEvent3` 事件触发时执行，负责：
- 添加 `GlobalComponent`、`ResourcesLoaderComponent`、`PlayerComponent`、`CurrentScenesComponent`
- 初始化 `YIUIMgrComponent`（YIUI UI 管理器）
- 初始化成功后发布 `AppStartInitFinish` 事件，驱动后续 UI 流程

```csharp
[Event(SceneType.StateSync)]
public class EntryEvent3_InitClient : AEvent<Scene, EntryEvent3>
{
    protected override async ETTask Run(Scene root, EntryEvent3 args)
    {
        root.AddComponent<GlobalComponent>();
        root.AddComponent<ResourcesLoaderComponent>();
        root.AddComponent<PlayerComponent>();
        root.AddComponent<CurrentScenesComponent>();

        var result = await root.AddComponent<YIUIMgrComponent>().Initialize();
        if (!result) { Log.Error("初始化UI失败"); return; }

        await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
    }
}
```

---

### 2. LoginPanelComponent（及 Gen）
**文件**: `ModelView/Client/YIUIComponent/Login/LoginPanelComponent.cs`
         `ModelView/Client/YIUIGen/Login/LoginPanelComponentGen.cs`
**特性**: `[YIUI(EUICodeType.Panel, EPanelLayer.Panel)]`, `[ComponentOf(typeof(YIUIChild))]`
**实现接口**: `IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen`

登录面板数据组件。手写部分为空壳（仅声明 `partial class`），Gen 部分由 YIUI 工具自动生成，包含：

| 字段 | 类型 | 说明 |
|------|------|------|
| `u_UIBase` / `UIBase` | `EntityRef<YIUIChild>` | UI 根节点引用 |
| `u_UIWindow` / `UIWindow` | `EntityRef<YIUIWindowComponent>` | 窗口组件 |
| `u_UIPanel` / `UIPanel` | `EntityRef<YIUIPanelComponent>` | 面板组件 |
| `u_ComAccount` | `UnityEngine.UI.InputField` | 账号输入框 |
| `u_ComPassword` | `UnityEngine.UI.InputField` | 密码输入框 |
| `u_EventLogin` | `UITaskEventP0` | 登录按钮事件 |
| `u_EventLoginHandle` | `UITaskEventHandleP0` | 登录事件句柄（用于注销） |
| `OnEventLoginInvoke` | `const string` | 事件回调标识 = `"LoginPanelComponent.OnEventLoginInvoke"` |

**Gen 中的 `UIBind()` 配置**:
```csharp
self.UIWindow.WindowOption = EWindowOption.None;
self.UIPanel.Layer = EPanelLayer.Panel;
self.UIPanel.PanelOption = EPanelOption.TimeCache;
self.UIPanel.StackOption = EPanelStackOption.VisibleTween;
self.UIPanel.Priority = 0;
self.UIPanel.CachePanelTime = 10;  // 关闭后缓存 10 秒
// 绑定登录事件
self.u_EventLoginHandle = self.u_EventLogin.Add(self, LoginPanelComponent.OnEventLoginInvoke);
```

---

### 3. LobbyPanelComponent（及 Gen）
**文件**: `ModelView/Client/YIUIComponent/Lobby/LobbyPanelComponent.cs`
         `ModelView/Client/YIUIGen/Lobby/LobbyPanelComponentGen.cs`

大厅面板数据组件。字段结构类似 `LoginPanelComponent`，差异在于：
- 无输入框组件
- `u_EventEnterMap` / `u_EventEnterMapHandle` — 进入地图按钮事件
- `OnEventEnterMapInvoke` = `"LobbyPanelComponent.OnEventEnterMapInvoke"`

UIBind 配置与 LoginPanel 完全相同（TimeCache 10s, VisibleTween, Priority 0）。

---

### 4. MainPanelComponent（及 Gen）
**文件**: `ModelView/Client/YIUIComponent/Main/MainPanelComponent.cs`
         `ModelView/Client/YIUIGen/Main/MainPanelComponentGen.cs`

主场景面板数据组件。为最简面板，**无任何自定义 UI 控件和事件**，仅有标准三件套（UIBase / UIWindow / UIPanel）。
UIBind 配置同上（TimeCache 10s, VisibleTween, Priority 0）。

---

### 5. LoginPanelComponentSystem（手写 + 生成）
**文件**: `HotfixView/Client/YIUISystem/Login/LoginPanelComponentSystem.cs`
         `HotfixView/Client/YIUIGen/Login/LoginPanelComponentSystemGen.cs`

登录面板逻辑系统：

**生成部分** (`SystemGen`)：
- `[EntitySystem] Awake` — 空，框架自动调用
- `[EntitySystem] YIUIBind` — 调用 `UIBind()` 完成控件绑定

**手写部分** (`System`)：
- `[EntitySystem] YIUIInitialize` — 面板初始化（当前为空，供扩展）
- `[EntitySystem] Destroy` — 面板销毁（当前为空）
- `[EntitySystem] async ETTask<bool> YIUIOpen` — 面板打开（当前直接返回 true）
- `[YIUIInvoke] OnEventLoginInvoke` — 登录按钮点击，调用 `LoginHelper.Login()`

```csharp
[YIUIInvoke(LoginPanelComponent.OnEventLoginInvoke)]
private static async ETTask OnEventLoginInvoke(this LoginPanelComponent self)
{
    Log.Info($"登录");
    GlobalComponent globalComponent = self.Root().GetComponent<GlobalComponent>();
    await LoginHelper.Login(self.Root(),
        globalComponent.GlobalConfig.Address,
        self.u_ComAccount.text,
        self.u_ComPassword.text);
}
```

---

### 6. LobbyPanelComponentSystem
**文件**: `HotfixView/Client/YIUISystem/Lobby/LobbyPanelComponentSystem.cs`

大厅面板逻辑系统。核心逻辑在 `OnEventEnterMapInvoke`：
1. 先隐藏 UI：`self.UIBase.SetActive(false)`（避免视觉残留）
2. 调用 `EnterMapHelper.EnterMapAsync()` 进入地图（异步）
3. 关闭大厅面板：`await self.UIPanel.CloseAsync()`

```csharp
[YIUIInvoke(LobbyPanelComponent.OnEventEnterMapInvoke)]
private static async ETTask OnEventEnterMapInvoke(this LobbyPanelComponent self)
{
    self.UIBase.SetActive(false);
    await EnterMapHelper.EnterMapAsync(self.Root());
    await self.UIPanel.CloseAsync();
}
```

---

### 7. MainPanelComponentSystem
**文件**: `HotfixView/Client/YIUISystem/Main/MainPanelComponentSystem.cs`

主场景面板逻辑系统。当前所有生命周期方法（YIUIInitialize、Destroy、YIUIOpen）均为空实现，无任何业务逻辑，作为扩展骨架存在。

---

### 8. UI 事件处理类（驱动面板生命周期）

| 类名 | 监听事件 | SceneType | 动作 |
|------|---------|-----------|------|
| `AppStartInitFinish_CreateLoginUI` | `AppStartInitFinish` | `StateSync` | `OpenPanelAsync<LoginPanelComponent>` |
| `LoginFinish_RemoveLoginUI` | `LoginFinish` | `StateSync` | `ClosePanelAsync<LoginPanelComponent>` |
| `LoginFinish_CreateLobbyUI` | `LoginFinish` | `StateSync` | `OpenPanelAsync<LobbyPanelComponent>` |
| `SceneChangeFinishEvent_CreateUIHelp` | `SceneChangeFinish` | **`Current`** | `OpenPanelAsync<MainPanelComponent>` |

> **注意**：`SceneChangeFinishEvent_CreateUIHelp` 使用的是 `SceneType.Current`（游戏内场景），而非 `SceneType.StateSync`，因为此时已进入游戏地图场景，场景类型发生了变化。

`LoginFinish` 事件同时触发两个监听器（`RemoveLoginUI` 和 `CreateLobbyUI`），它们并行执行，实现关闭登录界面与打开大厅界面的同步切换。

---

### 9. YIUIDemoWindow（编辑器）
**位置**: `Editor/YIUIDemoWindow.cs`
**命名空间**: `YIUIFramework.Editor`
**基类**: `OdinEditorWindow`
**菜单**: `ET/YIUI Demo`

核心 Demo 管理工具窗口。包含 `EDemoType` 枚举（`YIUI` / `ET`）和以下功能：

#### 主操作：`Switch()` — 切换 Demo 模式
依次执行四步：
```
SyncYooAssetSetting() → CopyET() → ChangeFile() → SwitchToScene()
```
切换后调用 `CloseWindowRefresh()` 刷新资源，再调用 `ScriptsReferencesHelper.Run()` 修复脚本引用。

#### `SyncYooAssetSetting()` — 同步 YooAsset 设置
- 从 `YIUIYooAssetSetting` 中取 YIUI 专属的 `DefaultPackage` 配置
- 将其 Groups 合并覆盖到项目全局 YooAsset `DefaultPackage`
- 强制开启 `EnableAddressable = true` 和 `UniqueBundleName = true`（YIUI 必需）

#### `CopyET()` — 拷贝 ET.sln
将对应 Demo 包（`cn.etetet.yiuistatesync` 或 `cn.etetet.statesync`）内的 `ET.sln` 拷贝到项目根目录，覆盖现有文件。需关闭 IDE 再操作。

#### `ChangeFile()` — 注释/取消注释文件（双模式共存核心）
实现步骤：
1. `ReplaceEventSystem()` — 将 `cn.etetet.core` 中 `EventSystem.cs` 的 `public class EventSystem` 替换为 `public partial class EventSystem`（YIUI 需要将 EventSystem 声明为 partial 来扩展）
2. `ReplaceUIComponentSystem()` — 注释 `cn.etetet.ui` 中 `UIComponentSystem.cs` 的某行（`self.UIGlobalComponent =` → `//self.UIGlobalComponent =`），避免编译冲突
3. 遍历 `cn.etetet.yiuistatesync/Scripts/HotfixView/Client` 下所有 `.cs` 文件：
   - YIUI 模式：取消注释 yiuistatesync 文件，注释同路径的 statesync 文件
   - ET 模式：注释 yiuistatesync 文件，取消注释 statesync 文件

**注释机制**：在文件首尾添加 `/*` 和 `*/` 将整个文件包裹为注释，使 Unity 编译器忽略该文件，实现"软禁用"而不删除文件。

#### `SwitchToScene()` — 场景切换
打开对应 Demo 包（`cn.etetet.yiuistatesync` 或 `cn.etetet.loader`）下的 `Scenes/Init.unity`。

#### `DemoCoverScene()` — 覆盖 Init 场景（不可逆）
将 YIUI 的 `Init.unity` 拷贝覆盖到 `cn.etetet.loader/Scenes/Init.unity`，此后无法再切换回 ET Demo。

#### `SetTMP()` — 设置 TMP 中文字体
通过反射将 `TMP_Settings` 的 `m_defaultFontAsset` 字段设置为 `SourceHanSansSC-VF SDF`（思源黑体）。

#### `SetYooAssetSetting()` — 打开 YooAsset 设置
在 YIUI 模式下，用 YIUI 的设置替换全局 YooAsset 设置并打开 AssetBundle Collector 窗口；ET 模式则恢复全局设置。

---

## 实现原理

### YIUI Panel 代码分层模式

本包展示了 YIUI 框架标准的 Panel 四层代码结构：

```
ModelView/
  YIUIComponent/{Panel}Component.cs       ← 手写：声明部分类（可为空，用于扩展数据字段）
  YIUIGen/{Panel}ComponentGen.cs          ← 自动生成：声明 UI 字段、事件常量、实现接口

HotfixView/
  YIUISystem/{Panel}ComponentSystem.cs    ← 手写：业务逻辑（YIUIOpen/Destroy/事件回调）
  YIUIGen/{Panel}ComponentSystemGen.cs    ← 自动生成：Awake + UIBind（控件与事件绑定）
```

**关键原则**：
- `YIUIGen` 目录下的文件由 YIUI 工具自动生成，注释明确标注"请勿修改"，修改会在下次生成时被覆盖
- 业务逻辑必须写在 `YIUISystem` 目录下的手写文件中
- C# `partial class` 机制将手写和生成代码无缝合并

### UI 驱动流程（事件驱动）

YIUI StateSync Demo 完全基于 ET 事件系统驱动 UI 流程：

```
EntryEvent3
    └─► EntryEvent3_InitClient
            初始化 GlobalComponent/ResourcesLoaderComponent/PlayerComponent/CurrentScenesComponent
            + await YIUIMgrComponent.Initialize()
            └─► 发布 AppStartInitFinish [SceneType.StateSync]
                    └─► AppStartInitFinish_CreateLoginUI
                            await OpenPanelAsync<LoginPanelComponent>()
                            [用户输入账号密码，点击登录按钮]
                            └─► LoginPanelComponentSystem.OnEventLoginInvoke
                                    await LoginHelper.Login(address, account, password)
                                    └─► 发布 LoginFinish [SceneType.StateSync]
                                            ├─► LoginFinish_RemoveLoginUI
                                            │       await ClosePanelAsync<LoginPanelComponent>()
                                            └─► LoginFinish_CreateLobbyUI
                                                    await OpenPanelAsync<LobbyPanelComponent>()
                                                    [用户点击进入游戏]
                                                    └─► LobbyPanelComponentSystem.OnEventEnterMapInvoke
                                                            SetActive(false)
                                                            await EnterMapHelper.EnterMapAsync()
                                                            await UIPanel.CloseAsync()
                                                            └─► 发布 SceneChangeFinish [SceneType.Current]
                                                                    └─► SceneChangeFinishEvent_CreateUIHelp
                                                                            await OpenPanelAsync<MainPanelComponent>()
```

### Demo 切换机制（双模式共存）

项目同时包含 `cn.etetet.yiuistatesync`（YIUI Demo）和 `cn.etetet.statesync`（ET 原生 Demo）两个平行包。

两个包的 `HotfixView/Client` 目录结构完全对称，文件名相同。通过"注释文件"机制实现无缝切换：
- YIUI 模式：`yiuistatesync/*.cs` 生效，`statesync/*.cs` 被包裹为 `/* ... */`
- ET 模式：`statesync/*.cs` 生效，`yiuistatesync/*.cs` 被包裹为 `/* ... */`

此外，切换时还会修改两个外部包的源码（`cn.etetet.core` 和 `cn.etetet.ui`），这是一种侵入性操作，值得注意。

---

## 关键流程图

### UI 面板状态机

```
[初始] ──AppStartInitFinish──► [登录界面 LoginPanel]
                                    │
                              LoginFinish（双事件）
                                    │ ClosePanelAsync<LoginPanel>
                                    │ OpenPanelAsync<LobbyPanel>
                                    ▼
                              [大厅界面 LobbyPanel]
                                    │
                              EnterMapInvoke
                                    │ SetActive(false)
                                    │ EnterMapAsync()
                                    │ CloseAsync()
                                    ▼
                              [场景切换进行中...]
                                    │
                              SceneChangeFinish
                                    │ OpenPanelAsync<MainPanel>
                                    ▼
                              [主游戏界面 MainPanel]
```

---

## 三个面板配置对比

| 配置项 | LoginPanel | LobbyPanel | MainPanel |
|--------|-----------|------------|-----------|
| Package | Login | Lobby | Main |
| Prefab | LoginPanel | LobbyPanel | MainPanel |
| Layer | Panel | Panel | Panel |
| PanelOption | TimeCache | TimeCache | TimeCache |
| StackOption | VisibleTween | VisibleTween | VisibleTween |
| Priority | 0 | 0 | 0 |
| CachePanelTime | 10s | 10s | 10s |
| WindowOption | None | None | None |
| UI 控件 | InputField×2 | 无 | 无 |
| UI 事件 | EventLogin | EventEnterMap | 无 |

三个面板的 Panel 配置完全一致，差异仅在 UI 控件和事件数量上。

---

## 依赖关系

### 直接依赖（package.json 中声明）
- `cn.etetet.statesync` (3.0.11) — 提供 `SceneType.StateSync`、`LoginHelper`、`EnterMapHelper`、`GlobalComponent`、`PlayerComponent`、`LoginFinish`、`SceneChangeFinish` 等
- `cn.etetet.yiuiframework` (3.0.1) — YIUI 框架核心，提供 `YIUIMgrComponent`、`YIUIChild`、`YIUIWindowComponent`、`YIUIPanelComponent`、`EPanelLayer`、`EPanelOption` 等
- `cn.etetet.yiuiyooassets` (3.0.0) — YooAsset 集成，资源加载
- `cn.etetet.yiuiloopscrollrectasync` — 无限循环列表
- `cn.etetet.yiuigm` — GM 命令工具
- `cn.etetet.yiuireddot` — 红点系统
- `cn.etetet.yiuitips` — Tips 提示
- `cn.etetet.yiui3ddisplay` — 3D 模型展示
- `cn.etetet.yiuieffect` — UI 特效
- `cn.etetet.packagemanager` — 包管理工具

### 间接修改的外部包（非依赖关系，而是 YIUIDemoWindow 的补丁操作）
- `cn.etetet.core/Scripts/Core/Share/World/EventSystem/EventSystem.cs` — 替换 `class` 为 `partial class`
- `cn.etetet.ui/Scripts/HotfixView/Client/UIComponentSystem.cs` — 注释特定赋值行

### 被依赖关系
- 本包为 Demo 演示包，通常不被其他包依赖
- 开发者可将其作为项目 UI 集成的参考模板

---

## 注意事项

1. **代码分层（手写 vs 自动生成）**：`YIUIGen` 目录下文件由 YIUI 工具自动生成，标注"请勿修改"，修改会在下次生成时被覆盖。业务逻辑应写在 `YIUISystem` 目录下。

2. **SceneType 绑定**：UI 事件处理类通过 `[Event(SceneType.StateSync)]` 或 `[Event(SceneType.Current)]` 与特定场景类型绑定，确保事件只在正确的场景中处理。`MainPanel` 的创建事件绑定的是 `SceneType.Current`，而非 `StateSync`。

3. **Panel 缓存配置**：三个面板均配置了 `TimeCache`（10 秒）+ `VisibleTween` 模式，关闭后不立即销毁，以支持平滑动画和快速重开。

4. **Demo 切换的 ET.sln 拷贝**：切换 Demo 模式时，工具会拷贝对应的 `ET.sln` 到项目根目录，需要关闭 IDE 再操作，否则文件可能被锁定。

5. **侵入性补丁**：`YIUIDemoWindow` 的 `ChangeFile()` 会修改 `cn.etetet.core` 和 `cn.etetet.ui` 中的源码文件（`ReplaceEventSystem` 和 `ReplaceUIComponentSystem`），这是超出本包范围的补丁操作，可能影响其他功能，需谨慎。

6. **LoginFinish 并发**：`LoginFinish` 事件触发两个并发处理器（`RemoveLoginUI` 和 `CreateLobbyUI`），ET 事件系统会并发执行两者，实现登录面板关闭与大厅面板打开的同步。

7. **`AssemblyReference.asmref`**：`HotfixView/Client` 下有 `asmref` 文件，说明本包的热更代码被纳入 ET 热更程序集，而非独立程序集，这是 ET 框架的标准热更代码组织方式。
