# cn.etetet.statesync

## 概述

**版本**: 3.0.12
**描述**: StateSync demo（状态同步演示）
**作者**: tanghai
**Unity**: 2022.3+

`cn.etetet.statesync` 是整个 ET 框架的**核心演示包**，也是最重要的集成包。它实现了一个完整的 MMO 状态同步游戏流程 Demo，涵盖：客户端登录 → 进入地图 → 场景切换 → 单位创建/移动/同步 → 跨地图传送。

该包依赖了几乎所有其他 package，是项目的顶层组装层。

---

## 目录结构

```
cn.etetet.statesync/
├── Scripts/
│   ├── Model/
│   │   ├── Share/          # 客户端+服务端共享的数据定义
│   │   │   ├── EventType.cs            # 客户端事件类型定义
│   │   │   ├── SceneType.cs            # 场景类型常量
│   │   │   ├── PackageType.cs          # 包类型ID
│   │   │   ├── CurrentScenesComponent.cs
│   │   │   └── SerializerTest.cs
│   │   ├── Client/Main/    # 客户端模型（AI巡逻路径、等待信号等）
│   │   │   ├── AI/XunLuoPathComponent.cs
│   │   │   ├── Move/Wait_UnitStop.cs
│   │   │   └── Scene/Wait_CreateMyUnit.cs, Wait_SceneChangeFinish.cs
│   │   └── Server/
│   │       └── Robot/      # 机器人测试相关模型
│   ├── Hotfix/
│   │   ├── Share/          # 共享逻辑
│   │   ├── Client/Main/    # 客户端逻辑（登录、进图、移动、AI）
│   │   │   ├── AI/         # 客户端 AI（AI_XunLuo 巡逻、AI_Attack 攻击）
│   │   │   ├── Move/       # 移动消息处理
│   │   │   ├── Scene/      # 场景切换
│   │   │   └── Unit/       # Unit 创建/移除处理
│   │   └── Server/         # 服务端逻辑
│   │       ├── Gate/       # Gate 服务（登录入图）
│   │       ├── Map/        # Map 服务（单位管理、AOI、移动）
│   │       │   ├── AOI/    # 视野事件处理
│   │       │   ├── Move/   # 服务端移动处理
│   │       │   ├── Transfer/ # 跨服场景传送
│   │       │   └── Unit/   # 单位进出视野通知
│   │       └── Robot/      # 机器人压测
│   ├── HotfixView/Client/  # 客户端表现层（UI、渲染）
│   │   ├── Opera/          # 键鼠输入处理（OperaComponentSystem）
│   │   ├── UI/             # UILogin、UILobby、UIHelp
│   │   └── Unit/           # 单位 GameObject 绑定与动画
│   ├── ModelView/Client/   # 客户端表现层数据模型
│   └── Editor/             # 编辑器辅助
├── Proto/
│   ├── StateSyncOuter_C_11001.proto    # 客户端-服务端协议
│   └── StateSyncInner_S_21001.proto    # 服务器内部协议
├── Excel/
│   ├── StartConfig/        # 起服配置（Localhost/Release 两套）
│   └── UnitConfig.xlsx     # 单位配置表
├── Config/Recast/          # 寻路网格数据（Map1、Map2）
└── DotNet~/                # 独立 .NET 服务器项目（可脱离 Unity 运行）
```

---

## 场景类型定义

```csharp
// SceneType.cs
public static partial class SceneType
{
    public const int Http      = PackageType.StateSync * 1000 + 1;   // HTTP 服务
    public const int Map       = PackageType.StateSync * 1000 + 2;   // 地图服务器
    public const int Robot     = PackageType.StateSync * 1000 + 3;   // 机器人
    public const int StateSync = PackageType.StateSync * 1000 + 20;  // 客户端主逻辑
    public const int Current   = PackageType.StateSync * 1000 + 21;  // 当前游戏场景
    public const int StateSyncView = PackageType.StateSync * 1000 + 24; // 表现层
}
```

---

## 客户端事件定义（EventType.cs）

| 事件 | 触发时机 |
|------|---------|
| `SceneChangeStart` | 开始切换到新场景（可在此创建 Loading UI） |
| `SceneChangeFinish` | 场景切换完成（CurrentScene 已就绪，创建 UIHelp） |
| `AfterCreateClientScene` | 客户端主 Scene 创建后 |
| `AfterCreateCurrentScene` | 当前游戏场景创建后 |
| `AppStartInitFinish` | 客户端应用初始化完成（创建 UILogin） |
| `LoginFinish` | 登录成功（创建 UILobby，关闭 UILogin） |
| `EnterMapFinish` | 进入地图成功 |
| `AfterUnitCreate` | Unit 创建后（携带 Unit 引用，触发视图创建） |

---

## 核心组件

### CurrentScenesComponent

管理客户端当前游戏场景的组件，挂载在客户端 Root Scene 上。

```csharp
[ComponentOf(typeof(Scene))]
public class CurrentScenesComponent : Entity, IAwake
{
    public Scene Scene { get; set; }  // EntityRef 弱引用，当前游戏场景
}

// 扩展方法
public static Scene CurrentScene(this Scene root)
{
    return root.GetComponent<CurrentScenesComponent>()?.Scene;
}
```

支持大世界多块场景加载（预留扩展字段）。

### XunLuoPathComponent（客户端巡逻路径）

```csharp
// Scripts/Model/Client/Main/AI/XunLuoPathComponent.cs
// 存储巡逻路径点列表，循环遍历
// GetCurrent() → 当前目标点
// MoveNext()   → 推进到下一个路径点（到达末尾则循环）
```

### OperaComponent（玩家输入）

```csharp
// Scripts/ModelView/Client/Opera/OperaComponent.cs
// 挂载在客户端 Root Scene 上，负责鼠标/键盘输入
public int mapMask;  // Unity Layer Mask for "Map"
```

### 动画相关组件

```csharp
// AnimatorComponent — 封装 Unity Animator
public Animator Animator;
public MotionType MotionType;    // 待播放动作类型
public float MontionSpeed;       // 播放速度
public bool isStop;              // 是否暂停
Dictionary<string, AnimationClip> animationClips;
HashSet<string> Parameter;

// GameObjectComponent — 绑定 Unit 与 Unity GameObject
public GameObject GameObject;
```

---

## 服务端架构

### Gate 层

**C2G_EnterMapHandler**（Gate 服务器）
- 接收客户端的 `C2G_EnterMap` 请求
- 在 Gate 上动态创建临时 `GateMapComponent` 及 GateMap Scene
- 调用 `UnitFactory.Create(scene, player.Id, UnitType.Player)` 创建玩家 Unit
  - 初始位置 `(-10, 0, -10)`
  - 速度属性 `ENumericType.Speed1 = 6f`（6 米/秒）
  - AOI 视野 `ENumericType.AOI1 = 15f`（15 米）
- 查找目标 Map 场景的 `StartSceneConfig`（Map1）
- 调用 `TransferHelper.TransferAtFrameFinish()` 延帧传送（确保 G2C_EnterMap 先返回）

```
Client → C2G_EnterMap → Gate
  Gate → 创建 GateMap Scene
       → UnitFactory.Create(player, UnitType.Player)
       → StartSceneConfig.GetBySceneName("Map1")
       → G2C_EnterMap 先返回（携带 MyId）
       → 等下一帧
       → TransferHelper.Transfer(unit, Map1.ActorId, "Map1")
```

### Map 层初始化（FiberInit_Map）

Map Fiber 启动时注册的核心组件：

| 组件 | 说明 |
|------|------|
| `MailBoxComponent(UnOrderedMessage)` | 无序消息邮箱 |
| `TimerComponent` | 定时器 |
| `CoroutineLockComponent` | 协程锁 |
| `ProcessInnerSender` | 进程内消息发送 |
| `MessageSender` | Actor 消息发送 |
| `UnitComponent` | Unit 管理容器 |
| `AOIManagerComponent` | AOI 视野管理 |
| `LocationProxyComponent` | Location 服务代理 |
| `MessageLocationSenderComponent` | 通过 Location 服务发送消息 |

### 场景传送（TransferHelper）

```csharp
// 延帧传送（等当前帧结束后执行）
public static async ETTask TransferAtFrameFinish(Unit unit, ActorId sceneInstanceId, string sceneName)
{
    await unit.Fiber().WaitFrameFinish();
    await TransferHelper.Transfer(unit, sceneInstanceId, sceneName);
}

// 实际传送逻辑
public static async ETTask Transfer(Unit unit, ActorId sceneInstanceId, string sceneName)
{
    // 1. 构造 M2M_UnitTransferRequest
    //    - 序列化 Unit (BSON)
    //    - 序列化所有 ITransfer 接口的组件
    // 2. unit.Dispose() - 销毁本地 Unit
    // 3. LocationProxy.Lock(LocationType.Unit, unitId)  // 阻塞发给 Unit 的消息
    // 4. MessageSender.Call(targetScene, request)       // 发送到目标 Map
}
```

**M2M_UnitTransferRequestHandler**（目标 Map 接收）：

```csharp
// 1. MongoHelper.Deserialize<Unit>(request.Unit)  - BSON 反序列化还原 Unit
// 2. 还原所有 ITransfer 组件（MongoHelper.Deserialize<Entity>）
// 3. 添加 MoveComponent + PathfindingComponent（使用目标场景名加载寻路网格）
// 4. 设置位置 (-10, 0, -10)
// 5. 添加 MailBoxComponent(OrderedMessage) — 有序消息邮箱，保证消息顺序
// 6. SendToClient: M2C_StartSceneChange → 客户端开始切场景
// 7. SendToClient: M2C_CreateMyUnit     → 客户端创建自己的 Unit
// 8. unit.AddComponent<AOIEntity>(9000, position)  → 加入 AOI
// 9. LocationProxy.UnLock(unit)          → 解锁，开始接收消息
```

### AOI 与视野同步

**ChangePosition_NotifyAOI**（位置变化 → 更新 AOI Cell）：

```
Unit 位置改变 (ChangePosition 事件)
→ 计算旧/新 CellX = (int)(pos.x * 1000) / CellSize
→ 计算旧/新 CellY = (int)(pos.z * 1000) / CellSize
→ 如果跨 Cell → AOIManagerComponent.Move(aoiEntity, newX, newY)
（乘以 1000 是因为位置精度用毫米表示，CellSize 也是毫米单位）
```

**UnitEnterSightRange_NotifyClient**（进入视野）：

```
AOI 进入视野事件(UnitEnterSightRange{A, B})
→ A == B 则跳过（自己不通知自己）
→ 若 A 是 Player → MapMessageHelper.NoticeUnitAdd(A.Unit, B.Unit)
   → M2C_CreateUnits(UnitInfo) 发给 A 玩家
```

**UnitLeaveSightRange_NotifyClient**（离开视野）：

```
AOI 离开视野事件(UnitLeaveSightRange{A, B})
→ A == B 则跳过
→ 若 A 是 Player → MapMessageHelper.NoticeUnitRemove(A.Unit, B.Unit)
   → M2C_RemoveUnits([B.Id]) 发给 A 玩家
```

### 消息广播（MapMessageHelper）

```csharp
public static partial class MapMessageHelper
{
    // 通知某玩家有新 Unit 进入视野
    public static void NoticeUnitAdd(Unit unit, Unit sendUnit)
    // 通知某玩家有 Unit 离开视野
    public static void NoticeUnitRemove(Unit unit, Unit sendUnit)
    // 广播给所有能看到该 Unit 的玩家（通过 AOIEntity.GetBeSeePlayers()）
    // 注意：(message as MessageObject).IsFromPool = false → 防止消息被提前回收
    public static void Broadcast(Unit unit, IMessage message)
    // 发送给单个玩家（通过 MessageLocationSenderComponent → GateSession）
    public static void SendToClient(Unit unit, IMessage message)
    // 发送 Actor 消息
    public static void Send(Scene root, ActorId actorId, IMessage message)
}
```

### UnitHelper（服务端）

```csharp
// 将 Unit 状态序列化为 UnitInfo（用于发给客户端或传送）
public static UnitInfo CreateUnitInfo(Unit unit)
{
    // 包含：UnitId, ConfigId, Type, Position, Forward
    // 包含所有 NumericDataComponent 的 KV 属性
    // 如果单位正在移动，包含剩余路径点列表（MoveInfo）
}

// 获取能看见该 Unit 的所有玩家（AOIEntity 方法的扩展）
public static Dictionary<long, EntityRef<AOIEntity>> GetBeSeePlayers(this Unit self)
```

---

## 客户端架构

### 进入地图流程（EnterMapHelper）

```csharp
public static async ETTask EnterMapAsync(Scene root)
{
    // 1. Call C2G_EnterMap → Gate（获取 MyId）
    G2C_EnterMap g2CEnterMap = await root.GetComponent<ClientSenderComponent>()
                                          .Call(C2G_EnterMap.Create()) as G2C_EnterMap;
    // 2. 等待场景切换完成（SceneChangeHelper 通知）
    await root.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
    // 3. 发布 EnterMapFinish 事件
    EventSystem.Instance.Publish(root, new EnterMapFinish());
}
```

### 场景切换流程（SceneChangeHelper）

```csharp
public static async ETTask SceneChangeTo(Scene root, string sceneName, long sceneInstanceId)
{
    // 1. 销毁旧 CurrentScene（若存在）
    currentScenesComponent.Scene?.Dispose();
    // 2. 创建新 CurrentScene（CurrentSceneFactory.Create）
    Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);
    // 3. 为新场景添加 UnitComponent
    UnitComponent unitComponent = currentScene.AddComponent<UnitComponent>();
    // 4. 发布 SceneChangeStart（此处可创建 Loading UI）
    EventSystem.Instance.Publish(root, new SceneChangeStart());
    // 5. 等待 M2C_CreateMyUnit
    Wait_CreateMyUnit waitCreateMyUnit = await root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();
    // 6. 用 UnitFactory 创建自己的 Unit
    Unit unit = UnitFactory.Create(currentScene, waitCreateMyUnit.Message.Unit);
    unitComponent.Add(unit);
    // 7. 发布 SceneChangeFinish（触发 UIHelp 创建）
    EventSystem.Instance.Publish(currentScene, new SceneChangeFinish());
    // 8. 通知 EnterMapHelper 中的等待
    root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());
}
```

### M2C_StartSceneChangeHandler（触发场景切换协程）

```csharp
[MessageHandler(SceneType.StateSync)]
public class M2C_StartSceneChangeHandler : MessageHandler<Scene, M2C_StartSceneChange>
{
    // 收到消息后，调用 SceneChangeHelper.SceneChangeTo() 启动协程
}
```

### 客户端 UnitFactory

```csharp
public static Unit Create(Scene currentScene, UnitInfo unitInfo)
{
    // 1. UnitComponent.AddChildWithId<Unit, int>(unitInfo.UnitId, unitInfo.ConfigId)
    // 2. 设置 Position 和 Forward
    // 3. 添加 NumericDataComponent，从 KV 初始化属性
    // 4. 添加 MoveComponent
    // 5. 如果 UnitInfo 包含 MoveInfo（正在移动），立即 MoveToAsync
    // 6. 添加 ObjectWait（用于 Wait_UnitStop 等信号等待）
    // 7. 添加 XunLuoPathComponent（巡逻路径）
    // 8. Publish AfterUnitCreate → 触发视图创建（加载 Skeleton 模型）
}
```

### 客户端消息处理

| 消息 | Handler | 作用 |
|------|---------|------|
| `M2C_StartSceneChange` | `M2C_StartSceneChangeHandler` | 启动场景切换协程 |
| `M2C_CreateMyUnit` | `M2C_CreateMyUnitHandler` | 通过 ObjectWait 通知 SceneChangeHelper |
| `M2C_CreateUnits` | `M2C_CreateUnitsHandler` | 批量创建视野内其他 Unit |
| `M2C_RemoveUnits` | `M2C_RemoveUnitsHandler` | 批量删除视野外 Unit |
| `M2C_PathfindingResult` | `M2C_PathfindingResultHandler` | 取速度，调用 MoveComponent.MoveToAsync |
| `M2C_Stop` | `M2C_StopHandler` | 停止 Unit 移动，纠正位置/朝向 |

### 移动同步流程

```
[服务端]
客户端右键点击地面
→ OperaComponentSystem.Update() 检测鼠标右键 Input.GetMouseButtonDown(1)
→ 射线检测 Physics.Raycast(ray, out hit, 1000, mapMask)
→ Send C2M_PathfindingResult(hit.point) → Map Server

[Map Server]
C2M_PathfindingResultHandler
→ unit.FindPathMoveToAsync(message.Position)  // 寻路计算
→ 路径计算后 Broadcast M2C_PathfindingResult(points) 给视野内所有玩家

[客户端]
M2C_PathfindingResultHandler
→ float speed = unit.GetComponent<NumericDataComponent>().GetAsFloat(ENumericType.Speed0)
→ unit.GetComponent<MoveComponent>().MoveToAsync(message.Points, speed)
```

**注意**：`ENumericType.Speed0` 是客户端读取速度属性的键（Speed0 = 最终速度），而服务端创建 Unit 时用 `Speed1`（基础速度）设置。

### 输入处理（OperaComponentSystem）

```csharp
// 鼠标右键 → C2M_PathfindingResult（移动）
// 键盘 R → CodeLoader.Instance.Reload()（热重载代码）
// 键盘 Q → Test1（测试协程锁：6秒持锁）
// 键盘 W → Test2（测试协程锁：1秒持锁）
// 键盘 A → TestCancelAfter（测试1秒超时取消）
// 键盘 T → C2M_TransferMap（请求传送到另一地图）
```

### 表现层（HotfixView）

**AfterUnitCreate_CreateUnitView**（事件作用域：SceneType.Current）：
```csharp
// 监听 AfterUnitCreate 事件
→ scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>("Skeleton")
→ Instantiate 到 GlobalComponent.Unit 节点下
→ go.transform.position = unit.Position   // 初始化位置
→ unit.AddComponent<GameObjectComponent>().GameObject = go
→ unit.AddComponent<AnimatorComponent>()   // 初始化动画控制器
```

**AnimatorComponentSystem**（动画控制器系统）：

| 方法 | 说明 |
|------|------|
| `Play(motionType, speed)` | 下一帧播放指定动作（通过 SetTrigger） |
| `PlayInTime(motionType, time)` | 在指定时长内播放动作（计算速度缩放） |
| `PauseAnimator()` | 暂停动画（speed = 0） |
| `RunAnimator()` | 恢复动画 |
| `SetBoolValue/SetFloatValue/SetIntValue/SetTrigger` | 设置参数 |
| `AnimationTime(motionType)` | 获取动作时长 |

**UI 流程**：
```
AppStartInitFinish → 创建 UILogin
LoginFinish        → 创建 UILobby + 关闭 UILogin
SceneChangeFinish  → 创建 UIHelp（游戏内操作提示）
```

**位置/旋转同步**：
- `ChangePosition_SyncGameObjectPos`：监听 Unit 位置变化事件，同步 `go.transform.position`
- `ChangeRotation_SyncGameObjectRotation`：监听 Unit 旋转变化事件，同步 `go.transform.rotation`

---

## 客户端 AI（Robot AI）

### AI_XunLuo（巡逻 AI）

```csharp
// Check: 每 15 秒循环，10 秒内优先级为 0（不执行），最后 5 秒优先级为 1（执行巡逻）
public override int Check(AIComponent aiComponent, AIConfig aiConfig)
{
    long sec = TimeInfo.Instance.ClientNow() / 1000 % 15;
    return sec < 10 ? 0 : 1;
}

// Execute: 循环巡逻 XunLuoPathComponent 中的路径点
public override async ETTask Execute(...)
{
    // 持续循环：GetCurrent() → MoveToAsync() → MoveNext() → 循环
    // 支持 CancellationToken 取消
}
```

### AI_Attack（攻击 AI）

攻击逻辑 AI，与 AI_XunLuo 配合，用于演示 AI 状态机切换。

---

## 协议定义

### Outer 协议（客户端 ↔ 服务端）

| 消息 | 类型 | 方向 | 说明 |
|------|------|------|------|
| `C2G_EnterMap` / `G2C_EnterMap` | SessionRequest/Response | C→S / S→C | 请求进入地图，返回 MyId |
| `C2M_PathfindingResult` | LocationMessage | C→S | 寻路目标位置 |
| `C2M_Stop` | LocationMessage | C→S | 客户端请求停止移动 |
| `M2C_CreateUnits` | Message | S→C | 批量创建单位（进入视野） |
| `M2C_CreateMyUnit` | Message | S→C | 创建自己的单位 |
| `M2C_StartSceneChange` | Message | S→C | 通知客户端切换场景 |
| `M2C_RemoveUnits` | Message | S→C | 批量移除单位（离开视野） |
| `M2C_PathfindingResult` | Message | S→C | 广播寻路结果（路径点） |
| `M2C_Stop` | Message | S→C | 广播单位停止（含位置校正） |
| `C2M_TransferMap` | LocationRequest | C→S | 请求传送到另一地图 |
| `C2M_TestRobotCase` | LocationRequest | C→S | 机器人测试用例 |
| `C2G_Benchmark` | SessionRequest | C→S | 性能基准测试 |
| `RouterSync` | - | - | 路由器同步（ConnectId + Address） |

**UnitInfo 结构**：
```protobuf
message UnitInfo {
    int64 UnitId       // Unit 唯一 ID
    int32 ConfigId     // 配置表 ID
    int32 Type         // UnitType 枚举
    float3 Position    // 世界坐标
    float3 Forward     // 朝向
    map<int32, int64> KV  // 数值属性（NumericType → 值）
    MoveInfo MoveInfo  // 当前移动路径（移动中才有）
}

message MoveInfo {
    repeated float3 Points  // 剩余路径点
    quaternion Rotation     // 旋转
    int32 TurnSpeed         // 转向速度
}
```

### Inner 协议（服务器内部）

| 消息 | 说明 |
|------|------|
| `M2M_UnitTransferRequest/Response` | 跨进程 Unit 传送（包含序列化 Unit + ITransfer 组件） |

---

## 依赖关系

### 直接依赖的包（共 26 个）

| 包 | 用途 |
|----|------|
| `cn.etetet.core` | 基础框架（Entity、Scene、EventSystem 等） |
| `cn.etetet.actorlocation` | Unit Location 锁、ActorId、跨 Fiber 消息定位 |
| `cn.etetet.aoi` | 视野管理（AOIManagerComponent、AOIEntity） |
| `cn.etetet.move` | 移动组件（MoveComponent、FindPathMoveToAsync） |
| `cn.etetet.recast` | 寻路网格（PathfindingComponent） |
| `cn.etetet.unit` | Unit 实体、UnitType、UnitComponent |
| `cn.etetet.router` | 网络路由（RouterSync、ClientSenderComponent） |
| `cn.etetet.login` | 登录流程（SessionPlayerComponent） |
| `cn.etetet.ui` | UI 框架 |
| `cn.etetet.netinner` | 内网通信（ProcessInnerSender、MessageSender） |
| `cn.etetet.ai` | AI 状态机（AIComponent、AAIHandler） |
| `cn.etetet.http` | HTTP 服务 |
| `cn.etetet.numeric` | 数值系统（NumericDataComponent、ENumericType） |
| `cn.etetet.console` | 控制台命令 |
| `cn.etetet.demores` | 演示资源（Skeleton 预制体） |
| `cn.etetet.loader` | 资源加载（ResourcesLoaderComponent） |
| `cn.etetet.mathematics` | 数学库（float3、quaternion） |
| `cn.etetet.referencecollector` | 引用收集器 |
| `cn.etetet.watcher` | 文件监听 |
| `cn.etetet.excel` | 配置导出 |
| `cn.etetet.proto` | Protobuf 支持 |
| `cn.etetet.startconfig` | 起服配置（StartSceneConfig） |
| `cn.etetet.hybridclr` | 热更新 |
| `cn.etetet.sourcegenerator` | 代码生成 |
| `cn.etetet.yooassets` | 资产管理 |
| `cn.etetet.memorypack` | 序列化 |

### 被依赖的包

无（这是顶层集成包）。

---

## 完整游戏流程

```
[启动]
AppStartInitFinish → UILogin 显示

[登录]
UILogin 点击登录 → LoginFinish 事件
→ UILobby 显示，UILogin 关闭

[进入地图]
UILobby 点击进图 → EnterMapHelper.EnterMapAsync()
→ C2G_EnterMap → Gate Server
→ Gate 创建 GateMapComponent + Scene
→ UnitFactory.Create(player, Player) — 初始位置(-10,0,-10), 速度6, AOI 15米
→ G2C_EnterMap 返回（携带 MyId）
→ 等一帧
→ TransferHelper.Transfer(unit, Map1)

[服务端传送到 Map]
M2M_UnitTransferRequest → Map Server
→ BSON 反序列化还原 Unit + ITransfer 组件
→ AddComponent<MoveComponent> + PathfindingComponent(场景名)
→ AddComponent<MailBoxComponent(OrderedMessage)>
→ SendToClient: M2C_StartSceneChange
→ SendToClient: M2C_CreateMyUnit（含移动状态快照）
→ 加入 AOI
→ LocationProxy.UnLock(unit)

[客户端场景切换]
M2C_StartSceneChange → M2C_StartSceneChangeHandler
→ SceneChangeHelper.SceneChangeTo(sceneName, sceneInstanceId)
   → 销毁旧 CurrentScene
   → 创建新 CurrentScene，添加 UnitComponent
   → SceneChangeStart 事件（可创建 Loading UI）
   → 等待 Wait_CreateMyUnit...

M2C_CreateMyUnit → M2C_CreateMyUnitHandler
→ ObjectWait.Notify(Wait_CreateMyUnit)
→ SceneChangeHelper 继续
→ UnitFactory.Create(currentScene, unitInfo)
   → 创建 Unit 实体，初始化 NumericData、MoveComponent、XunLuoPathComponent
   → AfterUnitCreate 事件
   → AfterUnitCreate_CreateUnitView: 加载 Skeleton 预制体，绑定 GameObjectComponent + AnimatorComponent
→ SceneChangeFinish 事件 → UIHelp 创建
→ ObjectWait.Notify(Wait_SceneChangeFinish)

[EnterMapHelper 解除等待]
→ EnterMapFinish 事件

[游戏中 - 移动]
玩家右键点击地面
→ OperaComponentSystem: 射线检测 → C2M_PathfindingResult(position)
→ Map Server: C2M_PathfindingResultHandler
   → unit.FindPathMoveToAsync(position)  // 服务端寻路
   → Broadcast M2C_PathfindingResult(id, position, points)
→ 客户端: M2C_PathfindingResultHandler
   → 取速度 ENumericType.Speed0
   → MoveComponent.MoveToAsync(points, speed)
   → ChangePosition 事件 → ChangePosition_SyncGameObjectPos

[游戏中 - 视野同步]
Unit 移动跨 AOI Cell
→ ChangePosition_NotifyAOI → AOIManagerComponent.Move()
→ UnitEnterSightRange → MapMessageHelper.NoticeUnitAdd() → M2C_CreateUnits
→ UnitLeaveSightRange → MapMessageHelper.NoticeUnitRemove() → M2C_RemoveUnits

[传送到另一地图]
玩家按 T 键
→ OperaComponentSystem: Send C2M_TransferMap
→ C2M_TransferMapHandler: TransferHelper.TransferAtFrameFinish(unit, Map2)
→ 重走传送流程
```

---

## 机器人测试（Robot）

服务端包含 Robot Fiber，用于自动化压力测试：

| 类 | 说明 |
|----|------|
| `RobotManagerComponent` | 管理多个 Robot Fiber ID 列表 |
| `RobotManagerComponentSystem` | `NewRobot(account)` 创建新 Robot Fiber；Destroy 时清理所有 Fiber |
| `FiberInit_Robot` | Robot Fiber 初始化（SceneType.Robot） |
| `RobotCase_SecondCase` | 具体测试用例实现 |
| `CreateRobotConsoleHandler` | 控制台命令 `CreateRobot {account}` 创建机器人 |
| `CreateRobotArgs` | 控制台命令参数 |
| `RobotCaseType` | 测试用例类型枚举 |

```csharp
// 通过控制台触发：CreateRobot player1
// RobotManagerComponent.NewRobot("player1")
// → FiberManager.Create(ThreadPool, Zone, SceneType.Robot, account)
// → FiberInit_Robot 初始化（走完整的登录+进图流程）
```

---

## 架构模式

### 1. 服务端状态权威（Authoritative Server）
客户端发送意图（寻路目标位置），服务端进行寻路计算，将计算结果广播给所有观察者（视野内玩家），保证所有玩家看到相同的状态。客户端仅负责表现层渲染。

### 2. 延帧传送（Frame-Finish Transfer）
进入地图时，先返回 `G2C_EnterMap`（建立客户端连接期待，避免乱序），再在当前帧最后执行实际传送逻辑。

### 3. Location 锁机制
Unit 传送时先加 Location 锁（`LocationProxy.Lock`），确保传送期间发给 Unit 的消息全部排队等待，到达目标地图注册 MailBox 后解锁（`LocationProxy.UnLock`）。

### 4. ObjectWait 流程同步
使用 `ObjectWait` 在异步操作中等待特定网络消息到达，避免复杂的回调嵌套：

```csharp
// EnterMapHelper 等待场景切换完成
await root.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();

// SceneChangeHelper 等待 M2C_CreateMyUnit
Wait_CreateMyUnit w = await root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();

// 等待单位停止
await unit.GetComponent<ObjectWait>().Wait<Wait_UnitStop>();
```

### 5. 事件驱动 UI
通过发布 `EventType` 中定义的事件来驱动 UI 生命周期，解耦游戏逻辑与 UI 逻辑。各 UI 组件监听对应事件后自行创建/销毁。

### 6. BSON 序列化传送
Unit 传送时通过 MongoDB BSON 序列化，支持多态（可以还原具体的 Entity 子类），允许组件实现 `ITransfer` 接口来参与传送序列化。

### 7. AOI Cell 分格算法
将世界坐标乘以 1000（转毫米），再除以 `CellSize`（毫米单位），得到 Cell 索引。当 Unit 跨越 Cell 边界时才触发 AOI 更新计算，减少无效的视野检测开销。
