# cn.etetet.yiuiframework — YIUI Framework 核心框架

**版本**: 3.1.4
**命名空间**: `YIUIFramework`
**依赖**: `cn.etetet.core`, `cn.etetet.yiuiinvoke`
**文档**: https://lib9kmxvq7k.feishu.cn/wiki/ES7Gwz4EAiVGKSkotY5cRbTznuh

---

## 概述

`cn.etetet.yiuiframework` 是整个 YIUI UI 框架的核心基础层，提供：

1. **CDE 绑定系统**（Component/Data/Event 三表架构）—— UI 与逻辑解耦的核心机制
2. **数据绑定扩展**—— 将 UIData 值变化自动驱动 Unity 组件（显隐、文本、图片、动画等）
3. **事件绑定扩展**—— 将 Unity UI 事件（点击、拖拽、输入等）桥接到 ET 事件系统
4. **单例管理系统**（YIUISingleton / YIUIMgrCenter）—— 统一的管理器生命周期
5. **Panel 枚举体系**—— 层级、行为选项等所有 UI 状态枚举
6. **对象池系统**—— 多种池化工具减少 GC 压力
7. **编辑器自动化工具**（YIUIAutoTool）—— Odin Inspector 驱动的代码/资源生成工具
8. **常量配置**（YIUIConstAsset）—— 项目路径、屏幕尺寸、动画参数等全局配置
9. **Widget 组件**—— UIBlock、圆形裁剪、高斯模糊等运行时组件
10. **Invoke 事件结构体**—— 跨层通信的请求/响应结构体定义
11. **DotNet Source Generator**—— Roslyn 分析器，检测 YIUI 实体系统代码规范

---

## 目录结构

```
cn.etetet.yiuiframework/
├── package.json
├── Runtime/
│   ├── Core/
│   │   ├── YIUIBase/                   # 基础工具层
│   │   │   ├── Asset/                  # 常量配置资源（YIUIConstAsset）
│   │   │   ├── Cache/                  # 对象池（ListPool、DictionaryPool、RefPool 等）
│   │   │   ├── Disposer/               # IDisposer 接口与实现
│   │   │   ├── Extensions/             # Unity 类型扩展方法（Vector、Transform、Color 等）
│   │   │   ├── Helper/                 # UILogger、UIOperationHelper、UnityTipsHelper
│   │   │   └── Utils/                  # AppTick、StrUtil、MathUtil、PriorityQueue、Prefs 等
│   │   ├── YIUIBind/                   # CDE 绑定系统
│   │   │   ├── Code/
│   │   │   │   ├── CDE/                # UIBindCDETable（总表）+ 生命周期 + Panel 特性
│   │   │   │   ├── Component/          # UIBindComponentTable（组件表）
│   │   │   │   ├── Data/               # UIBindDataTable、UIData、UIDataValue 系列
│   │   │   │   ├── Event/              # UIBindEventTable、UIEventBase、UIEventP0~P5
│   │   │   │   └── TaskEvent/          # 异步 TaskEvent（UITaskEventP0~P5）
│   │   │   ├── Extend/                 # 具体绑定实现
│   │   │   │   ├── Data/               # 数据驱动扩展（Active、Text、Image、Rect、Animation）
│   │   │   │   └── Event/              # 事件桥接扩展（Click、Drag、Input、Dropdown、Toggle）
│   │   │   └── Panel/                  # Panel 相关枚举、属性、Bind 数据结构
│   │   │       ├── Bind/               # YIUIAttribute、YIUIBindVo、ICodeGenerator
│   │   │       └── Code/Enum/          # EPanelLayer、EPanelOption、EWindowOption 等
│   │   ├── YIUIMono/                   # 纯 Unity MonoBehaviour 组件
│   │   │   └── Widget/                 # UIBlock、UICircle、YIUIClickEffect、GaussianBlur 等
│   │   └── YIUISingleton/              # 单例管理体系
│   │       ├── Code/                   # IYIUISingleton、YIUISingletonAttribute、YIUISingletonHelper
│   │       ├── Manager/                # IYIUIManager、YIUIMgrCenter（注册/Update/Dispose）
│   │       └── Singleton/              # YIUISingleton<T>、YIUIMonoSingleton<T>、YIUIDisposerMonoSingleton
│   └── Event/
│       ├── YIUIInvokeEvent.cs          # 所有 Invoke 请求结构体（加载/卸载/倒计时/协程锁等）
│       └── YIUIInvokeGetEvent.cs       # 带返回值的 Invoke 结构体
├── Editor/
│   ├── YIUIAutoTool/                   # 主工具窗口
│   │   └── Window/
│   │       ├── UICreate/               # UI 代码生成模块
│   │       ├── UICheck/                # Prefab/Script 检查模块
│   │       ├── UIETCode/               # ET 脚本生成模块
│   │       ├── UIMacro/                # 宏定义管理模块
│   │       ├── UIPublish/              # 资源发布模块
│   │       └── UISetting/              # 图集、其他设置模块
│   ├── AutoBaseTool/                   # Odin 序列化辅助
│   ├── DrawEditor/                     # Hierarchy 图标
│   ├── EditorSceneTools/               # 场景工具（图层快选、摄像机聚焦）
│   ├── MenuItem/                       # 右键菜单快捷操作
│   └── Toolbar/                        # Unity 工具栏扩展（YIUI 快捷入口）
└── DotNet~/
    └── SourceGenerator/                # Roslyn 分析器
        ├── Analyzer/                   # YIUIEntitySystemAnalyzer（YIUI0001/0002）
        ├── CodeFixer/                  # YIUIEntitySystemCodeFixProvider
        └── Config/                     # YIUIDefinition（诊断 ID 常量）
```

---

## 核心类/接口详解

### 一、CDE 总表系统

#### `UIBindCDETable` (sealed partial class : SerializedMonoBehaviour)
**路径**: `Runtime/Core/YIUIBind/Code/CDE/UIBindCDETable.cs`

YIUI 每个 UI 预制体的根节点必须挂载此组件，是 UI 对象的"门面"（Facade）。

| 字段/属性 | 类型 | 说明 |
|---|---|---|
| `Entity` | `Entity` | 关联的 ET Entity（运行时注入） |
| `ComponentTable` | `UIBindComponentTable` | 组件表引用 |
| `DataTable` | `UIBindDataTable` | 数据表引用 |
| `EventTable` | `UIBindEventTable` | 事件表引用 |
| `PkgName` | `string` | UI 包名（只读） |
| `ResName` | `string` | 资源名（只读） |
| `AllChildCdeTable` | `List<UIBindCDETable>` | 编辑期：所有公共子组件 |
| `m_AllChildUIOwner` | `Dictionary<string, EntityRef<Entity>>` | 运行时：子 UI 的 Entity 映射 |
| `IsSplitData` | `bool` | 是否为源数据（面板拆分用） |
| `PanelSplitData` | `UIPanelSplitData` | 面板拆分数据（子界面父节点映射） |

**关键方法**:
- `InitializeCDE()` — 显式初始化三张表（解决同帧激活/关闭时 Awake 不触发的问题）
- `AddUIOwner(uiName, entity)` — 注册子 UI Entity
- `FindUIOwner(uiName)` / `FindUIOwner<T>(uiName)` — 查找子 UI Entity

**生命周期** (`UIBindCDETable_Life.cs`):
- `OnEnable` → 触发 `UIBaseOnEnable` Action
- `Start` → 触发 `UIBaseStart` Action
- `OnDisable` → 触发 `UIBaseOnDisable` Action
- `OnDestroy` → 触发 `UIBaseOnDestroy` Action

这些 Action 由 ET System 注入，将 Unity 生命周期桥接到 ET 实体系统。

---

#### `UIBindComponentTable` (sealed partial class : SerializedMonoBehaviour)
存储 UI 预制体上所有命名 Component 的字典（由编辑器工具自动生成）。

- `m_AllBindDic`: `Dictionary<string, Component>` — 名称→组件映射
- `FindComponent<T>(comName)` — 按名称查找组件

#### `UIBindDataTable` (sealed partial class : SerializedMonoBehaviour)
管理 UI 的所有 `UIData` 数据项，是数据响应式绑定的数据源。在 Awake 时递归初始化所有子节点上的 `UIDataBind` 组件。

#### `UIBindEventTable` (sealed partial class : SerializedMonoBehaviour)
管理 UI 的所有 `UIEventBase` 事件定义，在 Awake 时递归初始化所有子节点上的 `UIEventBind` 组件。

| 方法 | 说明 |
|---|---|
| `InitEventTable()` | 显式初始化（防重复） |
| `FindEvent(name)` / `FindEvent<T>(name)` | 查找指定事件 |
| `ClearEvent(name)` | 清除指定事件的监听器 |
| `ClearAllEvents()` | 清除所有事件（危险，运行时无需调用） |

---

#### `UIPanelSplitData`
**路径**: `Runtime/Core/YIUIBind/Code/CDE/UIPanelSplitData.cs`

面板拆分数据，用于面板的分块加载（每个 Panel 可声明其子界面结构）：

| 字段 | 说明 |
|---|---|
| `AllViewParent` | 所有通用子界面的父级 RectTransform |
| `AllCommonView` | 已存在于 Prefab 中、不需要创建的子界面列表 |
| `AllCreateView` | 需要在运行时动态创建的子界面列表 |
| `AllPopupViewParent` | 弹窗子界面的父级 |
| `AllPopupView` | 所有弹窗子界面列表 |

---

### 二、数据值系统

#### `UIDataBind` (abstract class : SerializedMonoBehaviour)
**路径**: `Runtime/Core/YIUIBind/Code/Data/Base/UIDataBind.cs`

所有数据绑定组件的抽象基类：
- 持有 `UIBindDataTable` 引用，通过 `FindData(name)` 查找数据项
- `Initialize(refresh)` — 由 `UIBindDataTable` 调用或自行调用
- 子类实现 `BindData()` / `UnBindData()` / `OnRefreshData()`

#### `UIDataBindBool` (abstract class : UIDataBindSelectBase)
Bool 类型绑定基类，支持**多变量逻辑运算**：
- `m_BooleanLogic`: `And` | `Or`（对多个 UIData 的结果执行与/或运算）
- 使用 `UIDataBoolRef` 列表存储多个 Bool 引用
- `GetResult()` — 返回逻辑运算最终结果

#### `UIDataValue` / `UIDataValueBase<T>` / 具体类型
支持的数据类型枚举（`EUIBindDataType`）:

| 类型 | 实现类 |
|---|---|
| Bool | `UIDataValueBool` |
| Int | `UIDataValueInt` |
| Long | `UIDataValueLong` |
| UInt | `UIDataValueUInt` |
| Float | `UIDataValueFloat` |
| String | `UIDataValueString` |
| Color | `UIDataValueColor` |
| Vector2 | `UIDataValueVector2` |
| Vector3 | `UIDataValueVector3` |
| ListInt | `UIDataValueListInt` |
| ListLong | `UIDataValueListLong` |

#### `UIData`
单个数据项：
- `Name`: 变量名
- `DataGuid`: 唯一 ID（`Guid.NewGuid().GetHashCode()`）
- `DataValue`: `UIDataValue` 实例

---

### 三、事件系统

#### `UIEventBase` (abstract partial class)
所有 UI 事件的基类：
- `EventName`: 事件名称
- `AllEventParamType`: 参数类型列表（`EUIEventParamType`）
- `IsTaskEvent`: 是否异步事件
- `Clear()`: 清除所有监听器

#### 泛型事件类族

| 类型 | 参数数量 | 说明 |
|---|---|---|
| `UIEventP0` ~ `UIEventP5` | 0~5 | 同步事件（Action 委托） |
| `UITaskEventP0` ~ `UITaskEventP5` | 0~5 | 异步事件（ETTask 委托） |
| `UIEventHandleP0` ~ `UIEventHandleP5` | 0~5 | 同步事件句柄 |
| `UITaskEventHandleP0` ~ `UITaskEventHandleP5` | 0~5 | 异步事件句柄 |

---

### 四、Panel 枚举体系（Round 2 新增）

#### `EPanelLayer` (enum)
UI 层级，**值不可修改（只能新增）**：

| 值 | 名称 | 用途 |
|---|---|---|
| 0 | Top | 最高层（新手引导等） |
| 1 | Tips | 提示层（飘字、确认弹窗、跑马灯） |
| 2 | Popup | 弹窗层（非全屏，可同时存在） |
| 3 | Panel | 面板层（全屏，受返回功能影响） |
| 4 | Scene | 场景层（2D 血条、头像等） |
| 5 | Bottom | 最低层 |
| 6 | Cache | 缓存层（不显示，强制隐藏） |

#### `EPanelOption` (Flags enum)
面板行为选项：

| 标志 | 说明 |
|---|---|
| `Container` | 容器类界面（如飘字） |
| `ForeverCache` | 永久缓存，关闭不销毁 |
| `TimeCache` | 倒计时缓存，关闭后 N 秒销毁 |
| `DisClose` | 禁止关闭（只能隐藏） |
| `IgnoreBack` | 忽略返回堆栈操作 |
| `IgnoreClose` | 可忽略非指向性关闭（如 GM 面板） |

#### `EPanelStackOption` (enum)
Panel 堆栈行为：

| 值 | 说明 |
|---|---|
| `None` | 不操作，叠加管理 |
| `Visible` | 显隐（不触发动画） |
| `VisibleTween` | 显隐（触发关闭/打开动画） |
| `Omit` | 省略：打开其他界面时自己关闭，且不进堆栈 |

#### `EViewStackOption` (enum)
View 堆栈行为（同 EPanelStackOption 但无 Omit）

#### `EWindowOption` (Flags enum)
窗口行为选项（最细粒度控制）：

| 标志 | 说明 |
|---|---|
| `CanUseBaseOpen` | 参数不匹配时允许用基础 Open |
| `BanParamOpen` | 禁止使用 ParamOpen |
| `HaveIOpenAllowOpen` | 有 IOpen 接口时仍允许用 Open |
| `FirstOpen` | 先开自己，再关其他（默认后开） |
| `LastClose` | 后关自己（默认先关） |
| `BanOpenTween` | 禁止所有打开动画 |
| `BanCloseTween` | 禁止所有关闭动画 |
| `BanRepetitionOpenTween` | 打开动画仅播放一次 |
| `BanRepetitionCloseTween` | 关闭动画仅播放一次 |
| `BanAwaitOpenTween` | 不等待打开动画完成 |
| `BanAwaitCloseTween` | 不等待关闭动画完成 |
| `SkipOtherOpenTween` | 我关闭时跳过其他界面的打开动画 |
| `SkipOtherCloseTween` | 我打开时跳过其他界面的关闭动画 |
| `AllowOptionByTween` | 动画播放时允许操作 |
| `WindowCloseTweenBefore` | 关闭事件在动画前触发 |

#### `YIUIAttribute` ([AttributeUsage(Class)])
标记 ET Component 与 UI 的关联关系：
```csharp
[YIUIAttribute(EUICodeType.Panel, EPanelLayer.Panel)]
public class LoginPanelComponent : Entity { }
```

#### `YIUIBindVo` (struct)
UI 资源绑定信息，用于运行时 Invoke 加载：
```csharp
public struct YIUIBindVo
{
    public EUICodeType CodeType;    // Panel/View/Common
    public Type ComponentType;       // ET Component 类型
    public string PkgName;           // 包名
    public string ResName;           // 资源名
    public EPanelLayer PanelLayer;   // 层级
}
```

---

### 五、单例管理系统（Round 2 新增）

#### `YIUISingleton<T>` (abstract class)
**路径**: `Runtime/Core/YIUISingleton/Singleton/YIUISingleton.cs`

基础单例，兼容 ET Entity 系统：

```csharp
public abstract class YIUISingleton<T> : IYIUIManagerAsyncInit
    where T : YIUISingleton<T>, new()
{
    public static T Inst { get; }     // 懒加载，初始化时自动注册
    public bool Disposed { get; }
    public bool Enabled { get; }
    public bool InitedSucceed { get; }

    protected virtual void OnInitSingleton() {}
    protected virtual void OnUseSingleton() {}   // 每次访问 Inst 时调用
    protected virtual async ETTask<bool> MgrAsyncInit() {}
    protected virtual void OnDispose() {}

    public static bool DisposeInst();  // 静态释放
    public bool Dispose();             // 实例释放
}
```

#### `YIUIMonoSingleton<T>` (abstract class : MonoBehaviour)
**路径**: `Runtime/Core/YIUISingleton/Singleton/Mono/YIUIMonoSingleton.cs`

绑定到 Unity GameObject 的单例，`YIUIMgrCenter` 就是此类型的派生。

#### `YIUIDisposerMonoSingleton<T>` (abstract class)
带 Disposer 模式的 Mono 单例，适用于需要在销毁时清理 ET 资源的管理器。

#### `YIUIMgrCenter` (class : YIUIMonoSingleton<YIUIMgrCenter>)
**路径**: `Runtime/Core/YIUISingleton/Manager/YIUIMgrCenter.cs`

全局管理器容器，是所有 `IYIUIManager` 的宿主：

| 方法 | 说明 |
|---|---|
| `Register(manager)` | 异步注册管理器，失败加入重试队列 |
| `GetFailInitedMgr()` | 出队一个失败的管理器 |
| `GetFailInitedCount()` | 获取失败数量 |
| `GetFailInitedMgrList()` | 获取所有失败管理器列表 |

内部 `MgrCore` 类处理 Update/LateUpdate/FixedUpdate 分发，以及 `Dispose` 时**倒序**释放所有管理器。

#### `YIUISingletonHelper` (static class)
全局单例注册表，统计所有 `IYIUISingleton` 实例：

| 方法/属性 | 说明 |
|---|---|
| `InitializeAll(entity)` | 初始化：按 `YIUISingletonAttribute.Order` 排序后依次注册 |
| `DisposeAll()` | 一键释放所有单例（退出游戏时无需调用） |
| `IsQuitting` | 是否正在退出游戏 |
| `Disposing` | 是否处于释放中 |
| `Count` | 当前单例数量 |

**自动注册机制**：`YIUISingletonAttribute` 标记的类会在 `InitializeAll` 时通过反射扫描并自动注册，按 `Order` 升序初始化。

#### `IYIUIManager` 接口族

| 接口 | 说明 |
|---|---|
| `IYIUIManager` | 基础接口：`Disposed`, `Enabled` |
| `IYIUIManagerAsyncInit` | 异步初始化：`ManagerAsyncInit()` |
| `IYIUIManagerUpdate` | Update 钩子：`ManagerUpdate()` |
| `IYIUIManagerLateUpdate` | LateUpdate 钩子：`ManagerLateUpdate()` |
| `IYIUIManagerFixedUpdate` | FixedUpdate 钩子：`ManagerFixedUpdate()` |

---

### 六、数据绑定扩展组件

#### `UIDataBindActive` (sealed class : UIDataBindBool)
支持 4 种过渡模式（`UITransitionModeEnum`）：
- **Instant**：立即 `SetActive`
- **Fade**：淡入淡出双向（`CanvasGroup.alpha`，Coroutine 实现）
- **FadeIn**：仅显示时淡入（隐藏时立即）
- **FadeOut**：仅隐藏时淡出（显示时立即）

> 编辑器模式下强制 Instant，不播动画

完整数据绑定扩展组件列表：

| 组件 | 功能 |
|---|---|
| `UIDataBindActive` | GameObject 显隐（支持 Instant/Fade/FadeIn/FadeOut 过渡） |
| `UIDataBindActiveComponent` | 指定 Behaviour 的 enabled 控制 |
| `UIDataBindActiveGameObjects` | 多个 GameObject 批量显隐 |
| `UIDataBindText` | uGUI Text 文本绑定 |
| `UIDataBindTextTMP` | TextMeshPro 文本绑定 |
| `UIDataBindImage` | Image sprite 绑定 |
| `UIDataBindImageFill` | Image fillAmount 绑定 |
| `UIDataBindColor` | 颜色绑定 |
| `UIDataBindRawImage` | RawImage texture 绑定 |
| `UIDataBindSlider` | Slider value 绑定 |
| `UIDataBindToggle` | Toggle isOn 绑定 |
| `UIDataBindScrollbar` | Scrollbar value 绑定 |
| `UIDataBindDropdown` | Dropdown value 绑定 |
| `UIDataBindVideoPlayer` | VideoPlayer 相关绑定 |
| `UIDataBindAnimation` | Animator 参数绑定 |
| `UIDataBindRectPos2/3` | RectTransform 位置绑定（2D/3D） |
| `UIDataBindRectRot1/3` | RectTransform 旋转绑定 |
| `UIDataBindRectScale1/3` | RectTransform 缩放绑定 |
| `UIDataBindRectSize` | RectTransform 尺寸绑定 |
| `UIDataBindRectSizeWidth/Height` | 单轴尺寸绑定 |
| `UIDataBindChange` | 值变化时触发事件 |

---

### 七、事件绑定扩展组件

| 组件 | 功能 |
|---|---|
| `UIEventBindClick` | 点击事件 |
| `UIEventBindClickDown` / `ClickUp` | 按下/抬起事件 |
| `UIEventBindDoubleClick` | 双击事件 |
| `UIEventBindClickPointerEventData` | 带 PointerEventData 的点击 |
| `UIEventBindClickInt` / `ClickString` | 带参数点击 |
| `UITaskEventBindClick` 系列 | 异步版点击事件 |
| `UIEventBindDrag/BeginDrag/EndDrag` | 拖拽事件 |
| `UIEventBindPress` / `KeepPress` | 按压 / 持续按压 |
| `UIEventBindInputField` / `InputFieldEnd` | 输入框事件（uGUI/TMP） |
| `UIEventBindDropdown` / `DropdownTMP` | 下拉框事件 |
| `UIEventBindToggle` | Toggle 切换事件 |
| `UIEventBindSlider` | Slider 变化事件 |
| `UIEventBindScrollbar` | Scrollbar 变化事件 |
| `UIEventBindActive` / `UITaskEventBindActive` | GameObject 激活/关闭事件 |
| `UIEventBindChangeDataValue` | 数据值变化事件 |

---

### 八、Invoke 事件结构体（Round 2 新增）

**路径**: `Runtime/Event/YIUIInvokeEvent.cs`

所有结构体通过 ET Invoke 系统跨层传递请求，无需直接引用实现层：

**资源加载/卸载**:
| 结构体 | 功能 |
|---|---|
| `YIUIInvokeEntity_LoadInstantiateByVo` | 按 BindVo 加载并实例化 UI |
| `YIUIInvokeEntity_InstantiateGameObject` | 实例化 GameObject |
| `YIUIInvokeEntity_ReleaseInstantiate` | 回收实例化资源 |
| `YIUIInvokeEntity_Release` | 回收 Unity 资源 |
| `YIUIInvokeEntity_Load` | 加载任意资源 |
| `YIUIInvokeEntity_LoadSprite` | 加载 Sprite |
| `YIUIInvokeEntity_ReleaseSprite` | 回收 Sprite |
| `YIUIInvokeEntity_LoadTexture2D` | 加载 Texture2D |
| `YIUIInvokeEntity_GetAssetInfo` | 按路径获取资产信息 |
| `YIUIInvokeEntity_GetAssetInfoByGUID` | 按 GUID 获取资产信息 |

**操作控制**:
| 结构体 | 功能 |
|---|---|
| `YIUIInvokeEntity_BanLayerOptionForever` | 屏蔽所有 YIUI 操作 |
| `YIUIInvokeEntity_RecoverLayerOptionForever` | 恢复屏蔽操作（传入 ForeverCode） |

**异步工具**:
| 结构体 | 功能 |
|---|---|
| `YIUIInvokeEntity_CountDownAdd` | 添加倒计时（支持循环、开始回调） |
| `YIUIInvokeEntity_CountDownRemove` | 移除倒计时 |
| `YIUIInvokeEntity_WaitFrameAsync` | 等待一帧（1ms） |
| `YIUIInvokeEntity_WaitAsync` | 等待指定毫秒（支持 CancellationToken） |
| `YIUIInvokeEntity_WaitSecondAsync` | 等待指定秒 |
| `YIUIInvokeEntity_CoroutineLock` | 协程锁 |

---

### 九、YIUIMono Widget 组件（Round 2 新增）

**路径**: `Runtime/Core/YIUIMono/Widget/`

| 组件 | 功能 |
|---|---|
| `UIBlock` | 不可见的射线阻挡组件（继承 Graphic，无渲染，纯阻挡） |
| `UIBlockPolygon` | 多边形射线阻挡 |
| `UICircle` | 圆形裁剪遮罩 |
| `YIUIClickEffect` | 点击特效触发 |
| `YIUIClickEventPenetration` | 点击事件穿透（允许点击穿透到下层） |
| `YIUIRectFactory` | RectTransform 工厂工具 |
| `ContentSizeFilterByChildren` | 根据子节点数量调整容器尺寸 |
| `ContentSizeFilterByRect` | 根据 Rect 调整容器尺寸 |
| `ResolutionAdapter` | 分辨率适配 |
| `YIUICameraGaussianBlur` | 相机高斯模糊（后处理） |
| `YIUICameraMotionBlur` | 相机运动模糊（后处理） |
| `DontDestroyOnLoadSelf` | 跨场景不销毁 |
| `YIUIReleaseInstantiate` | 释放实例化资源的辅助组件 |

---

### 十、对象池系统

| 类型 | 说明 |
|---|---|
| `ListPool<T>` | 线程安全的 `List<T>` 池（lock 保护） |
| `DictionaryPool<K,V>` | `Dictionary<K,V>` 池 |
| `HashSetPool<T>` | `HashSet<T>` 池 |
| `StackPool<T>` | `Stack<T>` 池 |
| `LinkedListPool<T>` | `LinkedList<T>` 池 |
| `SbPool` | `StringBuilder` 池 |
| `ObjectPool<T>` | 通用对象池（Stack 实现） |
| `RefPool` | 引用类型池（按 Type 分组，实现 `IRefPool` 接口） |
| `SimplePool<T>` | 简单对象池 |
| `ObjCache<T>` | 带容量上限的同步对象缓存 |
| `ObjAsyncCache<T>` | 异步对象缓存 |
| `AutoListPool<T>` | using 块自动回收的列表池 |
| `AutoRecycleObjPool<T>` | using 块自动回收的对象池 |

---

### 十一、常量与配置

#### `YIUIConstAsset` (partial class)
全局配置 ScriptableObject，通过 Odin Inspector 编辑：

| 分组 | 关键配置 |
|---|---|
| 项目配置 | 命名空间、根目录名、各种路径模板 |
| 基础设置 | 源文件拆分是否保留、CDE Inspector 模式 |
| Root | 屏幕设计分辨率（1920×1080）、层级偏移 |
| 安全区 | 刘海屏黑边、安全区 X/Y |
| 动画 | DOTween 默认缩放、全局禁用动画 |
| AI | OpenAI 客户端名称配置 |

---

### 十二、基础工具（补充）

#### `UILogger` (static class)
统一日志封装，代理 `UnityEngine.Debug`，Editor 下支持自动选中报错对象。

#### `AppTick` (partial class)
解决 `Environment.TickCount` 溢出问题的全局毫秒计数器，支持 24.9 天不翻转。**非线程安全**（性能考虑未加锁）。

#### `UIOperationHelper` (static class)
运行时/编辑器状态判断辅助：
- `IsPlaying()` — 是否运行中
- `CommonShowIf()` — Odin Inspector 显示条件
- `CheckUIOperationAll()` — 检查所有 UI 操作状态

#### `CountDownTimerCallback` (delegate)
```csharp
public delegate void CountDownTimerCallback(
    double residueTime,   // 剩余时间
    double elapseTime,    // 已过去时间
    double totalTime      // 总时间
);
```

#### `Prefs` (static class)
`PlayerPrefs` 封装，简化存取操作。

#### `AssemblyHelper` (static class)
反射辅助，支持 `GetClassesWithAttribute<T>()` 扫描全程序集标记特定 Attribute 的类型。

---

### 十三、编辑器工具 YIUIAutoTool

**路径**: `Editor/YIUIAutoTool/Window/YIUIAutoTool.cs`

基于 Odin Inspector `OdinMenuEditorWindow` 实现的多模块工具窗口，入口菜单：`ET/YIUI 自动化工具`。

**模块动态注册机制**：使用 `[YIUIAutoWindowAttribute]` 特性标记模块类，工具窗口启动时通过反射扫描所有程序集自动注册。

内置模块：

| 模块 | 功能 |
|---|---|
| `UIPublishModule` | UI 资源发布与包管理 |
| `UICreateModule` | UI 代码自动生成（Panel/View/Common/System） |
| `UICheckModule` | Prefab/Script 规范检查（含过滤器 EYIUICheckPrefabFiltrate / EYIUICheckScriptFiltrate） |
| `UIETCodeModule` | ET 组件代码生成 |
| `YIUIMacroModule` | ET/Unity 宏定义管理 |
| `UIConstModule` | 常量配置 |
| `YIUIUnityIconsModule` | Unity 内置图标浏览 |

**代码生成能力** (`UICreate/` 系列)：
- `UICreateBaseCode` — 基础代码框架（变量声明、Bind、方法桩）
- `UICreateSystemGenCode` — ET System 自动生成
- `UICreateCommonComponentGenCode` — 公共组件 Gen 代码
- `TemplateEngine` — 基于模板文件的代码生成引擎

**Toolbar 扩展**:
- `YIUIAutoToolBar` — 工具栏快捷入口按钮
- `YIUICLIToolBar` — CLI 工具栏按钮
- `YIUIToolbarExtender` — 工具栏扩展机制

---

### 十四、Source Generator (Roslyn)

| 诊断 ID | 说明 |
|---|---|
| `YIUI0001` | YIUI 实体系统分析规则（方法签名/继承不合规） |
| `YIUI0002` | 需要 `SystemOf` 特性的方法缺失特性 |

`YIUIEntitySystemCodeFixProvider` 提供自动修复建议。

---

## 架构模式

### CDE 三表模式
```
预制体 (GameObject)
└── UIBindCDETable              ← 门面，持有 ET Entity 引用
    ├── UIBindComponentTable    ← 所有 UI 组件的名称字典
    ├── UIBindDataTable         ← 所有数据变量（UIData）
    └── UIBindEventTable        ← 所有 UI 事件定义（UIEventBase）
```

### 数据响应链
```
ET 逻辑代码
  → 修改 UIData.DataValue 的值
  → UIDataBind 监听者触发 OnValueChanged()
  → 驱动 Unity 组件（Text、Image、GameObject.SetActive 等）

特殊场景（UIDataBindActive）：
  → 支持 Instant / Fade / FadeIn / FadeOut 四种过渡
  → Fade 通过 CanvasGroup + Coroutine 实现
```

### 事件回调链
```
Unity UI 事件（Button.onClick 等）
  → UIEventBind 组件捕获
  → 注册到 UIBindEventTable 的 UIEventBase
  → ET System 通过 FindEvent() 获取并订阅
  → 用户交互 → ET 逻辑响应
```

### 单例管理链
```
YIUISingletonHelper.InitializeAll(entity)
  → 反射扫描 [YIUISingletonAttribute] 类
  → 按 Order 排序
  → 依次调用 YIUIMgrCenter.Inst.Register(manager)
  → MgrCore.Add() → ManagerAsyncInit() → 注册 Update 钩子
```

### 生命周期桥接
```
Unity MonoBehaviour
  OnEnable / Start / OnDisable / OnDestroy
    ↓ Action 委托
  ET Entity System
    对应逻辑方法（打开/初始化/关闭/销毁）
```

---

## 关键流程

### UI 打开流程
1. ET 系统通过 Invoke 发送 `YIUIInvokeEntity_LoadInstantiateByVo`
2. 加载系统（`cn.etetet.yiuiyooassets`）加载预制体并实例化
3. 创建 ET Entity 并注入到 `UIBindCDETable.Entity`
4. 调用 `InitializeCDE()` 确保三表初始化
5. `UIBaseOnEnable` / `UIBaseStart` 触发 ET 系统的初始化逻辑

### 单例初始化流程
1. 游戏启动时调用 `YIUISingletonHelper.InitializeAll(etEntity)`
2. 反射找到所有标记 `[YIUISingletonAttribute]` 的类
3. 按 `Order` 升序排列后，逐个调用 `YIUIMgrCenter.Inst.Register`
4. `MgrCore` 调用 `IYIUIManagerAsyncInit.ManagerAsyncInit()` 做异步初始化
5. 初始化成功后注册进 Update/LateUpdate/FixedUpdate 列表

### 代码生成流程（编辑器）
1. 设计师在 Prefab 上配置好组件/数据/事件绑定
2. 打开 `YIUIAutoTool → UICreate`
3. 工具读取 `UIBindCDETable` 信息
4. 通过 `TemplateEngine` 生成：
   - `*Gen.cs`（自动生成，不可手改）
   - `*Component.cs`（ET Component，数据定义）
   - `*System.cs`（ET System，逻辑骨架）

---

## 代码示例

### 示例 1：注册自定义管理器

```csharp
// 标记为自动注册，Order=100
[YIUISingletonAttribute(Order = 100)]
public class MyGameManager : YIUISingleton<MyGameManager>
{
    protected override async ETTask<bool> MgrAsyncInit()
    {
        // 异步初始化逻辑
        await ETTask.CompletedTask;
        return true;
    }

    protected override void OnDispose()
    {
        // 清理
    }
}
```

### 示例 2：打开 UI 面板

```csharp
// 通过 Invoke 请求加载 UI
var vo = new YIUIBindVo
{
    CodeType = EUICodeType.Panel,
    ComponentType = typeof(LoginPanelComponent),
    PkgName = "LoginUI",
    ResName = "LoginPanel",
    PanelLayer = EPanelLayer.Panel,
};
await entity.YIUIInvokeAsync<YIUIInvokeEntity_LoadInstantiateByVo, bool>(
    new YIUIInvokeEntity_LoadInstantiateByVo { BindVo = vo, ... });
```

### 示例 3：响应 UI 事件

```csharp
// ET System 中订阅按钮点击
var btnEvent = cdeTable.EventTable.FindEvent<UIEventP0>("BtnConfirm");
if (btnEvent != null)
{
    btnEvent.AddListener(() => { /* 点击逻辑 */ });
}
```

### 示例 4：使用 AutoListPool 避免 GC

```csharp
using (var list = AutoListPool<int>.Get())
{
    list.Add(1);
    list.Add(2);
    // 离开 using 块自动归还
}
```

### 示例 5：EPanelOption 位运算操作

```csharp
var option = EPanelOption.ForeverCache;
option.Set(EPanelOption.IgnoreBack);   // 扩展方法
option.Unset(EPanelOption.ForeverCache);
```

---

## 依赖关系

```
cn.etetet.yiuiframework
  依赖 → cn.etetet.core           (ET 核心：Entity/Component/System/ETTask)
  依赖 → cn.etetet.yiuiinvoke     (YIUI Invoke 接口定义)
  依赖 → Sirenix.Odin.*           (Odin Inspector 序列化与 Editor UI)
  依赖 → TextMeshPro              (TMP 文本组件)
  依赖 → DOTween                  (动画，YIUIConstAsset 中配置参数)

  被依赖 ← cn.etetet.yiui         (YIUI 主框架：Panel/View/Common 管理)
  被依赖 ← cn.etetet.yiuigm       (GM 工具)
  被依赖 ← cn.etetet.yiuireddot   (红点系统)
  被依赖 ← cn.etetet.yiuieffect   (特效)
  被依赖 ← cn.etetet.yiuitips     (Tips 系统)
  被依赖 ← cn.etetet.yiui3ddisplay (3D 展示)
  被依赖 ← 其他所有 YIUI 子系统
```

---

## 注意事项

1. **`UIBindCDETable.InitializeCDE()`**：同一帧内先激活再立刻禁用时，`Awake` 不会触发，需手动调用此方法确保三表初始化。

2. **ListPool 线程安全**：`ListPool<T>` 的 `Get/Put` 使用了 `lock`，但回收后不应继续使用已 Put 的列表。

3. **EventTable.ClearAllEvents()**：文档注释标注"危险！运行时没这个需求"，仅用于编辑器或调试场景。

4. **AppTick 线程不安全**：注释明确说明为性能考虑未加锁，不应在多线程环境使用。

5. **生成代码（`*Gen.cs`）不可手动修改**：每次运行 `YIUIAutoTool` 的代码生成功能都会覆盖这些文件。

6. **EPanelLayer / EPanelOption 等枚举值不可修改**：已有界面依赖这些数值做序列化存储，修改会导致已存在界面配置错误，只允许新增。

7. **`YIUISingletonHelper.DisposeAll()` 慎用**：设计为"不退出游戏但需要完整重置"时才调用，正常退出游戏无需调用。

8. **`YIUISingleton<T>.OnUseSingleton()`**：每次访问 `Inst` 都会触发，不应在此做重量级操作。

9. **UIDataBindActive 的 Fade 模式**：内部使用 Coroutine + CanvasGroup，多次快速切换时会 `StopAllCoroutines()` 打断前一次过渡，确保状态一致。

10. **YIUIInvokeEvent 结构体**：所有 Invoke 通信均为值类型（struct），避免 GC 分配，是框架的关键设计决策。

---

## Round 3 深化分析：跨 Package 交互与边界异常处理

### 一、跨 Package 交互详解

#### 1.1 与 `cn.etetet.core` 的交互

`cn.etetet.core` 提供了 ET 框架的核心：`Entity`/`Component`/`System`/`ETTask`，`yiuiframework` 对其的依赖体现在以下几个关键连接点：

| 连接点 | 说明 |
|---|---|
| `EntityRef<Entity>` | `UIBindCDETable.m_EntityRef` 用 `EntityRef` 持有 ET Entity，防止直接引用导致的悬挂指针 |
| `ETTask<bool>` | `YIUISingleton<T>.MgrAsyncInit()`、`YIUIMgrCenter.Register()` 使用 ETTask 做异步初始化 |
| `ETCancellationToken` | `YIUIInvokeEntity_WaitAsync`/`WaitSecondAsync` 携带取消令牌，与 ET 协程系统集成 |
| `Logger` / `Debug` | `UIEventBind` 中引用 ET 的 `Logger.LogErrorContext`，而 `UIBindCDETable_Life` 则直接用 `Debug.LogError` |

**关键边界**：`UIBindCDETable.Entity` 属性通过 `EntityRef<Entity>` 间接访问。若关联的 Entity 已被释放（Disposed），`m_EntityRef` 会返回 `null`，生命周期回调（OnEnable/Start 等）中的 ET System 逻辑必须先判断 Entity 是否有效。

#### 1.2 与 `cn.etetet.yiuiinvoke` 的交互

`cn.etetet.yiuiinvoke` 定义了 YIUI Invoke 的接口规范（扩展方法 `YIUIInvokeAsync`/`YIUIInvoke`），`yiuiframework` 在此基础上定义所有请求/响应结构体（`YIUIInvokeEvent.cs`、`YIUIInvokeGetEvent.cs`）。

**调用链路**（以打开 UI 为例）：
```
ET System (cn.etetet.yiui)
  → entity.YIUIInvokeAsync<YIUIInvokeEntity_LoadInstantiateByVo, bool>(...)
      ↓ [cn.etetet.yiuiinvoke 扩展方法]
  → IYIUIInvokeHandler 实现（在 cn.etetet.yiuiyooassets 中注册）
      ↓ 加载 Prefab 并实例化
  → 注入 UIBindCDETable.Entity
      ↓
  → InitializeCDE() → 三表初始化
      ↓
  → OnEnable/Start 生命周期触发 ET System 逻辑
```

**关键设计**：结构体定义（`yiuiframework`）和结构体处理（其他包）完全分离，`yiuiframework` 不依赖具体加载实现，只定义协议。

#### 1.3 与 `cn.etetet.yiui`（主框架）的交互

`cn.etetet.yiui` 是 `yiuiframework` 的最主要消费者，负责实现 Panel/View/Common 的完整生命周期管理。`yiuiframework` 为其提供：

- **`YIUIAttribute`**：标记 Panel/View/Common ET Component 类型及其层级
- **`YIUIBindVo`**：运行时传递 UI 资源绑定信息
- **`EPanelLayer/EPanelOption/EWindowOption`** 等枚举：控制 UI 行为
- **`UIBindCDETable`**：每个预制体的门面，`cn.etetet.yiui` 的 System 通过此访问三表
- **`YIUIInvokeEvent` 结构体**：作为跨层通信协议

`cn.etetet.yiui` 的 System 典型流程：
```csharp
// 在 cn.etetet.yiui 的 PanelSystem 中
var cdeTable = panelComponent.GetParent<Entity>().GetComponent<UIBindCDETable>();
// 通过 ComponentTable 获取 UI 组件引用
var btnConfirm = cdeTable.ComponentTable.FindComponent<Button>("BtnConfirm");
// 通过 EventTable 绑定事件
var clickEvent = cdeTable.EventTable.FindEvent<UIEventP0>("OnBtnConfirmClick");
clickEvent?.AddListener(OnConfirmClicked);
// 通过 DataTable 驱动 UI 更新
var scoreData = cdeTable.DataTable.FindData("Score");
scoreData.DataValue.SetValue(100);
```

#### 1.4 与 `cn.etetet.yiuiyooassets`（资产加载）的交互

`yiuiframework` 不直接引用 `yiuiyooassets`，通过 Invoke 解耦：
- `YIUIInvokeEntity_Load*` 系列结构体定义在 `yiuiframework`
- `yiuiyooassets` 注册对应的 Invoke Handler，实现实际加载

**边界情况**：
- 若 `YIUIInvokeEntity_LoadInstantiateByVo` 的 Handler 未注册（`yiuiyooassets` 未初始化），Invoke 调用会静默失败或抛异常，UI 无法打开
- `YIUIInvokeEntity_GetAssetInfo` / `GetAssetInfoByGUID` 支持按路径或 GUID 查询，需确保资源已在 Addressables/YooAsset 中配置

#### 1.5 与 Odin Inspector 的交互

`yiuiframework` 深度集成 Odin，三个 CDE 表均继承 `SerializedMonoBehaviour`（Odin 提供），`UIData`/`UIEventBase` 等通过 `[OdinSerialize]` 序列化。

**边界风险**：
- Odin 序列化的数据在 `[NonSerialized]` + `[OdinSerialize]` 组合使用时，需要 Odin 的序列化系统正确运行；若 Odin 版本不匹配，可能导致 `m_AllChildUIOwner` 等字段丢失
- 编辑器模式下 `UIEventBind` 标注了 `[ExecuteInEditMode]`，在编辑模式变更时会自动 `OnRefreshEvent()`，频繁操作可能有性能开销

---

### 二、异常处理机制详析

#### 2.1 生命周期异常处理（`UIBindCDETable_Life.cs`）

```csharp
private void OnEnable()
{
    try { UIBaseOnEnable?.Invoke(); }
    catch (Exception e) { Debug.LogError(e); throw; }  // ← 重抛异常
}
// OnDisable / Start 同理

private void OnDestroy()
{
    try { UIBaseOnDestroy?.Invoke(); }
    catch (Exception e) { Debug.LogError(e); }  // ← OnDestroy 不重抛（避免 Unity 析构异常级联）
}
```

**关键差异**：`OnEnable`/`Start`/`OnDisable` **重抛异常**（throw），而 `OnDestroy` **吞异常**（只 LogError 不 throw）。这是有意设计：
- 销毁阶段已无法恢复，重抛只会污染 Unity 的析构流程
- 开启/禁用阶段重抛可以让上层感知失败并进行补救

#### 2.2 单例初始化异常处理（`YIUISingletonHelper.RegisterAll`）

```csharp
try
{
    instValue = instProperty.GetValue(null);
}
catch (Exception e)
{
    Debug.LogError($"类型{singleton.Name}的Inst属性获取失败 {e}");
    continue;  // ← 跳过失败的单例，继续其他单例初始化
}
```

失败单例会被 `YIUIMgrCenter.m_FailInited` 队列存储，调用方可通过 `GetFailInitedMgr()` 重试：
```csharp
// 重试失败的管理器
while (YIUIMgrCenter.Inst.GetFailInitedCount() > 0)
{
    var failedMgr = YIUIMgrCenter.Inst.GetFailInitedMgr();
    await YIUIMgrCenter.Inst.Register(failedMgr);
}
```

#### 2.3 引用池异常处理（`RefPool.cs`）

| 场景 | 处理方式 |
|---|---|
| `Put(null)` | `Debug.LogError` + return false |
| `Get(Type)` 类型不是非抽象类 | `Debug.LogError` + return null |
| `Get(Type)` 类型未实现 `IRefPool` | `Debug.LogError` + return null |
| 并发访问 | `lock(s_RefCollections)` 保护 |

#### 2.4 UIBindCDETable 子 UI 所有者异常处理

```csharp
internal void AddUIOwner(string uiName, Entity uiBase)
{
    if (!this.m_AllChildUIOwner.TryAdd(uiName, uiBase))
    {
        Debug.LogError($"{name} 已存在 {uiName} 请检查为何重复添加");
    }
}

public Entity FindUIOwner(string uiName)
{
    if (this.m_AllChildUIOwner.TryGetValue(uiName, out EntityRef<Entity> owner))
        return owner;
    Debug.LogError($"{this.name} 不存在 {uiName} 请检查");
    return null;  // ← 返回 null，调用方需判空
}
```

**注意**：`FindUIOwner` 返回 null 时不抛异常，调用方如不判空会产生 NullReferenceException。

#### 2.5 UIEventBind 事件获取异常处理

```csharp
private UIEventBase GetEvent(string eventName)
{
    if (string.IsNullOrEmpty(eventName)) return null;        // 空名称静默返回
    if (m_EventTable == null)
    {
        Logger.LogErrorContext(this, $"事件表==null 请检查");
        return null;
    }
    var uiEvent = m_EventTable.FindEvent(eventName);
    if (uiEvent == null)
        Logger.LogErrorContext(this, $"没找到事件 {eventName}"); // 找不到报错但不抛
    return uiEvent;
}
```

#### 2.6 退出时的安全保护

`YIUISingletonHelper` 对 `Application.quitting` 事件监听，设置 `IsQuitting = true`，后续的 `InitializeAll` 和 `DisposeAll` 会提前返回，避免退出过程中的非法访问。

---

### 三、边界情况总结

| 边界场景 | 现象 | 处理方式 |
|---|---|---|
| 同帧激活-关闭 UI | Awake 不触发，三表未初始化 | 手动调用 `InitializeCDE()` |
| Entity 已 Disposed 时触发生命周期 | ET System 访问 null Entity | 生命周期中需判断 `entity != null` |
| 单例 `Inst` 属性反射失败 | 单例未初始化，进入失败队列 | 通过 `GetFailInitedMgr()` 重试 |
| Invoke Handler 未注册 | UI 打开无响应 | 确保 `yiuiyooassets` 在 `yiuiframework` 之前完成初始化 |
| `FindUIOwner` 找不到子 UI | 返回 null，调用方崩溃 | 调用方需判空或使用安全版本 |
| `ListPool` 回收后继续使用 | 数据污染（多个持有者） | `Put` 后立即置 null |
| `AppTick` 在子线程使用 | 数据竞争（无锁） | 仅在主线程访问 |
| `UIDataBindActive` 快速多次切换 | 前一次 Fade 动画被打断 | `StopAllCoroutines()` 确保状态一致 |
| Odin 版本不匹配 | `[NonSerialized][OdinSerialize]` 字段丢失 | 保持 Odin 版本与框架要求一致 |
| 枚举值序列化后修改 | 已存界面配置错误 | EPanelLayer/EPanelOption 等枚举**只允许新增** |

---

### 四、`YIUIConstAsset` 路径配置与多包协作

`YIUIConstAsset` 中的路径模板（如 `UIETCreatePackagePath`、`UIETComponentPath`）通过 `{0}` 占位符支持指向任意包。`YIUIAutoTool` 的代码生成模块（`UICreateModule`、`UIETCodeModule`）在生成时会将 `{0}` 替换为目标包名：

```
生成路径模板: Assets/../Packages/cn.etetet.{0}/Scripts/ModelView/Client/YIUIGen
目标包名:     yiui
实际路径:     Assets/../Packages/cn.etetet.yiui/Scripts/ModelView/Client/YIUIGen
```

这使得 YIUI 框架可以将生成代码输出到任意 Package，实现框架代码与业务代码的物理隔离。

---

### 五、`MgrCore` 内部机制（补充）

**路径**: `Runtime/Core/YIUISingleton/Manager/YIUIMgrCenter_MgrCore.cs`（推测文件名）

`YIUIMgrCenter` 内部的 `MgrCore` 是真正的管理器容器：
- 维护三个 Update 列表（`IYIUIManagerUpdate`/`LateUpdate`/`FixedUpdate`）
- `Dispose` 时**倒序**释放（保证后注册的先释放，符合依赖顺序）
- `Add(manager)` 会先调用 `ManagerAsyncInit()`，成功后才加入 Update 列表

**边界**：若 `ManagerAsyncInit()` 返回 false，管理器不会加入 Update 列表，也不会触发 Update 钩子，但 `Dispose` 时不会对其调用 dispose（因为未加入管理列表）。开发者需在失败重试成功后手动 Register。

