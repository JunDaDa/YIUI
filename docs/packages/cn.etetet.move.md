# cn.etetet.move

## 概述

MMO 类型的移动组件，版本 3.0.0。为 `Unit` 实体提供路径移动、旋转插值、速度变更和瞬移等功能。基于帧定时器驱动，使用 ET 异步任务（ETTask）实现协程式移动控制流。

---

## 目录结构

```
cn.etetet.move/
├── package.json
├── Scripts/
│   ├── Model/Share/
│   │   ├── MoveComponent.cs         # 移动数据组件
│   │   ├── MoveEventType.cs         # 移动事件结构体
│   │   ├── PackageType.cs           # 包标识常量
│   │   └── TimerInvokeType.cs       # 定时器类型常量
│   └── Hotfix/Share/
│       └── MoveComponentSystem.cs   # 移动逻辑系统（热更层）
```

---

## 核心类与接口

### MoveComponent（Model/Share）

`[ComponentOf(typeof(Unit))]` — 挂载在 `Unit` 实体上的移动数据组件，实现 `IAwake`、`IDestroy`。

| 字段/属性 | 类型 | 说明 |
|---|---|---|
| `Targets` | `List<float3>` | 路径点列表，**第 0 个元素是路径起始位置（即调用时 Unit 的当前坐标）**，后续为目标点 |
| `N` | `int` | 当前正在走向的目标点索引（由 0 开始，每到新段 +1） |
| `PreTarget` | `float3` (get) | `Targets[N-1]`，当前段出发点 |
| `NextTarget` | `float3` (get) | `Targets[N]`，当前段目标点 |
| `FinalTarget` | `float3` (get) | `Targets[Count-1]`，最终目标点 |
| `RealPos` | `float3` (get) | `Targets[0]`，服务器同步的真实起始位置 |
| `BeginTime` | `long` | 整条路径移动协程开始的时间戳（ms） |
| `StartTime` | `long` | 当前路径段开始时间戳，每段更新 |
| `StartPos` | `float3` | 当前插值段起始位置（客户端真实位置，区别于服务器位置） |
| `NeedTime` | `long` | 当前段需要的毫秒数（= 距离/速度 × 1000） |
| `Speed` | `float` | 移动速度（m/s） |
| `MoveTimer` | `long` | 帧定时器 ID |
| `tcs` | `ETTask<bool>` | 异步任务句柄，移动结束时 SetResult |
| `TurnTime` | `int` | 转向插值时间（ms），0=立即转向，>0=平滑插值，默认 100ms |
| `IsTurnHorizontal` | `bool` | 是否只在水平面（XZ 平面）转向，`MoveToAsync` 中固定为 true |
| `From` / `To` | `quaternion` | 旋转插值起始/目标四元数 |

**重要约束**：传入 `MoveToAsync` 的路径列表，**第一个元素必须是 Unit 当前位置**（作为路径起点供 PreTarget 使用），后续元素才是实际的目标路径点。最少需要 2 个点。

---

### MoveComponentSystem（Hotfix/Share）

`[EntitySystemOf(typeof(MoveComponent))]` — 移动逻辑的静态偏分类，包含定时器、实体系统方法和公开 API。

#### 定时器

| 类 | 触发类型 | 说明 |
|---|---|---|
| `MoveTimer` | `TimerInvokeType.MoveTimer = 4001` | 每帧触发，调用 `MoveForward(true)` 推进位置 |

#### 实体系统方法

| 方法 | 说明 |
|---|---|
| `Awake` | 重置所有字段为默认值 |
| `Destroy` | 调用 `MoveFinish(false)` 取消移动，释放定时器，tcs.SetResult(false) |

#### 公开 API

| 方法签名 | 说明 |
|---|---|
| `IsArrived()` | 是否已到达（Targets 为空则返回 true） |
| `MoveToAsync(List<float3> target, float speed, int turnTime=100)` | 异步移动到路径，返回 `ETTask<bool>`（true=正常到达，false=被中断/取消） |
| `ChangeSpeed(float speed)` | 在移动中途改变速度；若 speed < 0.0001 或已到达则返回 false |
| `FlashTo(float3 target)` | 瞬移，直接设置 unit.Position，不经过插值和定时器 |
| `Stop(bool ret)` | 停止移动，先 MoveForward(ret) 更新位置到当前帧，再 MoveFinish(ret) 清理 |

#### 私有方法

| 方法 | 说明 |
|---|---|
| `StartMove()` | 初始化 BeginTime/StartTime，调用 SetNextTarget 计算第一段，注册帧定时器 |
| `SetNextTarget()` | N++，计算当前段 NeedTime（distance/speed×1000）、更新 StartPos，处理旋转 From/To |
| `MoveForward(bool ret)` | 核心推进循环：用经过时间做 lerp 位置和 slerp 旋转插值，自动切换段或结束 |
| `MoveFinish(bool ret)` | 清理全部状态（Targets.Clear、Speed=0 等），移除定时器，SetResult(ret) 到 tcs |
| `GetFaceV()` | 返回 `NextTarget - PreTarget`，当前段面朝方向向量 |

---

### MoveEventType（Model/Share）

```csharp
public struct MoveStart { public Unit Unit; }
public struct MoveStop  { public Unit Unit; }
```

- `MoveStart`：`MoveToAsync` 开始时通过 `EventSystem.Instance.Publish(scene, new MoveStart{Unit=...})` 广播
- `MoveStop`：`MoveToAsync` **正常完成**（ret==true）时才发布；被 Stop/Cancel 时不发布

---

### PackageType / TimerInvokeType（Model/Share）

```csharp
PackageType.Move = 4
TimerInvokeType.MoveTimer = 4 * 1000 + 1 = 4001
```

`partial` 声明，与其他 package 的类型 ID 合并编译，避免冲突。

---

## 实现原理

### Targets 列表结构

```
Targets = [pos0, pos1, pos2, ..., posN]
           ↑                          ↑
         起始位置（调用方传入）      最终目标
           (RealPos)                (FinalTarget)

N=1 时：PreTarget=Targets[0], NextTarget=Targets[1]
```

N 初始为 0，`SetNextTarget` 先 `++N`，所以首次调用后 N=1，对应第一个移动段 Targets[0]→Targets[1]。

### 路径移动核心流程

每帧 `MoveForward(true)` 执行 while 循环处理多段路径：

1. `moveTime = ClientNow() - StartTime`（当前段已经过的时间）
2. 若 `moveTime >= NeedTime`：Unit 直接到达 `NextTarget`，设置旋转为 `To`
3. 否则：`amount = moveTime / NeedTime`，`math.lerp(StartPos, NextTarget, amount)` 计算位置
4. 旋转插值（TurnTime > 0）：`amount = moveTime / TurnTime`（clamped to 1），`math.slerp(From, To, amount)`
5. `moveTime -= NeedTime`；若 `moveTime < 0` 则当前段未完成，return 等待下帧
6. 否则当前段完成，若是最后一段则 `MoveFinish(ret)`，否则 `SetNextTarget()` 继续循环

### 时间精度设计

- `StartTime` 每段累加 `NeedTime`（不直接赋值 now），消除帧间隔的浮点误差
- 位置插值从客户端实际坐标（`StartPos = unit.Position`）出发，确保客户端平滑表现
- 路径点以服务器下发为权威（`Targets`），保证最终位置精确

### 速度变更实现

```csharp
// ChangeSpeed 内部流程：
MoveForward(false)           // 先把 unit 推进到当前帧位置（不完成协程）
path[0] = unit.Position      // 以当前帧位置为新起点
path[1..N] = Targets[N..]    // 追加剩余路径点
MoveToAsync(path, newSpeed)  // 用新速度重新开始移动协程
```

调用 `Stop(false)` 不会发布 `MoveStop` 事件（ret=false），因此外层监听者不会误认为移动完成。

### 旋转模式

| TurnTime | 行为 |
|---|---|
| `> 0` | slerp 平滑插值，时间长度独立于移动时间 |
| `= 0` | 立即转向，每段开始时直接设置 unit.Rotation |
| `< 0` | 完全不旋转（SetNextTarget 提前 return） |

`IsTurnHorizontal = true` 时，旋转向量 faceV.y 强制为 0，只在 XZ 平面旋转，适合地面单位。

---

## 关键流程图

```
MoveToAsync(path, speed, turnTime=100)
  ├─ Stop(false)            // 取消旧移动（若有），不触发 MoveStop
  ├─ Targets.AddRange(path) // 设置路径
  ├─ EventSystem.Publish(MoveStart{Unit})
  ├─ StartMove()
  │    ├─ BeginTime = StartTime = ClientNow()
  │    ├─ SetNextTarget()   // N=1, 计算 NeedTime, From, To
  │    └─ NewFrameTimer(MoveTimer, self)
  └─ await tcs              // 挂起协程

每帧: MoveTimer.Run → MoveForward(true)
  ├─ moveTime = now - StartTime
  ├─ lerp 位置 / slerp 旋转
  ├─ 段完成 → SetNextTarget() → 继续循环
  └─ 全路径完成 → MoveFinish(true)
       ├─ Targets.Clear(), Speed=0, N=0 ...
       ├─ TimerComponent.Remove(MoveTimer)
       ├─ tcs.SetResult(true)
       └─ EventSystem.Publish(MoveStop{Unit})  ← 只有正常完成才发布
```

---

## 代码示例

### 基本使用

```csharp
// 让 unit 沿路径移动
var moveComp = unit.GetComponent<MoveComponent>();
var path = new List<float3>
{
    unit.Position,          // 第一个必须是当前位置（作为出发点）
    new float3(10, 0, 0),
    new float3(10, 0, 10),
    new float3(0,  0, 10),
};
bool arrived = await moveComp.MoveToAsync(path, speed: 5f, turnTime: 100);
if (arrived)
{
    // 正常到达
}
```

### 停止移动

```csharp
// 立即停止，更新 unit 到当前帧位置
unit.GetComponent<MoveComponent>().Stop(false);
// 注意：Stop(false) 不触发 MoveStop 事件；Stop(true) 会触发
```

### 瞬移

```csharp
unit.GetComponent<MoveComponent>().FlashTo(new float3(100, 0, 100));
// 直接设置位置，不影响当前移动协程状态
```

### 监听移动事件

```csharp
// 在某个 IEventSystem 实现中
[Event(SceneType.Current)]
public class MoveStartHandler : AEvent<MoveStart>
{
    protected override async ETTask Run(Scene scene, MoveStart args)
    {
        // 播放移动动画
        args.Unit.GetComponent<AnimationComponent>()?.PlayMove();
        await ETTask.CompletedTask;
    }
}
```

---

## 依赖关系

| 依赖 | 用途 |
|---|---|
| `cn.etetet.core` | Entity、IAwake、IDestroy、ETTask、EventSystem、TimerComponent、TimeInfo、Log |
| `cn.etetet.unit` | Unit 类型（ComponentOf 宿主，提供 Position/Rotation 属性）|
| `Unity.Mathematics` | float3、quaternion、math.lerp、math.slerp、math.length 等 |

**被依赖方（下游）**：
- 移动同步模块：调用 `MoveToAsync`/`Stop` 驱动位置同步
- 动画系统：监听 `MoveStart`/`MoveStop` 事件切换动画状态
- AOI 系统：监听移动事件触发视野更新
- AI/寻路系统：调用 `MoveToAsync` 执行寻路结果

---

## 边界情况与注意事项

1. **多次调用 MoveToAsync**：内部先调 `Stop(false)`，旧 tcs 返回 false，新移动立即开始，不会泄漏
2. **Destroy 时自动清理**：`MoveFinish(false)` 保证定时器和 tcs 均被释放
3. **速度为 0 段**：若某路径段距离为 0 则 `NeedTime=0`，`MoveForward` 中 while 条件 `moveTime >= 0` 会立即通过到下一段
4. **ChangeSpeed 的 speed 校验**：speed < 0.0001 直接返回 false，防止 NeedTime 溢出
5. **FlashTo 与移动协程共存**：FlashTo 只改 Position，不影响 Targets 和定时器，可能导致位置跳变后继续按旧路径插值

---

## 性能考量

- 使用 `NewFrameTimer` 而非 Update 注册，统一由 TimerComponent 批量驱动，减少 MonoBehaviour 开销
- `ListComponent<float3>.Create()` 在 `ChangeSpeed` 中使用对象池，避免频繁 GC
- 路径处理为纯数学运算（Unity.Mathematics SIMD），无额外内存分配
