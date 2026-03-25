# cn.etetet.yiuitips

**版本**: 3.0.3
**分类**: UI/YIUI
**命名空间**: `ET.Client`
**依赖**: `cn.etetet.core` ^1.0.0

## 概述

YIUI Tips 弹窗系统，提供游戏内通用提示弹窗功能。主要包含两类内置弹窗：
1. **飘字提示**（TipsTextView）：显示一段文字后自动消失（播放动画后关闭）
2. **消息弹窗**（TipsMessageView）：带确定/取消按钮的模态对话框，支持 `await` 等待用户操作结果

TipsPanel 位于 `EPanelLayer.Tips` 层级，内部使用**对象池**管理各类 View，实现高效复用。提供 `TipsHelper` 静态工具类作为统一入口，支持泛型类型安全调用和资源名称字符串调用两套 API。

---

## 目录结构

```
cn.etetet.yiuitips/Scripts/
├── ModelView/Client/
│   ├── YIUIComponent/Tips/          # 数据组件定义（partial class 前半部分）
│   │   ├── TipsPanelComponent.cs    # Panel 状态数据
│   │   ├── TipsViewComponent.cs     # View 标记组件 + EventPutTipsView 事件
│   │   ├── TipsTextViewComponent.cs # 飘字 View 组件（空壳，逻辑在 System）
│   │   └── TipsMessageViewComponent.cs # 消息弹窗组件 + MessageTipsExtraData
│   └── YIUIGen/Tips/                # YIUI 工具自动生成（不可手动修改）
│       ├── TipsPanelComponentGen.cs # Panel YIUI 属性与 UI 引用绑定
│       └── TipsMessageViewComponentGen.cs / TipsTextViewComponentGen.cs
└── HotfixView/Client/
    ├── YIUISystem/
    │   ├── Helper/                  # 静态工具类（调用入口）
    │   │   ├── TipsHelper.cs        # 泛型 Open / OpenToParent
    │   │   ├── TipsHelper_String.cs # 字符串资源名 Open / OpenToParent
    │   │   ├── TipsHelper_Wait.cs   # 泛型 OpenWait（可 await）
    │   │   └── TipsHelper_Wait_String.cs # 字符串资源名 OpenWait
    │   └── Tips/                    # System 逻辑（ECS 系统）
    │       ├── TipsPanelComponentSystem.cs  # Panel 开关 + 对象池管理
    │       ├── TipsViewComponentSystem.cs   # View 关闭时触发回收事件
    │       ├── TipsTextViewComponentSystem.cs  # 飘字播放动画 + 自动关闭
    │       └── TipsMessageViewComponentSystem.cs # 消息弹窗确认/取消处理
    └── GM/                          # GM 调试命令（依赖 yiuigm 包）
        ├── EGMType_Tips.cs
        ├── GM_Command_Tips.cs       # 5 条 GM 命令（消息弹窗测试、飘字测试等）
        └── GM_Command_WaitTips.cs   # await 弹窗 GM 测试
```

---

## 核心组件

### TipsPanelComponent

`TipsPanel` 是整个 Tips 系统的容器 Panel，位于 `EPanelLayer.Tips` 层级。

| 字段 | 类型 | 说明 |
|------|------|------|
| `_AllPool` | `Dictionary<Type, ObjAsyncCache<EntityRef<Entity>>>` | 按 View 类型分类的对象池 |
| `_RefCount` | `int` | 当前活跃 View 引用计数 |
| `_AllPoolLastTime` | `Dictionary<Type, float>` | 各类型最后一次使用时间（用于自动清理） |
| `_AutoDestroyTime` | `float` | 对象池自动销毁超时时间，默认 60 秒 |
| `_AllRefView` | `HashSet<EntityRef<Entity>>` | 当前所有活跃 View 的引用集合 |

**生命周期**：
- `_RefCount > 0` 时 Panel 保持打开状态
- 所有 View 关闭后 `_RefCount` 降为 0，Panel 自动调用 `Close()`
- Panel 销毁时强制 `Dispose` 所有残留 View

### TipsViewComponent

挂载在每个通过 TipsPanel 打开的 View 的 `YIUIWindowComponent` 上，作为标记组件。

| 字段 | 说明 |
|------|------|
| `m_IsFromTips` | 标记此 View 是否由 Tips 系统管理 |

**关键行为**：
- View 正常关闭（`YIUIWindowClose`）：等待一帧后发送 `EventPutTipsView`（`Destroy=false`），触发对象池回收
- View 被 Dispose（`Destroy`）：立即发送 `EventPutTipsView`（`Destroy=true`），从池中彻底移除

### TipsMessageViewComponent

消息确认弹窗，实现 `IYIUIOpen<ParamVo>`, `IYIUIOpenTween`, `IYIUICloseTween`。

| 字段 | 类型 | 说明 |
|------|------|------|
| `ExtraData` | `MessageTipsExtraData` | 弹窗额外配置 |

#### MessageTipsExtraData

```csharp
public class MessageTipsExtraData
{
    public string ConfirmName;      // 确定按钮文字（默认"确定"）
    public string CancelName;       // 取消按钮文字（默认"取消"）
    public bool   ShowCancelButton; // 是否显示取消按钮
    public bool   ShowCloseButton;  // 是否显示关闭按钮
}
```

**按钮逻辑**：
- 确定 → `NotifyWait(EHashWaitError.Success)` → 关闭 View
- 取消 / 关闭 → `NotifyWait(EHashWaitError.Cancel)` → 关闭 View
- 若 `ShowCancelButton=false` 且 `ShowCloseButton=false`，则强制显示取消按钮（确保用户有退出途径）

### TipsTextViewComponent

飘字提示 View，实现 `IYIUIOpen<ParamVo>`。

**行为**：
1. `YIUIOpen(ParamVo)` 接收字符串内容，设置文本
2. 播放 `u_ComAnimation`（Animation 组件）
3. 等待动画播放完毕（`WaitAsync(clipLength * 1000ms)`）
4. 自动调用 `CloseAsync()`

---

## TipsHelper 工具类

### 泛型 API（类型安全，推荐）

```csharp
// 非阻塞打开
TipsHelper.Open<TipsMessageViewComponent>(scene, "消息内容");
TipsHelper.OpenSync<TipsMessageViewComponent>(scene, "消息内容");

// 带父级
TipsHelper.OpenToParent<TipsXxxViewComponent>(scene, parentEntity, params...);

// 等待用户操作（阻塞式）
EHashWaitError result = await TipsHelper.OpenWait<TipsMessageViewComponent>(scene, "消息内容");
if (result == EHashWaitError.Success) { /* 用户点了确定 */ }
```

### 字符串 API（适用于不知道类型时）

```csharp
// 非阻塞打开
TipsHelper.Open(scene, "TipsMessageView", "消息内容");

// 等待用户操作
EHashWaitError result = await TipsHelper.OpenWait(scene, "TipsMessageView", "消息内容");
```

---

## 关键流程

### 打开 Tips View 流程

```
TipsHelper.Open<T>(scene, params)
  └── 获取 CoroutineLock（防并发）
  └── OpenPanelAsync<TipsPanelComponent, Type, Entity, ParamVo>(typeof(T), parent, vo)
        └── TipsPanelComponentSystem.YIUIOpen(viewType, parent, vo)
              └── OpenTips(uiType, parent, vo)
                    ├── CheckParent() - 验证父级存在
                    ├── _AllPool.TryGet(uiType) → ObjAsyncCache.Get()  // 从对象池取
                    │     若池空 → YIUIFactory.InstantiateAsync(uiType)  // 新建实例
                    ├── 验证 IYIUIOpen<ParamVo> 接口
                    ├── 添加/重置 TipsViewComponent (m_IsFromTips=true)
                    ├── 若有 waitId → 添加 YIUIWaitComponent
                    ├── SetParent 到 TipsPanel RectTransform
                    ├── SetAsLastSibling（置顶显示）
                    ├── _AllRefView.Add(view)
                    └── ViewComponent.Open(vo)
```

### View 关闭回收流程

```
用户点击按钮/View.CloseAsync()
  └── TipsViewComponentSystem.YIUIWindowClose(viewCloseResult)
        └── WaitFrameDynamicEvent → EventPutTipsView { View, Destroy=false }
              └── TipsPanelComponentSystem.DynamicEvent(EventPutTipsView)
                    └── PutTips(view, destroy=false)
                          ├── _AllRefView.Remove(view)
                          ├── _AllPoolLastTime[type] = Time.time
                          ├── ObjAsyncCache.Put(view)  // 归还对象池
                          ├── SetParent 回 TipsPanel（隐藏）
                          ├── _RefCount -= 1
                          └── CheckRefCount()
                                └── 若 _RefCount <= 0 → UIPanel.Close()
```

### 等待式弹窗（OpenWait）流程

```
await TipsHelper.OpenWait<TipsMessageViewComponent>(scene, "消息?")
  ├── 生成唯一 guid
  ├── HashWait.Wait(guid) → 挂起当前 ETTask
  ├── OpenTips(viewType, parent, vo, waitId=guid)
  │     └── YIUIWindowComponent.AddComponent<YIUIWaitComponent, long>(guid)
  └── 等待 HashWait...
        用户点确定 → TipsMessageViewComponentSystem.OnEventConfirmInvoke
                       └── UIWindow.NotifyWait(EHashWaitError.Success)
                             └── HashWait.SetResult(guid, Success)
                                   └── ETTask 恢复，返回 Success
```

### 对象池自动清理流程

```
每次 CheckAllPoolTips()（每次打开 Tips 时调用）
  └── 遍历 _AllPoolLastTime
        若 (Time.time - lastTime) > _AutoDestroyTime(60s)
          └── ObjAsyncCache.Clear(obj => obj.Parent.Dispose())
              _AllPoolLastTime.Remove(uiType)
              break（每次只清理一个）
```

---

## GM 调试命令

通过 GM 系统可在运行时测试弹窗功能：

| GM 命令 | 功能 |
|---------|------|
| `GM_TipsTest1` | 打开消息弹窗（需输入消息内容） |
| `GM_TipsTest2` | 打开消息弹窗（带回调，默认文字） |
| `GM_TipsTest3` | 打开消息弹窗（仅确定按钮） |
| `GM_TipsTest4` | 打开消息弹窗（自定义按钮名称 Confirm/Cancel） |
| `GM_TipsTest5` | 显示飘字提示（需输入消息内容） |

---

## 与其他 Package 的关系

```
cn.etetet.yiuitips
  ├── 依赖 cn.etetet.core          // Entity、ETTask、CoroutineLock 等基础设施
  ├── 使用 cn.etetet.yiuiframework  // YIUIChild、YIUIWindowComponent、YIUIViewComponent
  │                                 // YIUIFactory、HashWait、ParamVo、ObjAsyncCache 等
  └── 可选依赖 cn.etetet.yiuigm    // GM 命令注册（未强制引用，可删除 GM 文件）
```

**关键接口约定**：
- 所有通过 TipsHelper 打开的 View **必须实现 `IYIUIOpen<ParamVo>`** 接口
- 若需要 `OpenWait`，View 还需实现 `IYIUIBind`
- 建议命名规范：`Tips[XXX]View`

---

## 设计要点

1. **引用计数驱动 Panel 生命周期**：`_RefCount` 是 Panel 存在的唯一依据，避免 Panel 层被频繁创建销毁。
2. **对象池 + 超时清理**：高频飘字通过对象池复用，长时间不用的大型弹窗由 60s 超时机制自动清理，平衡性能与内存。
3. **CoroutineLock 防并发**：同一时刻只允许一个 TipsHelper.Open 的实例化流程执行，避免多协程同时创建重复 View。
4. **延迟一帧回收**：View 关闭后等一帧再发回收事件，避免在 Close 回调链中重入 Panel 的状态修改逻辑。
5. **ParamVo 生命周期管理**：同步调用时使用 `OpenToParent2NewVo` 克隆 vo，防止外部 vo 被提前回收导致空引用。
