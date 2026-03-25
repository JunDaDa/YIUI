# cn.etetet.aoi

## 概述

AOI（Area of Interest，感兴趣区域）系统，基于**九宫格算法**实现服务端空间感知。负责管理游戏场景中哪些 Unit 相互"可见"，是大世界场景同步的核心基础设施。

- **版本**：3.0.0
- **作者**：tanghai（ET 框架）
- **适用层**：Server Only（Model + Hotfix）
- **依赖**：无显式 package 依赖，运行时依赖 `cn.etetet.core`（Unit、Scene、EntitySystem）

---

## 目录结构

```
Scripts/
├── Model/Server/
│   ├── AOIEntity.cs           # AOI 实体组件（挂载在 Unit 上）
│   ├── AOIManagerComponent.cs # AOI 管理器（挂载在 Scene 上），常量 CellSize
│   ├── Cell.cs                # 网格单元格
│   └── AOIEventType.cs        # 视野事件结构体（UnitEnterSightRange / UnitLeaveSightRange）
└── Hotfix/Server/
    ├── AOIEntitySystem.cs      # AOIEntity 系统：Awake/Destroy + Sub/UnSub/EnterSight/LeaveSight
    ├── AOIManagerComponentSystem.cs # 管理器：Add/Remove/Move
    ├── AOIHelper.cs            # 九宫格坐标工具：CreateCellId / CalcEnterAndLeaveCell
    ├── AOISeeCheckHelper.cs    # 可视性检查（partial，可扩展）
    └── CellSystem.cs           # Cell 系统：Add/Remove + CellIdToString 调试工具
```

---

## 核心类说明

### AOIManagerComponent
```csharp
[ComponentOf(typeof(Scene))]
public class AOIManagerComponent : Entity, IAwake
{
    public const int CellSize = 10 * 1000;  // 10 个游戏单位 = 一格（坐标已×1000）
}
```
- 挂载在 Scene 上，全局唯一
- `CellSize = 10000`：游戏世界坐标已放大 1000 倍存储，所以 10 个游戏单位 = 10×1000 = 一格
- Cell 以 Child 方式懒创建（`GetChild → null → AddChildWithId`），不预分配整个地图网格

### Cell
```csharp
[ChildOf(typeof(AOIManagerComponent))]
public class Cell : Entity, IAwake, IDestroy
```
- 九宫格中的一个格子，Entity.Id 由坐标编码 `CreateCellId(x, y)` 生成
- 三个集合：

| 字段 | 说明 |
|------|------|
| `AOIUnits` | 当前物理位置在此格的所有 AOIEntity |
| `SubsEnterEntities` | 订阅了"有 Unit 进入此格"事件的 AOIEntity |
| `SubsLeaveEntities` | 订阅了"有 Unit 离开此格"事件的 AOIEntity |

### AOIEntity
```csharp
[ComponentOf(typeof(Unit))]
public class AOIEntity : Entity, IAwake<int, float3>, IDestroy
```
- 挂载在 Unit 上，Awake 参数：`distance`（视距）+ `pos`（初始位置）
- 核心字段：

| 字段 | 说明 |
|------|------|
| `ViewDistance` | 视野半径（游戏单位，未放大） |
| `Cell` | 当前所在格子（EntityRef 弱引用） |
| `SubEnterCells` | 当前订阅 Enter 事件的格子 ID 集合（持久，跨帧） |
| `SubLeaveCells` | 当前订阅 Leave 事件的格子 ID 集合（持久，跨帧） |
| `enterHashSet` / `leaveHashSet` | Move 时临时计算集合（复用引用，避免 GC） |
| `SeeUnits` | 我能看到的所有 AOIEntity |
| `BeSeeUnits` | 能看到我的所有 AOIEntity |
| `SeePlayers` | 我能看到的 Player AOIEntity（SeeUnits 子集） |
| `BeSeePlayers` | 能看到我的 Player AOIEntity（BeSeeUnits 子集，用于广播） |

### AOIEventType
```csharp
public struct UnitEnterSightRange { public AOIEntity A; public AOIEntity B; }
public struct UnitLeaveSightRange { public AOIEntity A; public AOIEntity B; }
```
- 通过 `EventSystem.Instance.Publish(scene, ...)` 广播
- `A` = 观察者（视野所有者），`B` = 被观察者
- 其他 package 订阅这两个事件来实现场景同步逻辑

---

## 完整方法列表

### AOIManagerComponentSystem（管理器操作）

| 方法 | 说明 |
|------|------|
| `Add(aoiEntity, x, y)` | 将 Unit 加入 AOI 系统，初始化订阅和视野 |
| `Remove(aoiEntity)` | 从 AOI 系统移除 Unit，清理所有订阅和视野状态 |
| `Move(aoiEntity, cellX, cellY)` | Unit 移动到新格子，更新订阅和视野 |
| `Move(aoiEntity, newCell, preCell)` | 内部静态方法：执行格子交换和通知 |
| `GetCell(cellId)` | 私有：懒创建 Cell |

### AOIEntitySystem（AOIEntity 操作）

| 方法 | 说明 |
|------|------|
| `SubEnter(cell)` | 订阅 cell 的 Enter 事件，并对 cell 内已有 Unit 调用 EnterSight |
| `UnSubEnter(cell)` | 取消 cell 的 Enter 事件订阅 |
| `SubLeave(cell)` | 订阅 cell 的 Leave 事件 |
| `UnSubLeave(cell)` | 取消 cell 的 Leave 事件订阅，**并对 cell 内当前 Unit 调用 LeaveSight** |
| `EnterSight(enter)` | enter 进入 self 视野：更新双向关系，Publish 事件 |
| `LeaveSight(leave)` | leave 离开 self 视野：更新双向关系，Publish 事件 |
| `GetSeeUnits()` | 获取我能看见的所有 Unit（返回 SeeUnits） |
| `GetSeePlayers()` | 获取我能看见的 Player（返回 SeePlayers） |
| `GetBeSeePlayers()` | 获取能看见我的 Player（返回 BeSeePlayers） |
| `IsBeSee(unitId)` | 判断指定 Player 是否能看见我 |

### AOIHelper（坐标工具）

| 方法 | 说明 |
|------|------|
| `CreateCellId(x, y)` | 将格子坐标编码为 long ID：`(long)((ulong)x << 32) \| (uint)y` |
| `CalcEnterAndLeaveCell(aoiEntity, cellX, cellY, enterCell, leaveCell)` | 计算当前位置的 Enter/Leave 格子集合 |

### CellSystem（Cell 操作）

| 方法 | 说明 |
|------|------|
| `Add(aoiEntity)` | 将 AOIEntity 加入 AOIUnits |
| `Remove(aoiEntity)` | 从 AOIUnits 移除 |
| `CellIdToString(cellId)` | 扩展方法：将 long cellId 解码为 "x:y" 格式 |
| `CellIdToString(cellIds)` | 扩展方法：将 HashSet<long> 转为逗号分隔字符串（调试用） |

---

## 核心算法详解

### 坐标编码
```csharp
// 编码：将 2D 格子坐标 (x, y) 压入 64 位 long
long cellId = (long)((ulong)x << 32) | (uint)y;

// 解码（CellSystem.CellIdToString）
int y = (int)(cellId & 0xffffffff);
int x = (int)((ulong)cellId >> 32);
```

### 游戏坐标 → 格子坐标
```csharp
// x 是游戏世界坐标（float）
int cellX = (int)(x * 1000) / AOIManagerComponent.CellSize;
// 等价于：cellX = (int)(x * 1000) / 10000 = (int)(x / 10)
// 即每 10 个游戏单位为一格
```

### 九宫格范围计算（CalcEnterAndLeaveCell）
```
r = ceil(ViewDistance / CellSize)      // Enter 范围半径（格数）
leaveR = r（普通 Unit） 或 r+1（Player）   // Leave 范围半径

Enter 集合：中心格周围 [-r, r] × [-r, r] 的所有格子
Leave 集合：中心格周围 [-leaveR, leaveR] × [-leaveR, leaveR] 的所有格子

Leave ⊃ Enter（Leave 多一圈"过渡环"）
过渡环的作用：格子边界移动时不会立即丢失视野，减少抖动
```

**Player 额外扩大 Leave 范围**的原因：
- Player 在格子边界来回移动时，Leave 范围更大意味着"离开通知"会更晚触发
- 避免频繁触发进入/离开事件造成网络风暴

---

## 关键流程

### Unit 进入场景（Add）

```
AOIManagerComponent.Add(aoiEntity, x, y)
  │
  ├── 1. 游戏坐标 → (cellX, cellY)
  ├── 2. CalcEnterAndLeaveCell → SubEnterCells, SubLeaveCells（全量初始化）
  ├── 3. 遍历 SubEnterCells → aoiEntity.SubEnter(cell)
  │       ├── cell.SubsEnterEntities.Add(self)
  │       └── 对 cell.AOIUnits 中已有 Unit（排除自己）调用 self.EnterSight(unit)
  ├── 4. 遍历 SubLeaveCells → aoiEntity.SubLeave(cell)
  │       └── cell.SubsLeaveEntities.Add(self)
  ├── 5. selfCell = GetCell(当前格子)，aoiEntity.Cell = selfCell
  ├── 6. selfCell.Add(aoiEntity)  → 加入 selfCell.AOIUnits
  └── 7. 通知 selfCell.SubsEnterEntities 中的订阅者 → e.EnterSight(aoiEntity)
         （让视野覆盖此格的人看到新进入的 Unit）
```

### Unit 移动（Move）

```
AOIManagerComponentSystem.Move(aoiEntity, cellX, cellY)
  │
  ├── [优化] 如果格子未变化 → return
  │
  ├── 1. 获取 newCell，调用 Move(aoiEntity, newCell, preCell)
  │       ├── aoiEntity.Cell = newCell
  │       ├── preCell.Remove(aoiEntity)
  │       ├── newCell.Add(aoiEntity)
  │       ├── 通知 newCell.SubsEnterEntities → EnterSight（仅旧格不在其 SubEnterCells 中的订阅者）
  │       └── 通知 preCell.SubsLeaveEntities → LeaveSight（仅新格不在其 SubLeaveCells 中的订阅者）
  │
  ├── 2. 重新计算 enterHashSet / leaveHashSet（新位置的格子集合）
  │
  ├── 3. 处理新增的 Leave 格子（leaveHashSet \ SubLeaveCells）
  │       └── SubLeave(cell)  →  加入 cell.SubsLeaveEntities
  │
  ├── 4. 处理需要取消的 Leave 格子（SubLeaveCells \ leaveHashSet）
  │       └── UnSubLeave(cell)  →  LeaveSight 该格所有 Unit + 取消订阅
  │
  ├── 5. Swap(SubLeaveCells, leaveHashSet)  →  零 GC 交换引用
  │
  ├── 6. 处理新增的 Enter 格子（enterHashSet \ SubEnterCells）
  │       └── SubEnter(cell)  →  对已有 Unit 调用 EnterSight + 加入订阅
  │
  ├── 7. 处理需要取消的 Enter 格子（SubEnterCells \ enterHashSet）
  │       └── UnSubEnter(cell)  →  取消订阅（不触发 LeaveSight，由步骤4处理）
  │
  └── 8. Swap(SubEnterCells, enterHashSet)  →  零 GC 交换引用
```

### Unit 离开场景（Remove）

```
AOIManagerComponent.Remove(aoiEntity)
  │
  ├── [Guard] aoiEntity.Cell == null → return
  │
  ├── 1. aoiEntity.Cell.Remove(aoiEntity)  →  从 Cell.AOIUnits 移除
  ├── 2. 遍历 Cell.SubsLeaveEntities → e.LeaveSight(aoiEntity)
  │       （视野覆盖此格的人看到 Unit 离开）
  ├── 3. 遍历 SubEnterCells → aoiEntity.UnSubEnter(cell)
  │       └── cell.SubsEnterEntities.Remove(aoiEntity.Id)
  ├── 4. 遍历 SubLeaveCells → aoiEntity.UnSubLeave(cell)
  │       ├── self.LeaveSight(unit) for all units in cell（清理 self.SeeUnits）
  │       └── cell.SubsLeaveEntities.Remove(aoiEntity.Id)
  └── 5. 断言检查：
          SeeUnits.Count > 1 → Log.Error（应为 0，>1 说明清理不完整）
          BeSeeUnits.Count > 1 → Log.Error（同上）
```

> **注意**：Remove 检查 `Count > 1` 而非 `Count > 0`，允许 1 个剩余，这可能是针对某种边界情况（如自我引用）的容忍，实际上正常移除后应为 0。

### EnterSight / LeaveSight 双向维护

```
EnterSight(self, enter):
  ├── 重复保护：SeeUnits.ContainsKey(enter.Id) → return
  ├── 可视性检查：AOISeeCheckHelper.IsCanSee(self, enter)（当前恒 true）
  ├── 分四种情况维护双向集合：
  │   ┌─────────────┬─────────────────────────────────────────────────────┐
  │   │ self\enter  │ Player          │ 非 Player                         │
  │   ├─────────────┼─────────────────────────────────────────────────────┤
  │   │ Player      │ SeeUnits+SeePlayers  BeSeeUnits+BeSeePlayers (双方)  │
  │   │ 非 Player   │ SeeUnits+SeePlayers(自)  BeSeeUnits+BeSeePlayers(对) │
  │   └─────────────┴─────────────────────────────────────────────────────┘
  └── Publish UnitEnterSightRange { A=self, B=enter }

LeaveSight(self, leave):
  ├── 自我检查：self.Id == leave.Id → return
  ├── 存在检查：!SeeUnits.ContainsKey(leave.Id) → return
  ├── 从 SeeUnits、SeePlayers（如果 leave 是 Player）移除
  ├── 从 leave.BeSeeUnits、leave.BeSeePlayers（如果 self 是 Player）移除
  └── Publish UnitLeaveSightRange { A=self, B=leave }
```

---

## 代码示例

### 创建 AOIEntity（在 Unit 初始化时）
```csharp
// 在 Unit 的 Awake 系统中，传入视野距离和初始位置
unit.AddComponent<AOIEntity, int, float3>(viewDistance, unit.Position());
```

### 移动时更新 AOI
```csharp
// 在 Move 系统处理位置更新后
AOIManagerComponent aoiManager = unit.Scene().GetComponent<AOIManagerComponent>();
float3 newPos = unit.Position();
int cellX = (int)(newPos.x * 1000) / AOIManagerComponent.CellSize;
int cellY = (int)(newPos.z * 1000) / AOIManagerComponent.CellSize;
aoiManager.Move(unit.GetComponent<AOIEntity>(), cellX, cellY);
```

### 广播消息给视野内 Player
```csharp
// 在 UnitEnterSightRange 事件处理中
[Event(SceneType.Map)]
public class UnitEnterSightRangeHandler : AEvent<Scene, UnitEnterSightRange>
{
    protected override async ETTask Run(Scene scene, UnitEnterSightRange args)
    {
        AOIEntity observer = args.A;  // 谁看见了对方
        AOIEntity target   = args.B;  // 谁进入了视野

        // 向 observer 所在的 Player 发送 target 的状态
        if (observer.Unit.Type() == UnitType.Player)
        {
            // 发送网络消息...
        }
    }
}
```

### 判断某 Unit 是否被 Player 看见
```csharp
AOIEntity aoiEntity = unit.GetComponent<AOIEntity>();
bool isVisible = aoiEntity.IsBeSee(playerId);

// 或获取所有能看见该 Unit 的 Player
Dictionary<long, EntityRef<AOIEntity>> bSeers = aoiEntity.GetBeSeePlayers();
foreach (var kv in bSeers)
{
    AOIEntity playerAoi = kv.Value;
    // 向每个 playerAoi.Unit 发消息
}
```

### Cell ID 调试
```csharp
// 将 cellId 转为可读字符串
string cellStr = aoiEntity.Cell.Id.CellIdToString();  // "5:3"

// 打印所有订阅的 Enter 格子
Log.Debug(aoiEntity.SubEnterCells.CellIdToString());  // "4:2,4:3,5:2,..."
```

---

## 事件系统集成

| 事件 | 触发时机 | 典型消费方 |
|------|---------|-----------|
| `UnitEnterSightRange` | B 进入 A 的视野时 | cn.etetet.unit（发送 UnitInfo），cn.etetet.move（发送初始位置） |
| `UnitLeaveSightRange` | B 离开 A 的视野时 | cn.etetet.unit（发送 UnitRemove 消息） |

事件结构体中 `A` 是观察者，`B` 是被观察的对象（进入/离开 A 的视野）。

---

## Player 特殊处理

| 特殊处理 | 原因 |
|---------|------|
| `BeSeePlayers` 单独维护 | 广播时直接遍历 Player 集合，O(n) 无需从 BeSeeUnits 过滤 |
| `Leave 范围额外扩大一圈（leaveR = r+1）` | 防止边界来回移动产生频繁进入/离开事件 |
| 四分支 EnterSight | Player-Player、Player-怪、怪-Player、怪-怪 各自维护对应集合 |

---

## 可扩展点

### AOISeeCheckHelper（partial class）
```csharp
// 当前实现恒 true，可通过 partial 扩展实现：
// - 障碍物/遮挡检测
// - 隐身技能（如角色处于隐身状态则 IsCanSee 返回 false）
// - 阵营/敌友区分（友军不互相通知）
// - 距离精确检测（当前仅格子级别粗判断）
public static partial class AOISeeCheckHelper
{
    public static bool IsCanSee(AOIEntity a, AOIEntity b) => true;
}
```

---

## 依赖关系

```
cn.etetet.aoi (Server Only)
  │
  ├── 运行时依赖
  │   ├── cn.etetet.core → Entity, Scene, Unit, EventSystem, UnitType, Log
  │   └── Unity.Mathematics → float3
  │
  └── 被以下 package 消费
      ├── cn.etetet.move      → 调用 AOIManagerComponent.Move/Add/Remove
      ├── cn.etetet.unit      → 订阅 UnitEnterSightRange/UnitLeaveSightRange 做网络同步
      ├── cn.etetet.statesync → 状态同步广播（通过 BeSeePlayers 遍历）
      └── cn.etetet.ui（客户端场景同步间接依赖）
```

---

## 性能设计

| 优化手段 | 实现位置 | 说明 |
|---------|---------|------|
| 懒创建 Cell | `GetCell` | Cell 按需生成，空旷地图节省内存 |
| HashSet Swap | `Move` | `ObjectHelper.Swap` 交换引用，Move 不 new HashSet |
| 格内移动短路 | `Move` 第一行 | `if (Cell.Id == newCellId) return` |
| BeSeePlayers 分离 | `AOIEntity` | 广播直接遍历，无需过滤 BeSeeUnits |
| enterHashSet/leaveHashSet 复用 | `AOIEntity` | 临时计算集合预分配，不随 Move 重新分配 |
| 重复进入保护 | `EnterSight` | `SeeUnits.ContainsKey` 防止重复处理 |
| 跨格移动优化 | `Move(aoiEntity, newCell, preCell)` | 仅通知不重叠的格子订阅者，避免冗余事件 |
