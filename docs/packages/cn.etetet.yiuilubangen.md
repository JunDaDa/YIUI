# cn.etetet.yiuilubangen — ET.YIUI.LubanGen 配置生成包

## 概述

**版本**: 0.0.0（由工具自动管理，不手动发版）
**分类**: Config/Luban | UI/YIUI
**依赖**:
- `cn.etetet.core` ^1.0.0
- `cn.etetet.loader` ^1.0.0

**描述**: Luban 配置工具**生成包**。这个包不在 git 仓库中直接存在，而是由 `cn.etetet.yiuiluban` 的 Editor 工具（`InitGen()`）从模板复制生成，并在每次执行 `ET/Excel/ExcelExporter` 时被 Luban CLI 更新内容。

它是整个配置数据管线的**输出端**，包含三类内容：
1. **源数据**（Excel 表格）：游戏业务配置 + 启动配置
2. **生成产物**（C# 代码 + 二进制/JSON 数据文件）：由 Luban 工具生成
3. **运行时 Invoker**（C# 脚本）：实现 Client/Server 端文件加载和反序列化

---

## 目录结构

```
cn.etetet.yiuilubangen/
├── package.json
├── README.md
│
├── Luban/                                  # 源数据目录（Excel 表格 + 生成脚本）
│   ├── Config/                             # 游戏业务配置（全局，Client+Server+ClientServer 三套生成）
│   │   └── Base/
│   │       ├── LubanGen1.ps1 / .bat        # 生成 Client 端代码和数据
│   │       ├── LubanGen2.ps1 / .bat        # 生成 Server 端代码和数据
│   │       ├── LubanGen3.ps1 / .bat        # 生成 ClientServer 端代码和数据
│   │       ├── __tables__.xlsx             # 表定义（由所有包聚合注入）
│   │       ├── __beans__.xlsx              # Bean 结构体定义
│   │       └── __enums__.xlsx              # 枚举定义
│   │   └── Datas/
│   │       ├── AI.xlsx                     # AI 配置数据
│   │       └── Unit.xlsx                   # 单位配置数据
│   │
│   ├── Localhost/                          # 本地开发启动配置（仅 Server 端）
│   │   ├── Base/
│   │   │   ├── LubanGen1.ps1 / .bat        # 生成 Server 启动配置
│   │   │   ├── LubanGen2.ps1 / .bat        # 生成 ClientServer 启动配置
│   │   │   └── __tables__ / __beans__ / __enums__.xlsx
│   │   └── Datas/
│   │       ├── StartMachine.xlsx           # 机器配置
│   │       ├── StartProcess.xlsx           # 进程配置
│   │       ├── StartScene.xlsx             # 场景配置
│   │       └── StartZone.xlsx             # 区域配置
│   │
│   └── Release/                            # 生产环境启动配置
│       ├── Base/ (同 Localhost 结构)
│       └── Datas/ (StartMachine/Process/Scene/Zone.xlsx)
│
├── Assets/LubanGen/                        # 生成产物：运行时数据文件（.bytes + .json）
│   ├── Config/
│   │   ├── Binary/
│   │   │   ├── Client/                     # 客户端专属二进制数据
│   │   │   ├── Server/                     # 服务端专属二进制数据
│   │   │   └── ClientServer/               # 共享二进制数据
│   │   └── Json/
│   │       ├── Client/ / Server/ / ClientServer/   # 对应 JSON 格式（调试用）
│   └── StartConfig/
│       ├── Localhost/Binary|Json/Server|ClientServer/
│       └── Release/Binary|Json/Server|ClientServer/
│
├── CodeMode/                               # 生成产物：C# 代码（.cs 文件）
│   └── Model/
│       ├── Client/LubanGen/Config/         # 客户端配置类（AIConfig, UnitConfig...）
│       ├── Server/LubanGen/
│       │   ├── Config/                     # 服务端配置类
│       │   └── StartConfig/                # 服务端启动配置类
│       └── ClientServer/LubanGen/Config/   # 共享配置类
│   （注：.cs 文件在生成前被清空，.meta 文件保留）
│
└── Scripts/                                # 运行时 Invoker（手写，非生成）
    ├── Hotfix/Server/
    │   └── LubanServerLoaderInvoker.cs     # 服务端加载实现（#if DOTNET）
    ├── Hotfix/Share/
    │   └── LubanConfigDeserialize.cs       # 反序列化 Invoker（共用）
    └── HotfixView/Client/
        └── LubanClientLoaderInvoker.cs     # 客户端加载实现
```

---

## 核心脚本详解

### 1. `LubanConfigDeserialize.cs`（Share）

```csharp
[Invoke(ConfigType.Luban)]
public class ConfigDeserialize_Luban : AInvokeHandler<ConfigLoader.ConfigDeserialize, object>
{
    public override object Handle(ConfigLoader.ConfigDeserialize args)
    {
        return Activator.CreateInstance(args.Type, new ByteBuf(args.ConfigBytes));
    }
}
```

响应 `ConfigDeserialize` 事件，使用反射将二进制数据（`byte[]`）通过 `ByteBuf` 构造函数创建 `ConfigCategory` 实例。Luban 生成的每个 Category 类都有接受 `ByteBuf` 的构造函数。

### 2. `LubanClientLoaderInvoker.cs`（Client，Unity）

共有三个 Invoker 类：

**`LubanClientLoaderInvokerGetAll`**（`[Invoke]`，无 ConfigType 限定，全局）：

```csharp
// 编辑器模式
var globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
var codeMode = globalConfig.CodeMode.ToString();
foreach (Type configType in allTypes) {
    await EventSystem.Instance.Invoke<ConfigLoader.LubanGetConfigBytes, ETTask<byte[]>>(
        configAttribute.ConfigType, new LubanGetConfigBytes { Type = configType, CodeMode = codeMode });
}

// 打包运行时（非编辑器）
var v = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>(type.Name);
output[type] = v.bytes;
```

编辑器模式下通过 `ConfigAttribute.ConfigType` 分发到 `LubanGetConfigBytes_Luban`；打包运行时直接通过 `ResourcesComponent` 按类型名加载资源（不区分 CodeMode）。

**`LubanGetConfigBytes_Luban`**（`[Invoke(ConfigType.Luban)]`）：

```csharp
if (LubanHelper.StartConfigs.Contains(configType.Name))
    // 启动配置路径
    configFilePath = $"{LubanHelper.ConfigResPath}/StartConfig/{Options.Instance.StartConfig}/Binary/{CodeMode.Server}/{configType.Name}.bytes";
else
    // 普通配置路径
    configFilePath = $"{LubanHelper.ConfigResPath}/Config/Binary/{codeMode}/{configType.Name}.bytes";
return await File.ReadAllBytesAsync(configFilePath);
```

**`LubanClientLoaderInvokerGetOne`**（`[Invoke(ConfigType.Luban)]`）：

```csharp
var codeMode = Resources.Load<GlobalConfig>("GlobalConfig").CodeMode.ToString();
var configFilePath = $"{LubanHelper.ConfigResPath}/Config/Binary/{codeMode}/{args.ConfigName}.bytes";
return await File.ReadAllBytesAsync(configFilePath);
```

### 3. `LubanServerLoaderInvoker.cs`（Server，仅 `#if DOTNET`）

共有三个 Invoker 类（与客户端对称）：

**`LubanServerLoaderInvokerGetAll`**（`[Invoke]`）：

```csharp
var codeMode = "Server";  // 服务端固定使用 Server 模式
foreach (Type configType in allTypes)
{
    var configBytes = await EventSystem.Instance.Invoke<ConfigLoader.LubanGetConfigBytes, ETTask<byte[]>>(
        configAttribute.ConfigType, new LubanGetConfigBytes { Type = configType, CodeMode = codeMode });
    if (configBytes == null) Log.Error($"没有读取到配置,{codeMode},{configType}");
}
```

**`LubanGetConfigBytes_Luban`**（`[Invoke(ConfigType.Luban)]`）：

```csharp
if (LubanHelper.StartConfigs.Contains(configType.Name))
    // 注意：服务端路径与客户端不同，没有 "StartConfig/" 前缀
    configFilePath = $"{LubanHelper.ConfigResPath}/{Options.Instance.StartConfig}/Binary/{codeMode}/{configType.Name}.bytes";
else
    configFilePath = $"{LubanHelper.ConfigResPath}/Config/Binary/{codeMode}/{configType.Name}.bytes";
```

> ⚠️ **路径差异（Client vs Server）**：
> - Client 启动配置：`.../StartConfig/{StartConfig}/Binary/{CodeMode.Server}/...`（含 `StartConfig/` 前缀，固定使用 `CodeMode.Server`）
> - Server 启动配置：`.../{StartConfig}/Binary/{codeMode}/...`（无前缀，使用传入的 codeMode）

**`LubanServerLoaderInvokerGetOne`**（`[Invoke(ConfigType.Luban)]`）：

```csharp
return await File.ReadAllBytesAsync($"{LubanHelper.ConfigResPath}/Config/Binary/Server/{args.ConfigName}.bytes");
// 服务端 GetOne 固定使用 "Server" 目录（不读 GlobalConfig）
```

---

## Luban 代码生成脚本分析

### Config 目录（游戏业务配置）

三个脚本并行执行，生成三套代码和数据：

| 脚本 | 目标(-t) | 代码输出 | 数据输出 |
|------|----------|---------|---------|
| `LubanGen1.ps1` | client | `CodeMode/Model/Client/LubanGen/Config/` | `Assets/LubanGen/Config/Binary/Client/` + `Json/Client/` |
| `LubanGen2.ps1` | server | `CodeMode/Model/Server/LubanGen/Config/` | `Assets/LubanGen/Config/Binary/Server/` + `Json/Server/` |
| `LubanGen3.ps1` | all | `CodeMode/Model/ClientServer/LubanGen/Config/` | `Assets/LubanGen/Config/Binary/ClientServer/` + `Json/ClientServer/` |

Luban CLI 参数：
```
dotnet Luban.dll --customTemplateDir <CUSTOM> -t <target> -c cs-bin -d bin -d json
  --conf <luban.conf>
  -x outputCodeDir=<代码输出路径>
  -x bin.outputDataDir=<二进制数据输出路径>
  -x json.outputDataDir=<JSON数据输出路径>
```

### Localhost / Release 目录（启动配置）

| 脚本 | 代码输出 | 数据输出 |
|------|---------|---------|
| `LubanGen1.ps1` | `CodeMode/Model/Server/LubanGen/StartConfig` | `Assets/LubanGen/StartConfig/Localhost|Release/Binary/Server/` |
| `LubanGen2.ps1` | （相同代码目录） | `Assets/LubanGen/StartConfig/Localhost|Release/Binary/ClientServer/` |

---

## 生成的 Bean 类型（Luban 数学/辅助结构）

Round 1 遗漏：Luban 会为配置中使用的自定义值类型生成辅助 struct，本包包含以下：

| 类型 | 字段 | 说明 |
|------|------|------|
| `f2` (struct) | `float X, Y` | 2D 浮点向量（对应 Unity Vector2） |
| `f3` (struct) | `float X, Y, Z` | 3D 浮点向量（对应 Unity Vector3） |
| `f4` (struct) | `float X, Y, Z, W` | 4D 浮点向量（对应 Unity Vector4） |
| `q4` (struct) | `float X, Y, Z, W` | 四元数（Quaternion，字段与 f4 相同，语义不同） |

这些 struct 全部实现：
- `ByteBuf` 构造函数（反序列化）
- `ResolveRef()` 方法（引用解析，当前为空）
- `partial void PostInit() / EndRef()`（扩展点）
- `ToString()`（调试输出，字段名用小写 x/y/z/w）

> 注意：`Tables.cs` 文件存在于每个 CodeMode 目录，但目前内容为空（仅有自动生成注释），是 Luban 生成的聚合入口占位文件。

## 当前配置数据（已生成内容）

### 游戏配置表（完整字段）

**`UnitConfig`** (`__ID__ = -568528378`)：单位/角色配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` | 主键 |
| `Type` | `int` | 单位类型 |
| `Name` | `string` | 名字 |
| `Position` | `int` | 位置（整数编码） |
| `Height` | `int` | 身高 |

> 注意：Round 1 文档中误写了 "Weight" 字段，实际无此字段（共5个字段，非6个）。

**`AIConfig`** (`__ID__ = -294143606`)：AI 行为节点配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` | 主键 |
| `AIConfigId` | `int` | 所属 AI（外键，关联 AI 组） |
| `Order` | `int` | 此 AI 中的顺序 |
| `Name` | `string` | 节点名字（对应 AI 行为节点类名） |
| `NodeParams` | `List<int>` | 节点参数列表 |

### 启动配置（Localhost / Release 两套，完整字段）

**`StartMachineConfig`** (`__ID__ = 1628109127`)：服务器机器配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` | 主键 |
| `InnerIP` | `string` | 内网地址 |
| `OuterIP` | `string` | 外网地址 |
| `WatcherPort` | `string` | 守护进程端口 |

**`StartProcessConfig`** (`__ID__ = 2140444015`)：进程配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` | 主键 |
| `MachineId` | `int` | 所属机器（外键） |
| `Port` | `int` | 外网端口 |

**`StartSceneConfig`** (`__ID__ = 1499456844`)：场景配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` | 主键 |
| `Process` | `int` | 所属进程（外键） |
| `Zone` | `int` | 所属区（外键） |
| `SceneType` | `string` | 类型（如 "Gate", "Map", "Login" 等） |
| `Name` | `string` | 名字 |
| `Port` | `int` | 外网端口 |

**`StartZoneConfig`** (`__ID__ = -457316368`)：区域配置

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` | 主键 |
| `DBConnection` | `string` | 数据库连接字符串 |
| `DBName` | `string` | 数据库名 |
| `Desc` | `string` | 说明 |

### Category 类型通用模式

所有 `XXXCategory` 类型均实现相同模式：
```csharp
[Config]
public partial class UnitConfigCategory : Singleton<UnitConfigCategory>, ILubanConfig
{
    private readonly Dictionary<int, UnitConfig> _dataMap;  // 按 Id 索引
    private readonly List<UnitConfig> _dataList;            // 顺序列表

    public Dictionary<int, UnitConfig> GetAll() => _dataMap;
    public Dictionary<int, UnitConfig> DataMap => _dataMap;
    public List<UnitConfig> DataList => _dataList;
    public UnitConfig GetOrDefault(int key) => _dataMap.GetValueOrDefault(key);
    public UnitConfig Get(int key) { /* 找不到时调用 LubanLog.Error */ }
    public void ResolveRef() { /* 解析跨表引用 */ }
}
```

`[Config]` 标注使 `ConfigLoader` 能发现并加载该类型；`Singleton<T>` 确保全局唯一实例。

---

## 数据流与加载路径

```
Assets/LubanGen/Config/Binary/{CodeMode}/{XXXConfigCategory}.bytes
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
                 Client          Server         ClientServer
              (Unity运行时)    (DOTNET服务端)    (Unity编辑器)
```

**路径规则**：
- 普通配置: `{ConfigResPath}/Config/Binary/{codeMode}/{TypeName}.bytes`
- 启动配置: `{ConfigResPath}/StartConfig/{StartConfig}/Binary/{codeMode}/{TypeName}.bytes`
  - `{StartConfig}` = `Options.Instance.StartConfig`（如 "Localhost" 或 "Release"）

---

## 与其他包的关系

| 包 | 关系 |
|----|------|
| `cn.etetet.yiuiluban` | 生成本包的工具，负责执行 Luban 脚本，同步 Invoker 脚本 |
| `cn.etetet.core` | 提供 `Singleton`、`EventSystem`、`ETTask`、`IInvokeHandler` 等基础设施 |
| `cn.etetet.loader` | 客户端通过 `ResourcesComponent` 加载资源时的依赖 |
| `cn.etetet.startconfig` | 消费本包生成的 StartConfig 数据（StartMachine/Process/Scene/Zone） |
| 所有游戏业务包 | 通过 `ConfigLoader` 单例访问本包生成的 `XXXConfigCategory` 单例数据 |

---

## 关键设计特点

1. **零依赖生成**：生成的 `CodeMode` C# 代码只依赖 `cn.etetet.core`（通过 `ByteBuf`、`ILubanConfig`），不依赖 `cn.etetet.yiuilubangen` 包本身。

2. **三模式分离**：Client/Server/ClientServer 三套代码完全分离，避免编译时错误和不必要的运行时开销。

3. **双格式输出**：Binary（.bytes，生产用）+ JSON（调试和版本对比用）。

4. **两环境启动配置**：Localhost（本地开发，可能有单机测试配置）和 Release（生产多机配置），通过 `Options.Instance.StartConfig` 在运行时切换。

5. **Invoker 同步机制**：`LubanClientLoaderInvoker.cs` 和 `LubanServerLoaderInvoker.cs` 的源文件在 `cn.etetet.yiuiluban/.Template/` 中维护，通过 `SyncInvoke()` 工具同步到本包，避免手动维护两份代码。
