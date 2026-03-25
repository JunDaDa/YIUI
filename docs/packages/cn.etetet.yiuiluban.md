# cn.etetet.yiuiluban — ET.YIUI.Luban 配置工具

## 概述

**版本**: 3.0.9
**分类**: Config/Luban | UI/YIUI
**依赖**: `cn.etetet.core` ^1.0.0
**描述**: Luban 配置工具，是整个项目 Excel/数据表驱动配置系统的核心包。提供从 Excel 表格到 C# 代码的代码生成流程，以及运行时配置加载和反序列化能力。

Luban 是 Code-Philosophy 开源的游戏数据导出工具，本包将其与 ET 框架、YIUI 编辑器工具链深度集成，支持 Client/Server/ClientServer 三种代码模式、二进制序列化和异步加载。

---

## 目录结构

```
cn.etetet.yiuiluban/
├── package.json                          # 包描述，依赖 cn.etetet.core
├── .Template/                            # 模板目录，用于自动创建初始包结构
│   ├── cn.etetet.yiui/                   # YIUI Base Luban Config 模板（表格骨架）
│   ├── cn.etetet.yiuilubandemo/          # Demo 演示包模板
│   └── cn.etetet.yiuilubangen/           # LubanGen 生成包模板（luban.conf、bat/ps1脚本）
│       └── Scripts/
│           ├── HotfixView/Client/LubanClientLoaderInvoker.cs   # 客户端文件加载 Invoker
│           ├── Hotfix/Server/LubanServerLoaderInvoker.cs       # 服务端文件加载 Invoker
│           └── Hotfix/Share/LubanConfigDeserialize.cs          # 反序列化 Invoker
│
├── Scripts/
│   └── Model/Share/
│       ├── Config/
│       │   ├── ConfigType.cs             # 配置类型枚举（Luban=0, Bson=1）
│       │   ├── ILubanConfig.cs           # 配置 Category 接口及系统基类
│       │   ├── LubanConfigAttribute.cs   # [Config(configType)] 标签
│       │   ├── LubanConfigLoader.cs      # 核心加载器 ConfigLoader（Singleton）
│       │   ├── LubanConfigProcessAttribute.cs # [ConfigProcess] 后处理标签
│       │   ├── LubanEditorHelper.cs      # 编辑器下按路径加载单个配置（#if UNITY_EDITOR）
│       │   ├── LubanHelper.cs            # 常量：ConfigResPath、StartConfigs 列表
│       │   └── LubanLog.cs               # 统一日志输出工具
│       └── LubanLib/
│           ├── BeanBase.cs               # Luban Bean 基类（多态 Bean 的根类）
│           ├── ByteBuf.cs                # 二进制读取器（Luban 序列化层）
│           └── ITypeId.cs                # 多态类型 ID 接口
│
├── CodeMode/                             # 多端代码模式目录（ExternalTypeUtil 位于此）
│   ├── Model/Client/ConfigExtend/Util/ExternalTypeUtil.cs        # Unity.Mathematics 类型转换（Client）
│   ├── Model/ClientServer/ConfigExtend/Util/ExternalTypeUtil.cs  # Unity.Mathematics 类型转换（共享）
│   └── Model/Server/ConfigExtend/Util/ExternalTypeUtil.cs        # Unity.Mathematics 类型转换（Server）
│
└── Editor/
    ├── Window/                           # 旧式简单工具窗口（LubanTools partial class）
    │   ├── LubanTools.cs                 # 主 OnGUI 入口
    │   ├── LubanTools_Gen.cs             # Luban 代码生成逻辑（CreateLubanConf、RunLubanGen）
    │   ├── LubanTools_Init.cs            # 初始化 LubanGen 包
    │   ├── LubanTools_Create.cs          # 为现有 ET 包创建 Luban 模板
    │   ├── LubanTools_Demo.cs            # Demo 包创建/删除
    │   ├── LubanTools_Replace.cs         # 将旧 ET.Excel 系统迁移为 Luban
    │   └── LubanTools_CodeModeChangeHelper.cs # CodeMode 切换辅助
    ├── YIUIToolbar/
    │   └── YIUILubanExcelToolBar.cs      # Unity 工具栏快速导出按钮
    └── YIUIWindow/Luban/                 # Odin Inspector 可视化管理窗口
        ├── YIUILubanTool.cs              # 主窗口（OdinMenuEditorWindow）
        ├── LubanToolRoot.cs              # 菜单树根节点，扫描所有包的 Luban 目录
        ├── LubanToolModule.cs            # 工具模块面板
        ├── LubanConfigModule.cs          # 单包配置模块（Base/Defines/Datas 子节点）
        ├── LubanAllDatasModule.cs        # 所有包配置汇总视图
        ├── LubanBaseModule.cs            # Base 表骨架视图
        ├── LubanDatasModule.cs           # Datas 数据表视图（递归显示 xlsx）
        ├── LubanDefinesModule.cs         # Defines xml 视图
        ├── LubanModuleBase.cs            # 模块基类
        ├── LubanEditorSerializationData.cs # 编辑器持久化设置（ProjectSettings 存储）
        ├── LubanConfigEditorData.cs      # 单配置文件编辑器元数据
        ├── LubanConfigEditorModule.cs    # 单配置文件查看/操作
        └── Toolbar/
            ├── LubanExcelToolBar.cs
            ├── ToolbarCallback.cs
            └── ToolbarExtender.cs
```

---

## 核心类详解

### 1. `ILubanConfig` / `LubanConfigSystem<T>`

位置：`Scripts/Model/Share/Config/ILubanConfig.cs`

```csharp
public interface ILubanConfig
{
    void ResolveRef();  // 所有配置加载完毕后解析跨表引用
}

public interface ILubanConfigSystem : ISystemType
{
    void LubanConfig(ILubanConfig data);  // Hotfix 中初始化时回调
}

[EntitySystem]
public abstract class LubanConfigSystem<T> : SystemObject, ILubanConfigSystem where T : ILubanConfig
{
    protected abstract void LubanConfig(T self);  // 子类实现热更逻辑
}
```

每个 `XXXConfigCategory`（由 Luban 生成）都实现此接口，`ResolveRef()` 用于在二次遍历时解析跨表引用（如 id → 对象）。

### 2. `ConfigLoader`（Singleton）

位置：`Scripts/Model/Share/Config/LubanConfigLoader.cs`

**核心加载流程**：

| 方法 | 作用 |
|------|------|
| `LoadAsync()` | 异步加载全部配置：触发 `LubanGetAllConfigBytes` → 并发反序列化 → `ResolveRef()` → `ConfigProcess()` |
| `Reload(Type)` | 热重载单个配置 |
| `LoadOneConfig(Type, byte[])` | 触发 `ConfigDeserialize` 事件，得到 Category 对象，注册为 World Singleton |
| `ResolveRef()` | 双遍历：先调所有 `ILubanConfig.ResolveRef()`，再调 `ILubanConfigSystem.LubanConfig()` |
| `ConfigProcess()` | 发现 `[ConfigProcess]` 标记的类型并实例化注册 |

**`ConfigLoader` 内部结构体（Invoke 消息类型）**：

```csharp
public struct ConfigDeserialize       { Type Type; byte[] ConfigBytes; }
public struct LubanGetConfigBytes     { Type Type; string CodeMode; }
public struct GetAllConfigBytes       {}
public struct GetOneConfigBytes       { string ConfigName; }
public struct LubanGetAllConfigBytes  {}
public struct LubanGetOneConfigBytes  { string ConfigName; }
```

**关键 Invoke 事件**（解耦序列化与加载器）：

| 事件结构体 | 触发时机 | 期望返回 |
|-----------|----------|---------|
| `LubanGetAllConfigBytes` | LoadAsync 时 | `Dictionary<Type, byte[]>` |
| `LubanGetOneConfigBytes` | Reload 时 | `byte[]` |
| `LubanGetConfigBytes` | 获取单个文件字节（编辑器路径拼接）| `byte[]` |
| `ConfigDeserialize` | 每个配置类型 | `object`（实际为 `ILubanConfig`） |

### 3. `ConfigAttribute` / `ConfigProcessAttribute`

```csharp
[Config(ConfigType.Luban)]   // 标记 ConfigCategory，指定反序列化方式（Luban=0, Bson=1）
[ConfigProcess]              // 标记需在所有配置加载完毕后执行的初始化类
```

- `ConfigAttribute`：标注在 Luban 生成的 `XXXConfigCategory` 类上，`ConfigLoader` 通过反射读取此特性判断用哪种反序列化器
- `ConfigProcessAttribute`：配置加载完后自动扫描此类，实例化并注册为 `World` 单例，用于跨配置的后处理逻辑

### 4. `LubanHelper`

```csharp
public static class LubanHelper
{
    public const string ConfigResPath = "Packages/cn.etetet.yiuilubangen/Assets/LubanGen";

    [StaticField]
    public static List<string> StartConfigs = new()
    {
        "StartMachineConfigCategory",
        "StartProcessConfigCategory",
        "StartSceneConfigCategory",
        "StartZoneConfigCategory",
    };
}
```

- `ConfigResPath`：生成的二进制配置文件根路径（指向 `yiuilubangen` 包的 Assets 目录）
- `StartConfigs`：启动配置表名列表。这些配置与普通配置路径不同：路径为 `StartConfig/{startConfigName}/Binary/Server/`

### 5. `LubanEditorConfigCategory`（仅编辑器）

位置：`Scripts/Model/Share/Config/LubanEditorHelper.cs`
条件编译：`#if UNITY_EDITOR`

```csharp
public static class LubanEditorConfigCategory
{
    // 编辑器下直接从磁盘加载单个配置文件（不经过 ResourcesComponent）
    public static T Get<T>(string codeMode = "", string startConfig = "")
        where T : Singleton<T>, ILubanConfig

    // 清除所有已缓存的 ILubanConfig Singleton 实例（通过反射清空 instance 字段）
    public static void ClearAll()
}
```

- `Get<T>()`：按文件路径读取 `.bytes` 文件，通过 `Activator.CreateInstance(type, new ByteBuf(...))` 直接构造
- **注意**：不调用 `ResolveRef()`，跨表引用字段为 null，仅用于数据存在性检查
- `ClearAll()`：遍历程序集中所有 `ILubanConfig` 类型，反射清空其 `Singleton<T>.instance` 静态字段（在代码生成前调用，防止旧实例干扰）

### 6. `LubanLog`

```csharp
public static class LubanLog
{
    public static void Error(object config, object key)
    {
        Log.Error($"{config.GetType().Name} 配置错误，键 {key} 不存在");
    }
}
```

统一的配置访问错误日志，生成的 `XXXConfigCategory` 中的 `Get(key)` 方法在键不存在时调用此方法。

### 7. Luban 底层库

位置：`Scripts/Model/Share/LubanLib/`

| 类/接口 | 说明 |
|---------|------|
| `ITypeId` | 接口，声明 `int GetTypeId()` —— 用于多态 Bean 的类型分发 |
| `BeanBase` | Luban 所有多态 Bean 的根类，`[EnableClass]` 标记，`GetTypeId()` 返回类型标识，`EndInit()` 初始化回调 |
| `ByteBuf` | 二进制反序列化读取器，Luban 生成代码中 `XXXConfigCategory(ByteBuf buf)` 构造函数使用此类读取字段 |

### 8. `ExternalTypeUtil`（多端）

位置：`CodeMode/Model/{Client|ClientServer|Server}/ConfigExtend/Util/ExternalTypeUtil.cs`

```csharp
// Client / ClientServer 版本（依赖 Unity.Mathematics）
public static partial class ExternalTypeUtil
{
    public static float2 UnityMathematicsFloat2(f2 f2)   => new float2(f2.X, f2.Y);
    public static float3 UnityMathematicsFloat3(f3 f3)   => new float3(f3.X, f3.Y, f3.Z);
    public static float4 UnityMathematicsFloat4(f4 f4)   => new float4(f4.X, f4.Y, f4.Z, f4.W);
    public static quaternion UnityMathematicsQuaternion4(q4 q4) => new quaternion(q4.X, q4.Y, q4.Z, q4.W);
}
```

Luban 自定义类型（f2/f3/f4/q4）到 Unity.Mathematics 类型的转换。`partial class` 设计允许各端独立扩展。

### 9. `LubanTools`（partial class，Editor）

位置：`Editor/Window/LubanTools*.cs`
菜单路径：`ET/Excel/ExcelExporter`

核心方法：

| 方法 | 描述 |
|------|------|
| `LubanGen()` | 先备份旧生成文件 → 调 `CreateLubanConf()` → 成功后清理备份，失败后还原 |
| `CreateLubanConf()` | 扫描所有 `cn.etetet.*` 包的 `Luban/*/Base/luban.conf`，动态注入 `schemaFiles` 字段，并行执行 ps1/bat 脚本 |
| `RunLubanGen(path)` | 在指定目录执行 PowerShell/bat 脚本（支持 Windows/Linux/Mac） |
| `ReplaceAll()` | 迁移工具：删除旧 `cn.etetet.excel` 包内容，替换 `statesync/Editor/InitHelper.cs` 中调用方式 |
| `SyncInvoke()` | 同步模板中的 `LubanClientLoaderInvoker.cs` / `LubanServerLoaderInvoker.cs` 到 yiuilubangen 包 |
| `InitGen()` | 首次使用：复制 `.Template/cn.etetet.yiuilubangen` 到实际包位置，并触发首次 LubanGen |
| `CreateToPackage(path)` | 为指定 ET 包创建 Luban 模板目录（`Luban/Config/Base` + `Datas`） |

**执行前备份机制**：
1. `CreateLubanBefore()`：将所有 `CodeMode/*/LubanGen/**/*.cs` 备份到系统临时目录 (`Path.GetTempPath()/LubanBackup`)
2. 执行脚本生成
3. 成功：`CreateLubanAfterSucceed()` 清理备份、删除空目录、可选转换为 Unix 换行符
4. 失败：`CreateLubanAfterFailed()` 从备份还原

**超时控制**：`WaitForExit(20000)` — 单个 Luban 导出超时 20 秒，超时后 Kill 进程。

### 10. Luban Invoker 实现（模板生成，位于 yiuilubangen）

位置：`.Template/cn.etetet.yiuilubangen/Scripts/`

#### `LubanClientLoaderInvoker`（Unity 客户端）

```csharp
// 加载全部配置
[Invoke]
class LubanClientLoaderInvokerGetAll : AInvokeHandler<LubanGetAllConfigBytes, ETTask<Dictionary<Type, byte[]>>>
// 编辑器下：通过 GlobalConfig.CodeMode 确定路径，逐个调 LubanGetConfigBytes
// 运行时：通过 ResourcesComponent.LoadAssetAsync<TextAsset>(type.Name) 加载

// 加载单个配置字节（编辑器/运行时均用文件路径）
[Invoke(ConfigType.Luban)]
class LubanGetConfigBytes_Luban : AInvokeHandler<LubanGetConfigBytes, ETTask<byte[]>>
// 路径：{ConfigResPath}/Config/Binary/{codeMode}/{configType.Name}.bytes
// 特例：StartConfig 路径：{ConfigResPath}/StartConfig/{Options.StartConfig}/Binary/Server/{name}.bytes

// 热重载单个（编辑器）
[Invoke(ConfigType.Luban)]
class LubanClientLoaderInvokerGetOne : AInvokeHandler<LubanGetOneConfigBytes, ETTask<byte[]>>
```

#### `LubanServerLoaderInvoker`（.NET 服务端，`#if DOTNET`）

```csharp
[Invoke]
class LubanServerLoaderInvokerGetAll
// 固定 codeMode = "Server"，逐个读取文件

[Invoke(ConfigType.Luban)]
class LubanGetConfigBytes_Luban
// 同客户端逻辑，路径使用 Path.Combine

[Invoke(ConfigType.Luban)]
class LubanServerLoaderInvokerGetOne
// 固定 Server 路径读取单个配置
```

#### `ConfigDeserialize_Luban`（Client + Server 共享）

```csharp
[Invoke(ConfigType.Luban)]
class ConfigDeserialize_Luban : AInvokeHandler<ConfigLoader.ConfigDeserialize, object>
{
    public override object Handle(ConfigLoader.ConfigDeserialize args)
    {
        return Activator.CreateInstance(args.Type, new ByteBuf(args.ConfigBytes));
    }
}
```

通过反射调用 Luban 生成的 `XXXConfigCategory(ByteBuf buf)` 构造函数完成反序列化。

### 11. `YIUILubanTool`（Odin 可视化工具）

位置：`Editor/YIUIWindow/Luban/YIUILubanTool.cs`
菜单路径：`ET/YIUI Luban 配置工具`

基于 `OdinMenuEditorWindow` 构建树形导航：
- 扫描所有包的 `Luban/*/` 目录，按包名分组
- 每个包节点下有 `Base`（骨架表）、`Defines`（xml枚举/Bean定义）、`Datas`（数据表 xlsx）三个子节点
- 支持包别名、描述，设置持久化到 `ProjectSettings/LubanEditorSerializationDataSettings.txt`

---

## 架构模式

### 配置数据流

```
Excel 文件 (Packages/cn.etetet.*/Luban/*/Datas/*.xlsx)
        │
        ▼  [ET/Excel/ExcelExporter 或 LubanGen()]
Luban CLI 工具 (ps1/bat 脚本调用 Luban.dll)
        │ 生成
        ├─▶ C# 代码 → Packages/cn.etetet.*/CodeMode/Model/{Client|Server|ClientServer}/LubanGen/
        └─▶ 二进制数据 → Packages/cn.etetet.yiuilubangen/Assets/LubanGen/Config/Binary/
        │
        ▼  [游戏启动时 ConfigLoader.LoadAsync()]
LubanGetAllConfigBytes (Invoke 事件)
        │
        ▼  [Client: LubanClientLoaderInvoker / Server: LubanServerLoaderInvoker]
读取 .bytes 文件（编辑器: File.ReadAllBytesAsync; 运行时: ResourcesComponent）
        │
        ▼  ConfigDeserialize (Invoke 事件) → ConfigDeserialize_Luban
Activator.CreateInstance(type, new ByteBuf(bytes))
        │
        ▼  World.Instance.AddSingleton(category)
注册为全局单例
        │
        ▼  ResolveRef() → LubanConfig() (ILubanConfigSystem 回调)
完成
```

### 多端代码模式

| 模式 | 路径 | 说明 |
|------|------|------|
| Client | `CodeMode/Model/Client/LubanGen/` | 仅客户端字段的数据 |
| Server | `CodeMode/Model/Server/LubanGen/` | 仅服务端字段的数据 |
| ClientServer | `CodeMode/Model/ClientServer/LubanGen/` | 客户端+服务端共享数据 |

ExternalTypeUtil 提供 Unity 类型（float2/float3/float4/quaternion）与 Luban 自定义类型（f2/f3/f4/q4）间的转换工具。

### Demo 包结构（`.Template/cn.etetet.yiuilubandemo`）

Demo 包展示了标准的用法模式：

```
cn.etetet.yiuilubandemo/
├── Luban/Config/
│   ├── Base/{__beans__, __enums__, __tables__}.xlsx  # Schema 骨架
│   └── Datas/YIUITest/*.xlsx                         # 实际数据（YIUIClient/Server/Item）
├── CodeMode/Model/{Client|ClientServer|Server}/LubanGen/Config/YIUITest/
│   ├── EYIUIAccessFlag.cs      # 枚举
│   ├── EYIUIQuality.cs         # 枚举
│   ├── YIUIClientConfig.cs     # Bean
│   ├── YIUIItemConfig.cs       # Bean（含 exchange 子 Bean）
│   ├── YIUIItemConfigCategory.cs   # Category（Singleton + ILubanConfig）
│   └── ...
└── Scripts/
    ├── Model/Share/
    │   ├── YIUIItemConfig_Expend.cs       # 手写扩展：添加 TryGet 方法
    │   ├── YIUIItemConfigCategory_Expend.cs # 手写扩展：Category 额外索引
    │   └── YIUIItemExtendSingleton.cs     # 运行时扩展 Singleton
    └── Hotfix/Server/YIUIItemConfigCategorySystem.cs  # LubanConfigSystem 实现
```

**关键模式**：Luban 生成的类是 `partial class`，用户通过 `_Expend.cs` 文件添加自定义方法而不修改生成代码。

---

## 与其他包的关系

| 包 | 关系 |
|----|------|
| `cn.etetet.core` | 唯一直接依赖，提供 `Singleton`、`EventSystem`、`World`、`ETTask`、`AInvokeHandler` 等基础设施 |
| `cn.etetet.yiuilubangen` | 由本包工具生成/初始化，存放 luban.conf、生成脚本、生成的二进制配置和 C# 配置类 |
| `cn.etetet.yiuilubandemo` | 由本包工具生成，演示配置结构（YIUITest 表）和扩展模式 |
| `cn.etetet.excel` | 被本包替代（`ReplaceAll()` 工具执行迁移，清空旧包内容） |
| `cn.etetet.loader` | 通过 `LubanGetAllConfigBytes` / `LubanGetOneConfigBytes` Invoke 事件与加载器协作 |
| `cn.etetet.statesync` | `ReplaceExcelInit()` 会修改其 `InitHelper.cs` 中的导出触发方式（`ExcelEditor.Init()` → ET 菜单项） |
| `cn.etetet.yiuiframework` | Editor 工具使用 `YIUIFramework.Editor` 中的 Odin 基础设施（`BaseYIUIToolModule`、`OdinMenuEditorWindow`） |
| `cn.etetet.startconfig` | 通过 `LubanHelper.StartConfigs` 列表区分 StartConfig 的特殊加载路径 |

---

## 关键流程图

### 首次初始化流程

```
点击 "创建 LubanGen包"
  └─▶ LubanTools.InitGen()
        ├─▶ ReplaceAll()  // 迁移旧 Excel 系统（删 cn.etetet.excel、改 statesync/loader）
        ├─▶ 复制 .Template/cn.etetet.yiuilubangen → Packages/cn.etetet.yiuilubangen
        │     └─▶ 创建空目录 Luban/Config/Datas 和 Luban/Config/Base/Defines
        └─▶ LubanGen()    // 生成第一次代码
```

### 日常配置导出流程

```
菜单 ET/Excel/ExcelExporter
  └─▶ LubanTools.LubanGen()
        ├─▶ ClearAll()                     // 反射清空旧的 ILubanConfig Singleton 实例
        └─▶ CreateLubanConf()
              ├─▶ 扫描所有 cn.etetet.* 包的 Luban/*/Base/luban.conf
              ├─▶ 为每个配置集合聚合 __tables__.xlsx、__beans__.xlsx、__enums__.xlsx、Defines/
              ├─▶ 将聚合后的 schemaFiles 注入 luban.conf（JSON 修改）
              ├─▶ 并行执行各 ps1 脚本（Luban CLI，20秒超时）
              ├─▶ 成功: 删除空目录、刷新 AssetDatabase、可选转 Unix 换行符
              └─▶ 失败: 从备份目录还原所有 LubanGen .cs 文件
```

### 运行时配置加载流程

```
ConfigLoader.LoadAsync()
  │
  ├─▶ EventSystem.Invoke<LubanGetAllConfigBytes>()
  │     └─▶ LubanClientLoaderInvoker / LubanServerLoaderInvoker
  │           编辑器：GlobalConfig.CodeMode → File.ReadAllBytesAsync
  │           运行时：ResourcesComponent.LoadAssetAsync<TextAsset>
  │
  ├─▶ 并发（Task.Run × N）：
  │     LoadOneConfig(type, bytes)
  │       └─▶ EventSystem.Invoke<ConfigDeserialize>(ConfigType.Luban)
  │             └─▶ Activator.CreateInstance(type, new ByteBuf(bytes))
  │             └─▶ World.Instance.AddSingleton(category)
  │
  ├─▶ ResolveRef()
  │     ├─▶ 第一遍：所有 ILubanConfig.ResolveRef()（解析跨表 id→对象引用）
  │     └─▶ 第二遍：EntitySystemSingleton 查找 ILubanConfigSystem 回调热更代码
  │
  └─▶ ConfigProcess()
        └─▶ 扫描 [ConfigProcess] 类型，实例化并注册 World Singleton
```

---

## 代码示例

### 访问配置数据

```csharp
// 运行时访问（所有配置已加载）
var itemConfig = YIUIItemConfigCategory.Instance.Get(itemId);
if (itemConfig == null) { LubanLog.Error(category, itemId); return; }

// 编辑器工具中访问（直接从磁盘读取，不调用 ResolveRef）
var category = LubanEditorConfigCategory.Get<YIUIItemConfigCategory>("ClientServer");
var item = category.Get(100);
```

### 扩展 Luban 生成类（partial class 模式）

```csharp
// YIUIItemConfig_Expend.cs（手写，不受代码生成覆盖）
public partial class YIUIItemConfig
{
    public bool IsValid() => Id > 0 && !string.IsNullOrEmpty(Name);
}

// YIUIItemConfigCategorySystem.cs（Hotfix 中的回调）
public class YIUIItemConfigCategorySystem : LubanConfigSystem<YIUIItemConfigCategory>
{
    protected override void LubanConfig(YIUIItemConfigCategory self)
    {
        // 所有配置加载完后执行，可以跨表访问其他 Category
    }
}
```

### 添加新的配置后处理器

```csharp
[ConfigProcess]
public class MyConfigPostProcess : ASingleton, ISingletonAwake
{
    public void Awake()
    {
        // 在所有 Category 加载、ResolveRef 完成后执行
        // 用于全局数据预计算、索引构建等
    }
}
```

---

## 关键设计决策

1. **解耦序列化与加载**：`ConfigLoader` 不直接读文件，而是通过 `EventSystem.Invoke` 触发 Invoker，使得 Client 和 Server 可以各自实现不同的文件加载方式（Resources、AssetBundle、文件系统等）。

2. **并发反序列化**：在 `DOTNET || UNITY_STANDALONE` 模式下使用 `Task.Run` 并发反序列化多个配置文件，提升加载性能。ByteBuf 本身线程安全（只读操作）。

3. **备份还原机制**：代码生成前备份所有 LubanGen .cs 文件到系统临时目录（`Path.GetTempPath()/LubanBackup`），失败时自动还原，避免中间状态破坏编译。

4. **跨平台脚本执行**：支持 Windows (PowerShell 20s超时/bat) 和 Linux/Mac (pwsh/bash) 两套脚本，通过 `RuntimeInformation.IsOSPlatform` 判断。可选 `m_ToUnixEOL` 标志统一换行符。

5. **编辑器与运行时共用模型**：`ILubanConfig`、`ConfigAttribute` 等接口/标签位于 Model/Share 中，编辑器和运行时均可使用。`LubanEditorConfigCategory` 用 `#if UNITY_EDITOR` 条件编译隔离编辑器专用逻辑。

6. **partial class 扩展模式**：Luban 生成的所有类均为 `partial class`，用户创建 `_Expend.cs` 文件添加自定义方法，代码重新生成不会覆盖手写扩展。

7. **StartConfig 特殊路径**：启动配置（机器/进程/场景/Zone）存放在独立路径，与普通业务配置分离，支持按环境（Localhost/Release）切换。

8. **动态 schemaFiles 注入**：不是每个包维护一份完整的 luban.conf，而是仅保存配置意图（JSON 中的其他字段），在导出时由工具扫描所有包并动态注入 `schemaFiles` 数组，实现配置的分包管理。
