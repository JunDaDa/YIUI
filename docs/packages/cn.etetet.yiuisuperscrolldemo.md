# cn.etetet.yiuisuperscrolldemo

**版本**: 0.0.2
**分类**: UI/YIUI
**命名空间**: `SuperScrollView`
**依赖**: `cn.etetet.core` ^1.0.0

## 概述

SuperScrollDemo 是 `cn.etetet.yiuisuperscroll`（SuperScrollView 循环列表）的官方演示包。本包提供了大量 Demo 场景、示例 Item 脚本、数据源管理器以及 UI 控制面板，涵盖了循环列表所有功能的使用方式，是开发者学习和参考 SuperScrollView 用法的重要资源。

本包**不对外导出业务逻辑**，而是作为示例工程集，演示如何将 LoopListView2 组件与实际数据绑定、如何实现各种滚动交互效果。

---

## 目录结构

```
cn.etetet.yiuisuperscrolldemo/
├── Assets/Demo/
│   ├── Prefabs/          # 各 Demo 场景用按钮面板 Prefab
│   └── Scenes/           # 按功能分类的 Demo 场景（约 70+ 个）
│       ├── Chat/         # 聊天视图 Demo
│       ├── DraggableView/# 可拖拽列表 Demo
│       ├── Gallery/      # 画廊视图 Demo
│       ├── GridView/     # 网格视图 Demo
│       ├── ListView/     # 普通列表 Demo
│       ├── PageView/     # 分页视图 Demo
│       ├── SpecialView/  # 特殊 GridView Demo（多尺寸 item）
│       ├── StaggeredView/# 瀑布流 Demo
│       └── TreeView/     # 树形视图 Demo
└── Runtime/Demo/Scripts/
    ├── Base/             # 辅助工具脚本
    ├── ButtonPanel/      # UI 控制面板脚本
    ├── DataSource/       # 数据源管理脚本
    └── Item/             # 列表 Item 组件脚本
```

---

## 核心类说明

### DataSource 层

#### `ItemDataBase`
所有数据模型的基类。使用静态自增计数器分配全局唯一 `mId`（跨场景唯一，程序生命周期内单调递增）。

```csharp
public class ItemDataBase {
    static int mItemDataTotalCount = 0; // 全局计数器（非重置）
    public int mId;                      // 唯一 ID，用于动画/删除定位
    public virtual void Init(int index) {}
    public virtual void Init(int index, int parentIndex) {}
    public virtual void OnIndexChanged(int index) {}
    public virtual void OnIndexChanged(int index, int parentIndex) {}
    public virtual bool IsFilterMatched(string filterStr) => true;
}
```

**注意**: `mItemDataTotalCount` 是静态字段，跨场景加载不会重置，新 `ItemDataBase` 子类实例的 `mId` 始终递增。

#### `ItemData : ItemDataBase`
通用数据模型，包含：
- `mName`, `mDesc`, `mDescExtend` - 文本内容
- `mIcon`, `mContentImage` - 图标/图片资源名称
- `mStarCount` - 星级（0-5）
- `mChecked`, `mIsExpand` - 状态标志
- `mSliderValue`, `mInputFieldText` - 滑块/输入框状态
- `mParentIndex` - 父节点索引（树形视图使用）

#### `DataSourceMgr<T> where T : ItemDataBase, new()`
泛型数据源管理器，核心字段：
```csharp
List<T> mItemDataList;
bool mIsWaittingRefreshData;   // 拼写注意：Waitting（双t）
bool mIsWaitLoadingMoreData;
float mDataRefreshLeftTime;    // 倒计时（秒）
float mDataLoadLeftTime;
int mLoadMoreCount = 20;       // 默认追加数量
```

| 方法 | 说明 |
|------|------|
| `DataSourceMgr(int count)` | 构造时立即调用 `DoRefreshDataSource(count)` 同步初始化 |
| `GetItemDataByIndex(int)` | 越界返回 null |
| `TotalItemCount` { get } | 返回列表总数 |
| `ItemDataList` { get } | 直接暴露内部 List（慎用引用修改） |
| `RequestRefreshDataList(Action)` | 设置 1 秒倒计时，倒计时结束后重建全部数据并回调 |
| `RequestLoadMoreDataList(int, Action)` | 设置 1 秒倒计时，倒计时结束后追加数据并回调 |
| `SetDataTotalCount(int)` | 多则 `AppendData`，少则 `RemoveRange` |
| `InsertData(int)` | 创建新数据插入，自动更新后续索引 |
| `InsertData(int, T)` | 插入指定数据实例 |
| `RemoveData(int)` | 按索引删除（**不**更新后续索引！） |
| `RemoveDataByItemId(int)` | 按 mId 查找删除，**会**更新后续索引 |
| `ExchangeData(int, int)` | 直接交换引用，不调用 OnIndexChanged |
| `AppendData(int)` | 批量在末尾追加 |
| `AppendData(T)` | 追加单条已有数据 |
| `GetFilteredItemList(string)` | 空 filterStr 直接返回原列表 |
| `Update()` | 在 MonoBehaviour.Update 中调用，驱动异步倒计时 |

**重要**：`RemoveData(int)` 与 `RemoveDataByItemId(int)` 行为不同——前者不更新后续数据索引，业务代码需注意。

#### `TreeViewItemData<T> where T : ItemDataBase, new()`
树形节点容器（非 MonoBehaviour，纯 C# 类）：

```csharp
public class TreeViewItemData<T> {
    public string mName;                        // 节点名称（"Item{index}"）
    public List<T> mChildItemDataList;          // 子条目数据列表

    void RefreshItemDataList(int index, int childCount) // 初始化节点及子条目
    T AddNewItemChild(int parentIndex, int childIndex)  // 新建并插入子条目
    void AddChildByIndex(int index, int childIndex, T data) // 插入已有数据并更新索引
    bool RemoveChildByIndex(int index, int childIndex)  // 删除子条目并更新索引
    T GetItemChildDataByIndex(int childIndex)           // 取子条目
}
```

#### `TreeViewDataSourceMgr<T>`
树形视图专用数据管理器，默认初始化 1000 个父节点（前 20 个子节点数量按预设数组，其余默认 30 个）：

```csharp
// 预设子节点数量：2,3,7,2,8,4,10,5,9,30（循环两次，共前20个）
static int[] mTreeViewChildItemCount = { 2,3,7,2,8,4,10,5,9,30, 2,3,7,2,8,4,10,5,9,30 };
int mTreeViewItemCount = 1000;
int mTreeViewChildItemCountDefault = 30; // 第21个起每节点30子项
```

| 方法 | 说明 |
|------|------|
| `TreeViewItemCount` { get } | 父节点总数 |
| `TotalTreeViewItemAndChildCount` { get } | 所有已展开节点的显示行总数（含子条目） |
| `GetItemDataByIndex(int)` | 取父节点数据 |
| `GetItemChildDataByIndex(int, int)` | 取某父节点的指定子条目 |
| `AddNewItemChild(int, int)` | 新建子条目 |
| `AddNewItem(int)` | 新建父节点并插入指定位置 |
| `RemoveItem(int)` | 删除父节点 |
| `AddItemChild(int, int, T)` | 插入已有子条目数据 |
| `RemoveItemChild(int, int)` | 删除子条目 |

#### `TreeViewItemCountMgr`
树形视图**索引映射管理器**（与数据管理器分离），负责维护展开/折叠状态到线性索引的映射：

```csharp
public class TreeViewItemCountData {
    public int mTreeItemIndex;  // 父节点索引
    public int mChildCount;     // 子条目数量
    public bool mIsExpand;      // 是否展开
    public int mBeginIndex;     // 展平后的起始索引（即父节点行）
    public int mEndIndex;       // 展平后的结束索引（最后子条目行）

    bool IsChild(int index)       // index != mBeginIndex 则为子条目
    int GetChildIndex(int index)  // 返回 index - mBeginIndex - 1
}
```

核心算法：`QueryTreeItemByTotalIndex(int totalIndex)` 用**二分查找**在展平索引中定位父节点，并缓存上次结果（`mLastQueryResult`）加速连续访问。`mIsDirty` 标记控制延迟重建索引。

| 方法 | 说明 |
|------|------|
| `AddTreeItem(int, bool)` | 追加父节点（子节点数, 是否展开） |
| `AddTreeItemBeforeIndex(int, int, bool)` | 在指定位置插入父节点，更新后续 mTreeItemIndex |
| `RemoveTreeItem(int)` | 删除父节点，更新后续 mTreeItemIndex |
| `SetItemChildCount(int, int)` | 更新子节点数量，标记 dirty |
| `AddItemChildCount(int, int)` | 增量更新子节点数量 |
| `SetItemExpand(int, bool)` | 设置展开状态，标记 dirty |
| `ToggleItemExpand(int)` | 切换展开状态 |
| `GetTotalItemAndChildCount()` | 返回当前展开状态下的总显示行数 |
| `QueryTreeItemByTotalIndex(int)` | 二分查找：根据展平索引找到对应父节点 |

#### `ChatMsgDataSourceMgr : MonoBehaviour`
聊天视图的**单例 MonoBehaviour** 数据管理器（挂载在 ResMgr Prefab 上）：

```csharp
static ChatMsgDataSourceMgr instance = null;
public static ChatMsgDataSourceMgr Get  // FindAnyObjectByType 懒加载单例
```

数据结构：
- `PersonInfo` { mId, mName, mHeadIcon } — 2 个固定人物（"Jaci" id=0，"Toc" id=1）
- `ChatMsg` { mPersonId, mMsgType(Str/Picture), mSrtMsg, mPicMsgSpriteName } — 消息结构
- `MsgTypeEnum` { Str=0, Picture, Count }
- 初始化 100 条随机消息（文本/图片各约 50%，随机分配发送人）

| 方法 | 说明 |
|------|------|
| `Init()` | 重建人物表和聊天消息 |
| `GetPersonInfo(int)` | 按 personId 取人物信息 |
| `GetChatMsgByIndex(int)` | 按索引取消息 |
| `AppendOneMsg(int personId)` | 追加一条随机消息（指定发送人） |

#### 枚举类型
```csharp
// AnimationType.cs
enum AnimationType { Clip, Fade, ClipFade, SlideLeft, SlideRight }

// ExpandAnimationType.cs（推测，未确认）
enum ExpandAnimationType { ... }
```

#### 其他数据类
| 类名 | 用途 |
|------|------|
| `SimpleItemData` | 仅有 name 的简单数据（继承 ItemDataBase） |
| `ContentFitterItemData` | 自适应高度数据（含随机长度字符串） |
| `DraggableItemData` | 可拖拽条目数据（mName, mIcon） |
| `NestedItemData` / `NestedSimpleItemData` | 嵌套列表数据 |
| `SimpleExpandItemData` | 可展开/折叠数据 |
| `DescList` | 描述文本列表（为 ItemData.mDesc 提供随机内容） |

---

### ButtonPanel 层（UI 控制面板）

每个 Demo 场景都挂载一个对应的 ButtonPanel。**注意**：大多数 ButtonPanel 类不是 MonoBehaviour，而是普通 C# 类，由场景中的 MonoBehaviour 持有并在 Start/Update 中委托调用。

#### `ButtonPanel`（标准面板）
核心控制按钮：
- **SetCount** - 输入数量后调用 `SetListItemCount` + `RefreshAllShownItem`
- **ScrollTo** - 调用 `MovePanelToItemIndex` 跳转到指定 item
- **Add** - 在指定位置插入一条新数据
- **Back** - 调用 `ButtonPanelMenuList.BackToMainMenu()` 返回主菜单

#### `ButtonPanelDelete`
增加了 Delete 按钮，支持带 `AnimationHelper` 的动画删除条目。

#### `ButtonPanelLoad`
增加了 Refresh（下拉刷新）和 LoadMore（上拉加载）操作，通过 `DataSourceMgr.RequestRefreshDataList / RequestLoadMoreDataList` 模拟 1 秒网络请求延迟。

#### `ButtonPanelGridView` / `ButtonPanelGridViewDelete` / `ButtonPanelGridViewLoad`
网格视图专用面板，功能与 ListView 对应面板类似。

#### `ButtonPanelGallery`
画廊视图面板，可能包含画廊效果参数控制。

#### `ButtonPanelStaggeredView`
瀑布流视图面板。

#### `ButtonPanelTreeView`
树形视图面板（非 MonoBehaviour，由场景 MonoBehaviour 持有）：
- **ScrollTo(itemIndex, childIndex)** - 展开指定父节点，跳转到子条目
- **ExpandAll** - 遍历所有父节点调用 `SetItemExpand(i, true)`，刷新列表
- **CollapseAll** - 收起所有父节点
- **Add** - 在指定父节点的指定位置插入新子条目
- 持有 `LoopListView2`, `TreeViewDataSourceMgr<ItemData>`, `TreeViewItemCountMgr` 三个核心对象

#### `ButtonPanelTreeViewSimple` / `ButtonPanelTreeViewSticky`
树形视图简化版/Sticky Header 版面板。

#### `ButtonPanelSpecial` / `ButtonPanelSpecialDelete` / `ButtonPanelSpecialLoad`
SpecialGridView（多尺寸 item）专用面板。

#### `ButtonPanelNested` / `ButtonPanelNestedSimple`
嵌套列表视图面板。

#### `ButtonPanelMenuList : MonoBehaviour`（主菜单列表）
静态维护所有 Demo 场景名称（5 组，共 61 个场景）：

| 数组 | 场景类型 | 数量 |
|------|---------|------|
| `sceneArray0` | ListView 系列（含 Gallery） | 15 |
| `sceneArray1` | GridView + SpinPicker 系列 | 15 |
| `sceneArray2` | Staggered/Chat/Tree/Page 系列 | 14 |
| `sceneArray3` | SpecialGridView + NestedListView | 15 |
| `sceneArray4` | 动画效果 + DraggableView | 17 |

主菜单场景名称：
```csharp
string[] mainMenuSceneArray = {
    "MenuListViewGallery", "MenuGridViewResponsiveSpin",
    "MenuStaggeredChatTreePage", "MenuSpecialViewNested",
    "MenuListAnimationDraggable"
};
```

`OnButtonClick()` 逻辑：根据自身在父节点中的子索引 + `sceneArrayIndex` 字段确定场景名称，调用 `SceneManager.LoadScene`。

#### `ButtonPanelMenu`
持有 `lastSelectSceneArrayIndex`（静态字段），用于返回主菜单时恢复上次分类页签。

---

### Item 层（列表条目组件）

所有 Item 脚本均为 `MonoBehaviour`，挂载在对应 Prefab 上，通过 `LoopListView2` 的回调函数被创建/复用。**已补充 Round 1 遗漏的 Base* 基类**。

#### 新增：Base* 基类体系

| 基类 | 说明 |
|------|------|
| `BaseVerticalItem` | 垂直布局 Item 基类 |
| `BaseVerticalItemList` | 持有 `List<BaseVerticalItem>` 并统一调用 `Init()` |
| `BaseHorizontalItem` | 水平布局 Item 基类 |
| `BaseHorizontalItemList` | 持有 `List<BaseHorizontalItem>` 并统一调用 `Init()` |
| `BaseVerticalLineItem` | 垂直行 Item（用于 GridView 行级别包装） |
| `BaseVerticalLineItemList` | 同上的 List |
| `BaseRowColItem` | 行列 Item（用于行内单元格） |
| `BaseRowColItemList` | 同上的 List |
| `BaseHorizontalToggleItem` | 带 Toggle 的水平 Item 基类 |
| `BaseHorizontalToggleItemList` | 同上的 List |

这些基类用于 GridView 的行-列二级结构：外层 `XxxItemList` 对应一行，内层 `XxxItem` 对应行内每个单元格。

#### 核心 Item 类型（完整）

| 类名 | 配套 List 类 | 说明 |
|------|------------|------|
| `SimpleItem` | `SimpleItemList` | 基础条目：文本+图标+点击回调 |
| `IconItem` | `IconItemList` | 纯图标条目 |
| `IconTextItem` | `IconTextItemList` | 带图标文本 |
| `IconTextDescItem` | `IconTextDescItemList` | 带图标+描述 |
| `ImageItem` | `ImageItemList` | 纯图片条目 |
| `TextDescRowColItem` | `TextDescRowColItemList` | 文本描述行列条目 |
| `ToggleItem` | `ToggleItemList` | 带 Toggle |
| `ToggleRowColItem` | `ToggleRowColItemList` | 带 Toggle 的行列条目 |
| `SliderItem` | `SliderItemList` | 带 Slider |
| `SliderComplexItem` | `SliderComplexItemList` | 带 Slider（复杂版） |
| `InputFieldItem` | `InputFieldItemList` | 带 InputField |
| `ExpandItem` | — | 可展开折叠 |
| `ExpandAnimationItem` | — | 带动画的展开折叠 |
| `ChatViewItem` | — | 聊天气泡（动态高度计算） |
| `TreeViewItem` | — | 树形视图子条目节点（带星级评分） |
| `TreeViewItemHead` | — | 树形视图父节点标头（带展开/折叠箭头） |
| `PageViewItem` | — | 翻页视图条目 |
| `GalleryHorizontalItem` | — | 水平画廊效果 |
| `GalleryVerticalItem` | — | 垂直画廊效果 |
| `LoadItem` | — | 上拉加载指示器 |
| `LoadClickItem` | — | 点击加载更多 |
| `LoadComplexItem` | — | 复杂加载指示器 |
| `SimpleLoadItem` | — | 简版加载指示器 |
| `AddAnimationItem` | — | 带插入动画 |
| `DeleteAnimationItem` | — | 带删除动画 |
| `DraggableVerticalItem` | — | 可拖拽排序（垂直）含 DragBar |
| `DraggableHorizonalItem` | — | 可拖拽排序（水平，注意拼写无 z） |
| `ContentFitterItem` | — | 自适应高度（ContentSizeFitter） |
| `NestedTopBottomItem` | — | 外层嵌套条目（内嵌垂直 LoopListView） |
| `NestedLeftRightItem` | — | 外层嵌套条目（内嵌水平 LoopListView） |
| `NestedGridViewTopBottomItem` | — | 外层嵌套条目（内嵌垂直 GridView） |
| `NestedGridViewLeftRightItem` | — | 外层嵌套条目（内嵌水平 GridView） |
| `NestedSimpleLeftRightItem` | — | 简版嵌套（水平） |
| `NestedSimpleGridViewTopBottomItem` | — | 简版嵌套（垂直 GridView） |
| `SpinPickerItem` | — | 转盘选择器条目（日期/时间） |

#### `TreeViewItem` 详解
```csharp
public class TreeViewItem : MonoBehaviour {
    public Text mNameText, mDesc, mDescExtend;
    public Image mIcon;
    public Image[] mStarArray;  // 星级数组，点击第 i 颗设置 mStarCount = i+1

    void Init()  // 给每颗星注册 ClickEventListener（依赖 yiuisuperscroll 中的 ClickEventListener）
    void SetItemData(ItemData, int itemIndex, int childIndex) // 接受父子双索引
    void SetStarCount(int count)  // 点亮前 count 颗（mRedStarColor），其余灰色
}
```

#### `ChatViewItem` 详解（动态高度核心逻辑）

```csharp
// 文本消息高度计算
mMsgText.GetComponent<ContentSizeFitter>().SetLayoutVertical(); // 强制布局
Vector2 bgSize = new Vector2(
    mMsgText.sizeDelta.x + 20,  // 宽度 = 文本宽 + padding 20
    mMsgText.sizeDelta.y + 34   // 高度 = 文本高 + padding 34
);
float itemHeight = Mathf.Max(bgSize.y, 75);  // 最小高度 75
tf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);

// 图片消息高度计算
mMsgPicMask.sizeDelta = new Vector2(
    sprite.rect.width * mMsgPicScaleX,   // 默认缩放 0.7
    sprite.rect.height * mMsgPicScaleY
);
float itemHeight = Mathf.Max(picMask.sizeDelta.y + 20, 75);
```

---

### Base 层（辅助工具）

#### `AnimationHelper`（完整 API）
基于时间驱动的**线性速度**动画系统（非 Unity Animator，无缓动曲线）：

```csharp
// 内部结构
class AnimationData {
    float mCurValue, mTargetValue, mSpeed;
    bool mIsFinished;
    bool Update(float deltaTime)  // 线性累加，到达目标设 mIsFinished
}
Dictionary<int, AnimationData> mAnimationDataDict;  // key: itemId
```

| 方法 | 说明 |
|------|------|
| `StartAnimation(itemId, startValue, targetValue, totalTime, forceFromStart=false)` | `speed = 1/totalTime`，若动画已存在且 !forceFromStart 则从当前值继续 |
| `RemoveAnimation(itemId)` | 移除动画记录 |
| `IsAnimationFinished(itemId)` | 不存在时返回 true |
| `UpdateAllAnimation(deltaTime)` | 遍历更新所有动画（每帧调用） |
| `GetCurAnimationValue(itemId)` | Clamp01 后返回，不存在时返回 -1f（注意！-1 需特殊处理） |
| `AllAnimationKeys` { get } | 返回当前所有动画的 itemId 列表 |

**注意**：`GetCurAnimationValue` 返回 -1f 表示该 itemId 无动画，调用方需判断。

#### `TweenHelper`
DOTween 风格的补间工具，支持 position/scale/alpha 动画。

#### `RotateScript`（Round 1 遗漏）
物体旋转脚本，用于 Demo 中的旋转效果展示。

#### `DragChangSizeScript`
通过拖拽改变 RectTransform 大小，用于演示动态视口高度变化。

#### `DragEventHelper` / `DragEventHelperEx` / `DragEventForward`
封装 Unity EventSystem 拖拽事件（`IDragHandler`、`IBeginDragHandler`），用于可拖拽排序 Demo。

#### `OneDirectionDragHelper`
单轴拖拽限制辅助（横向/纵向），防止嵌套滚动冲突。

#### `AutoSetAnchorPosForIphonex`
针对 iPhone X 刘海屏的安全区适配辅助。

#### `FPSDisplay`
屏幕实时 FPS 显示（仅 Demo 调试用）。

---

## 关键流程

### 基本列表初始化流程

```csharp
void Start() {
    mDataSourceMgr = new DataSourceMgr<ItemData>(100);  // 同步创建100条数据
    mLoopListView = GetComponent<LoopListView2>();
    mLoopListView.InitListView(mDataSourceMgr.TotalItemCount, OnGetItemByIndex);
}

LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index, int lastIndex) {
    ItemData itemData = mDataSourceMgr.GetItemDataByIndex(index);
    if (itemData == null) return null;
    LoopListViewItem2 item = listView.NewListViewItem("ItemPrefabName");
    item.GetComponent<XxxItem>().SetItemData(itemData, index);
    return item;
}
```

### 树形视图初始化流程（双管理器协作）

```csharp
void Start() {
    // 1. 初始化数据管理器
    mTreeViewDataSourceMgr = new TreeViewDataSourceMgr<ItemData>();

    // 2. 初始化索引管理器
    mTreeItemCountMgr = new TreeViewItemCountMgr();
    int itemCount = mTreeViewDataSourceMgr.TreeViewItemCount;
    for (int i = 0; i < itemCount; ++i) {
        var itemData = mTreeViewDataSourceMgr.GetItemDataByIndex(i);
        mTreeItemCountMgr.AddTreeItem(itemData.ChildCount, isExpand: false);
    }

    // 3. 初始化列表（展平后总行数）
    mLoopListView.InitListView(mTreeItemCountMgr.GetTotalItemAndChildCount(), OnGetItemByIndex);
}

LoopListViewItem2 OnGetItemByIndex(LoopListView2 listView, int index, int lastIndex) {
    // 通过二分查找确定当前行属于哪个父节点
    TreeViewItemCountData itemCountData = mTreeItemCountMgr.QueryTreeItemByTotalIndex(index);
    if (itemCountData.IsChild(index)) {
        // 子条目
        int childIndex = itemCountData.GetChildIndex(index);
        ItemData childData = mTreeViewDataSourceMgr.GetItemChildDataByIndex(
            itemCountData.mTreeItemIndex, childIndex);
        var item = listView.NewListViewItem("TreeViewItem");
        item.GetComponent<TreeViewItem>().SetItemData(childData, itemCountData.mTreeItemIndex, childIndex);
        return item;
    } else {
        // 父节点标头
        var item = listView.NewListViewItem("TreeViewItemHead");
        item.GetComponent<TreeViewItemHead>().SetItemData(...);
        return item;
    }
}
```

### 下拉刷新流程

```csharp
void OnPullDownRefresh() {
    mDataSourceMgr.RequestRefreshDataList(() => {
        mLoopListView.SetListItemCount(mDataSourceMgr.TotalItemCount, false);
        mLoopListView.RefreshAllShownItem();
    });
}

void Update() {
    mDataSourceMgr.Update();  // 必须调用，否则回调永不触发
}
```

### 上拉加载更多流程

```csharp
void OnPullUpLoadMore() {
    mDataSourceMgr.RequestLoadMoreDataList(20, () => {
        mLoopListView.SetListItemCount(mDataSourceMgr.TotalItemCount, false);
        mLoopListView.RefreshAllShownItem();
    });
}
```

### 动态删除带动画流程

```csharp
void OnDeleteButtonClick(int itemId) {
    mAnimationHelper.StartAnimation(itemId, 1f, 0f, 0.3f);  // 1→0，0.3秒
}

void Update() {
    mAnimationHelper.UpdateAllAnimation(Time.deltaTime);
    foreach (int itemId in mAnimationHelper.AllAnimationKeys.ToList()) {
        if (mAnimationHelper.IsAnimationFinished(itemId)) {
            mAnimationHelper.RemoveAnimation(itemId);
            mDataSourceMgr.RemoveDataByItemId(itemId);  // 用 ByItemId 确保索引更新
            mLoopListView.SetListItemCount(mDataSourceMgr.TotalItemCount);
            mLoopListView.RefreshAllShownItem();
        }
    }
}
```

### 树形视图展开/折叠 + 跳转流程

```csharp
void OnExpandAllButtonClicked() {
    for (int i = 0; i < mTreeItemCountMgr.TreeViewItemCount; ++i) {
        mTreeItemCountMgr.SetItemExpand(i, true);
    }
    // 重新计算总行数并刷新
    mLoopListView.SetListItemCount(mTreeItemCountMgr.GetTotalItemAndChildCount(), false);
    mLoopListView.RefreshAllShownItem();
}

void ScrollTo(int itemIndex, int childIndex) {
    mTreeItemCountMgr.SetItemExpand(itemIndex, true);
    TreeViewItemCountData data = mTreeItemCountMgr.GetTreeItem(itemIndex);
    int finalIndex = data.mBeginIndex + (childIndex > 0 ? childIndex + 1 : 0);
    mLoopListView.MovePanelToItemIndex(finalIndex, 0);
}
```

---

## 聊天视图特殊逻辑

`ChatViewItem` 需要动态计算每条消息的高度，关键点：
- **文本消息**：`ContentSizeFitter.SetLayoutVertical()` 强制布局 → 读 sizeDelta → 加 padding(20/34) → 最小高度 75
- **图片消息**：读 `overrideSprite.rect` 的原始像素尺寸 → 乘缩放系数(0.7) → 加 padding 20 → 最小高度 75
- 设置方式：`RectTransform.SetSizeWithCurrentAnchors(Axis.Vertical, height)`

**ChatMsgDataSourceMgr 是 MonoBehaviour 单例**（区别于 DataSourceMgr 是纯 C# 类）：必须挂载在场景中的 GameObject 上（ResMgr Prefab），通过 `ChatMsgDataSourceMgr.Get` 静态属性按需查找。

---

## 依赖关系

```
cn.etetet.yiuisuperscrolldemo
  ├── cn.etetet.core (^1.0.0)          // 核心框架（package.json 显式声明）
  └── cn.etetet.yiuisuperscroll        // SuperScrollView 主库（隐式运行时依赖）
        ├── LoopListView2               // 核心滚动列表组件
        ├── LoopListViewItem2           // 列表项包装
        ├── ClickEventListener          // 点击事件监听（TreeViewItem 使用）
        └── ResManager                  // 资源管理（ChatMsgDataSourceMgr 使用）
```

> `LoopListView2`、`ClickEventListener`、`ResManager` 等类定义在 `cn.etetet.yiuisuperscroll` 包中，本 Demo 包对其有隐式运行时依赖（未在 package.json 中声明）。

---

## Demo 场景分类汇总（完整 61 个）

| 分类 | 代表场景 | 数量 |
|------|---------|------|
| ListView | TopToBottom, LeftToRight, PullDown/PullUp, SelectDelete, Expand, Filter, ContentFitter, MultiplePrefab, Simple, Loop, ClickLoadMore | 15 |
| GridView | TopToBottom, LeftToRight, ClickLoadMore, SelectDelete, Filter, Diagonal, MultiplePrefab, Simple, Responsive | 10 |
| SpinPicker | SpinDatePicker, SpinTimePicker, SpinDateTimePicker | 3 |
| StaggeredView | TopToBottom, LeftToRight, BottomToTop, RightToLeft, Simple(×2) | 6 |
| ChatView | ChatViewDemo, ChatViewChangeViewportHeight | 2 |
| TreeView | TreeView, WithStickyHead, WithChildIndent, Simple | 4 |
| PageView | PageViewDemo, PageViewSimple | 2 |
| SpecialGridView | TopToBottom, LeftToRight, PullDown/PullUp, SelectDelete, Feature, Simple(×2) | 7 |
| NestedListView | Nested List/Grid TopToBottom/LeftToRight, NestedSimple(×3) | 8 |
| 动画效果 | Add/Delete/Expand × (Clip/Fade/ClipFade/SlideLeft/SlideRight 部分) | 13 |
| DraggableView | Fade/非Fade × TopToBottom/LeftToRight | 4 |

---

## 架构模式总结

本 Demo 包整体采用 **MVC 变体**：
- **Model**：`DataSourceMgr<T>` / `TreeViewDataSourceMgr<T>` / `ChatMsgDataSourceMgr` — 数据层
- **View**：各 `XxxItem` MonoBehaviour — 视图层，仅负责数据展示
- **Controller**：各 `XxxList`（MonoBehaviour，持有 LoopListView2 引用和 DataSourceMgr）— 控制层

树形视图额外引入**双管理器分离**模式：`TreeViewDataSourceMgr`（数据内容） + `TreeViewItemCountMgr`（展示索引），通过二分查找实现 O(log n) 的索引映射，支持大规模树形数据。

---

## 最佳实践（从 Demo 总结）

1. **数据与视图分离**：数据始终保存在 `DataSourceMgr` 中，Item 组件仅持有临时引用；增删必须先操作数据源，再通知 LoopListView。
2. **异步加载模拟**：`DataSourceMgr.Update()` 必须在 MonoBehaviour.Update 中调用；使用计时器模拟网络延迟，在回调中刷新列表。
3. **高度动态计算**：自适应内容高度的 Item 需在 `SetItemData` 时立即强制布局并设置 RectTransform 高度（最小高度兜底）。
4. **动画与列表分离**：使用独立的 `AnimationHelper` 管理 item 动画，动画结束后再实际修改列表数据；`GetCurAnimationValue` 返回 -1 时表示无动画需特殊处理。
5. **对象池复用**：通过 `LoopListView2.NewListViewItem(prefabName)` 取 item，支持多 Prefab 混排。
6. **删除方式区分**：`RemoveData(index)` 不更新后续索引（适用于批量删除后手动刷新），`RemoveDataByItemId` 会更新索引（适用于单条带动画删除）。
7. **树形视图双管理器同步**：修改数据后必须同步更新 `TreeViewItemCountMgr`（`SetItemChildCount`/`AddItemChildCount`），两者索引需保持一致。
