# cn.etetet.yiui3ddisplay

## 概述

**版本**: 3.1.0
**包类型 ID**: `PackageType.YIUI3DDisplay = 1303`
**分类**: UI/YIUI
**依赖**: `cn.etetet.yiuiframework >= 3.0.0`
**描述**: YIUI 框架中用于在 UI 界面内展示 3D 模型的功能包。通过 RenderTexture + 专用摄像机的方式，将 3D 模型渲染到 RawImage 上，实现在 2D UI 中显示 3D 对象的效果。

---

## 目录结构

```
cn.etetet.yiui3ddisplay/
├── Runtime/                          # 运行时核心 Mono 组件
│   ├── UI3DDisplay.cs                # 主 Mono 组件（挂载在 UI 预制上）
│   ├── UI3DDisplayCamera.cs          # 摄像机控制器（管理渲染层）
│   ├── UI3DDisplayRecord.cs          # 渲染层记录组件（记录原始层信息）
│   └── YIUIConstAsset_3DDisplay.cs   # 常量配置扩展（3D层名称）
├── Scripts/
│   ├── Model/Share/
│   │   └── PackageType.cs            # 包类型 ID 定义（YIUI3DDisplay = 1303）
│   ├── ModelView/Client/
│   │   ├── Display/
│   │   │   ├── YIUI3DDisplayChild.cs        # ECS Entity 主定义（实现多接口）
│   │   │   ├── YIUI3DDisplayChild_Base.cs   # 基础字段（渲染纹理、层级、旋转等）
│   │   │   ├── YIUI3DDisplayChild_Event.cs  # 事件字段（拖拽、点击）
│   │   │   └── YIUI3DDisplayChild_Multiple.cs # 多目标模式字段
│   │   ├── Event/
│   │   │   ├── YIUI3DDisplayClick.cs        # 点击回调接口 & 抽象基类
│   │   │   └── YIUI3DDisplayClickHelper.cs  # 点击事件分发 helper
│   │   ├── GM/
│   │   │   └── EGMType_3DDisplay.cs         # GM 分类枚举扩展
│   │   ├── YIUIComponent/ModelDisplay/
│   │   │   └── ModelDisplayDemoViewComponent.cs  # Demo 组件定义（手写部分）
│   │   └── YIUIGen/ModelDisplay/
│   │       └── ModelDisplayDemoViewComponentGen.cs  # 自动生成代码（勿修改）
│   └── HotfixView/Client/
│       ├── Display/
│       │   ├── YIUI3DDisplayChildSystem.cs        # System 主入口（Awake/Destroy）
│       │   ├── YIUI3DDisplayChildSystem_Base.cs   # 核心渲染逻辑
│       │   ├── YIUI3DDisplayChildSystem_API.cs    # 公开 API（清除/旋转/位移/缩放）
│       │   ├── YIUI3DDisplayChildSystem_Async.cs  # 异步加载显示
│       │   ├── YIUI3DDisplayChildSystem_Sync.cs   # 同步加载显示（宏控制）
│       │   ├── YIUI3DDisplayChildSystem_Event.cs  # 输入事件处理（拖拽/点击）
│       │   ├── YIUI3DDisplayChildSystem_Mono.cs   # 生命周期（Enable/Disable/LateUpdate）
│       │   └── YIUI3DDisplayChildSystem_Multiple.cs # 多目标模式逻辑
│       ├── GM/
│       │   └── GM_Command_3DDisplay.cs            # GM 命令（测试打开 Demo）
│       └── YIUISystem/ModelDisplay/
│           └── ModelDisplayDemoViewComponentSystem.cs # Demo 展示系统
├── Editor/
│   ├── MenuItem/YIUI3DDisplayMenuItem.cs  # 编辑器右键菜单（GameObject > YIUI > 3DDisplay）
│   └── TemplatePrefabs/YIUI3DDisplay.prefab # 模板预制体（编辑器快速创建用）
└── Assets/GameRes/YIUI/ModelDisplay/      # 示例资源（骨骼动画模型/材质/预制）
```

---

## 核心类与接口

### Mono 组件层（Runtime）

#### `UI3DDisplay` (YIUIFramework)
挂载在 UI 预制体上的主 Mono 组件，同时实现 `IDragHandler`、`IPointerDownHandler`、`IPointerUpHandler`。
继承 `SerializedMonoBehaviour`（Odin Inspector）。

| 字段 | 类型 | 说明 |
|------|------|------|
| `m_ShowObject` | `GameObject` | [动态] 当前展示的 3D 对象 |
| `m_LookCamera` | `Camera` | [动态] 参考摄像机（提供投影参数来源） |
| `m_ShowImage` | `RawImage` | [必须] RenderTexture 渲染目标 |
| `m_ShowCamera` | `Camera` | [必须] 专用渲染摄像机 |
| `m_ShowCameraCtrl` | `UI3DDisplayCamera` | [必须] 摄像机层级控制器 |
| `m_ShowLight` | `Light` | [必须] 专用渲染灯光 |
| `m_FitScaleRoot` | `Transform` | 自动缩放适配节点（补偿父级缩放） |
| `m_AutoChangeSize` | `bool` | 编辑器内自动设置 RawImage 尺寸（默认 true） |
| `m_ResolutionX/Y` | `int` | RenderTexture 分辨率（默认 512x512） |
| `m_RenderTextureDepthBuffer` | `int` | 深度缓冲位数（默认 16，不懂不要改） |
| `m_CanDrag` | `bool` | 是否允许拖拽旋转 |
| `m_DragSpeed` | `float` | 拖拽旋转速度（默认 10） |
| `m_ShowOffset` | `Vector3` | 显示对象位置偏移 |
| `m_ShowRotation` | `Vector3` | 显示对象初始旋转（欧拉角） |
| `m_ShowScale` | `Vector3` | 显示对象比例 |
| `m_ReflectionPlane` | `Transform` | 镜面反射面节点（跟随模型位置） |
| `m_ShadowPlane` | `Transform` | 阴影面节点（跟随模型位置） |
| `m_UseLookCameraColor` | `bool` | 使用参考摄像机背景色（否则透明） |
| `m_AutoSyncLookCamera` | `bool` | LateUpdate 自动同步摄像机位置旋转 |
| `m_MultipleTargetMode` | `bool` | 多目标模式（同一界面多个可点击模型） |
| `m_OnClickOffset` | `Vector2` | 点击容差（防手机误触，默认 50,50 像素） |
| `m_AutoSetColliderLayer` | `bool` | 自动把碰撞体设置到 YIUI3D 层（支持射线） |
| `m_YIUI3DDisplayChildRef` | `EntityRef<Entity>` | 关联的 ECS Entity 引用（双向绑定） |
| `OnClick` | `bool` | 是否开启点击事件（需手动调用 ResetOnClick） |

**Unity 事件转发**（Mono → ECS，通过 `YIUIInvokeSystem`）：
- `OnDrag(PointerEventData)` → `YIUI3DDisplayInvoke.OnDragInvoke`（条件：`m_CanDrag`）
- `OnPointerDown(PointerEventData)` → `YIUI3DDisplayInvoke.OnPointerDownInvoke`（条件：`OnClick`）
- `OnPointerUp(PointerEventData)` → `YIUI3DDisplayInvoke.OnPointerUpInvoke`（条件：`OnClick`）

**编辑器辅助**：`OnValidate()` 在 `m_AutoChangeSize == true` 时自动调整 `RectTransform.sizeDelta` 为分辨率值。

---

#### `UI3DDisplayCamera` (YIUIFramework)
管理 3D 对象渲染层，确保对象被专用摄像机正确渲染，与主摄像机隔离。

| 成员 | 说明 |
|------|------|
| `ShowObject { get; set; }` | 设置显示对象。旧对象自动 ResetRenderer（层归零），新对象自动 SetupRenderer |
| `ShowLayer { get; set; }` | 设置渲染层 index，变更时自动更新当前对象所有 Renderer |
| `SetupRenderer(Transform)` | 遍历所有子 Renderer，将其 gameObject.layer 设为 ShowLayer |
| `ResetRenderer(Transform)` | 遍历所有子 Renderer，将其 gameObject.layer 重置为 0 |

生命周期特殊处理：
- `OnEnable`：重新 SetupRenderer（防止摄像机重启后层丢失）
- `OnDisable`：ResetRenderer（避免 3D 对象被主摄像机误渲染）

使用 `ListPool<Renderer>` 避免 GC。

---

#### `UI3DDisplayRecord` (YIUIFramework)
附加在每个被渲染对象的节点上，记录原始层和渲染器可见性。

| 成员 | 说明 |
|------|------|
| `Initialize(Renderer, UI3DDisplayCamera)` | 记录原始层、可见性和关联摄像机 |
| `OnTransformParentChanged()` | 脱离摄像机层级时自动 SafeDestroySelf() |
| `OnDestroy()` | 自动恢复 Renderer 可见性和原始层 |

---

### ECS 层（Entity + System）

#### `YIUI3DDisplayChild` (ET.Client)
ECS Entity，标注 `[ChildOf]`，作为 UI Panel Entity 的子组件。

**实现接口**：
- `IAwake<UI3DDisplay>` — 传入 Mono 组件完成初始化
- `IDestroy` — 清理 RenderTexture 和引用
- `IYIUIEnable` / `IYIUIDisable` — Panel 显示/隐藏时申请/释放 RenderTexture
- `ILateUpdate` — 自动同步摄像机位置（`m_AutoSyncLookCamera`）

**字段汇总（Base 分部）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `m_UI3DDisplay` | `UI3DDisplay` | 关联的 Mono 组件 |
| `m_ShowCameraDefPos` | `Vector3` | ShowCamera 初始本地位置（用于多模型偏移计算） |
| `m_ShowCameraCtrl` | `UI3DDisplayCamera` | 摄像机控制器引用 |
| `m_ShowLayer` | `int` | YIUI3DLayer 的层 index |
| `m_DragRotation` | `float` | 拖拽累计旋转角度（单目标模式） |
| `m_ShowTexture` | `RenderTexture` | 当前使用的临时渲染纹理 |
| `m_ShowPosition` | `Vector3` | 记录显示位置（当前未用于逻辑，仅记录） |
| `m_OrthographicSize` | `float` | 正交摄像机大小（从 lookCamera 同步） |
| `g_DisPlayUIIndex` | `static int` | `[StaticField]` 全局实例计数器 |
| `m_ModelGlobalOffset` | `Vector3` | 模型 Y 方向全局偏移（`index * 100`） |
| `m_RenderList` | `List<Renderer>` | 已启用阴影投射的 Renderer 列表 |
| `m_ObjPool` | `Dictionary<string, GameObject>` | 按 resName 缓存的显示对象池 |
| `m_CameraPool` | `Dictionary<GameObject, Dictionary<string, Camera>>` | 按对象+摄像机名二级缓存 |

**字段汇总（Event 分部）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `m_DragTarge` | `GameObject` | 当前可拖拽目标（单目标=ShowObject，多目标=射线选中的） |
| `CanDrag` | `bool` | 映射到 m_UI3DDisplay.m_CanDrag |
| `m_OnClickDownPos` | `Vector2` | 按下时屏幕坐标（用于偏移检测） |
| `m_ClickRaycastHit` | `RaycastHit` | 点击射线检测结果 |
| `m_DragRaycastHit` | `RaycastHit` | 拖拽射线检测结果（多目标模式） |
| `m_OnClickedEntity` | `EntityRef<Entity>` | 点击回调触发实体（一般为 Panel Entity） |
| `YIUI3DDisplayClickTypeSystem` | `Type` | 动态构造 `IYIUI3DDisplayClick<T>` 泛型类型（惰性初始化） |
| `OnClick` | `bool` | 点击事件开关（同步 UI3DDisplay.OnClick） |

**字段汇总（Multiple 分部）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `MultipleTargetMode` | `bool` | 映射到 m_UI3DDisplay.m_MultipleTargetMode |
| `m_AllMultipleTarget` | `List<GameObject>` | 所有多目标对象列表 |
| `m_MultipleCache` | `Dictionary<GameObject, GameObject>` | 子对象 → 根目标的查找缓存 |
| `m_InitMultipleData` | `bool` | 多目标数据是否已初始化 |

---

#### `YIUI3DDisplayChildSystem` — 静态 partial 类群

通过 `[EntitySystemOf(typeof(YIUI3DDisplayChild))]` 注册，分多文件实现：

| 文件 | 主要方法 |
|------|---------|
| `_System.cs` | `Awake()`, `Destroy()` |
| `_Mono.cs` | `Awake3DDisplay()`, `Destroy3DDisplay()`, `LateUpdate()`, `YIUIEnable()`, `YIUIDisable()` |
| `_Base.cs` | `ShowByGameObject()`, `UpdateShowObject()`, `UpdateLookCamera()`, `SetTemporaryRenderTexture()`, 阴影管理 |
| `_API.cs` | `ClearShow()`, `ResetRotation()`, `SetRotation()`, `SetOffset()`, `SetScale()`, `ChangeResolution()`, `GetCamera()` |
| `_Async.cs` | `ShowAsync()`, `GetDisplayObjectAsync()`, `CreateObjectAsync()` |
| `_Sync.cs` | `ShowSync()`, `GetDisplayObject()`, `CreateObject()`（`#if !YIUIMACRO_SYNCLOAD_CLOSE`） |
| `_Event.cs` | `ResetOnClick()`, `Raycast()`, `OnDrag()`, `OnPointerDown()`, `OnPointerUp()`, `ClickSucceed()` |
| `_Multiple.cs` | `ResetMultipleTargetMode()`, `InitRotationData()`, `AddMultipleTarget()`, `RemoveMultipleTarget()`, `GetMultipleTargetByClick()` |

---

#### 点击回调系统

**`IYIUI3DDisplayClick`** (基础接口):
```csharp
void OnClick(Entity self, UI3DDisplay display, GameObject target, GameObject root);
```

**`IYIUI3DDisplayClick<T1>`** (泛型接口，用于类型系统注册):
```csharp
public interface IYIUI3DDisplayClick<in T1> : ISystemType, IYIUI3DDisplayClick { }
```

**`YIUI3DDisplayClickSystem<T1,T2,T3,T4>`** (抽象基类，业务层继承):
```csharp
[EntitySystem]
public abstract class YIUI3DDisplayClickSystem<T1, T2, T3, T4> : SystemObject, IYIUI3DDisplayClick<T1>
    where T1 : Entity, IYIUIBind, IYIUIInitialize
{
    protected abstract void YIUI3DDisplayClick(T1 self, UI3DDisplay display, GameObject target, GameObject root);
}
```

**`YIUI3DDisplayClickHelper`** (静态工具类):
通过 `EntitySystemSingleton.Instance.TypeSystems.GetSystems()` 查找已注册的事件系统并分发。

---

#### Demo 组件

**`ModelDisplayDemoViewComponent`** (`[ComponentOf(typeof(YIUIChild))]`):
- 自动生成部分（Gen）：声明 `u_ComDisplay`（`UI3DDisplay`），实现 `IYIUIBind`、`IYIUIInitialize`、`IYIUIOpen`
- 手写部分：持有 `m_Display`（`EntityRef<YIUI3DDisplayChild>`）

**`ModelDisplayDemoViewComponentSystem`**:
```csharp
// 初始化：创建 YIUI3DDisplayChild 子 Entity
self.m_Display = self.AddChild<YIUI3DDisplayChild, UI3DDisplay>(self.u_ComDisplay);

// 显示模型（带自定义摄像机名）
await self.Display.ShowAsync("DisplayDemoModel", "CustomCamera");
self.Display.ResetOnClick(true); // 开启点击事件

// 点击回调实现（通过 EntitySystem 注册）
[EntitySystem]
private static void YIUI3DDisplayClick(this ModelDisplayDemoViewComponent self,
    UI3DDisplay display, GameObject target, GameObject root)
{
    Log.Info($"点击模型 目标: {target.name}  根节点:{root.name}");
}
```

---

### 编辑器工具

#### `YIUI3DDisplayMenuItem` (Editor)
在 `GameObject > YIUI > 3DDisplay` 菜单中添加快捷创建入口，从模板预制克隆到当前选中对象下。

---

## 实现原理

### RenderTexture 渲染方案

```
UI Camera（主摄像机，cullingMask 不含 YIUI3DLayer）
    └── Canvas
          └── UI3DDisplay (RawImage，texture = RenderTexture)
                    │
                    ▼ 显示
              RenderTexture ◄── ShowCamera（cullingMask = 1 << YIUI3DLayer）
                                      │
                                      ▼ 渲染
                              3D Model GameObject
                              （Layer = YIUI3DLayer，Y 偏移 = index * 100 到屏幕外）
```

**5 个关键设计点**：
1. 3D 模型放置在专用 Layer（`YIUI3DLayer`，默认名 "YIUI3DLayer"）
2. 主摄像机 cullingMask **不包含**此层 → 3D 模型不被主摄像机渲染
3. ShowCamera cullingMask **只包含**此层 → 专门渲染 3D 模型
4. ShowCamera 渲染结果写入 RenderTexture，RawImage 显示该纹理
5. 每个实例的模型 Y 偏移 `index * 100`，确保多实例模型在世界空间中不重叠

---

### 全局偏移机制

```csharp
YIUI3DDisplayChild.g_DisPlayUIIndex++;
var offsetY = YIUI3DDisplayChild.g_DisPlayUIIndex * 100.0f;
if (YIUI3DDisplayChild.g_DisPlayUIIndex >= 2147)
    YIUI3DDisplayChild.g_DisPlayUIIndex = 0;
self.m_ModelGlobalOffset = new Vector3(0, offsetY, 0);
```

每创建一个 3DDisplay 实例，Y 偏移递增 100 单位。最多支持约 2147 个同时存在的实例（超出后重置为 0，可能导致位置重叠）。

---

### 摄像机参数同步

`UpdateLookCamera()` 将 `lookCamera` 的投影参数完整复制到 `ShowCamera`：
- `orthographic`, `orthographicSize`, `fieldOfView`
- `nearClipPlane`, `farClipPlane`
- `clearFlags = SolidColor`
- `backgroundColor`（由 `m_UseLookCameraColor` 决定是否使用参考摄像机颜色）

特殊情况：当 `lookCamera == ShowCamera`（使用默认摄像机）时，需要手动将摄像机位置加上 `m_ModelGlobalOffset`。

---

### FitScaleRoot 缩放补偿

当 UI 父层级有缩放时，`m_FitScaleRoot` 节点会自动计算逆缩放补偿：
```csharp
var lossyScale = m_FitScaleRoot.lossyScale;
var localScale = UI3DDisplay.transform.localScale;
m_FitScaleRoot.localScale = new Vector3(
    1f / lossyScale.x * localScale.x,
    1f / lossyScale.y * localScale.y,
    1f / lossyScale.z * localScale.z
);
```
确保 3D 对象在屏幕上的显示比例不受 UI 缩放影响。

---

### 动画始终运行

所有 Animator 强制设置 `cullingMode = AlwaysAnimate`：
```csharp
animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
```
保证模型在屏幕外（Y 偏移到不可见位置）也能正常播放动画，否则动画在屏幕外时会停止。

---

### RenderTexture 生命周期管理

| 时机 | 操作 |
|------|------|
| `ShowAsync/ShowSync` 调用 | `RenderTexture.GetTemporary(...)` 申请（或重建） |
| `YIUIEnable`（Panel 显示） | 若有 ShowObject 但 Texture 为 null，则申请 |
| `YIUIDisable`（Panel 隐藏） | `RenderTexture.ReleaseTemporary(...)` 释放 |
| `Destroy` | 彻底清理 Texture，禁用 Camera 和 Image |

---

## 关键流程

### 1. 初始化流程

```
UI Panel 创建
  → YIUI3DDisplayChild.Awake(UI3DDisplay)
    → SetLayer()                    // 获取 YIUI3DLayer 层 index
    → 初始化 ShowImage/ShowCamera   // 无 ShowObject 时默认禁用
    → 缓存 m_ShowCameraDefPos       // ShowCamera 初始本地位置
    → 计算 m_ModelGlobalOffset      // g_DisPlayUIIndex++ → Y 偏移
    → 若多目标模式 → InitRotationData()
    → 若已预设 ShowObject+LookCamera（非多目标）→ ShowByGameObject()
```

### 2. 异步显示流程（推荐）

```
await ShowAsync(resName, cameraName)
  → CoroutineLock 加锁（防并发，key = GetHashCode()）
  → GetDisplayObjectAsync(resName)
    → m_ObjPool 命中 → 返回缓存对象
    → 未命中 → YIUIFactory.InstantiateGameObjectAsync() → 加入 m_ObjPool
  → cameraName 为空 → 使用 ShowCamera
         非空 → GetCamera(obj, cameraName)（二级缓存）
  → ShowByGameObject(obj, camera)
    → SetTemporaryRenderTexture()    // 创建/更新 RenderTexture
    → UpdateShowObject(showObject)   // 设置父级、层、动画、位置/旋转/缩放、阴影
    → UpdateLookCamera(lookCamera)   // 同步摄像机投影参数
```

### 3. 拖拽旋转流程

```
UI3DDisplay.OnDrag(PointerEventData) → [条件: m_CanDrag]
  → YIUIInvokeSystem.Invoke(OnDragInvoke, eventData)
  → YIUI3DDisplayChildSystem.OnDrag()
    → 单目标模式：累积 m_DragRotation，重新计算 Quaternion（以 ShowRotation 为基准）
    → 多目标模式：当前 m_DragTarge 绕 World Vector3.up 旋转
```

### 4. 点击检测流程

```
UI3DDisplay.OnPointerDown() → [条件: OnClick]
  → Invoke OnPointerDownInvoke
    → 记录 m_OnClickDownPos
    → 多目标模式 → Raycast() 确定 m_DragTarge

UI3DDisplay.OnPointerUp() → [条件: OnClick]
  → Invoke OnPointerUpInvoke
    → ClickSucceed()              // 检查 Up-Down 偏移 ≤ m_OnClickOffset
    → Raycast(screenPoint)        // RectTransform坐标 → 射线 → Physics.Raycast(层过滤)
    → 命中 → 获取 clickObj
    → 非多目标：root = m_ShowObject
    → 多目标：GetMultipleTargetByClick() 递归查找 m_AllMultipleTarget 中的父级
    → YIUI3DDisplayClickHelper.OnClick(YIUI3DDisplayClickTypeSystem, ...)
        → EntitySystemSingleton.TypeSystems.GetSystems() 查找注册的回调
        → eventSystem.OnClick(self, display, target, root)
```

---

## 公开 API

| 方法 | 说明 |
|------|------|
| `ShowAsync(resName, cameraName)` | 异步加载并显示指定预制（**推荐使用**） |
| `ShowSync(resName, cameraName)` | 同步加载并显示（宏 `YIUIMACRO_SYNCLOAD_CLOSE` 未定义时可用） |
| `ClearShow()` | 清除当前显示的 3D 对象（隐藏但不销毁，保留对象池） |
| `ResetRotation()` | 重置旋转到 m_ShowRotation 初始值 |
| `SetRotation(Vector3)` | 设置显示对象旋转（更新 m_ShowRotation 并立即应用） |
| `SetOffset(Vector3)` | 设置显示对象位置偏移（相对于 m_ModelGlobalOffset） |
| `SetScale(Vector3)` | 设置显示对象缩放 |
| `ChangeResolution(Vector2)` | 更改 RenderTexture 分辨率（差异超 0.01 时重建） |
| `ResetOnClick(bool)` | 开/关点击事件（也同步到 UI3DDisplay.OnClick） |
| `Raycast(Vector2, out RaycastHit)` | 屏幕坐标 → RectTransform 本地坐标 → ShowCamera 射线 → 层过滤物理检测 |
| `AddMultipleTarget(obj, camera, parent)` | 多目标模式：添加可交互目标并设置层/动画/碰撞 |
| `RemoveMultipleTarget(obj)` | 多目标模式：移除目标 |
| `ResetMultipleTargetMode(bool)` | 动态切换多目标模式（InitRotationData 或 ClearMultipleData） |

---

## Invoke 事件系统

```csharp
public class YIUI3DDisplayInvoke
{
    public const string OnDragInvoke        = "YIUI3DDisplayInvoke.OnDragInvoke";
    public const string OnPointerDownInvoke = "YIUI3DDisplayInvoke.OnPointerDownInvoke";
    public const string OnPointerUpInvoke   = "YIUI3DDisplayInvoke.OnPointerUpInvoke";
}
```

Mono 层的 Unity 输入事件通过 `YIUIInvokeSystem.Instance.Invoke()` 转发到 ECS 层处理，保持 Mono 与 ECS 的解耦。
在 ECS 层用 `[YIUIInvoke(YIUI3DDisplayInvoke.OnDragInvoke)]` 特性注册处理方法。

---

## 多目标模式（MultipleTargetMode）

用于在同一个 3DDisplay 中显示多个可独立拖拽/点击的 3D 模型（例如装备橱窗、多角色展示）。

**使用流程**：
```csharp
// 1. 预设主对象（作为容器根节点）
await display.ShowAsync("SceneRoot");

// 2. 设置多目标模式
display.ResetMultipleTargetMode(true);

// 3. 添加各个子模型
display.AddMultipleTarget(model1, lookCamera1, parentTransform);
display.AddMultipleTarget(model2, lookCamera2, parentTransform);

// 4. 开启点击事件
display.ResetOnClick(true);
```

**多目标查找算法**：
`GetMultipleTargetByClick()` 从射线命中的 `clickObj` 向上递归查找，直到找到在 `m_AllMultipleTarget` 中注册的对象。`m_MultipleCache` 缓存已解析结果，避免重复遍历。

---

## 代码示例

### 基础使用

```csharp
// 1. 在 UI Panel Component 的 YIUIInitialize 中创建 YIUI3DDisplayChild
[EntitySystem]
private static void YIUIInitialize(this MyPanelViewComponent self)
{
    // u_Com3DDisplay 是 Gen 代码自动生成的 UI3DDisplay 引用
    self.m_Display = self.AddChild<YIUI3DDisplayChild, UI3DDisplay>(self.u_Com3DDisplay);
}

// 2. 异步显示模型（带自定义摄像机）
private static async ETTask ShowModel(this MyPanelViewComponent self, string modelName)
{
    EntityRef<MyPanelViewComponent> selfRef = self;
    await self.m_Display.ShowAsync(modelName, "CustomCamera");
    self = selfRef;
    self.m_Display.SetRotation(new Vector3(0, 180, 0)); // 设置朝向
    self.m_Display.ResetOnClick(true);                   // 开启点击
}

// 3. 实现点击回调
[EntitySystem]
private static void YIUI3DDisplayClick(this MyPanelViewComponent self,
    UI3DDisplay display, GameObject target, GameObject root)
{
    Log.Info($"点击了 {root.name} 的 {target.name} 部位");
}
```

### 切换模型

```csharp
// 多次调用 ShowAsync 切换模型，对象池自动管理
await display.ShowAsync("Model_Warrior");
// ... 稍后切换
await display.ShowAsync("Model_Mage");
// 两个模型都缓存在 m_ObjPool，切换无需重新加载
```

### 清除和资源管理

```csharp
// 清除显示（对象保留在池中，下次 ShowAsync 无需重新加载）
display.ClearShow();

// RenderTexture 在 Panel Disable 时自动释放，Enable 时自动重新申请
// 无需手动管理
```

---

## 依赖关系

```
cn.etetet.yiui3ddisplay
  ├── 依赖: cn.etetet.yiuiframework (YIUI框架基础)
  │         ├── YIUIInvokeSystem      — Mono→ECS 事件转发
  │         ├── YIUIFactory           — 异步/同步资源实例化
  │         ├── ListPool<T>           — 列表对象池（减少 GC）
  │         ├── YIUIConstHelper       — 常量配置访问（3DLayer名）
  │         └── CoroutineLockComponent — 协程锁（防异步并发）
  └── 依赖: ET框架核心
            ├── Entity / EntitySystem  — ECS 基础架构
            ├── ETTask                 — 异步任务
            └── Log                   — 日志系统
```

---

## 配置项

在 `YIUIConstAsset`（部分扩展 `YIUIConstAsset_3DDisplay`）中配置：
- **`YIUI3DLayer`**（默认 `"YIUI3DLayer"`）：Unity 项目中专用的 3D 显示层名称。
  - **必须**在 `Project Settings > Tags and Layers` 中手动添加，否则运行时报错。

---

## 使用注意事项

1. **必须添加 Layer**：Unity 项目需手动添加 `YIUI3DLayer` 层，否则 `SetLayer()` 返回 -1 并报错，3D 对象无法正常渲染。

2. **推荐异步加载**：`ShowAsync` 优于 `ShowSync`。已知：同步与异步同时加载相同资源可能报错，请避免并发调用。

3. **对象池复用**：同一 resName 的对象只加载一次，后续切换直接从 `m_ObjPool` 取出。`ClearShow()` 不销毁对象，仅 `SetActive(false)`。

4. **RenderTexture 自动管理**：Enable/Disable 时自动申请/释放，无需手动处理。但注意切换分辨率（`ChangeResolution`）会立即重建。

5. **点击事件需手动开启**：默认 `OnClick = false`，需调用 `ResetOnClick(true)` 才触发，避免不必要的射线检测开销。

6. **多实例偏移上限**：`g_DisPlayUIIndex` 是静态计数器，超 2147 后重置为 0，Y 偏移可能与早期实例重叠。在极端场景下（频繁创建销毁）需关注此问题。

7. **m_FitScaleRoot 用途**：当 UI 父节点有非 1 缩放时（如 SafeArea 适配），通过此节点自动抵消，防止模型显示比例失真。

8. **编辑器快速创建**：通过 `GameObject > YIUI > 3DDisplay` 右键菜单从模板预制克隆，避免手动配置摄像机/灯光/层级等复杂结构。
