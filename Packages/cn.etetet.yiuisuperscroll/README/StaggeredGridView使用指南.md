# YIUI SuperScroll StaggeredGridView 使用指南

本文档详细介绍了如何在 ET9.0 + YIUI 框架中使用 SuperScroll StaggeredGridView 组件，StaggeredGridView 支持不规则网格布局，项目可以有不同的大小，适合实现瀑布流、不规则卡片网格等复杂布局。

## 目录
- [架构概述](#架构概述)
- [基础用法](#基础用法)
- [动态项目大小](#动态项目大小)
- [多预制体支持](#多预制体支持)
- [组与索引操作](#组与索引操作)
- [API 参考](#api-参考)
- [最佳实践](#最佳实践)

## 架构概述

SuperScroll StaggeredGridView 在 ET9.0 + YIUI 框架中的架构分为以下几层：

```
View Component (Entity)
    ↓
YIUISuperScrollStaggeredGridComponent (ET Component)
    ↓
LoopStaggeredGridView (SuperScrollView Original)
```

### 核心组件

1. **View Component**: 继承自 `Entity`，实现 `IYIUIOpen<ParamVo>` 接口
2. **YIUISuperScrollStaggeredGridComponent**: ET 框架的错列网格组件包装器
3. **DataSourceMgr<T>**: 数据源管理器
4. **动态项目大小支持**: 每个项目可以有不同的大小和间距

## 基础用法

### 1. 创建基础 StaggeredGridView Component

```csharp
namespace ET.Client
{
    public partial class SuperScrollStaggeredGridViewDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        // 使用 EntityRef 弱引用管理 StaggeredGridView 组件
        public EntityRef<YIUISuperScrollStaggeredGridComponent> m_StaggeredGridScrollRef;
        public YIUISuperScrollStaggeredGridComponent StaggeredGridScroll => m_StaggeredGridScrollRef;
        
        // 数据源管理器
        public DataSourceMgr<StaggeredItemData> mDataSourceMgr;
        
        // 项目大小配置（可选，用于动态大小计算）
        public Dictionary<int, (float width, float height)> mItemSizeDict = new();
    }
}
```

### 2. 实现基础 System

```csharp
[FriendOf(typeof(SuperScrollStaggeredGridViewDemoViewComponent))]
public static partial class SuperScrollStaggeredGridViewDemoViewComponentSystem
{
    // 初始化方法
    [EntitySystem]
    private static void YIUIInitialize(this SuperScrollStaggeredGridViewDemoViewComponent self)
    {
        // 创建 StaggeredGridView 组件并绑定到 UI 对象
        self.m_StaggeredGridScrollRef = self.AddChild<YIUISuperScrollStaggeredGridComponent, LoopStaggeredGridView>(self.u_ComStaggeredGridView);
        
        // 设置点击事件
        self.StaggeredGridScroll.SetOnClick();
        
        // 设置动态项目大小获取（如果需要动态大小）
        self.StaggeredGridScroll.SetGetItemSize();
    }

    // 打开界面方法
    [EntitySystem]
    private static async ETTask<bool> YIUIOpen(this SuperScrollStaggeredGridViewDemoViewComponent self, ParamVo vo)
    {
        // 初始化数据源
        self.mDataSourceMgr = new DataSourceMgr<StaggeredItemData>(100);
        
        // 初始化项目大小数据（示例：随机大小）
        self.InitItemSizes();
        
        // 设置网格布局参数
        var layoutParam = new GridViewLayoutParam
        {
            mItemSize0 = 200f,      // 默认项目宽度
            mItemSize1 = 150f,      // 默认项目高度
            mPadding1 = 10f,        // 垂直间距
            mPadding2 = 10f,        // 水平间距
            mRowPadding = 5f,       // 行间距
            mColumnPadding = 5f     // 列间距
        };
        
        // 初始化错列网格参数
        var initParam = new StaggeredGridViewInitParam
        {
            mItemCountPerGroup = new int[] { 2, 3, 2, 4, 1 }  // 每组的项目数量
        };
        
        // 初始化错列网格
        self.StaggeredGridScroll.InitStaggeredGrid(100, layoutParam, initParam);
        
        await ETTask.CompletedTask;
        return true;
    }
    
    // 初始化项目大小数据
    private static void InitItemSizes(this SuperScrollStaggeredGridViewDemoViewComponent self)
    {
        var random = new System.Random();
        for (int i = 0; i < 100; i++)
        {
            // 随机生成项目大小（模拟不同大小的卡片）
            float width = 180f + random.Next(0, 80);   // 180-260
            float height = 120f + random.Next(0, 100); // 120-220
            self.mItemSizeDict[i] = (width, height);
        }
    }

    // 渲染方法 - 必须实现
    [EntitySystem]
    private static void YIUISuperScrollStaggeredGridRenderer(this SuperScrollStaggeredGridViewDemoViewComponent self, 
        SuperScrollStaggeredDemoItemComponent item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex, bool select)
    {
        // 刷新 UI 组件状态
        item.Refresh(itemIndex, select);

        // 获取数据并设置到项目
        var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
        
        // 获取组索引信息
        var (groupIndex, indexInGroup) = self.StaggeredGridScroll.GetGroupIndexData(item);
        
        // 获取项目大小
        var (width, height) = self.mItemSizeDict.GetValueOrDefault(itemIndex, (200f, 150f));
        
        // 设置项目数据
        item.SetItemData(itemData, itemIndex, groupIndex, indexInGroup, width, height);
    }

    // 动态项目大小获取 - StaggeredGridView 特有功能
    [EntitySystem]
    private static (float, float) YIUISuperScrollStaggeredGridGetItemSize(this SuperScrollStaggeredGridViewDemoViewComponent self, 
        YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex)
    {
        // 返回指定索引项目的 (大小, 间距)
        var (width, height) = self.mItemSizeDict.GetValueOrDefault(itemIndex, (200f, 150f));
        
        // 根据数据类型动态调整大小
        var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
        if (itemData.Type == StaggeredItemType.Image)
        {
            return (width, 10f);  // 图片类型项目，间距10
        }
        else if (itemData.Type == StaggeredItemType.Text)
        {
            return (height, 5f);  // 文本类型项目，间距5
        }
        
        return (Math.Max(width, height), 8f);  // 默认使用较大值作为大小，间距8
    }

    // 点击事件处理 - 可选实现
    [EntitySystem]
    private static void YIUISuperScrollStaggeredGridOnClick(this SuperScrollStaggeredGridViewDemoViewComponent self,
        SuperScrollStaggeredDemoItemComponent item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex, bool select)
    {
        item.Select(select);
        
        // 获取点击项的组信息
        var (groupIndex, indexInGroup) = self.StaggeredGridScroll.GetGroupIndexData(item);
        Debug.Log($"点击了第 {groupIndex} 组的第 {indexInGroup} 个项目，全局索引: {itemIndex}");
    }
}
```

## 动态项目大小

StaggeredGridView 的核心特性是支持动态项目大小，每个项目可以有不同的尺寸：

### 1. 实现动态大小接口

```csharp
// 在组件初始化时设置
self.StaggeredGridScroll.SetGetItemSize();

// 实现大小获取方法
[EntitySystem]
private static (float, float) YIUISuperScrollStaggeredGridGetItemSize(this SuperScrollStaggeredGridViewDemoViewComponent self, 
    YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex)
{
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    
    switch (itemData.ContentType)
    {
        case ContentType.ShortText:
            return (100f, 5f);  // 短文本：大小100，间距5
            
        case ContentType.LongText:
            return (200f, 8f);  // 长文本：大小200，间距8
            
        case ContentType.Image:
            return (itemData.ImageHeight, 10f);  // 图片：使用数据中的高度，间距10
            
        case ContentType.Video:
            return (300f, 15f);  // 视频：大小300，间距15
            
        default:
            return (150f, 8f);   // 默认：大小150，间距8
    }
}
```

### 2. 基于内容的动态大小

```csharp
[EntitySystem]
private static (float, float) YIUISuperScrollStaggeredGridGetItemSize(this SuperScrollStaggeredGridViewDemoViewComponent self, 
    YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex)
{
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    
    // 根据文本长度计算高度
    if (!string.IsNullOrEmpty(itemData.Text))
    {
        float textHeight = CalculateTextHeight(itemData.Text, 180f);  // 固定宽度180
        return (textHeight + 20f, 5f);  // 加上边距20，间距5
    }
    
    // 根据图片比例计算高度
    if (itemData.ImageAspectRatio > 0)
    {
        float height = 180f / itemData.ImageAspectRatio;  // 固定宽度180
        return (height, 8f);
    }
    
    return (150f, 8f);  // 默认值
}

// 辅助方法：计算文本高度
private static float CalculateTextHeight(string text, float width)
{
    // 这里应该使用实际的文本测量方法
    // 简化示例：根据字符数量估算
    int lineCount = Mathf.CeilToInt(text.Length * 0.1f);  // 假设每行10个字符
    return lineCount * 20f;  // 假设每行高度20
}
```

## 多预制体支持

StaggeredGridView 支持多预制体的方式与其他组件相同：

### 1. 预制体选择方法

```csharp
[EntitySystem]
private static int YIUISuperScrollStaggeredGridGetPrefab(this SuperScrollStaggeredGridViewMultipleDemoViewComponent self, 
    YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex)
{
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    
    // 根据组位置选择预制体
    var (groupIndex, indexInGroup) = self.StaggeredGridScroll.GetGroupIndexDataByIndex(itemIndex);
    
    if (groupIndex == 0) return 0;  // 第一组使用标题样式
    
    // 根据数据类型选择预制体
    switch (itemData.Type)
    {
        case StaggeredItemType.Image:
            return 1;  // 图片预制体
        case StaggeredItemType.Text:
            return 2;  // 文本预制体
        case StaggeredItemType.Video:
            return 3;  // 视频预制体
        default:
            return 0;  // 默认预制体
    }
}
```

### 2. 多个渲染方法

```csharp
// 图片项渲染
[EntitySystem]
private static void YIUISuperScrollStaggeredGridRenderer(this SuperScrollStaggeredGridViewMultipleDemoViewComponent self, 
    StaggeredImageItemComponent item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex, bool select)
{
    item.Refresh(itemIndex, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    var (groupIndex, indexInGroup) = self.StaggeredGridScroll.GetGroupIndexData(item);
    item.SetImageData(itemData, groupIndex, indexInGroup);
}

// 文本项渲染
[EntitySystem]
private static void YIUISuperScrollStaggeredGridRenderer(this SuperScrollStaggeredGridViewMultipleDemoViewComponent self, 
    StaggeredTextItemComponent item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex, bool select)
{
    item.Refresh(itemIndex, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    var (groupIndex, indexInGroup) = self.StaggeredGridScroll.GetGroupIndexData(item);
    item.SetTextData(itemData, groupIndex, indexInGroup);
}

// 视频项渲染
[EntitySystem]
private static void YIUISuperScrollStaggeredGridRenderer(this SuperScrollStaggeredGridViewMultipleDemoViewComponent self, 
    StaggeredVideoItemComponent item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex, bool select)
{
    item.Refresh(itemIndex, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    var (groupIndex, indexInGroup) = self.StaggeredGridScroll.GetGroupIndexData(item);
    item.SetVideoData(itemData, groupIndex, indexInGroup);
}
```

## 组与索引操作

StaggeredGridView 使用组（Group）的概念来管理项目布局：

### 1. 基础组操作

```csharp
// 获取指定实体的组索引信息
var (groupIndex, indexInGroup) = staggeredGridComponent.GetGroupIndexData(entity);

// 根据项目索引获取组信息
var (groupIndex, indexInGroup) = staggeredGridComponent.GetGroupIndexDataByIndex(itemIndex);

// 获取指定组的项目数量
int itemCountInGroup = staggeredGridComponent.GetItemCountInGroup(groupIndex);

// 获取总组数
int totalGroupCount = staggeredGridComponent.GetGroupCount();
```

### 2. 组范围操作

```csharp
// 获取当前显示的组范围
var (startGroup, endGroup) = staggeredGridComponent.GetShownGroupRange();

// 检查指定组是否在显示范围内
bool isGroupShown = staggeredGridComponent.IsGroupInShowRange(groupIndex);

// 获取指定组中显示的项目范围
var (startIndex, endIndex) = staggeredGridComponent.GetShownItemRangeInGroup(groupIndex);
```

### 3. 滚动到指定组

```csharp
// 滚动到指定组
staggeredGridComponent.MoveToGroup(targetGroupIndex);

// 立即移动到指定组（无动画）
staggeredGridComponent.MoveToGroupImmediate(targetGroupIndex);

// 滚动到指定组的指定项目
staggeredGridComponent.MoveToGroupItem(groupIndex, indexInGroup);
```

## API 参考

### YIUISuperScrollStaggeredGridComponent 核心方法

#### 初始化和配置
```csharp
// 初始化错列网格
void InitStaggeredGrid(int itemTotalCount, GridViewLayoutParam layoutParam, StaggeredGridViewInitParam initParam = null)

// 设置数据并刷新
void SetDataRefresh(int count, bool resetPos = true)
```

#### 动态大小配置
```csharp
// 设置动态项目大小获取
void SetGetItemSize()

// 清除动态大小设置
void ClearGetItemSize()
```

#### 点击事件配置
```csharp
// 设置单个预制体可点击
void SetOnClick()

// 设置所有预制体都可点击
void SetOnClickAll()

// 设置特定预制体不能选中
void SetBanSelect(int prefabIndex)

// 设置点击事件检查
void SetOnClickCheck()
```

#### 组信息获取
```csharp
// 获取实体的组索引信息
(int groupIndex, int indexInGroup) GetGroupIndexData(Entity entity)

// 根据项目索引获取组信息
(int groupIndex, int indexInGroup) GetGroupIndexDataByIndex(int itemIndex)

// 获取指定组的项目数量
int GetItemCountInGroup(int groupIndex)

// 获取总组数
int GetGroupCount()
```

#### 滚动控制
```csharp
// 移动到指定组
void MoveToGroup(int groupIndex)

// 立即移动到指定组
void MoveToGroupImmediate(int groupIndex)

// 移动到指定组的指定项目
void MoveToGroupItem(int groupIndex, int indexInGroup)
```

#### 选择状态管理
```csharp
// 获取当前选中的项目索引列表
List<int> GetSelect()

// 选择指定组的所有项目
void SelectGroup(int groupIndex, bool select)

// 选择指定组的指定项目
void SetSelectByGroupIndex(int groupIndex, int indexInGroup, bool select)
```

### 系统方法约定

#### 必须实现的方法
- `YIUISuperScrollStaggeredGridRenderer`: 渲染方法，每种预制体类型都需要一个对应的重载

#### StaggeredGridView 特有方法
- `YIUISuperScrollStaggeredGridGetItemSize`: 动态项目大小获取方法

#### 可选实现的方法
- `YIUISuperScrollStaggeredGridGetPrefab`: 多预制体支持时的预制体选择方法
- `YIUISuperScrollStaggeredGridOnClick`: 点击事件处理方法
- `YIUISuperScrollStaggeredGridOnClickCheck`: 点击前检查方法

## 最佳实践

### 1. 动态大小优化
```csharp
// 缓存项目大小计算结果
private Dictionary<int, (float, float)> sizeCache = new();

[EntitySystem]
private static (float, float) YIUISuperScrollStaggeredGridGetItemSize(this SuperScrollStaggeredGridViewComponent self, 
    YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int itemIndex)
{
    // 使用缓存避免重复计算
    if (self.sizeCache.TryGetValue(itemIndex, out var cachedSize))
    {
        return cachedSize;
    }
    
    // 计算大小
    var size = CalculateItemSize(itemIndex);
    self.sizeCache[itemIndex] = size;
    
    return size;
}
```

### 2. 组布局设计
```csharp
// 示例：瀑布流布局
private static void InitStaggeredGridLayout(this SuperScrollStaggeredGridViewComponent self)
{
    var layoutParam = new GridViewLayoutParam
    {
        mItemSize0 = 200f,      // 列宽
        mItemSize1 = 100f,      // 最小项目高度
        mPadding1 = 8f,         // 垂直间距
        mPadding2 = 8f,         // 水平间距
        mRowPadding = 5f,       // 行间距
        mColumnPadding = 5f     // 列间距
    };
    
    // 动态计算每组的项目数量
    var initParam = new StaggeredGridViewInitParam();
    var random = new System.Random();
    var groupCounts = new List<int>();
    
    for (int i = 0; i < 20; i++)  // 20个组
    {
        groupCounts.Add(random.Next(1, 6));  // 每组1-5个项目
    }
    
    initParam.mItemCountPerGroup = groupCounts.ToArray();
    
    self.StaggeredGridScroll.InitStaggeredGrid(100, layoutParam, initParam);
}
```

### 3. 性能优化
- 实现大小计算缓存机制
- 避免在 `GetItemSize` 方法中执行复杂计算
- 合理设置组大小，平衡性能和视觉效果

### 4. 数据结构设计
```csharp
public class StaggeredItemData
{
    public int Id;
    public StaggeredItemType Type;
    public string Content;
    public float PreferredHeight;  // 预计算的高度
    public float AspectRatio;      // 宽高比
    public Dictionary<string, object> Metadata;  // 额外数据
}
```

### 5. 错误处理
- 为 `GetItemSize` 方法提供默认返回值
- 检查组索引的有效性
- 处理数据与组配置不匹配的情况

## 常见问题

### Q: 如何实现真正的瀑布流效果？
A: 在 `YIUISuperScrollStaggeredGridGetItemSize` 方法中根据内容动态计算高度，并确保每组的项目数量设置合理。

### Q: 动态大小计算影响性能怎么办？
A: 实现大小缓存机制，将计算结果缓存起来，避免重复计算。也可以在数据预处理阶段预计算尺寸。

### Q: 如何实现不同类型内容的混合布局？
A: 结合多预制体功能和动态大小功能，在 `GetPrefab` 方法中根据内容类型选择预制体，在 `GetItemSize` 方法中返回对应的大小。

### Q: 组的概念如何理解？
A: 组是 StaggeredGridView 的布局单元，每组包含一定数量的项目。组内的项目会按照错列网格的规则进行排列，不同组之间可以有不同的布局。

### Q: 如何调试动态大小的问题？
A: 在 `GetItemSize` 方法中添加日志输出，检查返回的大小值是否符合预期。也可以通过可视化工具查看项目的实际布局效果。

本文档涵盖了 YIUI SuperScroll StaggeredGridView 的主要使用方法，StaggeredGridView 是三种组件中最复杂的一种，提供了最大的布局灵活性，适合实现各种复杂的不规则网格布局。