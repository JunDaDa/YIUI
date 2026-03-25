# cn.etetet.yiuisuperscroll

## 概述

**ET.YIUI.SuperScroll** 是 YIUI 框架中的高性能循环滚动列表组件包。它基于对象池技术，只渲染视口内可见的 Item，支持超大数据量列表的高效展示。提供三种核心组件：

- **LoopListView2** — 线性循环列表（上下/左右滚动）
- **LoopGridView** — 固定网格循环列表（行/列固定布局）
- **LoopStaggeredGridView** — 瀑布流循环列表（各列高度不一）

所有组件均通过 `partial class` 扩展机制与 YIUI 框架（EntityRef、UIBindCDETable）深度集成。

**版本**: 0.0.1
**依赖**: `cn.etetet.core: 1.0.0`
**命名空间**: `SuperScrollView`

---

## 目录结构

```
cn.etetet.yiuisuperscroll/
├── Runtime/
│   ├── Common/
│   │   ├── ClickEventListener.cs      # 点击/双击/按压事件监听工具（MonoBehaviour）
│   │   ├── CommonDefine.cs            # 枚举和通用结构体定义
│   │   └── ItemPosMgr.cs             # Item 位置管理器（分组二分查找）
│   ├── ListView/
│   │   ├── LoopListView2.cs          # 线性循环列表主类（partial）
│   │   ├── LoopListViewItem2.cs      # 列表 Item 组件（partial）
│   │   └── LoopListItemPool.cs       # ListView Item 对象池（ItemPool partial class）
│   ├── GridView/
│   │   ├── LoopGridView.cs           # 网格循环列表主类（partial）
│   │   ├── LoopGridViewItem.cs       # 网格 Item 组件
│   │   ├── LoopGridItemPool.cs       # GridView Item 对象池
│   │   └── GridItemGroup.cs          # 网格行/列分组（双向链表）
│   └── StaggeredGridView/
│       ├── LoopStaggeredGridView.cs  # 瀑布流列表主类（partial）
│       ├── LoopStaggeredGridViewItem.cs
│       ├── StaggeredGridItemGroup.cs  # 瀑布流列的 Item 分组
│       └── StaggeredGridItemPool.cs
├── RuntimeExtend/                     # YIUI 框架集成扩展（partial class）
│   ├── ListView/
│   │   ├── LoopListView2_Extend.cs   # 添加点击配置、NewListViewItem 等
│   │   ├── LoopListViewItem2_Extend.cs # 添加 OwnerEntity、UIBindCDETable
│   │   └── ItemPool_Extend.cs        # 添加 ResName 获取
│   ├── GridView/
│   │   ├── LoopGridView_Extend.cs
│   │   ├── LoopGridViewItem_Extend.cs
│   │   └── GridItemPool_Extend.cs
│   └── StaggeredGridView/
│       ├── LoopStaggeredGridView_Extend.cs
│       ├── LoopStaggeredGridViewItem_Extend.cs
│       ├── StaggeredGridItemGroup_Extend.cs
│       └── StaggeredGridItemPool_Extend.cs
├── Editor/                            # Unity 编辑器自定义面板
│   ├── LoopListViewEditor2.cs
│   ├── LoopGridViewEditor.cs
│   └── LoopStaggeredGridViewEditor.cs
└── EditorExtend/                      # 编辑器扩展（YIUI 集成）
    ├── LoopListViewEditor2_Extend.cs
    ├── LoopGridViewEditor_Extend.cs
    └── LoopStaggeredGridViewEditor_Extend.cs
```

---

## 核心类和接口

### CommonDefine.cs — 公共定义

| 类型 | 名称 | 说明 |
|------|------|------|
| enum | `SnapStatus` | 吸附状态：NoTargetSet / TargetHasSet / SnapMoving / SnapMoveFinish |
| enum | `ItemCornerEnum` | Item 角位：LeftBottom / LeftTop / RightTop / RightBottom |
| enum | `ListItemArrangeType` | 列表排列方向：TopToBottom / BottomToTop / LeftToRight / RightToLeft |
| enum | `GridItemArrangeType` | 网格排列方向：TopLeftToBottomRight 等4种 |
| enum | `GridFixedType` | 网格固定维度：ColumnCountFixed / RowCountFixed |
| struct | `RowColumnPair` | 行列对，用于网格中标识 Item 位置，实现了 == 操作符 |

---

### ClickEventListener.cs — 点击事件监听器

实现 `IPointerClickHandler, IPointerDownHandler, IPointerUpHandler` 的工具 MonoBehaviour。

```csharp
public class ClickEventListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
```

| 方法 | 说明 |
|------|------|
| `static Get(GameObject)` | 获取或添加组件（单例模式） |
| `SetClickEventHandler(Action<GameObject>)` | 注册单击回调 |
| `SetDoubleClickEventHandler(Action<GameObject>)` | 注册双击回调（clickCount==2） |
| `SetPointerDownHandler(Action<GameObject>)` | 注册按下回调 |
| `SetPointerUpHandler(Action<GameObject>)` | 注册抬起回调 |
| `IsPressd` | 当前是否处于按下状态 |

---

### ItemPosMgr.cs — Item 位置管理器

高效管理大量 Item 的位置信息，使用**分组 + 二分查找**算法。

**ItemSizeGroup**：每组最多管理 100 个 Item 的尺寸和起始位置。
- 支持 dirty 标记延迟计算，避免每次修改都全量更新
- `SetItemSize(index, size)` — 设置 Item 尺寸并标记 dirty
- `UpdateAllItemStartPos()` — 从 dirty 位置开始重算起始位置
- `GetItemIndexByPos(pos)` — 二分查找给定位置所在的 Item

**ItemPosMgr**：管理所有分组。
- `const mItemMaxCountPerGroup = 100` — 每组 Item 数上限
- `SetItemMaxCount(maxCount)` — 设置 Item 总数，初始化/调整分组结构
- `SetItemSize(itemIndex, size)` — 更新指定 Item 尺寸
- `GetItemPos(itemIndex)` — 获取 Item 的起始坐标
- `GetItemIndexAndPosAtGivenPos(pos, ref index, ref itemPos)` — 根据滚动位置查找对应 Item

---

### LoopListViewInitParam — 初始化参数类

用于自定义 `InitListView` 的可选参数，所有字段都有默认值：

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `mDistanceForRecycle0` | 300 | 顶部/左侧回收距离（必须 > New距离） |
| `mDistanceForNew0` | 200 | 顶部/左侧创建距离 |
| `mDistanceForRecycle1` | 300 | 底部/右侧回收距离 |
| `mDistanceForNew1` | 200 | 底部/右侧创建距离 |
| `mSmoothDumpRate` | 0.3f | Snap 平滑阻尼率 |
| `mSnapFinishThreshold` | 0.01f | Snap 完成阈值 |
| `mSnapVecThreshold` | 145 | Snap 速度阈值（超过此速度时 Snap 激活） |
| `mItemDefaultWithPaddingSize` | 100 | Item 默认尺寸（含 Padding，用于 ItemPosMgr 初始化） |
| `mNeedReplaceScrollbarEventHandler` | true | 是否替换 Scrollbar 事件处理器 |

```csharp
// 使用自定义参数
var param = LoopListViewInitParam.CopyDefaultInitParam();
param.mDistanceForRecycle0 = 500;
listView.InitListView(count, onGetItem, param);
```

---

### ItemPrefabConfData — 预制体配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `mItemPrefab` | `GameObject` | Item 预制体 |
| `mPadding` | float | Item 间距 |
| `mInitCreateCount` | int | 初始创建数量（预热对象池） |
| `mStartPosOffset` | float | 起始偏移量 |

---

### ItemPosStruct — Item 位置结构体

```csharp
public struct ItemPosStruct {
    public int mItemIndex;
    public float mItemOffset;
}
```

用于记录某个 Item 在列表中的绝对偏移位置。

---

### ListViewAutoMoveToItemData — 自动移动数据

管理平滑滚动到指定 Item 的状态：

| 字段 | 说明 |
|------|------|
| `mStartFloatItemIndex` | 开始移动时的浮点 Item 索引 |
| `mTargetItemIndex` | 目标 Item 索引 |
| `mTargetItemOffset` | 目标偏移量 |
| `mDuration` | 移动持续时间 |
| `mPreciseMoveDuration` | 精确移动阶段持续时间 |
| `mIsInPreciseMove` | 是否处于精确移动阶段（靠近目标时切换） |

移动分两阶段：粗略移动（快速接近）→ 精确移动（缓慢对齐）。

---

### ItemPool — 对象池（partial class）

**双层池结构**（核心设计）：

```csharp
List<LoopListViewItem2> mTmpPooledItemList;  // 临时回收池（帧内复用优先）
List<LoopListViewItem2> mPooledItemList;      // 正式回收池
```

**工作原理**：
1. Item 移出视口时调用 `RecycleItem(item)` → 放入 `mTmpPooledItemList`（同帧可快速复用）
2. 帧结束后调用 `ClearTmpRecycledItem()` → 临时池转移到正式池（`SetActive(false)`）
3. 获取 Item 时优先从临时池查找（按 `ItemIndex` 匹配，减少数据重设）；无则从正式池取；池空则 `Instantiate`

| 方法 | 说明 |
|------|------|
| `Init(prefab, padding, offset, count, parent)` | 初始化并预热 |
| `GetItem(itemIndexForSearch)` | 优先复用同 Index 的 Item |
| `GetItemFromTmpPool(itemIndex)` | 仅从临时池查找，不降级 |
| `RecycleItem(item)` | 帧内软回收到临时池 |
| `RecycleItemReal(item)` | 硬回收到正式池（SetActive false） |
| `ClearTmpRecycledItem()` | 清空临时池（转入正式池） |
| `DestroyAllItem()` | 销毁所有对象 |

**YIUI 扩展（ItemPool_Extend.cs）**：
- `YIUICDETable` — 从预制体根节点懒加载 `UIBindCDETable`
- `ResName` — 通过 CDETable 获取资源名

---

### LoopListView2 — 线性循环列表

继承 `MonoBehaviour`，实现 `IBeginDragHandler, IEndDragHandler, IDragHandler`。

**完整 Inspector 字段（含 YIUI 扩展）**：
| 字段 | 来源 | 说明 |
|------|------|------|
| `mArrangeType` | Runtime | 排列方向（上下/左右） |
| `mItemPrefabDataList` | Runtime | 预制体配置列表 |
| `mSupportScrollBar` | Runtime | 是否支持滚动条 |
| `mItemSnapEnable` | Runtime | 是否启用 Snap 吸附 |
| `mViewPortSnapPivot` | Runtime | 视口 Snap 中心点（0=顶/左，1=底/右） |
| `mItemSnapPivot` | Runtime | Item Snap 中心点 |
| `u_MaxClickCount` | RuntimeExtend | 最大可同时选中 Item 数（最小1） |
| `u_AutoCancelLast` | RuntimeExtend | 超出最大数时自动取消最早选中 |
| `u_RepetitionCancel` | RuntimeExtend | 再次点击已选中 Item 是否取消 |

**关键公开回调**：
```csharp
Func<LoopListView2, int, LoopListViewItem2> mOnGetItemByIndex;      // 必须提供
Func<int, (float, float)> mOnGetItemSizeByIndex;                    // 可选，用于预知尺寸
Action<LoopListView2, LoopListViewItem2> mOnSnapItemFinished;        // Snap 完成
Action<LoopListView2, LoopListViewItem2> mOnSnapNearestChanged;      // 最近 Item 变更
Action<LoopListView2, int, float> mOnSmoothMovePanelToItemFinished; // 平滑移动完成
Action mOnBeginDragAction;                                           // 拖动开始
Action mOnDragingAction;                                             // 拖动中
Action mOnEndDragAction;                                             // 拖动结束
Action<LoopListView2> OnListViewStart;                               // Start 时
```

**InitListView 完整签名**：
```csharp
public void InitListView(
    int itemTotalCount,                                           // -1 = 无限列表
    Func<LoopListView2, int, LoopListViewItem2> onGetItemByIndex,
    LoopListViewInitParam initParam = null,                       // 可选参数
    Func<int, (float, float)> onGetItemSizeByIndex = null         // 可选尺寸回调
)
```

**YIUI 扩展方法**：
- `NewListViewItem(prefabIndex)` — 从对象池获取 Item 并挂到 Container
- `GetItemPoolResName(prefabIndex)` — 获取指定 pool 的 ResName（用于资源加载）
- `GetFirstItemIndex()` / `GetLastItemIndex()` — 当前可见首/末 Item 的 ItemIndex（空时返回-1）

**OnItemPrefabChanged(prefabName)**：运行时热替换预制体，会回收所有当前 Item、销毁对应池、重新初始化池并刷新显示。

---

### LoopListViewItem2 — 列表 Item 组件（partial）

**完整字段汇总**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ItemIndex` | int | 数据索引（-1为未初始化） |
| `ItemId` | int | 用户自定义搜索 ID |
| `ItemPrefabName` | string | 对应预制体名称 |
| `Padding` | float | 与下一个 Item 的间距（来自 ItemPrefabConfData） |
| `StartPosOffset` | float | 起始偏移（来自 ItemPrefabConfData） |
| `IsInitHandlerCalled` | bool | 是否已调用初始化回调（避免重复初始化） |
| `ItemCreatedCheckFrameCount` | int | 创建时的帧计数（用于延迟检查） |
| `DistanceWithViewPortSnapCenter` | float | 与视口 Snap 中心的距离（Snap 计算用） |
| `UserObjectData` | object | 用户自定义对象数据 |
| `UserIntData1/2` | int | 用户自定义整数数据 |
| `UserStringData1/2` | string | 用户自定义字符串数据 |
| `TopY/BottomY` | float | Item 在垂直方向的边界坐标（考虑排列方向） |
| `LeftX/RightX` | float | Item 在水平方向的边界坐标 |
| `ItemSize` | float | Item 尺寸（垂直列表取高度，水平列表取宽度） |
| `ItemSizeWithPadding` | float | ItemSize + Padding |
| `CachedRectTransform` | RectTransform | 缓存的 RectTransform（懒初始化） |

**YIUI 扩展字段（LoopListViewItem2_Extend.cs）**：

| 字段/属性 | 类型 | 说明 |
|-----------|------|------|
| `m_OwnerEntityRef` | `EntityRef<Entity>` | 对应 ET Entity 的弱引用 |
| `OwnerEntity` | `Entity` | 通过 EntityRef 访问 |
| `SetOwnerEntity(entity)` | bool | 设置 Entity；传 null 则清除引用 |
| `m_CDETable` | `UIBindCDETable` | 缓存的 CDETable（懒加载） |
| `YIUICDETable` | `UIBindCDETable` | GetComponent 懒加载 |
| `ResName` | string | CDETable?.ResName（空安全） |

---

### LoopGridView — 网格循环列表

与 LoopListView2 相似，但管理二维 Item 分组。

**YIUI 扩展字段（相同模式）**：
```csharp
[SerializeField] public int u_MaxClickCount = 1;
[SerializeField] public bool u_AutoCancelLast = true;
[SerializeField] public bool u_RepetitionCancel;
```

**YIUI 扩展方法**：
- `NewListViewItem(prefabIndex)` → `LoopGridViewItem`
- `GetItemPoolResName(prefabIndex)` → string
- `GetFirstShownItemIndex()` / `GetLastShownItemIndex()` — 使用 `mCurFrameItemRangeData` 的行列范围计算
- `GetFirstShownRowColumn()` / `GetLastShownRowColumn()` → `RowColumnPair`（不存在时返回 (-1,-1)）

**GridItemGroup — 网格行/列分组**：内部维护**双向链表**管理一行或一列的 Item：
- `AddFirst(item)` / `AddLast(item)` / `RemoveFirst()` / `RemoveLast()`
- `GetItemByColumn(column)` / `GetItemByRow(row)` — 遍历链表查找
- `ReplaceItem(curItem, newItem)` — 链表内替换

---

### LoopStaggeredGridView — 瀑布流循环列表

支持各列 Item 高度不同。

**YIUI 扩展（相同模式）**：
- `u_MaxClickCount`, `u_AutoCancelLast`, `u_RepetitionCancel`
- `NewListViewItem(prefabIndex)` → `LoopStaggeredGridViewItem`
- `GetItemPoolResName(prefabIndex)` → string
- `GetFirstShownItemIndex()` — 遍历 `mItemGroupList` 所有 group，取各 group 首 Item 中最小索引
- `GetLastShownItemIndex()` — 遍历所有 group，取各 group 末 Item 中最大索引

注意：StaggeredGridView 的首/末索引计算需要遍历所有列（O(列数)），因为瀑布流各列滚动位置不同，无法像 ListView 那样直接取 `mItemList[0]`。

---

## 实现原理

### 1. 虚拟列表（Virtual Scrolling）

核心思想：内容容器（`mContainerTrans`）随滚动移动，但只有进入视口的 Item 才会被激活和渲染。

**回收/创建阈值（双边）**：
```
[  ← mDistanceForRecycle0=300 → | ← mDistanceForNew0=200 → | VIEWPORT | ← mDistanceForNew1=200 → | ← mDistanceForRecycle1=300 → ]
```
回收阈值 > 创建阈值（差值=100），形成迟滞区，防止频繁创建/回收振荡。

### 2. ItemPosMgr 分组二分查找

将所有 Item 分为若干组（每组 100 个），每次滚动只需：
1. 在组级别二分查找（O(log n/100)）
2. 在组内二分查找（O(log 100)）

总体接近 O(log n)，支持百万级 Item。延迟更新（dirty 标记）确保批量修改后仅重算一次。

### 3. 双层对象池机制

```
每帧回收流程：
  Item 移出视口 → RecycleItem() → mTmpPooledItemList（未 SetActive false）
                                         ↓ 帧结束
                                  ClearTmpRecycledItem() → mPooledItemList（SetActive false）

获取流程：
  GetItem(itemIndex)
    ├── 优先查 mTmpPooledItemList（按 ItemIndex 匹配 → 同帧双向滚动时复用率高）
    │       找不到 → 取 tmpList 末尾元素
    └── tmpList 空 → 查 mPooledItemList（取末尾）
              池空 → Instantiate
```

双层池的关键优化：同帧内滚动方向反转时，刚回收的 Item（还在 tmp 池中，未 SetActive false）可被直接复用，节省一次 SetActive 开销。

### 4. Snap 吸附状态机

```
SnapData 状态转换：
  NoTargetSet
    → 用户拖拽结束（OnEndDrag）
    → 根据速度方向（mSnapVecThreshold=145）确定目标 Item
    → TargetHasSet

  TargetHasSet
    → 开始 SmoothDamp 移动（mSmoothDumpRate=0.3）
    → SnapMoving

  SnapMoving
    → |距离| < mSnapFinishThreshold (0.01)
    → SnapMoveFinish → 触发 mOnSnapItemFinished
    → 清空 → NoTargetSet
```

支持临时目标（`mIsTempTarget`）和强制 Snap（`mIsForceSnapTo`），以及最大速度限制（`mMoveMaxAbsVec`）。

### 5. 平滑移动（SmoothMovePanelToItem）两阶段

1. **粗略移动**：快速接近目标区域（基于时间 `mDuration`）
2. **精确移动**：以 `mPreciseMoveSpeed` 缓慢靠近，直到 `mPreciseMoveLeftDistance ≤ 0`

完成后触发 `mOnSmoothMovePanelToItemFinished(listView, targetIndex, targetOffset)`。

### 6. partial class 扩展架构

原始库代码位于 `Runtime/`，YIUI 集成代码位于 `RuntimeExtend/`。通过 `partial class` 机制：
- 无需修改原始代码即可扩展功能
- 编译后两部分合并为同一类，零运行时开销
- `LoopListViewItem2_Extend` 添加 `EntityRef<Entity>` 字段，实现 ET ECS 与 UI Item 的绑定
- `ItemPool_Extend` 通过 `UIBindCDETable` 读取资源名，支持动态资源加载

---

## 关键流程

### ListView 初始化流程

```
InitListView(itemTotalCount, onGetItemByIndex, [initParam], [onGetItemSizeByIndex])
  ├─ 验证参数（ScrollRect 存在，Recycle > New 阈值）
  ├─ 设置方向（mIsVertList）
  ├─ SetScrollbarListener()    → 替换滚动条事件处理
  ├─ AdjustPivot()             → 调整 ViewPort Pivot
  ├─ AdjustAnchor()            → 调整 Container 锚点
  ├─ AdjustContainerPivot()    → 调整 Container Pivot
  ├─ InitItemPool()            → 为每个预制体创建 ItemPool 并预热
  ├─ mItemPosMgr = new ItemPosMgr(defaultSize)
  └─ MovePanelToItemIndex(0) → UpdateListView() → 填充视口
```

### 每帧 Update 循环

```
Update()
  ├─ 检查 mCurReadyMinItemIndex / mCurReadyMaxItemIndex
  ├─ [顶部] 若首 Item 超出 RecycleDistance0 → RecycleItem() → 尝试在底部创建新 Item
  ├─ [底部] 若末 Item 超出 RecycleDistance1 → RecycleItem() → 尝试在顶部创建新 Item
  ├─ 新创建 Item 调用 mOnGetItemByIndex(listView, index) 回调
  ├─ 更新 SupportScrollBar → 同步滚动条位置
  ├─ 处理 mListViewAutoMoveToItemData（两阶段平滑移动）
  ├─ ClearAllTmpRecycledItem()  ← 帧末清理临时池
  └─ 处理 Snap 逻辑（SmoothDump）
```

### 动态更新 Item 数量

```
SetListItemCount(newCount, [resetPos=true])
  ├─ mItemTotalCount = newCount
  ├─ ItemPosMgr.SetItemMaxCount(newCount)
  ├─ 若 newCount < 当前可见范围 → 回收越界 Item
  ├─ 若 resetPos == true → MovePanelToItemIndex(0)（回到顶部）
  └─ RefreshAllShownItem() → 对所有可见 Item 重调 mOnGetItemByIndex
```

---

## 代码示例

### ListView 基本使用

```csharp
// 1. Inspector 配置：LoopListView2 挂到 ScrollRect GameObject
//    mArrangeType = TopToBottom
//    mItemPrefabDataList 添加预制体（必须有 LoopListViewItem2 组件）

// 2. 代码初始化
[SerializeField] LoopListView2 loopListView;
List<MyData> dataList = new List<MyData>();

void Start() {
    loopListView.InitListView(dataList.Count, OnGetItemByIndex);
}

LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index) {
    if (index < 0 || index >= dataList.Count) return null;

    // YIUI 扩展：从池获取并挂到 Container
    var item = listView.NewListViewItem(0);

    // 获取业务脚本
    var myItem = item.GetComponent<MyItemView>();
    myItem.SetData(dataList[index]);

    // 绑定 ET Entity（YIUI 场景）
    item.SetOwnerEntity(dataList[index].entity);

    return item;
}

// 3. 动态增加数据（如加载更多）
void LoadMore(List<MyData> newData) {
    dataList.AddRange(newData);
    loopListView.SetListItemCount(dataList.Count, false);  // false=不重置位置
    loopListView.RefreshAllShownItem();
}

// 4. 滚动到指定 Item
loopListView.MovePanelToItemIndex(targetIndex, 0f);  // 第二个参数：目标偏移量
```

### 可变尺寸 ListView

```csharp
// 提供尺寸回调（size, padding）
loopListView.InitListView(count, OnGetItemByIndex, null, (index) => {
    float size = dataList[index].IsExpanded ? 200f : 80f;
    return (size, 4f);  // (高度, padding)
});

// 运行时更新某个 Item 的尺寸
loopListView.UpdateItemSizeAtOnce(index, newSize);
```

### GridView 基本使用

```csharp
// Inspector 配置：mFixedRowOrColumnCount=3, mArrangeType=TopLeftToBottomRight
gridView.InitGridView(dataList.Count, (gv, itemIndex, row, column) => {
    var item = gv.NewListViewItem(0);
    item.GetComponent<MyGridItem>().SetData(dataList[itemIndex]);
    return item;
});
```

### 获取可见范围

```csharp
// ListView
int first = listView.GetFirstItemIndex();
int last  = listView.GetLastItemIndex();

// GridView
int first = gridView.GetFirstShownItemIndex();
RowColumnPair rc = gridView.GetFirstShownRowColumn();  // 获取行列信息

// StaggeredGridView（遍历所有列，O(列数)）
int first = staggeredView.GetFirstShownItemIndex();
int last  = staggeredView.GetLastShownItemIndex();
```

---

## 依赖关系

```
cn.etetet.yiuisuperscroll
    ├── cn.etetet.core          (package.json 依赖)
    ├── YIUIFramework           (RuntimeExtend: UIBindCDETable)
    ├── ET                      (RuntimeExtend: EntityRef<Entity>)
    ├── Sirenix.OdinInspector   (RuntimeExtend: LabelText, MinValue 特性)
    └── UnityEngine.UI          (ScrollRect, RectTransform, IPointerClickHandler 等)
```

**被以下 package 使用**（通过 YIUI 业务层）:
- `cn.etetet.yiuisuperscrolldemo` — Demo 示例
- 所有需要大量数据滚动列表的业务 UI panel

---

## 三种组件对比

| 特性 | LoopListView2 | LoopGridView | LoopStaggeredGridView |
|------|--------------|--------------|----------------------|
| 布局 | 单列/单行 | 固定行列网格 | 瀑布流（变高） |
| Item 尺寸 | 可变（动态回调） | 固定 | 各列可不同 |
| Snap 支持 | ✅ | ✅ | ❌ |
| 多 Prefab 支持 | ✅ | ✅ | ✅ |
| 排列方向 | 4方向 | 4角方向 | 垂直/水平 |
| 位置管理 | ItemPosMgr | 行列坐标计算 | 各列独立 ItemPosMgr |
| GetFirstShown 复杂度 | O(1) | O(1) via mCurFrameItemRangeData | O(列数) 遍历 |
| 无限列表支持 | ✅（itemTotal=-1） | 需验证 | 需验证 |

---

## 注意事项和边界情况

1. **ScrollbarVisibility 限制**：`ScrollRect.horizontalScrollbarVisibility` / `verticalScrollbarVisibility` 不能设置为 `AutoHideAndExpandViewport`，否则报错。

2. **OnItemPrefabChanged 热替换**：会强制 RecycleAll + DestroyAll + 重新初始化，会有一帧闪烁，应在非运行时或加载态使用。

3. **无限列表（itemTotalCount=-1）**：ItemIndex 可从 -MaxInt 到 +MaxInt，不支持 Scrollbar。

4. **StaggeredGridView 首末索引**：O(列数) 遍历，列数较多时（如20列）不应每帧调用。

5. **ItemPool.GetItem 的索引匹配优化**：优先从临时池找同 ItemIndex 的 Item，减少 UI 数据重置（因为同帧回收再使用时数据可能未变）。

6. **SnapData 内的 mIsTempTarget**：某些情况下 Snap 先吸附临时目标（最近 Item），等确定惯性方向后再切换为真实目标，实现更自然的 Snap 感。

7. **mCurCreatingItemIndex**：调用 `NewListViewItem` 前必须通过 `mCurCreatingItemIndex` 传入当前 Index，由列表内部在调用 `mOnGetItemByIndex` 前设置，用户侧不需要关心。
