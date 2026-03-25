# cn.etetet.unit

## 概述

ET.Unit 包提供了一个包含位置（Position）和旋转（Rotation）的场景实体基类 `Unit`，是 MMO 游戏中所有可移动/可定位场景对象（玩家、怪物、NPC 等）的核心抽象。该包定义了 Unit 实体的数据模型、容器组件及事件类型，是其他依赖空间信息的系统（移动、AOI、AI 等）的基础。

- **版本**：3.0.0
- **Unity 版本**：2022.3
- **作者**：tanghai（ET 框架作者）
- **PackageType 常量**：`5`

---

## 目录结构

```
cn.etetet.unit/
├── package.json                         # 包元信息
├── Scripts/
│   ├── Model/Share/
│   │   ├── Unit.cs                      # Unit 实体定义（位置/旋转数据）
│   │   ├── UnitComponent.cs             # UnitComponent，Scene 的子组件，管理所有 Unit
│   │   ├── UnitEventType.cs             # 事件结构体：ChangePosition、ChangeRotation
│   │   ├── UnitType.cs                  # Unit 类型枚举常量
│   │   └── PackageType.cs               # 本包的 PackageType 常量（Unit = 5）
│   └── Hotfix/Share/
│       ├── UnitSystem.cs                # Unit 的 EntitySystem（Awake、Config、Type）
│       └── UnitComponentSystem.cs       # UnitComponent 的 EntitySystem（Add/Get/Remove）
```

---

## 核心类与接口

### `Unit` （Model/Share/Unit.cs）

```csharp
[ChildOf(typeof(UnitComponent))]
public partial class Unit : Entity, IAwake<int>
```

场景中的基础实体，持有位置和旋转信息。

| 成员 | 类型 | 说明 |
|------|------|------|
| `ConfigId` | `int` | 配置表 ID，初始化时赋值 |
| `position` | `float3`（private BsonElement） | MongoDB 序列化存储的坐标 |
| `Position` | `float3`（BsonIgnore） | 坐标属性，设置时发布 `ChangePosition` 事件 |
| `rotation` | `quaternion`（private BsonElement） | MongoDB 序列化存储的旋转 |
| `Rotation` | `quaternion`（BsonIgnore） | 旋转属性，设置时发布 `ChangeRotation` 事件 |
| `Forward` | `float3` | 朝向向量，通过 Rotation 换算 |
| `ViewName` | `string`（override） | 调试用显示名称，格式：`{FullName} ({Id})` |

**关键设计**：
- `position`/`rotation` 使用 `[BsonElement]` 标注以支持 MongoDB 持久化
- `Position`/`Rotation` 属性拦截 set 操作，自动发布事件，驱动 AOI、移动、View 等子系统响应

---

### `UnitComponent` （Model/Share/UnitComponent.cs）

```csharp
[ComponentOf(typeof(Scene))]
public class UnitComponent : Entity, IAwake, IDestroy
```

挂载在 `Scene` 上的组件，作为所有 `Unit` 的父容器（通过 ET 框架的 `ChildOf` 层级管理）。

---

### 事件结构体 （Model/Share/UnitEventType.cs）

| 结构体 | 字段 | 触发时机 |
|--------|------|---------|
| `ChangePosition` | `Unit Unit`, `float3 OldPos` | `Unit.Position` setter 被调用时 |
| `ChangeRotation` | `Unit Unit` | `Unit.Rotation` setter 被调用时 |

---

### `UnitType` （Model/Share/UnitType.cs）

```csharp
public static partial class UnitType
{
    public const int Player  = PackageType.Unit * 1000 + 1;  // 5001
    public const int Monster = PackageType.Unit * 1000 + 2;  // 5002
    public const int NPC     = PackageType.Unit * 1000 + 3;  // 5003
}
```

Unit 类型常量，基于 PackageType（5）× 1000 偏移，避免跨包 ID 冲突。`partial` 允许其他包扩展。

---

### `PackageType` （Model/Share/PackageType.cs）

```csharp
public static partial class PackageType
{
    public const int Unit = 5;
}
```

本包在全局 PackageType 命名空间中的编号。

---

## Hotfix 系统

### `UnitSystem` （Hotfix/Share/UnitSystem.cs）

```csharp
[EntitySystemOf(typeof(Unit))]
public static partial class UnitSystem
```

| 方法 | 说明 |
|------|------|
| `Awake(Unit self, int configId)` | 初始化，将 configId 赋给 Unit |
| `Config(Unit self)` | 返回对应的 `UnitConfig` 配置表对象 |
| `Type(Unit self)` | 返回 Unit 的类型（来自配置表 `Type` 字段） |

---

### `UnitComponentSystem` （Hotfix/Share/UnitComponentSystem.cs）

```csharp
public static partial class UnitComponentSystem
```

| 方法 | 说明 |
|------|------|
| `Add(UnitComponent self, Unit unit)` | 添加 Unit（当前实现为空，子类或其他包扩展） |
| `Get(UnitComponent self, long id)` | 通过 id 获取 Unit（使用 ET 父子层级查找） |
| `Remove(UnitComponent self, long id)` | 通过 id 移除并 Dispose Unit |

---

## 架构模式

1. **ET ECS 实体-组件模式**：`Unit` 是 `Entity` 的子类，挂载在 `UnitComponent` 下。
2. **事件驱动**：位置/旋转变更通过 `EventSystem.Instance.Publish` 广播，解耦后续处理（AOI 格子更新、View 同步、移动系统等）。
3. **Model / Hotfix 分层**：数据（Model）与逻辑（Hotfix）分离，支持 HybridCLR 热更。
4. **partial 扩展**：`UnitType`、`PackageType`、`UnitSystem`、`UnitComponentSystem` 均为 `partial`，其他包可无侵入地追加类型和方法。

---

## 关键流程

### Unit 创建流程
```
调用方 (如 LoginSystem)
  → Scene.GetComponent<UnitComponent>()
  → scene.AddChild<Unit, int>(configId)
      → UnitSystem.Awake(unit, configId)  // 设置 ConfigId
  → UnitComponent.Add(unit)               // 注册（当前空实现，供扩展）
```

### 位置变更通知流程
```
unit.Position = newPos
  → EventSystem.Publish(scene, new ChangePosition { Unit=unit, OldPos=old })
      → AOI 系统订阅：更新格子归属
      → Move 系统订阅：同步插值
      → View 系统订阅：同步到客户端渲染
```

---

## 依赖关系

### 本包依赖
- **ET Core**：`Entity`、`IAwake`、`IDestroy`、`EventSystem` 等基础框架
- **Unity.Mathematics**：`float3`、`quaternion`、`math` 等数学类型
- **MongoDB.Bson**：`[BsonElement]`、`[BsonIgnore]` 序列化标注
- **UnitConfigCategory**（配置表）：由外部 Excel/Luban 生成，`UnitSystem.Config()` 依赖

### 被以下包依赖（典型）
| 包 | 依赖原因 |
|----|---------|
| `cn.etetet.move` | 使用 `unit.Position` 驱动移动 |
| `cn.etetet.aoi` | 订阅 `ChangePosition` 事件更新 AOI 格子 |
| `cn.etetet.ai` | 读取 Unit 位置进行 AI 决策 |
| `cn.etetet.actorlocation` | Actor 定位需要 Unit 作为场景实体 |
| `cn.etetet.statesync` | 状态同步依赖 Unit 的位置旋转数据 |
| `cn.etetet.ui` | UI 系统可能读取 Unit 信息显示 |

---

## 备注

- `UnitComponentSystem.Add` 方法当前为空实现，实际的注册逻辑（如 AOI 注册）由依赖本包的上层包通过 `partial` 或事件订阅扩展。
- `Unit` 的 `position`/`rotation` 字段使用 `[BsonElement]` 支持服务端 MongoDB 持久化，客户端侧此标注无实际影响。
- `UnitType` 使用 `PackageType * 1000 + N` 的编号规范，确保全局唯一性。
