# cn.etetet.yiuisuperscroll ET 扩展设计方案

## 设计目标
为 `cn.etetet.yiuisuperscroll` 包创建 ET 框架扩展，参考 `cn.etetet.yiuiloopscrollrectasync` 的设计模式，为 SuperScrollView 的三个核心组件提供 Entity 支持。

## 核心扩展组件

### 1. YIUISuperScrollChild (基础 Entity)
```csharp
[ChildOf]
public partial class YIUISuperScrollChild : Entity, IAwake, IDestroy
{
    public EntityRef<Entity> m_OwnerEntity;
    public Entity OwnerEntity => m_OwnerEntity;
    
    public YIUIBindVo m_BindVo;
    public Type m_DefaultDataType;
    public Type m_DataType;
    public Type m_ItemType;
    
    // 通用的渲染器和点击事件系统类型
    public Type m_RendererSystemType;
    public Type m_OnClickSystemType;
    public Type m_OnClickCheckSystemType;
    
    // 异步加载和缓存
    public YIUIInvokeEntity_LoadInstantiateByVo m_InvokeLoadInstantiate;
    public HashSet<long> m_BanLayerOptionForeverHashSet = new();
    
    // 数据管理
    private IList m_Data;
    public IList Data { get; set; }
}
```

### 2. YIUIListViewChild (ListView 专用)
```csharp
[ChildOf]
public partial class YIUIListViewChild : YIUISuperScrollChild, 
    IAwake<LoopListView2, Type>, IAwake<LoopListView2, Type, string>
{
    public LoopListView2 m_ListView;
    public ObjAsyncCache<EntityRef<Entity>> m_ItemPool;
    public Dictionary<Transform, EntityRef<Entity>> m_ItemTransformDic = new();
    public Dictionary<Transform, int> m_ItemTransformIndexDic = new();
    public HashSet<int> m_OnClickItemHashSet = new();
}
```

### 3. YIUIGridViewChild (GridView 专用)
```csharp
[ChildOf]
public partial class YIUIGridViewChild : YIUISuperScrollChild,
    IAwake<LoopGridView, Type>, IAwake<LoopGridView, Type, string>
{
    public LoopGridView m_GridView;
    public ObjAsyncCache<EntityRef<Entity>> m_ItemPool;
    public Dictionary<Transform, EntityRef<Entity>> m_ItemTransformDic = new();
    public Dictionary<Transform, int> m_ItemTransformIndexDic = new();
    public HashSet<int> m_OnClickItemHashSet = new();
}
```

### 4. YIUIStaggeredGridViewChild (StaggeredGridView 专用)
```csharp
[ChildOf]
public partial class YIUIStaggeredGridViewChild : YIUISuperScrollChild,
    IAwake<LoopStaggeredGridView, Type>, IAwake<LoopStaggeredGridView, Type, string>
{
    public LoopStaggeredGridView m_StaggeredGridView;
    public ObjAsyncCache<EntityRef<Entity>> m_ItemPool;
    public Dictionary<Transform, EntityRef<Entity>> m_ItemTransformDic = new();
    public Dictionary<Transform, int> m_ItemTransformIndexDic = new();
    public HashSet<int> m_OnClickItemHashSet = new();
}
```

## System 设计

### 1. YIUIListViewChildSystem
```csharp
[FriendOf(typeof(YIUIListViewChild))]
[EntitySystemOf(typeof(YIUIListViewChild))]
public static partial class YIUIListViewChildSystem
{
    [EntitySystem]
    private static void Awake(this YIUIListViewChild self, LoopListView2 listView, Type itemType)
    {
        self.Initialize(listView, itemType);
    }
    
    // 实现 LoopListView2 的回调接口
    private static async ETTask<LoopListViewItem2> OnCreateItemRenderer(this YIUIListViewChild self, int index)
    {
        // 异步创建 Item
    }
    
    private static void OnItemRenderer(this YIUIListViewChild self, LoopListViewItem2 item, int index)
    {
        // 渲染 Item 数据
    }
    
    // API 方法
    public static void RefreshAllShownItem(this YIUIListViewChild self)
    public static void SetListItemCount(this YIUIListViewChild self, int itemCount, bool resetPos = true)
    public static void MoveTo(this YIUIListViewChild self, int itemIndex, float offset = 0)
    // ... 其他 ListView 相关方法
}
```

### 2. YIUIGridViewChildSystem
```csharp
[FriendOf(typeof(YIUIGridViewChild))]
[EntitySystemOf(typeof(YIUIGridViewChild))]
public static partial class YIUIGridViewChildSystem
{
    [EntitySystem]
    private static void Awake(this YIUIGridViewChild self, LoopGridView gridView, Type itemType)
    {
        self.Initialize(gridView, itemType);
    }
    
    // 实现 LoopGridView 的回调接口
    private static async ETTask<LoopGridViewItem> OnCreateItemRenderer(this YIUIGridViewChild self, int index)
    {
        // 异步创建 Grid Item
    }
    
    // API 方法
    public static void InitGridView(this YIUIGridViewChild self, int itemTotalCount, 
        System.Func<LoopGridView, int, LoopGridViewItem> onGetItemByIndex,
        LoopGridViewInitParam initParam = null)
    public static void SetGridViewItemCount(this YIUIGridViewChild self, int itemCount)
    // ... 其他 GridView 相关方法
}
```

### 3. YIUIStaggeredGridViewChildSystem
```csharp
[FriendOf(typeof(YIUIStaggeredGridViewChild))]
[EntitySystemOf(typeof(YIUIStaggeredGridViewChild))]
public static partial class YIUIStaggeredGridViewChildSystem
{
    [EntitySystem]
    private static void Awake(this YIUIStaggeredGridViewChild self, LoopStaggeredGridView staggeredView, Type itemType)
    {
        self.Initialize(staggeredView, itemType);
    }
    
    // 实现 LoopStaggeredGridView 的回调接口
    private static async ETTask<LoopStaggeredGridViewItem> OnCreateItemRenderer(this YIUIStaggeredGridViewChild self, int index)
    {
        // 异步创建 Staggered Item
    }
    
    // API 方法
    public static void InitStaggeredGridView(this YIUIStaggeredGridViewChild self, int itemTotalCount,
        System.Func<LoopStaggeredGridView, int, LoopStaggeredGridViewItem> onGetItemByIndex,
        StaggeredGridViewInitParam initParam = null)
    // ... 其他 StaggeredGridView 相关方法
}
```

## 接口设计

### 1. 渲染器接口
```csharp
public interface IYIUISuperScrollRenderer<TParent, TItem, TData>
    where TParent : Entity
    where TItem : Entity  
    where TData : class
{
    void OnRenderer(TParent parent, TItem item, TData data, int index, bool selected);
}
```

### 2. 点击事件接口
```csharp
public interface IYIUISuperScrollOnClick<TParent, TItem, TData>
    where TParent : Entity
    where TItem : Entity
    where TData : class
{
    void OnClick(TParent parent, TItem item, TData data, int index);
}

public interface IYIUISuperScrollOnClickCheck<TParent, TItem, TData>
    where TParent : Entity
    where TItem : Entity
    where TData : class
{
    bool OnClickCheck(TParent parent, TItem item, TData data, int index);
}
```

## 目录结构

```
cn.etetet.yiuisuperscroll/
├── Scripts/
│   ├── ModelView/
│   │   └── Client/
│   │       ├── SuperScroll/
│   │       │   ├── YIUISuperScrollChild.cs (基础)
│   │       │   ├── YIUIListViewChild.cs (ListView)
│   │       │   ├── YIUIGridViewChild.cs (GridView)
│   │       │   └── YIUIStaggeredGridViewChild.cs (StaggeredView)
│   │       └── Event/
│   │           ├── YIUISuperScrollHelper.cs
│   │           ├── YIUISuperScrollRenderer.cs
│   │           └── YIUISuperScrollOnClick.cs
│   └── HotfixView/
│       └── Client/
│           └── SuperScroll/
│               ├── YIUIListViewChildSystem.cs
│               ├── YIUIGridViewChildSystem.cs
│               └── YIUIStaggeredGridViewChildSystem.cs
```

## 使用示例

```csharp
// 创建 ListView
var listViewChild = parent.AddChild<YIUIListViewChild, LoopListView2, Type>(
    GetComponent<LoopListView2>(), typeof(TestItemComponent));

// 设置数据
listViewChild.SetListData(dataList);

// 刷新显示
listViewChild.RefreshAllShownItem();

// 实现渲染器
[EntitySystem]
public class TestItemRenderer : IYIUISuperScrollRenderer<TestPanelComponent, TestItemComponent, TestData>
{
    public void OnRenderer(TestPanelComponent parent, TestItemComponent item, TestData data, int index, bool selected)
    {
        // 更新 item 显示
        item.SetItemData(data, index, selected);
    }
}
```

## 特点和优势

1. **统一的架构**：三种视图组件使用相同的基础 Entity，保持一致的使用体验
2. **异步支持**：完全支持异步加载，避免卡顿
3. **类型安全**：使用泛型接口确保类型安全
4. **易于扩展**：基于 ET 的 Entity-System 架构，便于扩展功能
5. **完整替换**：可以完全替换 yiuiloopscrollrectasync，提供更强大的功能

这个设计方案保持了与 yiuiloopscrollrectasync 相同的使用模式，同时为 SuperScrollView 的三个核心组件提供了完整的 ET 框架支持。