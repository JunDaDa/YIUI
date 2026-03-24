# YIUI SuperScroll ListView 使用指南

本文档基于 `cn.etetet.yiuisuperscrolldemo` 包中的示例代码，详细介绍了如何在 ET9.0 + YIUI 框架中使用 SuperScroll ListView 组件。

## 目录
- [架构概述](#架构概述)
- [基础用法](#基础用法)
- [多预制体支持](#多预制体支持)
- [水平滚动](#水平滚动)
- [API 参考](#api-参考)
- [最佳实践](#最佳实践)

## 架构概述

SuperScroll ListView 在 ET9.0 + YIUI 框架中的架构分为以下几层：

```
View Component (Entity)
    ↓
YIUISuperScrollListComponent (ET Component)
    ↓
LoopListView2 (SuperScrollView Original)
```

### 核心组件

1. **View Component**: 继承自 `Entity`，实现 `IYIUIOpen<ParamVo>` 接口
2. **YIUISuperScrollListComponent**: ET 框架的组件包装器
3. **DataSourceMgr<T>**: 数据源管理器

## 基础用法

### 1. 创建基础 ListView Component

```csharp
namespace ET.Client
{
    public partial class SuperScrollListViewTopToBottomDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        // 使用 EntityRef 弱引用管理 ListView 组件
        public EntityRef<YIUISuperScrollListComponent> m_ListScrollRef;
        public YIUISuperScrollListComponent ListScroll => m_ListScrollRef;
        
        // 数据源管理器
        public DataSourceMgr<ItemData> mDataSourceMgr;
    }
}
```

### 2. 实现基础 System

```csharp
[FriendOf(typeof(SuperScrollListViewTopToBottomDemoViewComponent))]
public static partial class SuperScrollListViewTopToBottomDemoViewComponentSystem
{
    // 初始化方法
    [EntitySystem]
    private static void YIUIInitialize(this SuperScrollListViewTopToBottomDemoViewComponent self)
    {
        // 创建 ListView 组件并绑定到 UI 对象
        self.m_ListScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComListView);
        
        // 设置点击事件
        self.ListScroll.SetOnClick();
    }

    // 打开界面方法
    [EntitySystem]
    private static async ETTask<bool> YIUIOpen(this SuperScrollListViewTopToBottomDemoViewComponent self, ParamVo vo)
    {
        // 初始化数据源
        self.mDataSourceMgr = new DataSourceMgr<ItemData>(100);
        
        // 设置列表项数量
        self.ListScroll.SetListItemCount(100);
        
        await ETTask.CompletedTask;
        return true;
    }

    // 渲染方法 - 必须实现
    [EntitySystem]
    private static void YIUISuperScrollListRenderer(this SuperScrollListViewTopToBottomDemoViewComponent self, 
        SuperScrollDemoItemComponent item, YIUISuperScrollListComponent superScrollList, int index, bool select)
    {
        // 刷新 UI 组件状态
        item.Refresh(index, select);

        // 获取数据并设置到项目
        var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
        var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseVerticalItem>();
        itemScript.SetItemData(itemData, index);
    }

    // 点击事件处理 - 可选实现
    [EntitySystem]
    private static void YIUISuperScrollListOnClick(this SuperScrollListViewTopToBottomDemoViewComponent self,
        SuperScrollDemoItemComponent item, YIUISuperScrollListComponent superScrollList, int index, bool select)
    {
        item.Select(select);
    }
}
```

## 多预制体支持

### 核心方法说明

当需要在同一个 ListView 中使用多种不同的预制体时，需要实现以下方法：

#### 1. 预制体选择方法

```csharp
[EntitySystem]
private static int YIUISuperScrollListGetPrefab(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, 
    YIUISuperScrollListComponent superScrollList, int index)
{
    // 根据数据或算法返回预制体的索引
    // 返回值对应 SuperScrollView 中设置的预制体列表索引
    return index % 3;  // 示例：循环使用 3 种预制体
}
```

#### 2. 多个渲染方法

为每种预制体类型实现对应的渲染方法：

```csharp
// 第一种预制体渲染
[EntitySystem]
private static void YIUISuperScrollListRenderer(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, 
    SuperScrollDemoItemComponent item, YIUISuperScrollListComponent superScrollList, int index, bool select)
{
    item.Refresh(index, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
    var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseVerticalItem>();
    itemScript.SetItemData(itemData, index);
}

// 第二种预制体渲染
[EntitySystem]
private static void YIUISuperScrollListRenderer(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, 
    SuperScrollDemoItem2Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
{
    item.Refresh(index, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
    var itemScript = item.UIBase.OwnerGameObject.GetComponent<SliderItem>();
    itemScript.SetItemData(itemData, index);
}

// 第三种预制体渲染
[EntitySystem]
private static void YIUISuperScrollListRenderer(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, 
    SuperScrollDemoItem4Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
{
    item.Refresh(index, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
    var itemScript = item.UIBase.OwnerGameObject.GetComponent<InputFieldItem>();
    itemScript.SetItemData(itemData, index);
}
```

#### 3. 多个点击事件处理

同样为每种预制体类型实现点击事件：

```csharp
[EntitySystem]
private static void YIUISuperScrollListOnClick(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self,
    SuperScrollDemoItemComponent item, YIUISuperScrollListComponent superScrollList, int index, bool select)
{
    item.Select(select);
}

[EntitySystem]
private static void YIUISuperScrollListOnClick(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self,
    SuperScrollDemoItem2Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
{
    item.Select(select);
}

[EntitySystem]
private static void YIUISuperScrollListOnClick(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self,
    SuperScrollDemoItem4Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
{
    item.Select(select);
}
```

### 高级点击配置

```csharp
[EntitySystem]
private static void YIUIInitialize(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self)
{
    self.m_ListScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComListView);
    
    // 设置所有预制体都可以点击
    self.ListScroll.SetOnClickAll();
    
    // 设置第二个预制体（索引1）可以点击但不能选中
    self.ListScroll.SetBanSelect(1);
}
```

## 水平滚动

水平滚动的实现与垂直滚动基本相同，主要区别在于使用不同的预制体组件：

```csharp
[EntitySystem]
private static void YIUISuperScrollListRenderer(this SuperScrollListViewLeftToRightDemoViewComponent self, 
    SuperScrollDemoItem3Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
{
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
    // 注意：使用 BaseHorizontalItem 而不是 BaseVerticalItem
    var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseHorizontalItem>();
    itemScript.SetItemData(itemData, index);
}
```

**注意**：水平滚动示例中没有设置点击事件，因此不需要实现 `YIUISuperScrollListOnClick` 方法。

## API 参考

### YIUISuperScrollListComponent 核心方法

#### 初始化和配置
```csharp
// 设置列表项总数
void SetListItemCount(int itemCount)

// 设置数据并刷新
void SetDataRefresh(int count, bool resetPos = true)
```

#### 点击事件配置
```csharp
// 设置单个预制体可点击（默认索引0）
void SetOnClick()

// 设置所有预制体都可点击
void SetOnClickAll()

// 设置特定预制体不能选中（可点击但不能选中状态）
void SetBanSelect(int prefabIndex)

// 设置点击事件检查
void SetOnClickCheck()
```

#### 选择状态管理
```csharp
// 获取当前选中的项目索引列表
List<int> GetSelect()

// 取消所有选择
void CancelSelect()

// 设置选中状态
void SetSelect(int itemIndex, bool select)

// 检查是否选中
bool IsSelect(int itemIndex)
```

#### 滚动控制
```csharp
// 滚动到指定位置
void MovePanelToItemIndex(int itemIndex, float offsetX, float offsetY)

// 立即滚动到指定位置（无动画）
void MovePanelToItemIndexImmediate(int itemIndex, float offsetX, float offsetY)
```

### 系统方法约定

#### 必须实现的方法
- `YIUISuperScrollListRenderer`: 渲染方法，每种预制体类型都需要一个对应的重载

#### 可选实现的方法
- `YIUISuperScrollListGetPrefab`: 多预制体支持时的预制体选择方法
- `YIUISuperScrollListOnClick`: 点击事件处理方法
- `YIUISuperScrollListOnClickCheck`: 点击前检查方法

## 最佳实践

### 1. 数据管理
```csharp
// 推荐使用 DataSourceMgr 管理数据
self.mDataSourceMgr = new DataSourceMgr<ItemData>(totalCount);

// 在渲染方法中获取数据
var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
```

### 2. 性能优化
- 使用 `EntityRef<T>` 进行弱引用管理，避免循环引用
- 在渲染方法中只执行必要的UI更新操作
- 合理使用多预制体，避免在单个预制体中处理过多逻辑分支

### 3. 错误处理
- 在渲染方法中检查数据有效性
- 在点击事件中验证索引范围
- 使用适当的日志记录调试信息

### 4. 架构设计
- 保持 Component 只包含数据和引用
- 将所有逻辑放在 System 方法中
- 使用接口和抽象类提高代码复用性

### 5. 命名约定
- System 方法必须以特定前缀命名（如 `YIUISuperScrollListRenderer`）
- 使用清晰的参数命名
- 遵循 ET 框架的命名规范

## 常见问题

### Q: 如何处理动态数据更新？
A: 调用 `SetDataRefresh(newCount)` 方法更新数据总数，ListView 会自动重新渲染可见项目。

### Q: 如何实现多选功能？
A: 使用 `SetOnClickAll()` 启用点击，然后在点击事件中调用 `SetSelect(itemIndex, !IsSelect(itemIndex))` 切换选择状态。

### Q: 如何优化滚动性能？
A: 
1. 减少渲染方法中的复杂计算
2. 使用对象池缓存复杂对象
3. 避免在渲染方法中执行异步操作

### Q: 预制体选择逻辑应该如何设计？
A: 在 `YIUISuperScrollListGetPrefab` 方法中基于数据类型、索引规律或业务逻辑返回对应的预制体索引。

本文档涵盖了 YIUI SuperScroll ListView 的主要使用方法，更多高级功能请参考源码实现和 SuperScrollView 官方文档。