# cn.etetet.yiuiloopscrollrectasync

**版本**: 3.0.3
**分类**: UI/YIUI
**描述**: YIUI 无限循环列表（异步）
**依赖**: cn.etetet.core

---

## 概述

本包基于开源 LoopScrollRect 组件（对 Unity ScrollRect 的扩展），在 YIUI/ET 框架体系内实现了**异步加载、对象池复用、无限循环滚动**的列表组件。其核心特性是：

- 虚拟化列表：只实例化可见区域内的 Item，其余通过对象池缓存，支持海量数据
- 异步加载：Item 的创建使用 ETTask 异步流程，不阻塞主线程
- ECS 集成：通过 YIUILoopScrollChild 实体与 EntitySystem 方式解耦业务逻辑
- 内置点击选中管理：多选/单选/重复取消/自动取消上一个
- UI 交互保护：刷新期间自动禁止 UI 层操作，防止并发问题

---

## 目录结构

```
cn.etetet.yiuiloopscrollrectasync/
├── Runtime/                         # Unity 运行时组件（UGUI 扩展）
│   ├── LoopScrollRectBase.cs        # 核心基类（继承 UIBehaviour，实现拖拽、布局接口）
│   ├── LoopScrollRect.cs            # 抽象中间类（partial）：连接 dataSource/prefabSource
│   ├── LoopScrollRectMulti.cs       # 多列/多行抽象基类
│   ├── LoopVerticalScrollRect.cs    # 垂直单列滚动实现
│   ├── LoopHorizontalScrollRect.cs  # 水平单行滚动实现
│   ├── LoopVerticalScrollRectMulti.cs   # 垂直多列
│   ├── LoopHorizontalScrollRectMulti.cs # 水平多行
│   ├── LoopScrollDataSource.cs      # 数据源接口定义（LoopScrollDataSource / YIUI 适配）
│   ├── LoopScrollPrefabSource.cs    # Prefab 源接口（异步 GetObject / ReturnObject）
│   ├── LoopScrollSizeHelper.cs      # 尺寸辅助（精确滚动计算）
│   └── Extend/
│       └── LoopScrollRect_Extend.cs # LoopScrollRect 的 YIUI 扩展字段（缓存父级、点击配置等）
├── Scripts/
│   ├── ModelView/Client/
│   │   ├── LoopScroll/
│   │   │   ├── YIUILoopScrollChild.cs        # 核心 Entity 数据类（字段定义 + Data 属性）
│   │   │   ├── YIUILoopScrollChild_Extend.cs # 只读属性（TotalCount/Content/ItemStart 等）
│   │   │   └── YIUILoopScrollChild_OnClick.cs # 点击选中状态字段
│   │   ├── Event/
│   │   │   ├── YIUILoopRenderer.cs        # IYIUILoopRenderer 接口 + 基类
│   │   │   ├── YIUILoopOnClick.cs         # IYIUILoopOnClick 接口 + 基类
│   │   │   ├── YIUILoopOnClickCheck.cs    # IYIUILoopOnClickCheck 接口 + 基类
│   │   │   └── YIUILoopHelper.cs          # 静态分发助手（TypeSystems 查找并调用）
│   │   ├── GM/
│   │   │   └── EGMType_Loop.cs            # GM 命令枚举（调试用）
│   │   └── YIUIComponent/               # Demo 用的 YIUI 组件定义（Panel / Item / View）
│   └── HotfixView/Client/
│       ├── GM/
│       │   └── GM_Command_Loop.cs         # GM 命令处理（测试用）
│       ├── LoopScroll/
│       │   ├── YIUILoopScrollChildSystem.cs         # 核心 EntitySystem（Awake/Destroy/Interface实现）
│       │   ├── YIUILoopScrollChildSystem_API.cs     # 公开 API（SetDataRefresh/GetItem/ClearSelect...）
│       │   ├── YIUILoopScrollChildSystem_Extend.cs  # RefillCells/RefreshCells/ScrollToCell...
│       │   └── YIUILoopScrollChildSystem_OnClick.cs # 点击初始化与选中状态管理
│       ├── YIUIGen/                     # 代码生成的 Demo ComponentSystem（Gen）
│       └── YIUISystem/                  # Demo 具体业务逻辑（手写扩展系统）
├── Editor/
│   ├── LoopScrollRectInspector.cs   # 自定义 Inspector
│   ├── SGDefaultControls.cs         # 编辑器 UI 控件
│   ├── SGMenuOptions.cs             # 编辑器菜单（创建 LoopScrollRect 对象）
│   └── TemplatePrefabs/             # 预制体模板（水平/垂直/分组/反向 6 种）
└── Assets/GameRes/YIUI/             # Demo 预制体资源
```

---

## 核心类/接口说明

### Runtime 层

#### `LoopScrollRectBase` (Runtime/LoopScrollRectBase.cs)

所有 LoopScrollRect 的基类，继承 `UIBehaviour`，实现：
- `IInitializePotentialDragHandler / IBeginDragHandler / IEndDragHandler / IDragHandler / IScrollHandler` — 拖拽事件处理
- `ICanvasElement / ILayoutElement / ILayoutGroup` — 布局系统集成

关键字段：
| 字段 | 类型 | 说明 |
|------|------|------|
| `prefabSource` | `IYIUILoopScrollPrefabAsyncSource` | 异步 Prefab 源（由 YIUILoopScrollChild 设置） |
| `totalCount` | `int` | 总数据量（负数 = 无限模式） |
| `sizeHelper` | `LoopScrollSizeHelper` | 精确尺寸辅助 |
| `itemTypeStart` | `int` | 当前可见第一个 Item 的 Index |
| `itemTypeEnd` | `int` | 当前可见最后一个 Item 的 Index + 1 |
| `reverseDirection` | `bool` | 反向滚动 |
| `threshold` | `float` | 预加载阈值（≥ 1.5 * itemSize） |
| `m_GridLayout` | `GridLayoutGroup` | Grid 布局缓存 |
| `m_ContentSpacing` | `float` | 内容间距缓存（延迟初始化） |
| `m_ContentConstraintCount` | `int` | 每行/列的项目数（GridLayout 时取 constraintCount，否则为 1） |

内部计算属性（只读）：
| 属性 | 说明 |
|------|------|
| `StartLine` | 可见的第一行 = ceil(itemTypeStart / contentConstraintCount) |
| `CurrentLines` | 当前可见行数 = ceil((itemTypeEnd - itemTypeStart) / contentConstraintCount) |
| `TotalLines` | 总行数 = ceil(totalCount / contentConstraintCount) |

枚举：
```csharp
enum MovementType { Unrestricted, Elastic, Clamped }
enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
enum LoopScrollRectDirection { Vertical, Horizontal }
```

抽象方法（子类实现方向差异）：
```csharp
protected abstract float GetSize(RectTransform item, bool includeSpacing = true);
protected abstract float GetDimension(Vector2 vector);
protected abstract float GetAbsDimension(Vector2 vector);
protected abstract Vector2 GetVector(float value);
```

虚方法：
```csharp
protected virtual async ETTask<(bool, Bounds, Bounds)> UpdateItems(Bounds viewBounds, Bounds contentBounds)
```

---

#### `LoopScrollRect` (Runtime/LoopScrollRect.cs)

继承 `LoopScrollRectBase` 的 partial 抽象类，关键实现：

```csharp
public IYIUILoopScrollDataSource dataSource = null;

// 从对象池取出 Item（优先复用 deletedItemTypeStart/End 中的）
protected override async ETTask<RectTransform> GetFromTempPool(int itemIdx)

// 标记 Item 可复用（延迟归还，先放入 deletedItemTypeStart/End）
protected override void ReturnToTempPool(bool fromStart, int count)

// 实际归还 Item 到 prefabSource
protected override void ClearTempPool()
```

**TempPool 机制**：滚动时先用 `ReturnToTempPool` 标记旧 Item 可复用（不立即销毁），`GetFromTempPool` 时优先取标记 Item（改变 sibling 位置复用），最后 `ClearTempPool` 统一归还真正回收。这减少了对象池 Get/Put 调用频率。

---

#### `LoopScrollRect` (Runtime/Extend/LoopScrollRect_Extend.cs)

partial 类，为 LoopScrollRect 添加 YIUI Inspector 扩展字段：
| 字段 | 说明 |
|------|------|
| `u_CacheRect` | 对象池缓存的父级 RectTransform（不可见节点） |
| `u_MaxClickCount` | 最大可同时选中数量（默认 1，Inspector 中 MinValue=1） |
| `u_AutoCancelLast` | 超过最大选中数时是否自动取消最早选中的（默认 true） |
| `u_RepetitionCancel` | 重复点击已选中项时是否取消选中 |
| `u_CreateInterval` | Item 创建间隔时间（0 = 无间隔，减少同帧创建卡顿） |
| `u_ForeverInterval` | 滚动时也使用创建间隔（默认 false） |
| `u_RefreshCanOption` | 刷新期间是否允许 UI 交互（默认 false） |
| `u_PreLoadCount` | 预加载实例数量（0 = 不预加载） |

只读属性（透出内部状态）：
```csharp
u_StartLine / u_CurrentLines / u_TotalLines / u_EndLine   // 行信息
u_ContentConstraintCount / u_ContentSpacing                // 布局参数
u_ItemStart / u_ItemEnd                                    // 当前可见 Index 范围
```

---

#### `LoopScrollDataSource` (Runtime/LoopScrollDataSource.cs)

三层接口体系：

```csharp
// 1. 原始同步接口（供 LoopScrollRectBase 调用）
interface LoopScrollDataSource {
    void ProvideData(Transform transform, int idx);
}

// 2. YIUI 标记接口（由 Entity 实现，空接口用于类型标识）
interface IYIUILoopScrollDataSource {}

// 3. EntitySystem 接口（通过 TypeSystems 查找实现）
interface IYIUILoopScrollDataSourceSystem : ISystemType {
    void ProvideData(Entity self, Transform transform, int idx);
}

// 4. EntitySystem 基类（泛型，业务层继承实现）
abstract class YIUILoopScrollDataSourceSystem<T> : SystemObject, IYIUILoopScrollDataSourceSystem
    where T : Entity, IYIUILoopScrollDataSource
```

扩展方法入口：
```csharp
// 通过 EntitySystemSingleton.TypeSystems 动态查找并调用具体实现
YIUILoopScrollDataSourceExtensions.ProvideData(source, transform, idx)
```

---

#### `LoopScrollPrefabSource` (Runtime/LoopScrollPrefabSource.cs)

同样的三层接口体系，但 GetObject 是异步的：

```csharp
interface IYIUILoopScrollPrefabAsyncSource {}

interface IYIUILoopScrollPrefabAsyncSourceSystem : ISystemType {
    ETTask<GameObject> GetObject(Entity self, int index);
    void ReturnObject(Entity self, Transform trans);
}

abstract class YIUILoopScrollPrefabAsyncSourceSystem<T>
    where T : Entity, IYIUILoopScrollPrefabAsyncSource
```

**注意**：`GetObject` 系统实现数量必须恰好为 1，否则报错（与 DataSource 不同，DataSource 允许多个）。

---

### ModelView 层（Entity 数据定义）

#### `YIUILoopScrollChild` (Scripts/ModelView/Client/LoopScroll/)

核心 Entity，同时实现 `IYIUILoopScrollPrefabAsyncSource` 和 `IYIUILoopScrollDataSource`。

**主文件字段** (`YIUILoopScrollChild.cs`)：

```csharp
[ChildOf]  // 必须作为子 Entity 存在
public partial class YIUILoopScrollChild : Entity,
    IAwake, IAwake<LoopScrollRect, Type>, IAwake<LoopScrollRect, Type, string>,
    IDestroy,
    IYIUILoopScrollPrefabAsyncSource, IYIUILoopScrollDataSource
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `m_OwnerEntity` | `EntityRef<Entity>` | 所属父 Entity（用 EntityRef 防止内存泄漏） |
| `m_BindVo` | `YIUIBindVo` | Item 的 YIUI 绑定信息（PkgName/ResName/ComponentType） |
| `m_DefaultDataType` | `Type` | 泛型列表时需手动指定数据类型 |
| `m_DataType` | `Type` | 当前实际数据类型（从 Data 赋值时自动推断） |
| `m_Owner` | `LoopScrollRect` | 关联的 LoopScrollRect 组件 |
| `m_ItemType` | `Type` | Item Entity 的类型 |
| `m_LoopRendererSystemType` | `Type` | 构造出的 `IYIUILoopRenderer<Owner,Item,Data>` 泛型类型 |
| `m_LoopOnClickSystemType` | `Type` | 构造出的 `IYIUILoopOnClick<Owner,Item,Data>` 泛型类型 |
| `m_LoopOnClickCheckSystemType` | `Type` | 构造出的 `IYIUILoopOnClickCheck<Owner,Item,Data>` 泛型类型 |
| `m_ItemPool` | `ObjAsyncCache<EntityRef<Entity>>` | 异步对象池 |
| `m_ItemTransformDic` | `Dictionary<Transform, EntityRef<Entity>>` | Transform → Entity 反查映射 |
| `m_ItemTransformIndexDic` | `Dictionary<Transform, int>` | Transform → 数据索引映射（-1 = 在池中） |
| `m_InvokeLoadInstantiate` | `YIUIInvokeEntity_LoadInstantiateByVo` | 预先构建的 Invoke 参数对象（避免重复分配） |
| `m_BanLayerOptionForeverHashSet` | `HashSet<long>` | 当前持有的禁止操作 Code 集合（Destroy 时全部释放） |

**Data 属性的关键逻辑**（`YIUILoopScrollChild.cs` 中 setter）：
```csharp
public IList Data {
    set {
        m_Data = value;
        if (m_Data is { Count: > 0 }) {
            // 自动推断数据类型（支持泛型列表的 DefaultDataType 覆盖）
            m_DataType = m_DefaultDataType ?? m_Data[0].GetType();
            // 动态构建三个泛型系统类型，供 YIUILoopHelper 查找
            m_LoopRendererSystemType  = typeof(IYIUILoopRenderer<,,>).MakeGenericType(OwnerEntity?.GetType(), m_ItemType, m_DataType);
            m_LoopOnClickSystemType   = typeof(IYIUILoopOnClick<,,>).MakeGenericType(OwnerEntity?.GetType(), m_ItemType, m_DataType);
            m_LoopOnClickCheckSystemType = typeof(IYIUILoopOnClickCheck<,,>).MakeGenericType(OwnerEntity?.GetType(), m_ItemType, m_DataType);
        } else {
            // 清空全部系统类型引用
        }
    }
}
```

**辅助属性** (`YIUILoopScrollChild_Extend.cs`)：
```csharp
public int TotalCount => m_Owner.totalCount;
public RectTransform Content => m_Owner.content;
public RectTransform CacheRect => m_Owner.u_CacheRect;
public int StartLine / CurrentLines / TotalLines / EndLine
public int ContentConstraintCount / float ContentSpacing
public int ItemStart => m_Owner.u_ItemStart;    // 当前可见第一个的 Index
public int ItemEnd   => m_Owner.u_ItemEnd;      // 当前可见最后一个的 Index + 1
public int PreLoadCount => m_Owner.u_PreLoadCount;
```

**点击字段** (`YIUILoopScrollChild_OnClick.cs`)：
| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `m_OnClickInit` | `bool` | false | 是否已初始化点击（初始化后不可修改） |
| `m_ItemClickEventName` | `string` | - | Item EventTable 中的点击事件名 |
| `m_ItemClickCheck` | `bool` | false | 是否启用点击前检查 |
| `m_OnClickItemQueue` | `Queue<int>` | new | 选中顺序队列（FIFO，用于超出上限时移除最早的） |
| `m_OnClickItemHashSet` | `HashSet<int>` | new | 选中集合（O(1) 查找） |
| `m_MaxClickCount` | `int` | 1 | 最大选中数（1=单选，≥2=多选） |
| `m_RepetitionCancel` | `bool` | true | 重复点击已选中项时取消选中 |
| `m_AutoCancelLast` | `bool` | true | 超出最大选中数时自动出队最早的 |

---

### Event 接口层

#### `IYIUILoopRenderer<T1,T2,T3>`

```csharp
// 非泛型基础接口（供 YIUILoopHelper 使用）
public interface IYIUILoopRenderer {
    void Renderer(Entity self, Entity item, object data, int index, bool select);
}

// 泛型接口（标识类型参数，TypeSystems 按 T1 注册）
public interface IYIUILoopRenderer<in T1, in T2, in T3> : ISystemType, IYIUILoopRenderer {}

// 业务层继承的抽象基类（5个泛型参数：Owner/Item/Data + 两个占位 T4/T5）
public abstract class YIUILoopRendererSystem<T1, T2, T3, T4, T5> : SystemObject
    where T1 : Entity, IYIUIBind, IYIUIInitialize
    where T2 : Entity, IYIUIBind, IYIUIInitialize
```

**T4/T5 的作用**：占位泛型参数，允许同一个 Owner+Item+Data 组合有不同的实现（通过不同 T4/T5 区分，避免系统冲突）。

#### `IYIUILoopOnClick<T1,T2,T3>` / `YIUILoopOnClickSystem<T1,T2,T3,T4,T5>`

结构同 Renderer，回调方法为 `YIUILoopOnClick`。

#### `IYIUILoopOnClickCheck<T1,T2,T3>` / `YIUILoopOnClickCheckSystem<T1,T2,T3,T4,T5>`

结构同 Renderer，回调方法为 `bool YIUILoopOnClickCheck`（返回 false 拦截点击）。

#### `YIUILoopHelper` (Scripts/ModelView/Client/Event/YIUILoopHelper.cs)

静态分发类，通过 `EntitySystemSingleton.TypeSystems.GetSystems(self.GetType(), rendererType)` 查找匹配的系统实现：

```csharp
public static void Renderer(Type rendererType, Entity self, Entity item, object data, int index, bool select)
public static void OnClick(Type onclickType, Entity self, Entity item, object data, int index, bool select)
public static bool OnClickCheck(Type onclickCheckType, Entity self, Entity item, object data, int index, bool select)
```

**查找 key**：使用 `self.GetType()`（即 OwnerEntity 类型）在 TypeSystems 中查找，找到的系统集合中过滤出 `rendererType`（泛型构造类型）。第一个匹配的系统执行后立即返回（`return`，只取第一个）。

---

### HotfixView 层（EntitySystem 逻辑）

#### `YIUILoopScrollChildSystem` — 生命周期 + 接口实现

**三种 Awake 重载**：
```csharp
// 1. 无参 Awake：记录 OwnerEntity，触发预加载（适合代码生成中初始化后再手动 Initialize）
Awake(self)

// 2. 标准初始化
Awake(self, owner: LoopScrollRect, itemType: Type)

// 3. 带点击事件初始化（最常用）
Awake(self, owner: LoopScrollRect, itemType: Type, itemClickEventName: string)
```

**Initialize 流程**：
```
GetBindVoByType(itemType)          // 通过 itemType 查找 YIUI 绑定 VO
  → 构建 ObjAsyncCache（异步池）
  → m_Owner.prefabSource = self    // 注入 Prefab 源
  → m_Owner.dataSource = self      // 注入 Data 源
  → InitClearContent()             // 销毁 Content 下已有子节点（保证初始干净）
  → InitCacheParent()              // 若无 u_CacheRect 则创建隐藏节点
  → 构建 m_InvokeLoadInstantiate   // 设置 ParentEntity=self，ParentTransform=CacheRect
```

**Destroy**：
```csharp
self.m_ItemPool?.Clear((obj) => { ((Entity)obj)?.Parent?.Dispose(); }); // 销毁所有池内 Entity
foreach (var code in self.m_BanLayerOptionForeverHashSet)
    self.YIUIMgr()?.RecoverLayerOptionForever(code);  // 释放所有禁操作锁
```

**嵌套 EntitySystem（在 YIUILoopScrollChildSystem.cs 中定义）**：
```csharp
[EntitySystem]
public class YIUILoopScrollPrefabAsyncSource : YIUILoopScrollPrefabAsyncSourceSystem<YIUILoopScrollChild>
{
    protected override async ETTask<GameObject> GetObject(YIUILoopScrollChild self, int index)
        => await self.GetObject(index);
    protected override void ReturnObject(YIUILoopScrollChild self, Transform trans)
        => self.ReturnObject(trans);
}

[EntitySystem]
public class YIUILoopScrollDataSource : YIUILoopScrollDataSourceSystem<YIUILoopScrollChild>
{
    protected override void ProvideData(YIUILoopScrollChild self, Transform transform, int idx)
        => self.ProvideData(transform, idx);
}
```

---

#### `YIUILoopScrollChildSystem_API` — 公开 API 完整列表

**数据刷新**：
| 方法 | 说明 |
|------|------|
| `SetDataRefresh(data)` | 设置数据并从头全量刷新 |
| `SetDataRefresh(data, int index)` | 设置数据，默认选中指定索引后刷新 |
| `SetDataRefresh(data, List<int>)` | 设置数据，默认选中多个索引后刷新 |
| `SetDataRefresh(data, index, scrollTo)` | 设置数据，选中并滚动到 scrollTo 位置 |
| `SetDataRefreshShowAll(data)` | 全量显示（offset=99999，强制全部 Cell 可见，适合少量数据） |
| `SetDataRefreshShowAll(data, int)` | 同上 + 默认选中 |
| `SetDataRefreshShowAll(data, List<int>)` | 同上 + 多选 |

**查询 API**：
| 方法 | 说明 |
|------|------|
| `GetItemIndex(Entity item)` | 获取 Entity 的数据索引（< 0 表示在池中） |
| `GetItemByIndex(int index, bool log)` | 获取可见 Item 的 Entity（不可见返回 null） |
| `IsSelect(Entity item)` | 判断某 Entity 是否被选中 |
| `GetShowData<T>()` | 获取当前可见区域的数据列表（类型必须匹配 m_DataType） |
| `GetShowItem<T>()` | 获取当前可见区域的 Entity 列表 |

**选中管理**：
| 方法 | 说明 |
|------|------|
| `ClearSelect(reset=true)` | 清除所有选中状态（reset=true 时逐项触发取消回调） |
| `GetSelectIndex()` | 获取所有选中的索引列表（按选中顺序） |
| `GetSelectItem()` | 获取所有选中的可见 Entity |
| `GetSelectData<T>()` | 获取所有选中项的数据 |
| `RemoveSelectIndexRefresh(index)` | 移除某个选中索引并刷新 Cell |
| `ReRenderer()` | 原地刷新所有可见项（不改变位置/选中状态，纯数据更新） |

**动态配置**：
| 方法 | 说明 |
|------|------|
| `SetDefaultDataType(Type)` | 设置泛型列表的数据类型（必须在 SetDataRefresh 前调用） |
| `ChangeCreateInterval(float)` | 动态修改 Item 创建间隔 |
| `Vertical(bool)` | 动态切换垂直滚动开关 |
| `Horizontal(bool)` | 动态切换水平滚动开关 |

---

#### `YIUILoopScrollChildSystem_Extend` — 滚动控制

| 方法 | 说明 |
|------|------|
| `RefillCells(startItem=0, offset=0)` | 从 startItem 开始重建所有 Cell（清空重填） |
| `RefillCellsFromEnd(endItem=0, alignStart=false)` | 从末尾方向重建 |
| `RefreshCells()` | 刷新当前可见 Cell（不改变位置，重新 ProvideData） |
| `ClearCells()` | 清除所有 Cell（归还到池） |
| `ScrollToCell(index, speed)` | 以固定速度滚动到指定项 |
| `ScrollToCellWithinTime(index, time)` | 在指定时间内滚动（0 = 瞬间） |
| `StopMovement()` | 停止惯性滚动 |
| `GetFirstItem(out offset)` | 获取第一个可见项的索引及其偏移量 |
| `GetLastItem(out offset)` | 获取最后一个可见项的索引及其偏移量 |
| `PreLoadAsync(count)` | 异步预加载指定数量的实例（使用协程锁防并发） |

**所有刷新操作的统一保护**：
```csharp
var code = self.BanLayerOptionForever();    // 禁止 UI 交互
self.SyncPoolCreateInterval(true);          // 开启创建间隔
try { await self.m_Owner.RefillCells(...); }
finally {
    self.SyncPoolCreateInterval(false);
    self.RecoverLayerOptionForever(code);   // 恢复 UI 交互
}
```

**创建间隔同步逻辑**：
```csharp
private static void SyncPoolCreateInterval(this YIUILoopScrollChild self, bool open) {
    // open=true 或 ForeverInterval=true 时使用创建间隔，否则为 0
    self.m_ItemPool.ChangeCreateInterval((open || self.m_Owner.u_ForeverInterval) ? self.m_Owner.u_CreateInterval : 0);
}
```

---

#### `YIUILoopScrollChildSystem_OnClick` — 点击管理

**初始化**：
```csharp
SetOnClick(string eventName)
// 从 LoopScrollRect 读取配置（MaxClickCount/AutoCancelLast/RepetitionCancel）
// 设置 m_OnClickInit = true（只能调用一次）
// 实际绑定在 AddOnClickEvent(item) 中完成（每个新建 Item 时绑定）
```

**绑定事件的时机**：`OnCreateItemRenderer` 创建 Item 后调用 `AddOnClickEvent(item)`，从 `item.GetParent<YIUIChild>().EventTable.FindEvent<UIEventP0>(eventName)` 获取事件，注册匿名 Lambda（使用 EntityRef 避免悬空引用）。

**选中队列算法**：
```
OnClickItemQueueEnqueue(index):
  ① index 已在 HashSet 中（已选中）:
     - RepetitionCancel=true → RemoveSelectIndex(index) → return false（取消选中）
     - RepetitionCancel=false → return true（重复回调，select=true）
  ② Queue.Count >= MaxClickCount:
     - AutoCancelLast=true → OnClickItemQueuePeek()（出队最早，触发 false 回调）→ 继续入队
     - AutoCancelLast=false → return false（拒绝选中）
  ③ 正常入队: HashSet.Add(index) + Queue.Enqueue(index) → return true
```

**外部触发点击的两种方式**：
```csharp
// 方式1：传入 Entity
self.OnClickItem(item);

// 方式2：传入索引（item 不可见时不触发回调，但选中状态会记录）
self.OnClickItem(index);
```

动态配置：
```csharp
SetOnClickCheck(bool)         // 启用/禁用点击前检查（需先 SetOnClick）
ChangeAutoCancelLast(bool)    // 动态修改自动取消最早选中
ChangeRepetitionCancel(bool)  // 动态修改重复点击取消选中
ChangeMaxClickCount(int, bool reset=true)  // 动态修改最大选中数（会先 ClearSelect）
```

---

## 关键流程图

### Item 创建流程（首次/对象池为空）

```
RefillCells()
  → LoopScrollRectBase.UpdateItems() 需要新 Item
  → LoopScrollRect.GetFromTempPool(itemIdx)
    → deletedItemTypeStart/End > 0 → 复用已标记的 Item（改 SiblingIndex）
    → 否则 → prefabSource.GetObject(itemIdx) [ETTask]
      → LoopScrollPrefabAsyncSourceExtensions → TypeSystems → YIUILoopScrollPrefabAsyncSource
        → YIUILoopScrollChild.GetObject(index)
          → m_ItemPool.Get() [异步，有创建间隔]
            → OnCreateItemRenderer()  ← 池为空时触发
              → EventSystem.YIUIInvokeEntityAsync(LoadInstantiateByVo)  [异步加载 Prefab]
              → AddItemRendererByDic(transform, item)   [注册 Transform→Entity 映射]
              → AddOnClickEvent(item)                   [绑定点击事件]
            → 返回 Entity
          → 取 OwnerRectTransform 作为 GameObject
  → 设置 Transform 位置/父级
  → ProvideData(nextItem, itemIdx)
    → LoopScrollRect.ProvideData → dataSource.ProvideData → TypeSystems → YIUILoopScrollDataSource
      → YIUILoopScrollChild.ProvideData(transform, index)
        → GetItemRendererByDic(transform)    [Transform → Entity]
        → ResetItemIndex(transform, index)   [记录当前 Index]
        → YIUILoopHelper.Renderer()          [分发到业务 Renderer]
```

### Item 回收流程

```
LoopScrollRectBase 滚出可见区 → ReturnToTempPool(fromStart, count)
  → deletedItemTypeStart/End += count  （标记可复用，不立即归还）

ClearTempPool() 时：
  → 遍历标记 Item → prefabSource.ReturnObject(transform)
    → LoopScrollPrefabAsyncSourceExtensions → YIUILoopScrollChild.ReturnObject(transform)
      → GetItemRendererByDic(transform)
      → m_ItemPool.Put(item)                   [归还对象池]
      → ResetItemIndex(transform, -1)           [标记为池中状态]
      → transform.SetParent(u_CacheRect, false) [移到隐藏缓存节点]
```

### SetDataRefresh 完整流程

```
SetDataRefresh(data)
  → self.Data = data              [触发 Data setter：推断类型，构建系统类型]
  → m_Owner.totalCount = data.Count
  → RefillCells()
    → BanLayerOptionForever()     [禁止 UI 交互]
    → SyncPoolCreateInterval(true)
    → m_Owner.RefillCells(0, 0)   [LoopScrollRectBase 重建所有 Cell]
    → SyncPoolCreateInterval(false)
    → RecoverLayerOptionForever() [恢复 UI 交互]
```

---

## Demo 示例（来自 LoopScrollVerticalViewComponentSystem）

```csharp
// YIUIInitialize 中初始化（代码生成 + 手写扩展）
[EntitySystem]
private static void YIUIInitialize(this LoopScrollVerticalViewComponent self) {
    // 使用 Awake<LoopScrollRect, Type, string> 完整初始化
    self.m_Loop = self.AddChild<YIUILoopScrollChild, LoopScrollRect, Type, string>(
        self.u_ComLoopScrollVertical,         // Inspector 引用的 LoopScrollRect
        typeof(LoopScrollRectDemoItemComponent),  // Item 的 Entity 类型
        "u_EventSelect"                       // Item 上的点击事件名
    );
}

// YIUIOpen 中设置数据
[EntitySystem]
private static async ETTask<bool> YIUIOpen(this LoopScrollVerticalViewComponent self) {
    List<int> list = Enumerable.Range(0, 100).ToList();
    self.Loop.ClearSelect();
    self.Loop.SetDataRefresh(list, 0).NoContext();  // 默认选中第 0 项
    return true;
}

// 渲染逻辑（EntitySystem 自动注册到 TypeSystems）
[EntitySystem]
private static void YIUILoopRenderer(this LoopScrollVerticalViewComponent self,
    LoopScrollRectDemoItemComponent item, int data, int index, bool select) {
    item.u_DataIndex.SetValue(index);
    item.u_DataSelect.SetValue(select);
}

// 点击回调
[EntitySystem]
private static void YIUILoopOnClick(this LoopScrollVerticalViewComponent self,
    LoopScrollRectDemoItemComponent item, int data, int index, bool select) {
    item.u_DataSelect.SetValue(select);
}
```

**关键**：`YIUILoopRenderer` 和 `YIUILoopOnClick` 方法签名直接写在 ComponentSystem 类中即可（无需显式继承 YIUILoopRendererSystem），框架通过代码生成的 Gen 文件自动识别并注册。

---

## 依赖关系

```
cn.etetet.yiuiloopscrollrectasync
  直接依赖:
    cn.etetet.core          (ET 框架核心：Entity/EntitySystem/ETTask/Log/EntityRef)
  运行时引用 (非 package 依赖，通过 asmref 关联):
    YIUIFramework           (YIUIBindVo/YIUIChild/UIEventP0/ObjAsyncCache/YIUIMgr/IYIUIBind 等)
    ET.Client               (CoroutineLockComponent/YIUIInvokeEntity_LoadInstantiateByVo)
  被以下包/系统使用:
    cn.etetet.yiuisuperscroll  (超级滚动列表，类似组件)
    业务逻辑层               (通过 YIUILoopRendererSystem/YIUILoopOnClickSystem 实现各业务列表)
```

---

## 架构模式

1. **ECS 系统模式**：`YIUILoopScrollChild` 是纯数据 Entity，`YIUILoopScrollChildSystem` 是无状态静态系统，符合 ET ECS 规范

2. **三层泛型动态分发**：业务 Renderer/OnClick 通过 `MakeGenericType(OwnerType, ItemType, DataType)` 构造精确类型，由 TypeSystems 在运行时动态查找实现，实现了强类型安全的多态分发

3. **TempPool 延迟回收**：滚动时先标记（`deletedItemTypeStart/End`），优先复用已标记 Item（移动 SiblingIndex），最后批量 `ClearTempPool` 归还，减少对象池操作次数

4. **双向接口适配**：同时保留原始 `LoopScrollDataSource` / `LoopScrollPrefabAsyncSource` 接口和 YIUI 的 ECS 接口，通过 EntitySystem 桥接，保持与原版 LoopScrollRect 的兼容性

5. **UI 交互保护**：刷新列表期间自动调用 `YIUIMgr.BanLayerOptionForever()` 禁止 UI 层操作，防止刷新中用户点击导致状态错误；使用 HashSet 追踪持有的 code，Destroy 时自动释放防止资源泄漏

6. **EntityRef 安全引用**：异步操作前后使用 `EntityRef<T>` 模式（`selfRef = self` → `await` → `self = selfRef`），防止异步期间 Entity 被销毁导致空引用

---

## 注意事项与边界情况

- **Item 必须有 LayoutElement/LayoutGroup**：编辑器模式下首次创建时会检查，缺少则报错（仅 Editor 检查，Runtime 不检查）
- **SetOnClick 只能初始化一次**：再次调用会报错，不可修改事件名
- **GetItemByIndex 只能获取可见 Item**：不可见的返回 null，需先检查 index >= ItemStart && index < ItemEnd
- **SetDataRefreshShowAll 注意**：通过 offset=99999 强制所有 Cell 可见，会创建 data.Count 个实例，数据量大时慎用
- **默认选中是 += 操作**：`SetDataRefresh(data, index)` 内部调用 `SetDefaultSelect`（等价于 Queue.Enqueue），多次调用前需先 `ClearSelect`
- **泛型列表须先 SetDefaultDataType**：当 `IList` 是 `List<interface>` 或 `List<object>` 时，`m_Data[0].GetType()` 无法得到正确类型，必须调用 `SetDefaultDataType(typeof(ConcreteType))`
- **GridLayout 不支持 Flexible 约束**：使用 GridLayoutGroup 时仅支持 FixedColumnCount/FixedRowCount，Flexible 会报警
- **PreLoadCount 预加载时机**：在 Awake 阶段触发（调用 `AwakePreLoad`），使用协程锁保证不并发，适合打开即刷新且数据量固定的场景
- **u_ForeverInterval**：滚动期间的 Item 创建也受间隔控制（默认关闭），开启后每帧只创建固定数量，适合低端设备优化
