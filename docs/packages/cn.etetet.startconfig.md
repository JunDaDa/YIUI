# cn.etetet.startconfig

## 概述

**版本**: 3.0.0
**描述**: 服务器起服配置（Server startup configuration）
**作者**: tanghai
**Unity**: 2022.3+

服务器启动配置包，定义了服务器启动时所需的机器配置、进程配置、场景配置和区域配置。这些配置通过 Excel 表格驱动，由代码生成工具（cn.etetet.excel）生成对应的 C# 配置类。

---

## 目录结构

```
cn.etetet.startconfig/
├── Excel/
│   └── StartConfig/
│       └── Example/
│           ├── StartMachineConfig@s.xlsx   # 机器配置表
│           ├── StartProcessConfig@s.xlsx   # 进程配置表
│           ├── StartSceneConfig@s.xlsx     # 场景配置表
│           └── StartZoneConfig@s.xlsx      # 区域配置表
├── Scripts/
│   └── Model/
│       └── Server/
│           ├── StartProcessConfig.cs       # 进程配置扩展
│           └── StartSceneConfig.cs         # 场景配置扩展
├── Ignore.ET.StartConfig.asmdef            # 程序集定义
└── package.json
```

---

## 核心类

### StartProcessConfig（partial 扩展）

进程配置，描述服务器进程的网络信息。

| 属性/方法 | 类型 | 说明 |
|-----------|------|------|
| `InnerIP` | `string` | 内网 IP（来自 StartMachineConfig） |
| `OuterIP` | `string` | 外网 IP（来自 StartMachineConfig） |
| `IPEndPoint` | `IPEndPoint` | 内网 IP + Port 的端点（懒加载缓存） |
| `StartMachineConfig` | `StartMachineConfig` | 关联的机器配置（通过 MachineId 查找） |

**关键设计**：
- `IPEndPoint` 使用懒加载模式，首次访问时调用 `NetworkHelper.ToIPEndPoint()` 创建并缓存。
- 通过 `MachineId` 从 `StartMachineConfigCategory.Instance` 查找关联机器配置。

---

### StartSceneConfig（partial 扩展）

场景配置，是最核心的启动配置，描述每个服务器 Scene 的位置与类型。

| 属性/方法 | 类型 | 说明 |
|-----------|------|------|
| `ActorId` | `ActorId` | Actor 唯一标识（由 Process + Id + 1 构成） |
| `Type` | `int` | 场景类型的整数 ID |
| `StartProcessConfig` | `StartProcessConfig` | 所在进程的配置（通过 Process 查找） |
| `StartZoneConfig` | `StartZoneConfig` | 所在区域的配置（通过 Zone 查找） |
| `InnerIPPort` | `IPEndPoint` | 内网 IP:Port 端点（懒加载） |
| `OuterIPPort` | `IPEndPoint` | 外网 IP:Port 端点（懒加载，?= 运算符） |
| `EndInit()` | `void` | 初始化 ActorId 和 Type 字段 |

**EndInit 流程**：
```
StartSceneConfig.EndInit()
├── ActorId = new ActorId(Process, Id, 1)
└── Type = SceneTypeSingleton.Instance.GetSceneType(SceneType)
```

---

### StartSceneConfigCategory（partial 扩展）

场景配置的分类索引，提供多维度查询能力。

**内部索引数据结构**：

| 字段 | 类型 | 用途 |
|------|------|------|
| `processScenes` | `MultiMap<int, StartSceneConfig>` | 按进程 ID 索引 |
| `zoneScenesByName` | `Dictionary<long, Dictionary<string, StartSceneConfig>>` | 按区域+名称索引 |
| `zoneSceneByType` | `Dictionary<long, MultiMap<int, StartSceneConfig>>` | 按区域+类型索引 |
| `sceneByType` | `MultiMap<int, StartSceneConfig>` | 全局按类型索引 |

**查询方法**：

| 方法 | 参数 | 说明 |
|------|------|------|
| `GetByProcess(int process)` | process | 获取某进程下的所有场景 |
| `GetBySceneName(int zone, string name)` | zone, name | 按区域+名称查找场景 |
| `GetBySceneType(int zone, int type)` | zone, type | 按区域+类型查找所有场景 |
| `GetBySceneType(int type)` | type | 全局按类型查找场景 |
| `GetOneBySceneType(int zone, int type)` | zone, type | 按区域+类型获取第一个场景 |

**EndInit 流程**（构建索引）：
```
StartSceneConfigCategory.EndInit()
└── foreach StartSceneConfig
    ├── sceneByType.Add(Type, config)
    ├── processScenes.Add(Process, config)
    ├── zoneScenesByName[Zone].Add(Name, config)
    └── zoneSceneByType[Zone].Add(Type, config)
```

---

## Excel 配置表说明

表名中的 `@s` 后缀表示这是服务端专用配置（不导出到客户端）。

### StartMachineConfig（机器配置）
配置每台物理/虚拟机器的网络信息（InnerIP、OuterIP）。

### StartProcessConfig（进程配置）
每个服务器进程的配置，包含：
- `Id`：进程唯一 ID
- `MachineId`：关联机器 ID
- `Port`：监听端口

### StartSceneConfig（场景配置）
最细粒度的配置，每行代表一个服务器 Scene：
- `Id`：场景唯一 ID
- `Process`：所在进程 ID
- `Zone`：所在区域 ID
- `Name`：场景名称（如 "Gate"、"Map"）
- `SceneType`：场景类型字符串（用于映射为 int Type）
- `Port`：网络监听端口

### StartZoneConfig（区域配置）
游戏区域（服务器组）的配置，用于多区服务器的分区管理。

---

## 依赖关系

### 依赖的包
- **cn.etetet.core**：基础框架，`ActorId`、`MultiMap`、`NetworkHelper` 等
- **cn.etetet.excel**（间接）：Excel 配置生成工具，生成基类代码
- **cn.etetet.actorlocation**（间接）：`ActorId` 定义来源

### 被依赖的包
- **cn.etetet.login**：登录服务需要读取 Gate 场景配置
- **cn.etetet.router**：路由转发时查找目标场景
- **cn.etetet.ui**：客户端可能读取服务器地址配置
- 几乎所有服务端 package 在初始化时都读取此配置

---

## 架构模式

### 1. 配置驱动（Data-Driven）
所有配置通过 Excel 表格定义，代码生成工具自动生成：
- `StartXxxConfigCategory`：配置集合基类，含 `GetAll()` 方法
- `StartXxxConfig`：单条配置的数据类
- 手写的 `partial` 类负责扩展业务逻辑

### 2. 单例访问
通过 `XxxConfigCategory.Instance` 静态单例访问配置数据，确保全局唯一。

### 3. EndInit 模式
ET 框架的配置初始化模式：
- 基类 `EndInit()` 在所有字段反序列化后调用
- 子类重写 `EndInit()` 进行额外的索引构建和字段初始化

### 4. 懒加载 IPEndPoint
网络端点使用懒加载模式，避免在不需要网络的场景下创建对象。

---

## 关键流程

### 服务器启动查询流程
```
服务器启动
└── 读取命令行参数（ProcessId）
    └── StartProcessConfigCategory.Instance.Get(processId)
        └── StartSceneConfigCategory.Instance.GetByProcess(processId)
            └── 遍历场景列表，创建 Scene 实体
                └── scene.ActorId = startSceneConfig.ActorId
```

### 配置加载顺序
```
StartMachineConfig → StartProcessConfig → StartZoneConfig → StartSceneConfig
     （机器层）          （进程层）           （区域层）          （场景层）
```
（后者依赖前者，所以加载顺序需保证）

---

## 备注

- 此包是纯服务端配置（Scripts 目录下使用 Server assembly reference），客户端不引用。
- 程序集定义文件名为 `Ignore.ET.StartConfig.asmdef`，表示在标准编译中可能被忽略（热更新时动态加载）。
- `StartSceneConfig.Type` 字段的整型值由 `SceneTypeSingleton` 负责将字符串类型名转换为枚举整型。

---

## Round 2 补充

### 细节修正

1. **索引 Key 类型**：`zoneScenesByName` 和 `zoneSceneByType` 的 Zone key 实际是 `long`（对应 `StartZoneConfig.Id`），而 `processScenes` 的 Process key 是 `int`。第一轮文档已正确描述，无需修正。

2. **`StartProcessConfig.IPEndPoint` 是内网地址**：`IPEndPoint` 属性组合 `InnerIP + Port`，用于服务器内部通信。外网端口由 `StartSceneConfig.OuterIPPort` 提供（OuterIP + Port）。

3. **`ActorId` 第三参数含义**：`new ActorId(Process, Id, 1)` 中第三个参数为 fiberId（初始值为 1），代表该 Scene 所在的 Fiber 编号。

4. **生成类不在此包中**：`StartMachineConfig`、`StartProcessConfig`（基类）等由 `cn.etetet.excel` 工具从 xlsx 生成，生成代码通常在项目的 `Assets/` 或独立目录下，此包只包含 `partial` 扩展逻辑。

5. **`EndInit()` 调用时机**：`EndInit()` 在 ET 配置系统完成反序列化（从 JSON/二进制）后自动调用，先调用每条记录的 `EndInit()`，再调用 Category 级别的 `EndInit()` 构建索引。

### 代码示例

#### 典型：Fiber 初始化时查询本进程所有场景

```csharp
// 服务器进程启动，根据 ProcessId 找到本进程的所有 Scene 配置
int processId = Options.Instance.Process;
List<StartSceneConfig> scenes = StartSceneConfigCategory.Instance.GetByProcess(processId);
foreach (StartSceneConfig cfg in scenes)
{
    // cfg.Type 已由 SceneTypeSingleton 转换为整型
    // cfg.ActorId 已构建好，可直接用于 Actor 通信
    // cfg.InnerIPPort 提供内网监听地址
    await root.GetComponent<FiberManager>().Create(SchedulerType.ThreadScheduler, cfg.Id, cfg.Zone, cfg.Type, cfg.Name);
}
```

#### 典型：根据区域和类型找到 Gate 地址

```csharp
// 找到 zone=1 的 Gate 场景，取其内网地址
StartSceneConfig gateConfig = StartSceneConfigCategory.Instance.GetOneBySceneType(1, SceneType.Gate);
IPEndPoint gateEndPoint = gateConfig.InnerIPPort; // 用于内网 Actor 消息投递
```

#### 典型：HTTP /get_router 枚举所有 Router/Realm

```csharp
// 跨所有 zone 查找全部 Router
List<StartSceneConfig> routers = StartSceneConfigCategory.Instance.GetBySceneType(SceneType.Router);
foreach (var r in routers)
    response.Routers.Add($"{r.StartProcessConfig.OuterIP}:{r.Port}");
```

### 配置层级关系

```
StartMachineConfig  (机器层：InnerIP, OuterIP)
       └── StartProcessConfig  (进程层：MachineId, Port)
               └── StartSceneConfig  (场景层：Process, Zone, Name, SceneType, Port)
                       └── StartZoneConfig  (区域元数据)
```

每层通过 ID 关联，Category 的 `EndInit()` 负责建立各维度索引以支持高效查询。

### 跨 Package 交互补充

| 调用方 | 使用方式 |
|--------|----------|
| `cn.etetet.router` | `GetBySceneType(SceneType.Router/Realm)` 枚举 Router/Realm 列表 |
| `cn.etetet.login` | `GetOneBySceneType(zone, SceneType.Realm)` 取 Realm 地址 |
| `cn.etetet.netinner` | `Get(processId)` 取进程网络端口 |
| `cn.etetet.actorlocation` | `startSceneConfig.ActorId` 注册 Actor 位置 |
| `cn.etetet.ui`（客户端） | 不直接使用，仅服务端访问 |
