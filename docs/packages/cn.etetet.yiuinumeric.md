# cn.etetet.yiuinumeric

**版本**: 4.0.8
**分类**: UI/YIUI
**命名空间**: `ET`
**依赖**: `cn.etetet.yiuicodeanalysis` ^0.0.1

## 概述

YIUI Numeric 数值系统，是游戏中所有数值属性（生命、攻击、速度、等级等）的统一管理框架。设计核心是：

1. **分层数值 ID 编码**：每个最终属性对应一个 Final ID，其下的 6 个子层（个位 1-6）分别代表基础值、附加值、百分比等，系统自动按公式计算最终值
2. **对象池化数据**：`NumericData` 使用对象池管理，可独立存在（无需 Entity），支持快照/拷贝
3. **事件驱动变化通知**：数值变化后自动推送 `NumericChange` 事件，通过 `NumericHandler` 或 `NumericHandlerDynamic` 监听
4. **配置驱动公式与限制**：支持 Luban 配置表定义上下限、数值影响关系和重置逻辑

---

## 目录结构

```
cn.etetet.yiuinumeric/
├── Scripts/
│   ├── Model/Share/Numeric/
│   │   ├── Core/
│   │   │   ├── NumericData.cs           # 核心数据容器（对象池）
│   │   │   ├── NumericDataComponent.cs  # Entity 挂载组件
│   │   │   ├── NumericConst.cs          # ID 范围常量
│   │   │   ├── NumericConfigData.cs     # 配置表数值数据扩展（延迟初始化）
│   │   │   └── Unit_Numeric_Extend.cs   # Unit 快捷访问扩展
│   │   ├── Event/
│   │   │   └── NumericEventType.cs      # 事件结构体定义（NumericChange/NumericAffect/...）
│   │   ├── Formula/
│   │   │   └── Invoke_Formula.cs        # 公式 Invoke 结构体（含 6 个子 ID 字段）
│   │   ├── Handler/
│   │   │   ├── INumericHandler.cs       # 静态 Handler 接口 + NumericHandlerSystem<T> 抽象类
│   │   │   ├── INumericHandlerDynamic.cs # 动态 Handler 接口
│   │   │   ├── NumericHandlerAttribute.cs
│   │   │   ├── NumericHandlerDynamicAttribute.cs
│   │   │   ├── NumericHandlerComponent.cs       # 静态分发单例（双K字典）
│   │   │   └── NumericHandlerDynamicComponent.cs # 动态分发单例（三K字典）
│   │   └── Pool/
│   │       └── NumericDictionaryPool.cs
│   ├── Hotfix/Share/
│   │   ├── Data/                        # NumericData 扩展方法（所有 API 实现）
│   │   │   ├── NumericDataExtend.cs     # 核心私有方法：GetByKey/ChangeByKey/ChangeValue/UpdateResult/PushEvent
│   │   │   ├── NumericDataExtend_Get.cs     # Get 系列（GetAsBool/Float/Int/Long/RealValue/ObjectValue）
│   │   │   ├── NumericDataExtend_Set.cs     # Set/SetNoEvent/SetUnCheck 系列
│   │   │   ├── NumericDataExtend_Change.cs  # Change/ChangeNoEvent/ChangeUnCheck 系列
│   │   │   ├── NumericDataExtend_Init.cs    # InitSet/InitToServer 初始化方法
│   │   │   ├── NumericDataExtend_Add.cs     # Add（多数据合并，返回新对象）
│   │   │   ├── NumericDataExtend_Subtract.cs # Subtract（多数据相减，返回新对象）
│   │   │   ├── NumericDataExtend_Copy.cs    # Copy（完整拷贝）
│   │   │   ├── NumericDataExtend_Create.cs  # Create（从列表创建合并对象）
│   │   │   ├── NumericDataExtend_Difference.cs # Difference（获取差值快照）
│   │   │   ├── NumericDataExtend_Push.cs    # PushEvent/PushEventAll（内部私有）
│   │   │   ├── NumericDataExtend_Limit.cs   # Limit 系列（上下限/重置/ForceNumeric 检查）
│   │   │   └── NumericChange.cs             # NumericChangeHelper 扩展方法
│   │   ├── Formula/
│   │   │   └── On_Invoke_NumericFormula_Handler.cs  # 默认公式 Handler（自动生成）
│   │   ├── Handler/
│   │   │   └── NumericChangeEvent_NotifyHandler.cs  # AEvent 分发入口
│   │   ├── System/                      # NumericDataComponent ECS System 各操作实现
│   │   └── Localization/
│   │       └── NumericLocalization.cs
│   └── Loader/Client/
│       └── YIUIGameObjectEntityRef_Numeric.cs
├── Editor/
│   ├── Window/                          # Numeric 代码生成工具（CreateNumeric 等）
│   └── ComponentView/                   # Inspector 数值 GM 调试窗口
└── .Template/                           # 新建 Numeric Demo 的模板文件
```

---

## 数值 ID 编码系统

这是本包最关键的设计，理解它才能正确使用。

### ID 范围

```
NumericConst.Min       = 100_000      // 最小 Final ID（只读结果，不可直接修改）
NumericConst.Max       = 1_000_000    // 最大 Final ID
NumericConst.ChangeMin = 1_000_001    // 最小可修改子 ID（= Min * 10 + 1）
NumericConst.ChangeMax = 10_000_006   // 最大可修改子 ID（= Max * 10 + RangeMax）
NumericConst.RangeMax  = 6            // 子层最大个位数
NumericConst.FloatRate = 10000f       // float ↔ long 转换系数
```

### Final ID 与子 ID 的关系

每个最终属性（如 HP）对应一个 `Final ID`（例如 `100100`），其实际存储值由 6 个子 ID 决定：

```
Final = 100100  →  最终计算结果（只读，系统自动写入）
  Bas       = Final * 10 + 1 = 1001001  // 基础值
  Add       = Final * 10 + 2 = 1001002  // 附加值（装备/技能叠加）
  Pct       = Final * 10 + 3 = 1001003  // 成长百分比（×10000，如 1.5倍 = 15000）
  FinalAdd  = Final * 10 + 4 = 1001004  // 最终附加值（在 FinalPct 之前加算）
  FinalPct  = Final * 10 + 5 = 1001005  // 最终百分比
  ResultAdd = Final * 10 + 6 = 1001006  // 结果附加值（最后加算）
```

### 实际公式（已修正 Round 1 错误）

由 `On_Invoke_NumericFormula_Handler.cs` 自动生成，实际计算顺序为：

```
Final = ((Bas + Add) * (10000 + Pct) / 10000 + FinalAdd)
        * (10000 + FinalPct) / 10000
        + ResultAdd
```

**注意**：`FinalAdd` 是在 `FinalPct` **之前**加算的（被 FinalPct 放大），而非 Round 1 所写的在 FinalPct 之后。整数除法用 `IntRate=10000` 防止精度损失。

### Float 精度

所有数值内部以 `long` 存储，float 值乘以 `FloatRate = 10000` 后存入。读取时通过 `GetAsFloat()` 自动除以 10000 还原。

---

## 核心类

### `NumericData`

数值的原始数据容器，**不依赖 Entity**，可独立使用（用于快照/拷贝）。

```csharp
[EnableClass]
public class NumericData : IPool
{
    [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
    public Dictionary<int, long> NumericDic;  // key=numericType, value=long(含精度)

    public Entity OwnerEntity => m_OwnerEntity;  // 绑定的 Entity（NumericDataComponent 本身）
}
```

| 方法 | 说明 |
|------|------|
| `NumericData.Create()` | 从对象池创建 |
| `Dispose()` | 清空 NumericDic 并回收到对象池 |
| `UpdateOwnerEntity(entity)` | 绑定宿主 Entity（Awake 时由 Component 调用） |

**重要**：`OwnerEntity` 是 `NumericDataComponent` 本身，推送事件时取的是 `OwnerEntity.Parent`（即挂载组件的 Entity）。

### `NumericDataComponent`

挂载在任意 Entity 上的 ECS 组件，封装 `NumericData`。

```csharp
[ComponentOf]   // 任意 Entity 均可挂载，每个 Entity 只能一个
public class NumericDataComponent : Entity, IAwake, IDestroy, ITransfer, IDeserialize
{
    public NumericData NumericData;           // 实际数据（禁止直接访问）
    public Dictionary<int, long> NumericDic => NumericData.NumericDic;
}
```

- `IAwake`：调用 `NumericData.UpdateOwnerEntity(self)` 绑定宿主
- `IDestroy`：调用 `NumericData.Dispose()` 回收对象池
- `ITransfer + IDeserialize`：支持场景迁移和 MongoDB 反序列化，持久化安全

### `NumericConfigData`（局部类扩展）

配置表中 `sealed partial class NumericConfigData` 的扩展，提供懒加载的 `NumericData` 属性：

```csharp
public NumericData NumericData
{
    get => m_NumericData ??= EventSystem.Instance.Invoke<Invoke_Numeric_CreateNumericData, NumericData>(
               new Invoke_Numeric_CreateNumericData(ConfigData));
}
```

通过 `Invoke_Numeric_CreateNumericData` Invoke 创建，第一次访问时才初始化，避免无用数据常驻内存。

---

## 数值操作 API

所有操作均通过 `NumericDataComponent`（或 `NumericData`）的扩展方法进行。两个类的 API 一一对应，Component 版本内部直接转发到 `NumericData`。

### Change（推荐，累加操作）

```csharp
// Change = += （传负值则为 -=）
component.Change(ENumericType.HP_Bas, 100L);      // HP基础值 +100
component.Change(ENumericType.HP_Bas, -50L);      // HP基础值 -50
component.Change(ENumericType.HPPct_Bas, 0.5f);   // HP百分比基础 +0.5 (内部×10000存储)

// 不触发事件版本（批量初始化时使用）
component.ChangeNoEvent(ENumericType.HP_Bas, 100L);

// 跳过 ID 合法性检查（不得不用时才用）
component.ChangeUnCheck(ENumericType.HP_Bas, 100L);
```

### Set（覆盖赋值，谨慎使用）

```csharp
component.Set(ENumericType.HP_Bas, 1000L);            // 覆盖赋值（触发事件）
component.SetNoEvent(ENumericType.HP_Bas, 1000L);     // 覆盖赋值（不触发事件）
component.SetUnCheck(ENumericType.HP_Bas, 1000L);     // 跳过 ID 检查（触发事件）
component.SetNoEventUnCheck(ENumericType.HP_Bas, 1000L); // 跳过检查且不触发事件
```

**Change vs Set 区别**：
- `Change`：`newValue = oldValue + delta`（累加）
- `Set`：`newValue = value`（覆盖，isAdd=false）

### Get（读取值）

```csharp
float hp   = component.GetAsFloat(ENumericType.HP);    // 读取最终 HP（float，已除精度）
int   lv   = component.GetAsInt(ENumericType.Level);   // 读取等级
long  raw  = component.GetAsLong(ENumericType.HP);     // 读取原始 long（已除精度，整数）
bool  flag = component.GetAsBool(ENumericType.IsAlive);

// 读取原始底层 long（不做精度处理，直接返回字典值）
long real = component.GetRealValue(ENumericType.HP);

// 根据配置类型自动识别返回 object（GM/调试场景使用）
object val = component.GetObjectValue(ENumericType.HP);
```

### 初始化

```csharp
// 从配置表字典初始化（会重新计算 Final 值）
numericData.InitSet(configDic);                // 不推送事件
numericData.InitSet(configDic, isPushEvent: true); // 推送事件

// 同步服务器数据（服务端已计算好，直接覆盖，无需重计算）
numericData.InitToServer(serverDic);           // 不推送事件
numericData.InitToServer(serverDic, true);     // 推送所有 Key 的变化事件
```

**两者区别**：`InitSet` 会按优先级处理 Limit 配置并触发公式计算；`InitToServer` 直接赋值，假设数据已是最终值。

### Add / Subtract（多数据合并，生成新对象）

```csharp
// Add：将 self 和 target 的子 ID 值相加，返回新 NumericData（对象池）
// 注意：返回的新对象 OwnerEntity 为 null，需手动绑定
NumericData merged = numericData.Add(otherData);
NumericData merged2 = numericData.Add(data1, data2, data3);

// Subtract：相减（用于移除 buff 等）
NumericData result = numericData.Subtract(buffData);
```

**注意**：这两个方法**不会修改 self**，而是创建一个新的 `NumericData` 对象（从对象池），包含合并后的值并自动重新计算 Final 值。使用完毕后需调用 `result.Dispose()`。

### Difference（差值快照）

```csharp
// 获取 self 与 target 的差值（对象池对象，用完 Dispose）
NumericData diff = component.Difference(otherData);
```

### Copy（完整拷贝）

```csharp
// 深拷贝当前数值数据到新的 NumericData（对象池对象）
NumericData snapshot = component.Copy();
```

---

## 事件系统

### 事件结构体

#### `NumericChange`（数值变化通知）

```csharp
public struct NumericChange
{
    public Entity _ChangeEntity;  // NumericDataComponent.Parent（持有数值的业务实体，如 Unit）
    public int    _NumericType;   // 变化的数值类型 ID（子 ID 或 Final ID）
    public long   _Old;           // 变化前的值（raw long，含精度）
    public long   _New;           // 变化后的值（raw long，含精度）
}
```

通过 `NumericChangeHelper` 扩展方法读取：

```csharp
// 获取数值
data.GetAsFloat()       // 当前值（float）
data.GetAsInt()         // 当前值（int）
data.GetAsLong()        // 当前值（long，已按精度转换）
data.GetAsFloatOld()    // 变化前值（float）
data.GetChangeAsFloat() // 变化量（new - old，float）

// 实体相关
data.GetEntity()                   // 获取 _ChangeEntity（含空/销毁检查）
data.GetEntity<Unit>()             // 类型转换获取
data.GetEntityId()                 // 实体 ID
data.GetNumericTypeEnum()          // ENumericType 枚举
```

#### `NumericAffect`（数值影响联动）

当一个数值变化联动触发另一个数值时，`Invoke<NumericAffect, long>` 使用此结构：

```csharp
public struct NumericAffect
{
    public NumericData Data;          // 当前数值数据
    public int ENumericType;          // 触发方的数值类型 ID
    public long Old, New;             // 触发方的新旧值
    public int AffectNumericType;     // 被影响的数值类型 ID
    public long AffectCurrent;        // 被影响数值的当前值
    // 简写别名：D, NT, O, N, AT, AC
}
```

#### `NumericGMChange`（GM 调试修改通知）

GM 窗口修改数值时发送，结构与 `NumericChange` 类似，`OwnerEntity` 是 `NumericDataComponent` 本身（而非 Parent）。

### 监听方式一：静态 Handler（`NumericHandlerAttribute`）

适用于业务逻辑响应，注册在 `NumericHandlerComponent` 单例中（启动时按 Entity 类型 + 数值 ID 索引）：

```csharp
[NumericHandler(SceneType.Current, ENumericType.HP)]
public class HPChangeHandler : NumericHandlerSystem<Unit>  // T = 持有数值组件的父 Entity 类型
{
    protected override async ETTask Run(Unit self, NumericChange data)
    {
        Log.Info($"Unit {self.Id} HP changed: old={data.GetAsFloatOld()} new={data.GetAsFloat()}");
        await ETTask.CompletedTask;
    }
}
```

- `SceneType = 0/None/All`：不区分场景（适用于服务端大分发）
- `ENumericType = 0`（或无参）：监听该类型下所有数值变化
- **执行机制**：先 Run 指定 numericType，再 Run numericType=0 的全量监听，两次均用 `WaitAll` 并行执行

### 监听方式二：动态 Handler（`NumericHandlerDynamicAttribute`）

适用于 UI 等需要接收到具体组件实例的场景，注册在 `NumericHandlerDynamicComponent` 单例中（三维字典：响应组件类型 × 数值父级类型 × 数值 ID）：

```csharp
// 1. 响应组件实现动态监听接口
[ComponentOf(typeof(Unit))]
public class UnitHPBar : Entity, IAwake, INumericHandlerDynamic<Unit, NumericChange> { }

// 2. 定义 Handler
[NumericHandlerDynamic(SceneType.Current, ENumericType.HP, 1)]  // 1=精准响应层数
[FriendOf(typeof(UnitHPBar))]
public class UnitHPBar_HPHandler : NumericHandlerDynamicSystem<UnitHPBar, Unit, NumericChange>
{
    protected override async ETTask Run(UnitHPBar self, Unit entity, NumericChange data)
    {
        // self = UnitHPBar 实例（精准对应），entity = 数值所在的 Unit
        await ETTask.CompletedTask;
    }
}
```

**精准响应 InvokeParentLayerCount**：
- `0`（默认）：不检查父级，任何 Unit 数值变化都通知（需自行判断是否是自己的 Unit）
- `N`（>0）：只有当 `component.parent^N == changeEntity` 时才触发，避免创建大量无效异步 Task

**执行机制**：通过 Fiber EntitySystem 的 `Queue<INumericHandlerDynamic<T>>` 遍历所有存活组件实例，筛选匹配类型和层数后并行 WaitAll 执行。

### 事件分发链（完整流程）

```
数值修改（Change/Set/InitToServer...）
  └── NumericData.ChangeByKey()
        ├── [可选] numericType.CheckChangeNumeric()  // Editor 下检查 ID 合法性
        ├── ChangeValue()
        │     ├── CheckForceNumeric()               // 检查是否强制更新（limitConfig.Reset 非 None）
        │     ├── 跳过：isAdd && value==0 时不处理
        │     ├── 跳过：!isAdd && oldValue==value 时不处理
        │     ├── 计算 newValue（累加或覆盖）
        │     ├── [可选] NumericValueLimitConfig.Clamp(min, max)  // 应用上下限
        │     ├── [可选] NumericValueAffectConfig               // 级联影响其他数值
        │     └── [可选] PushEvent() → EventSystem.Publish<NumericChange>
        └── UpdateResult()
              └── [若为成长数值] UpdateResultInvoke()
                    ├── 计算 formulaId（numericType 对应的 Invoke ID）
                    ├── Invoke<Invoke_NumericFormula, long>  // 执行公式
                    └── ChangeValue(Final, result, isPushEvent, false)  // 写入 Final 并推事件

EventSystem.Publish<NumericChange>（Scene 级别）
  └── NumericChangeEvent_NotifyHandler [Event(SceneType.All)]
        ├── NumericHandlerComponent.Instance.Run()         // 静态 Handler 分发（按 Entity 类型 + 数值 ID）
        └── NumericHandlerDynamicComponent.Instance.Run()  // 动态 Handler 分发（队列遍历 + 精准响应）
```

---

## 公式系统

### `Invoke_NumericFormula` 结构体

```csharp
public struct Invoke_NumericFormula
{
    public NumericData Data;        // 数值数据引用
    public int ENumericType;        // 触发计算的子 ID
    public int Final;               // = ENumericType / 10
    public int Bas, Add, Pct, FinalAdd, FinalPct, ResultAdd;  // 各子 ID 的完整 ID
}
```

### 默认公式实现

`On_Invoke_NumericFormula_Handler.cs` 由工具**自动生成**（注释说明：请勿修改）：

```csharp
[Invoke(0)]  // formulaId=0 对应默认公式
public class On_Invoke_NumericFormula_Handler_0_0 : AInvokeHandler<Invoke_NumericFormula, long>
{
    public override long Handle(Invoke_NumericFormula args)
    {
        var data = args.Data;
        return ((data.GetRealValue(args.Bas) + data.GetRealValue(args.Add))
                * (NumericConst.IntRate + data.GetRealValue(args.Pct)) / NumericConst.IntRate
                + data.GetRealValue(args.FinalAdd))
               * (NumericConst.IntRate + data.GetRealValue(args.FinalPct)) / NumericConst.IntRate
               + data.GetRealValue(args.ResultAdd);
    }
}
```

每个 Final ID 通过 `numericType.GetNumericFormulaId()` 获取对应的 `formulaId`，可为不同属性注册不同公式 Handler。

---

## 配置表支持（Luban）

### `NumericValueLimitConfig`（数值上下限配置）

每种数值可配置：
- `Min` / `Max`：限制类型（`NumericValueLimitNone` / `NumericValueLimitNumber` 固定值 / `NumericValueLimitNumeric` 引用另一数值）
- `Reset`：重置累加类型（`NumericValueLimitNone` / `NumericValueLimitFormula` 公式重置 / `NumericValueLimitNumericAdd` 多数值累加）
- `Priority`：初始化时的处理优先级（影响 Limit 应用顺序）

`Reset != None` 的数值被视为 **ForceNumeric**，即使 `value == 0` 或 `oldValue == newValue` 也强制执行更新，用于依赖其他数值动态计算的场景（如 HP 上限=MaxHP）。

### `NumericValueAffectConfig`（数值联动配置）

当某数值变化时，自动触发对其他数值的影响计算：
- `Affects`：联动目标数值类型列表
- 通过 `Invoke<NumericAffect, long>` 计算影响量，然后 `ChangeByKey` 应用到目标

### `NumericConfigData.NumericData`

配置表中的数值数据延迟初始化：

```csharp
// 第一次访问时通过 Invoke<Invoke_Numeric_CreateNumericData> 创建
// ConfigData = Dictionary<ENumericType, long>，即配置表行数据
public NumericData NumericData => m_NumericData ??= EventSystem.Instance.Invoke<...>(new(...));
```

---

## Singleton 组件

### `NumericHandlerComponent`（静态分发）

```
[CodeProcess] Singleton，ISingletonAwake
数据结构：Dictionary<Type, Dictionary<int, List<NumericHandlerInfo>>>
  K1 = 持有数值的 Entity 类型（如 Unit）
  K2 = 监听的数值 ID（0=全量）
  V  = Handler 信息列表（包含 SceneType 过滤）
```

启动时扫描所有 `[NumericHandler]` 标记类，反射实例化后存入字典。分发时先精确匹配 numericType，再触发 numericType=0 全量监听。

### `NumericHandlerDynamicComponent`（动态分发）

```
[CodeProcess] Singleton，ISingletonAwake
数据结构：Dictionary<Type, Dictionary<Type, Dictionary<int, List<NumericHandlerDynamicInfo>>>>
  K1 = 响应组件类型（如 UnitHPBar）
  K2 = 数值父级 Entity 类型（如 Unit）
  K3 = 监听的数值 ID
  V  = 动态 Handler 信息列表（含 SceneType + InvokeParentLayerCount）
```

分发时通过 Fiber EntitySystem Queue 遍历所有活跃的 K1 类型实例，检查父级层数匹配后并行执行。

---

## Editor 工具

### CreateNumeric（数值生成工具）

Unity 菜单 → `ET/YIUI/Numeric`：
- 生成 `ENumericType` 枚举定义（含 Final ID + 6 个子 ID 的命名）
- 生成 Luban 配置表模板
- 生成对应 Formula Handler 模板代码（`On_Invoke_NumericFormula_Handler.cs`）

### NumericGMWindow / NumericDataDrawer（GM 调试工具）

运行时 Inspector 中可查看和修改任意 Entity 的 `NumericDataComponent`：
- 实时显示所有 `NumericDic` 的键（转 ENumericType 枚举名）和值（转 float/int/bool 显示）
- 直接修改数值并通过 `NumericGMChange` 事件通知

---

## 与其他 Package 的关系

```
cn.etetet.yiuinumeric
  ├── 依赖
  │     └── cn.etetet.yiuicodeanalysis   // Editor 代码生成工具依赖的 Roslyn/NPOI DLL
  ├── 使用（运行时隐式依赖）
  │     ├── cn.etetet.core               // Entity、ETTask、EventSystem、ObjectPool、Singleton
  │     └── cn.etetet.yiuinumericconfig  // Luban 生成的配置表类（Limit/Affect/CheckConfig）
  └── 被以下 Package 使用
        ├── cn.etetet.yiuistatesync      // 数值同步（服务端→客户端，用 InitToServer）
        ├── cn.etetet.yiuireddot         // 数值变化驱动红点刷新
        ├── cn.etetet.ui                 // UI 组件通过 NumericHandlerDynamic 监听数值
        └── cn.etetet.unit               // Unit 数值绑定（Unit_Numeric_Extend 扩展）
```

---

## 关键流程示例

### 1. 初始化角色数值

```csharp
// 创建 Entity 并挂载数值组件
var unit = scene.AddChild<Unit>();
var numeric = unit.AddComponent<NumericDataComponent>();

// 从配置表初始化（不触发事件，批量设置效率高）
numeric.NumericData.InitSet(configData.NumericData.NumericDic);
```

### 2. 应用装备/技能 buff

```csharp
// 装备增加 HP 附加值（Change 累加，会触发 Final 重算 + 事件推送）
unit.GetComponent<NumericDataComponent>().Change(ENumericType.HP_Add, 500L);

// 技能增加 HP 百分比（+10% = 1000 precision）
unit.GetComponent<NumericDataComponent>().Change(ENumericType.HP_Pct, 1000L);
```

### 3. 读取最终值

```csharp
var hpComponent = unit.GetComponent<NumericDataComponent>();
float hp    = hpComponent.GetAsFloat(ENumericType.HP);   // 最终 HP（float）
int   maxHp = hpComponent.GetAsInt(ENumericType.MaxHP);  // 最大 HP（int）
```

### 4. 监听数值变化更新 UI

```csharp
[NumericHandlerDynamic(SceneType.Current, ENumericType.HP, 1)]
public class HPBarComponent_HPHandler : NumericHandlerDynamicSystem<HPBarComponent, Unit, NumericChange>
{
    protected override async ETTask Run(HPBarComponent self, Unit entity, NumericChange data)
    {
        self.SetHP(data.GetAsFloat(), data.GetAsFloatOld());
        await ETTask.CompletedTask;
    }
}
```

---

## 最佳实践

1. **修改方式优先级**：`Change` > `Set` > `UnCheck` 系列，尽量用 Change 累加，避免直接 Set
2. **不要直接访问 `NumericDic`**：所有读写都应通过扩展方法，确保精度转换和事件触发
3. **批量初始化用 NoEvent**：`InitSet`/`ChangeNoEvent`/`SetNoEvent` 在初始化时避免重复触发事件
4. **动态监听优先于静态监听**：UI 刷新用 `NumericHandlerDynamic`（精准响应），业务逻辑用 `NumericHandler`
5. **精准响应 InvokeParentLayerCount**：能用精准响应时一定用，避免大量无效异步 Task 开销
6. **快照/合并用对象池**：`Add`/`Subtract`/`Copy`/`Difference` 都返回对象池对象，用完必须 `Dispose`
7. **Float 数值**：读写 float 时用 `GetAsFloat`/传 float 的 `Change`，不要手动乘除 FloatRate
8. **ForceNumeric 注意**：配置了 Limit.Reset 的数值即使不变化也会强制重算，设计时注意性能影响
