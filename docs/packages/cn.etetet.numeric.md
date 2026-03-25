# cn.etetet.numeric

**版本**: 3.0.0
**分类**: 数值系统 / 元数据包
**作者**: tanghai (ET Framework)
**Package ID**: 23 (packagegit.json)

---

## 概述

`cn.etetet.numeric` 是 ET 框架数值系统（Numeric KV）的**元数据/占位包**。与 `cn.etetet.yiuinumeric`（完整实现包）不同，此包**不包含任何 C# 源代码文件**，其主要作用是：

1. **包管理标识**：在 PackageManager 中注册 "Numeric" 功能的存在，Id=23
2. **依赖声明占位**：其他包可通过此包标识符声明对 Numeric 功能的依赖
3. **Assembly Definition 占位**：包含 `Ignore.ET.Numeric.asmdef`，使用 `IGNORE` define 约束，表示该 asmdef 下的文件永不参与编译（ET 框架的"忽略模式"标准模式）

---

## 目录结构

```
cn.etetet.numeric/
├── Ignore.ET.Numeric.asmdef        # 带 IGNORE 约束的 Assembly Definition（占位）
├── package.json                    # 包元数据
├── packagegit.json                 # 包 Git ID 信息 (Id=23, Name="Numeric")
└── LICENSE
```

---

## 核心文件说明

### `package.json`

```json
{
  "name": "cn.etetet.numeric",
  "displayName": "ET.Numeric",
  "version": "3.0.0",
  "unity": "2022.3",
  "description": "一个数值组件，提供了kv的实现",
  "relatedPackages": {},
  "publishConfig": {
    "registry": "https://npm.pkg.github.com/@ET-Packages"
  }
}
```

描述为"一个数值组件，提供了kv的实现"，来源于 ET 主仓库（github.com/egametang/ET）。

### `Ignore.ET.Numeric.asmdef`

```json
{
  "name": "Ignore.ET.Numeric",
  "defineConstraints": ["IGNORE"],
  "autoReferenced": true
}
```

`defineConstraints: ["IGNORE"]` 是 ET 框架的标准"忽略"模式：
- Unity 中"IGNORE"符号通常不在 Player Settings 中定义
- 因此此 Assembly Definition 下的文件**永远不会被编译**
- 此模式用于标记"此目录存在但不编译"的意图，是 ET 代码生成工作流的一部分

---

## 与实现包的区别

| 特性 | cn.etetet.numeric | cn.etetet.yiuinumeric |
|------|------------------|----------------------|
| C# 源码文件 | ❌ 无 | ✅ 完整实现 |
| 功能定位 | 元数据/占位 | 真正的数值系统实现 |
| 版本 | 3.0.0 | 4.0.8 |
| 依赖声明 | 无 | yiuicodeanalysis 等 |
| asmdef 模式 | Ignore（IGNORE 约束） | 正常编译 |

---

## 依赖关系

此包本身无依赖（`relatedPackages: {}`），也无 C# 代码，因此不对其他包产生运行时影响。

**被依赖关系**（通过 packagegit Id=23 被 PackageManager 识别为 "Numeric" 功能）：
- `cn.etetet.packagemanager` — 用于包的注册和识别
- `cn.etetet.yiuinumeric` — 是 Numeric 功能的真正实现，在 YIUI 层扩展

---

## ET 框架"Ignore"模式说明

ET 框架中大量使用 `Ignore.*.asmdef` 模式，原因如下：

1. **分离关注点**：ET 核心框架代码（如 ET 主仓库中的原始文件）和 YIUI 扩展包各自独立发布
2. **防止编译冲突**：当 ET 核心代码被 YIUI 包替代后，原始文件需要被"忽略"以避免重复定义
3. **代码生成占位**：部分文件是代码生成系统的输出目标，运行时不需要编译原始模板

对于 `cn.etetet.numeric`，真正的数值系统实现由 `cn.etetet.yiuinumeric` 提供（包含完整的 NumericData、NumericDataComponent、事件系统、Editor 工具等）。

---

## Round 2 补充：包关系交叉验证

### 与 cn.etetet.yiuinumeric 的完整对应关系

经 Round 2 交叉核验，`cn.etetet.yiuinumeric`（v4.0.8）是本包功能的**唯一完整实现**，两包的分工如下：

| 职责 | cn.etetet.numeric（本包） | cn.etetet.yiuinumeric |
|------|--------------------------|----------------------|
| 包标识 / PackageId | ✅ Id=23, Name="Numeric" | — |
| Numeric KV 数据容器 | ❌ | ✅ NumericData（对象池） |
| Entity 挂载组件 | ❌ | ✅ NumericDataComponent |
| 分层 ID 公式计算 | ❌ | ✅ 6 层子ID自动聚合 |
| 数值变化事件 | ❌ | ✅ NumericChange / NumericHandler |
| 配置驱动公式 | ❌ | ✅ Luban 配置表集成 |

### IGNORE 约束在项目全局中的位置

项目中使用 `defineConstraints: ["IGNORE"]` 的包（截至 Round 2 已分析）均遵循同一规律：
- 这些包是 ET 主仓库功能的**占位元数据包**
- 对应的 YIUI 扩展包（`cn.etetet.yiui*`）提供真正实现
- PackageManager（`cn.etetet.packagemanager`）通过 `packagegit.json` 中的 `Id` 字段识别功能，不依赖 C# 代码是否存在

### 无遗漏项确认

由于本包无 C# 源文件，Round 1 文档已覆盖所有内容，无补充遗漏类或方法。

---

## 参考

数值系统的完整文档请参阅：[cn.etetet.yiuinumeric.md](cn.etetet.yiuinumeric.md)
