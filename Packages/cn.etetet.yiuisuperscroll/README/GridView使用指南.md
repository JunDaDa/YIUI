# YIUI SuperScroll GridView 使用指南

本文档详细介绍了如何在 ET9.0 + YIUI 框架中使用 SuperScroll GridView 组件，基于 ListView 的实现模式和 GridView 的扩展功能。

## 目录
- [架构概述](#架构概述)
- [基础用法](#基础用法)
- [多预制体支持](#多预制体支持)
- [行列操作](#行列操作)
- [API 参考](#api-参考)
- [最佳实践](#最佳实践)

## 架构概述

SuperScroll GridView 在 ET9.0 + YIUI 框架中的架构分为以下几层：

```
View Component (Entity)
    ↓
YIUISuperScrollGridComponent (ET Component)
    ↓
LoopGridView (SuperScrollView Original)
```

### 核心组件

1. **View Component**: 继承自 `Entity`，实现 `IYIUIOpen<ParamVo>` 接口
2. **YIUISuperScrollGridComponent**: ET 框架的网格组件包装器
3. **DataSourceMgr<T>**: 数据源管理器

## 基础用法

### 1. 创建基础 GridView Component

```csharp
namespace ET.Client
{
    public partial class SuperScrollGridViewDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        // 使用 EntityRef 弱引用管理 GridView 组件
        public EntityRef<YIUISuperScrollGridComponent> m_GridScrollRef;
        public YIUISuperScrollGridComponent GridScroll => m_GridScrollRef;
        
        // 数据源管理器
        public DataSourceMgr<ItemData> mDataSourceMgr;
    }
}
```

### 2. 实现基础 System

```csharp
[FriendOf(typeof(SuperScrollGridViewDemoViewComponent))]
public static partial class SuperScrollGridViewDemoViewComponentSystem
{
    // 初始化方法
    [EntitySystem]
    private static void YIUIInitialize(this SuperScrollGridViewDemoViewComponent self)
    {
        // 创建 GridView 组件并绑定到 UI 对象
        self.m_GridScrollRef = self.AddChild<YIUISuperScrollGridComponent, LoopGridView>(self.u_ComGridView);
        
        // 设置点击事件
        self.GridScroll.SetOnClick();
    }

    // 打开界面方法
    [EntitySystem]
    private static async ETTask<bool> YIUIOpen(this SuperScrollGridViewDemoViewComponent self, ParamVo vo)
    {
        // 初始化数据源（假设100个项目，5行4列）
        self.mDataSourceMgr = new DataSourceMgr<ItemData>(100);
        
        // 设置网格数据（总数，行数，列数）
        self.GridScroll.SetDataRefreshByRowColumn(100, 5, 4, true);
        
        await ETTask.CompletedTask;
        return true;
    }

    // 渲染方法 - 必须实现
    [EntitySystem]
    private static void YIUISuperScrollGridRenderer(this SuperScrollGridViewDemoViewComponent self, 
        SuperScrollDemoGridItemComponent item, YIUISuperScrollGridComponent superScrollGrid, int itemIndex, bool select)
    {
        // 刷新 UI 组件状态
        item.Refresh(itemIndex, select);

        // 获取数据并设置到项目
        var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
        
        // 获取行列位置信息
        var (row, column) = self.GridScroll.GetRowColumn(item);
        
        // 设置项目数据（这里需要根据实际的 GridItem 组件进行调整）
        item.SetItemData(itemData, itemIndex, row, column);
    }

    // 点击事件处理 - 可选实现
    [EntitySystem]
    private static void YIUISuperScrollGridOnClick(this SuperScrollGridViewDemoViewComponent self,
        SuperScrollDemoGridItemComponent item, YIUISuperScrollGridComponent superScrollGrid, int itemIndex, bool select)
    {
        item.Select(select);
        
        // 获取点击项的行列信息
        var (row, column) = self.GridScroll.GetRowColumn(item);
        Debug.Log($"点击了第 {row} 行第 {column} 列的项目，索引: {itemIndex}");
    }
}
```

## 多预制体支持

### 核心方法说明

GridView 支持多预制体的方式与 ListView 相同：

#### 1. 预制体选择方法

```csharp
[EntitySystem]
private static int YIUISuperScrollGridGetPrefab(this SuperScrollGridViewMultipleDemoViewComponent self, 
    YIUISuperScrollGridComponent superScrollGrid, int itemIndex)
{
    // 根据数据或算法返回预制体的索引
    // 示例：根据行列位置选择不同预制体
    var (row, column) = self.GridScroll.GetRowColumnByIndex(itemIndex);
    
    if (row == 0) return 0;      // 第一行使用预制体0
    if (column == 0) return 1;   // 第一列使用预制体1
    return 2;                    // 其他位置使用预制体2
}
```

#### 2. 多个渲染方法

为每种预制体类型实现对应的渲染方法：

```csharp
// 标题项渲染（第一行）
[EntitySystem]
private static void YIUISuperScrollGridRenderer(this SuperScrollGridViewMultipleDemoViewComponent self, 
    GridTitleItemComponent item, YIUISuperScrollGridComponent superScrollGrid, int itemIndex, bool select)
{
    item.Refresh(itemIndex, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    var (row, column) = self.GridScroll.GetRowColumn(item);
    item.SetTitleData(itemData, column);
}

// 标签项渲染（第一列）
[EntitySystem]
private static void YIUISuperScrollGridRenderer(this SuperScrollGridViewMultipleDemoViewComponent self, 
    GridLabelItemComponent item, YIUISuperScrollGridComponent superScrollGrid, int itemIndex, bool select)
{
    item.Refresh(itemIndex, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    var (row, column) = self.GridScroll.GetRowColumn(item);
    item.SetLabelData(itemData, row);
}

// 普通内容项渲染
[EntitySystem]
private static void YIUISuperScrollGridRenderer(this SuperScrollGridViewMultipleDemoViewComponent self, 
    GridContentItemComponent item, YIUISuperScrollGridComponent superScrollGrid, int itemIndex, bool select)
{
    item.Refresh(itemIndex, select);
    var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
    var (row, column) = self.GridScroll.GetRowColumn(item);
    item.SetContentData(itemData, row, column);
}
```

#### 3. 多个点击事件处理

```csharp
[EntitySystem]
private static void YIUISuperScrollGridOnClick(this SuperScrollGridViewMultipleDemoViewComponent self,
    GridTitleItemComponent item, YIUISuperScrollGridComponent superScrollGrid, int itemIndex, bool select)
{
    // 标题项点击逻辑
    var (row, column) = self.GridScroll.GetRowColumn(item);
    Debug.Log($"点击了标题列: {column}");
}

[EntitySystem]
private static void YIUISuperScrollGridOnClick(this SuperScrollGridViewMultipleDemoViewComponent self,
    GridContentItemComponent item, YIUISuperScrollGridComponent superScrollGrid, int itemIndex, bool select)
{
    item.Select(select);
    // 内容项点击逻辑
    var (row, column) = self.GridScroll.GetRowColumn(item);
    Debug.Log($"选择了内容项 [{row}, {column}]: {select}");
}
```

## 行列操作

GridView 提供了丰富的行列操作功能：

### 1. 基础行列信息

```csharp
// 获取指定实体的行列位置
var (row, column) = gridComponent.GetRowColumn(entity);

// 根据索引获取行列位置
var (row, column) = gridComponent.GetRowColumnByIndex(itemIndex);

// 根据行列位置获取索引
int itemIndex = gridComponent.GetItemIndexByRowColumn(row, column);
```

### 2. 行列范围操作

```csharp
// 获取当前显示的行列范围
var (startRow, endRow, startColumn, endColumn) = gridComponent.GetShowRange();

// 检查指定行列是否在显示范围内
bool isShown = gridComponent.IsRowColumnInShowRange(row, column);
```

### 3. 滚动到指定行列

```csharp
// 滚动到指定行列
gridComponent.MoveToRowColumn(targetRow, targetColumn);

// 立即移动到指定行列（无动画）
gridComponent.MoveToRowColumnImmediate(targetRow, targetColumn);

// 滚动到指定行列并居中
gridComponent.MoveToRowColumnWithCenter(targetRow, targetColumn);
```

### 4. 行列选择操作

```csharp
// 选择整行
gridComponent.SelectRow(rowIndex, true);

// 选择整列
gridComponent.SelectColumn(columnIndex, true);

// 选择矩形区域
gridComponent.SelectRange(startRow, startColumn, endRow, endColumn, true);

// 获取选中的行列信息
var selectedRowColumns = gridComponent.GetSelectedRowColumns();
```

## API 参考

### YIUISuperScrollGridComponent 核心方法

#### 初始化和配置
```csharp
// 设置网格数据（总数，行数，列数）
void SetDataRefreshByRowColumn(int count, int row, int column, bool resetPos = true)

// 设置网格数据（总数量）
void SetDataRefresh(int count, bool resetPos = true)

// 初始化网格布局
void InitGridView(int itemTotalCount, GridViewLayoutParam layoutParam)
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

#### 行列信息获取
```csharp
// 获取实体的行列位置
(int row, int column) GetRowColumn(Entity entity)

// 根据索引获取行列位置
(int row, int column) GetRowColumnByIndex(int itemIndex)

// 根据行列获取索引
int GetItemIndexByRowColumn(int row, int column)

// 获取显示范围
(int startRow, int endRow, int startColumn, int endColumn) GetShowRange()
```

#### 滚动控制
```csharp
// 移动到指定行列
void MoveToRowColumn(int row, int column)

// 立即移动到指定行列
void MoveToRowColumnImmediate(int row, int column)

// 移动到指定行列并居中
void MoveToRowColumnWithCenter(int row, int column)
```

#### 选择状态管理
```csharp
// 获取当前选中的项目索引列表
List<int> GetSelect()

// 选择指定行列的项目
void SetSelectByRowColumn(int row, int column, bool select)

// 选择整行
void SelectRow(int row, bool select)

// 选择整列
void SelectColumn(int column, bool select)

// 选择矩形区域
void SelectRange(int startRow, int startColumn, int endRow, int endColumn, bool select)
```

### 系统方法约定

#### 必须实现的方法
- `YIUISuperScrollGridRenderer`: 渲染方法，每种预制体类型都需要一个对应的重载

#### 可选实现的方法
- `YIUISuperScrollGridGetPrefab`: 多预制体支持时的预制体选择方法
- `YIUISuperScrollGridOnClick`: 点击事件处理方法
- `YIUISuperScrollGridOnClickCheck`: 点击前检查方法

## 最佳实践

### 1. 数据管理
```csharp
// 推荐使用行列索引计算数据索引
int dataIndex = row * columnCount + column;
var itemData = self.mDataSourceMgr.GetItemDataByIndex(dataIndex);

// 或者使用组件提供的转换方法
int itemIndex = self.GridScroll.GetItemIndexByRowColumn(row, column);
var itemData = self.mDataSourceMgr.GetItemDataByIndex(itemIndex);
```

### 2. 性能优化
- 合理设置行列数，避免创建过多不可见项目
- 在渲染方法中缓存复杂计算结果
- 使用行列信息优化数据查询逻辑

### 3. 布局设计
```csharp
// 示例：表格类布局
private static int YIUISuperScrollGridGetPrefab(this SuperScrollGridViewComponent self, 
    YIUISuperScrollGridComponent superScrollGrid, int itemIndex)
{
    var (row, column) = self.GridScroll.GetRowColumnByIndex(itemIndex);
    
    // 第一行和第一列使用表头样式
    if (row == 0 || column == 0) return 0;
    
    // 奇偶行使用不同样式
    return row % 2 == 0 ? 1 : 2;
}
```

### 4. 交互设计
```csharp
// 示例：行列选择功能
private static void YIUISuperScrollGridOnClick(this SuperScrollGridViewComponent self,
    GridItemComponent item, YIUISuperScrollGridComponent superScrollGrid, int itemIndex, bool select)
{
    var (row, column) = self.GridScroll.GetRowColumn(item);
    
    // Ctrl+点击选择整行
    if (Input.GetKey(KeyCode.LeftControl))
    {
        self.GridScroll.SelectRow(row, !self.GridScroll.IsRowSelected(row));
    }
    // Shift+点击选择整列
    else if (Input.GetKey(KeyCode.LeftShift))
    {
        self.GridScroll.SelectColumn(column, !self.GridScroll.IsColumnSelected(column));
    }
    // 普通点击选择单个项目
    else
    {
        item.Select(select);
    }
}
```

### 5. 错误处理
- 检查行列索引的有效性
- 验证数据源与网格大小的一致性
- 处理布局参数的边界情况

## 常见问题

### Q: 如何动态调整网格的行列数？
A: 调用 `SetDataRefreshByRowColumn(newCount, newRow, newColumn)` 方法，GridView 会自动重新布局。

### Q: 如何实现不规则网格布局？
A: 使用多预制体功能，在 `YIUISuperScrollGridGetPrefab` 方法中根据行列位置返回不同的预制体索引。

### Q: 如何优化大数据量的网格性能？
A: 
1. 合理设置可见区域大小
2. 使用虚拟化技术，只渲染可见项目
3. 在渲染方法中避免复杂的数据查询

### Q: 如何实现网格的多选功能？
A: 使用 `SetOnClickAll()` 启用所有项目的点击，然后结合 `SelectRange()` 等方法实现范围选择。

### Q: 行列索引和数据索引如何转换？
A: 使用公式 `itemIndex = row * columnCount + column` 或组件提供的转换方法 `GetItemIndexByRowColumn(row, column)`。

本文档涵盖了 YIUI SuperScroll GridView 的主要使用方法，GridView 相比 ListView 提供了更丰富的二维布局和行列操作功能，适合实现表格、网格选择器等复杂UI组件。