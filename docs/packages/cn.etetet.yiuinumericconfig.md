# cn.etetet.yiuinumericconfig

**版本**: 0.0.0
**分类**: UI/YIUI
**命名空间**: `ET`
**依赖**: 无（运行时无强制依赖，Editor 层隐式依赖 cn.etetet.yiuinumeric）

## 概述

YIUI NumericConfig 是 `cn.etetet.yiuinumeric` 数值系统的**配置数据包**，由 Luban 代码生成工具自动生成。本包定义了游戏中所有数值属性的：

1. **枚举定义**（`ENumericType`）：所有数值 ID 的强类型枚举
2. **数值检查配置**（`NumericValueCheckConfig`）：每种数值的值类型、通知策略、是否存档等元信息
3. **上下限配置**（`NumericValueLimitConfig`）：每种数值的最小值/最大值及重置规则
4. **联动影响配置**（`NumericValueAffectConfig`）：数值 A 变化时联动影响数值 B
5. **公式配置**（`NumericFormulaConfig`）：自定义每个数值的计算公式
6. **本地化配置**（`NumericLocalizationConfig`）：数值名称的多语言文本
7. **初始值数据**（`NumericConfigData`）：每种数值类型的初始默认值

---

## 目录结构

```
cn.etetet.yiuinumericconfig/
├── CodeMode/                           # Luban 生成代码（三种部署模式）
│   ├── Model/Client/LubanGen/          # 纯客户端模式
│   ├── Model/Server/LubanGen/          # 纯服务端模式
│   └── Model/ClientServer/LubanGen/   # 客户端+服务端共用（当前项目使用）
│       └── Config/
│           ├── ENumericType.cs                   # 所有数值类型枚举
│           ├── ENumericDefinitionType.cs         # 数值层级定义枚举（Result/Base/Add/Pct...）
│           ├── ENumericValueType.cs              # 数值值类型枚举（None/Int/Long/Bool/Float）
│           ├── ENumericNoticeType.cs             # 通知类型枚举（None/Self/Broadcast/...）
│           ├── ENumericTag.cs                    # 数值标签枚举（Flags，Tag1~Tag31）
│           ├── ENumericValueLimitType.cs         # 限制来源类型（Number/Numeric）
│           ├── NumericConfigData.cs              # 数值初始值字典（ENumericType → long）
│           ├── NumericValueCheckConfig.cs        # 数值元信息检查配置（类型/存档/通知）
│           ├── NumericValueCheckConfigCategory.cs # 检查配置集合（单例）
│           ├── NumericValueLimitConfig.cs        # 数值上下限配置
│           ├── NumericValueLimitConfigCategory.cs # 上下限配置集合（单例）
│           ├── NumericValueLimitData.cs          # 上下限数据基类（多态）
│           ├── NumericValueLimitNone.cs          # 无限制
│           ├── NumericValueLimitNumber.cs        # 固定数字限制
│           ├── NumericValueLimitNumeric.cs       # 引用另一个数值作为限制
│           ├── NumericValueLimitNumericAdd.cs    # 引用数值+固定值作为限制
│           ├── NumericValueLimitFormula.cs       # 自定义公式限制
│           ├── NumericValueAffectConfig.cs       # 数值联动影响配置
│           ├── NumericValueAffectConfigCategory.cs # 影响配置集合（单例）
│           ├── NumericFormulaConfig.cs           # 公式配置
│           ├── NumericFormulaConfigCategory.cs  # 公式配置集合（单例）
│           ├── NumericLocalizationConfig.cs      # 数值本地化配置
│           └── NumericLocalizationConfigCategory.cs # 本地化配置集合（单例）
└── Scripts/
    ├── Model/Share/Config/
    │   └── NumericValueAffectConfig_Extend.cs    # AffectConfig 手写扩展（唯一 ID 生成+缓存）
    └── Hotfix/Share/Affect/
        └── NumericAffectSystem.cs                # 数值影响 Invoke Handler（手写业务逻辑）
```

---

## 枚举定义

### `ENumericType`（数值类型枚举）

项目中所有数值属性的强类型枚举，由数值生成工具根据配置表生成。ID 编码规则：`{分类}{序号}{层级位}` 其中个位表示层级（0=Final）。

| 枚举值 | ID | 类型 | 说明 |
|--------|-----|------|------|
| `None` | 0 | - | 无 |
| `NumericTest0` | 100000 | Int | 测试数值（Final） |
| `Level0` | 100001 | Int | 等级（Final） |
| `Exp0` | 100002 | Int | 经验值（Final） |
| `Money0` | 100003 | Int | 金钱（Final） |
| `Diamond0` | 100004 | Int | 钻石（Final） |
| `Hp0` | 200001 | Int | 当前血量（Final） |
| `MaxHp0` | 200002 | Int | 最大血量（Final） |
| `MaxHp1`~`MaxHp6` | 2000021~2000026 | Int/Float | 最大血量子层（Bas/Add/Pct/FinalAdd/FinalPct/ResultAdd） |
| `Speed0` | 200003 | Float | 速度（Final） |
| `Speed1`~`Speed6` | 2000031~2000036 | Float | 速度子层 |
| `AOI0` | 200004 | Float | AOI 范围（Final） |
| `AOI1`~`AOI6` | 2000041~2000046 | Float | AOI 子层 |
| `Temp1Data0`~`Temp10Data0` | 990001~990010 | Int | 临时数据槽（用于 buff/临时状态） |

**命名规范**：`{属性名}{N}` 中 N=0 是 Final 结果值，N=1~6 对应子层（Base/Add/Pct/FinalAdd/FinalPct/ResultAdd）。

### `ENumericDefinitionType`（数值层级定义）

| 枚举值 | 个位 | 含义 |
|--------|------|------|
| `Result` | 0 | 最终计算结果（只读） |
| `Base` | 1 | 基础值（角色基础属性） |
| `Add` | 2 | 基础附加值（装备/技能叠加） |
| `Pct` | 3 | 基础百分比（×10000 精度） |
| `FinalAdd` | 4 | 最终附加值（后置加法） |
| `FinalPct` | 5 | 最终百分比（×10000 精度） |
| `ResultAdd` | 6 | 结果增加（最终后增加） |

### `ENumericValueType`（数值值类型）

| 枚举值 | 值 | 说明 |
|--------|-----|------|
| `None` | 0 | 无类型 |
| `Int` | 1 | 整型（32位） |
| `Long` | 2 | 长整型（64位） |
| `Bool` | 3 | 布尔型 |
| `Float` | 4 | 浮点型 |

> 注：原始 Excel 配置中 Int 的 LabelText 写的是 "EV_Init"，是配置表中的笔误，实际映射到 `Int` 类型。

### `ENumericNoticeType`（通知类型）

| 枚举值 | 值 | 说明 |
|--------|-----|------|
| `None` | 0 | 不通知 |
| `Self` | 1 | 只通知自身 |
| `Broadcast` | 2 | 广播（含自身） |
| `BroadcastWithoutSelf` | 3 | 广播（不含自身） |

### `ENumericTag`（数值标签，Flags 枚举）

`[System.Flags]` 标记，支持位运算。包含 Tag1~Tag31（值 1~1073741824，2 的幂次），目前标签含义由项目自定义，本包只定义枚举值，具体语义由 `cn.etetet.yiuinumeric` 业务层决定。

### `ENumericValueLimitType`（限制来源类型）

| 枚举值 | 值 | 说明 |
|--------|-----|------|
| `Number` | 0 | 固定数字 |
| `Numeric` | 1 | 引用另一个数值 |

---

## 数值检查配置（NumericValueCheckConfig）【Round 2 新增补充】

**第一轮遗漏**，此配置是数值系统中**最核心的元数据表**，每个数值类型都应有对应条目。

```csharp
public sealed partial class NumericValueCheckConfig : Luban.BeanBase
{
    public readonly ENumericType Id;           // 数值类型 ID（主键）
    public readonly ENumericValueType Check;   // 此数值的存储类型（Int/Long/Bool/Float）
    public readonly string Alias;              // 数值别名（调试用）
    public readonly string Desc;               // 数值描述（文档用）
    public readonly bool NotGrow;              // 是否不会增长（true = 只减不增，如 Hp）
    public readonly bool NeedSave;             // 是否需要持久化存档
    public readonly ENumericNoticeType NoticeType; // 数值变化时的网络通知策略
    public readonly int FormulaId;             // 关联的计算公式 ID
    public NumericFormulaConfig FormulaId_Ref; // ResolveRef 后的公式引用
}
```

**ResolveRef 机制**：Luban 在加载完所有配置表后调用 `ResolveRef()`，通过 `NumericFormulaConfigCategory.Instance.GetOrDefault(FormulaId)` 将 `FormulaId` 整数解析为 `NumericFormulaConfig` 对象引用，实现跨表引用。

**代码示例**：
```csharp
// 查询某数值是否需要存档
var checkConfig = NumericValueCheckConfigCategory.Instance.GetOrDefault(ENumericType.Hp0);
if (checkConfig != null && checkConfig.NeedSave)
{
    // 将 Hp0 值存入存档
}

// 查询数值的网络通知范围
if (checkConfig.NoticeType == ENumericNoticeType.Broadcast)
{
    // 需要同步给周围玩家
}
```

---

## 数值初始值数据（NumericConfigData）

```csharp
public sealed partial class NumericConfigData : Luban.BeanBase
{
    // 每个 ENumericType 对应的初始值（long 统一存储）
    public readonly Dictionary<ENumericType, long> ConfigData;
}
```

`NumericConfigData` 不是单例 Category，而是一个**可被多个单元持有的数据 Bean**。不同单元（角色、NPC）可根据其类型持有不同的 `NumericConfigData` 实例，作为初始化数值组件的基础数据。

**代码示例**：
```csharp
// 用 ConfigData 初始化角色数值
foreach (var kv in numericConfigData.ConfigData)
{
    unit.SetNumeric(kv.Key, kv.Value);
}
```

---

## 数值限制配置（NumericValueLimitConfig）

每条配置定义一种数值的约束规则：

```csharp
public sealed partial class NumericValueLimitConfig : Luban.BeanBase
{
    public readonly ENumericType Id;          // 目标数值类型（主键）
    public readonly NumericValueLimitData Reset; // 被修改时的重置规则
    public readonly NumericValueLimitData Min;   // 最小值约束
    public readonly NumericValueLimitData Max;   // 最大值约束
    public readonly int Priority;              // 优先级（多条规则时的处理顺序）
}
```

### `NumericValueLimitData` 子类型（多态限制数据）

| 子类 | 说明 | 参数 |
|------|------|------|
| `NumericValueLimitNone` | 无限制 | - |
| `NumericValueLimitNumber` | 固定数字 | `long Value` |
| `NumericValueLimitNumeric` | 引用另一个数值 | `ENumericType Value` |
| `NumericValueLimitNumericAdd` | 引用数值+固定偏移 | `ENumericType Value + long Add` |
| `NumericValueLimitFormula` | 自定义公式（Invoke） | Formula ID |

**典型配置**：当前血量（Hp0）的 Max 设为 `NumericValueLimitNumeric(MaxHp0)`，即 HP ≤ MaxHP。

---

## 数值联动影响配置（NumericValueAffectConfig）

```csharp
public sealed partial class NumericValueAffectConfig : Luban.BeanBase
{
    public readonly ENumericType Id;                // 触发变化的数值（主键）
    public readonly List<ENumericType> Affects;     // 受影响的数值列表
}
```

### 扩展方法（NumericValueAffectConfig_Extend.cs）

手写扩展 partial class，为 Luban 生成类添加运行时缓存优化：

```csharp
public sealed partial class NumericValueAffectConfig
{
    private readonly HashSet<ENumericType> m_AffectHash = new();          // O(1) 查找缓存
    private readonly Dictionary<ENumericType, long> m_AffectUniqueId = new(); // 唯一 ID 缓存

    // 快速检查数值是否在影响列表中
    public bool IsAffect(ENumericType type) => m_AffectHash.Contains(type);

    // 获取(触发者,被影响者)对应的 Invoke 唯一 ID（懒加载）
    public long GetAffectUniqueId(ENumericType affectType)
    {
        if (!m_AffectUniqueId.TryGetValue(affectType, out var uniqueId))
        {
            uniqueId = ((long)(int)Id << 32) | ((int)affectType & 0xFFFFFFFFL);
            m_AffectUniqueId.Add(affectType, uniqueId);
        }
        return uniqueId;
    }

    // EndRef() 钩子：Luban 加载完成后自动初始化 m_AffectHash
    partial void EndRef()
    {
        foreach (var affect in Affects)
            m_AffectHash.Add(affect);
    }
}
```

**唯一 ID 公式**：`uniqueId = ((long)triggerId << 32) | (affectId & 0xFFFFFFFFL)`

例：MaxHp0(200002) → Hp0(200001) 的 ID = `(200002L << 32) | 200001 = 859002049334593`

### 当前配置的影响关系

```
MaxHp0(200002) → Hp0(200001)   ID: 859002049334593
  // 最大血量变化时，若当前血量 > 最大血量，截断为最大血量

Level0(100001) → Exp0(100002)  ID: 429501024667298
  // 等级变化（升级）时，经验值重置为 0
```

---

## Invoke Handler（NumericAffectSystem.cs）

```csharp
[Invoke(859002049334593)]  // MaxHp0 → Hp0
public class NumericAffectInvokeHandler_859002049334593 : AInvokeHandler<NumericAffect, long>
{
    public override long Handle(NumericAffect A)
    {
        // A.AC = 当前血量当前值, A.N = MaxHp 新值
        if (A.AC > A.N) return A.N;  // 超过则截断
        return A.AC;                  // 否则保持不变
    }
}

[Invoke(429501024667298)]  // Level0 → Exp0
public class NumericAffectInvokeHandler_429501024667298 : AInvokeHandler<NumericAffect, long>
{
    public override long Handle(NumericAffect A) => 0;  // 升级后经验清零
}
```

`NumericAffect` 结构体字段：
- `A.AC` - 被影响数值的**当前值**（Affected Current）
- `A.N` - 触发数值的**新值**（New value）

---

## Luban 生命周期钩子

所有 Luban 生成类均遵循以下 partial 方法生命周期：

```
反序列化构造函数调用 → EndInit()  [可在此做额外初始化]
                           ↓
全部配置加载完成后调用 → ResolveRef() → EndRef()  [可在此解析跨表引用]
```

- `EndInit()`：构造完成后立即调用，适合做字段验证或本地预计算
- `EndRef()`：所有表都加载后调用，适合做跨表引用初始化（如 m_AffectHash 的初始化）

---

## 公式配置（NumericFormulaConfig）

定义每种属性的计算公式（可覆盖 `cn.etetet.yiuinumeric` 中的默认公式），通过 Invoke Handler 注册自定义公式逻辑。配置由公式 ID 关联，在 `NumericValueCheckConfig.FormulaId_Ref` 和 `NumericValueLimitFormula` 中被引用。

---

## 与其他 Package 的关系

```
cn.etetet.yiuinumericconfig
  ├── 被 cn.etetet.yiuinumeric 消费：
  │     NumericValueCheckConfigCategory.Instance.GetOrDefault(type)   // 获取数值元信息
  │     NumericValueLimitConfigCategory.Instance.GetOrDefault(type)   // 获取上下限配置
  │     NumericValueAffectConfigCategory.Instance.GetOrDefault(type)  // 获取联动配置
  │     NumericFormulaConfigCategory.Instance.GetOrDefault(id)        // 获取公式配置
  │     NumericLocalizationConfigCategory.Instance.GetOrDefault(type) // 获取本地化名称
  └── 配置数据由 cn.etetet.yiuiluban / cn.etetet.excel 在 Editor 工具中生成
```

---

## 完整枚举速查

### ENumericTag（32位 Flags）
共 31 个标签位（Tag1=1, Tag2=2, ..., Tag31=1073741824），业务含义由 cn.etetet.yiuinumeric 或上层业务包自定义。

---

## 使用说明

1. **添加新数值属性**：在 Luban Excel 配置表中添加行 → 重新生成代码 → `ENumericType` 自动更新
2. **配置数值元信息**：在 `NumericValueCheckConfig`（对应 XML: `numeric_check.xml`）表中设置类型、存档策略、通知范围
3. **设置血量上限约束**：在 `NumericValueLimitConfig` 表中为 `Hp0` 添加 Max = `NumericValueLimitNumeric(MaxHp0)`
4. **实现属性联动**：在 `NumericValueAffectConfig` 表添加触发关系 → 在 `NumericAffectSystem.cs` 实现 Handle 逻辑（使用 `GetAffectUniqueId` 生成的 ID 注册 `[Invoke]`）
5. **所有 LubanGen 文件不可手动修改**，扩展逻辑在 `Scripts/` 下的 `_Extend.cs` 文件中实现（partial class）
