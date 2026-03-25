# cn.etetet.excel

## 概述

`cn.etetet.excel` 是 ET 框架的 Excel 导出工具包。该包本身不包含运行时代码，而是作为一个**工具配置包**存在，描述了将 Excel 表格导出为代码和配置数据的工具链集成。

- **版本**：3.0.0
- **Unity 要求**：2022.3+
- **作者**：tanghai（ET 框架作者）
- **包 ID**：13（packagegit.json）

## 目录结构

```
cn.etetet.excel/
├── package.json          # 包元数据和依赖声明
└── packagegit.json       # Git 依赖配置（无依赖）
```

## 核心功能

### Excel 导出工具链

该包描述了一个 Excel → 代码/数据 的导出工作流，用于：
- 读取 Excel 配置表（如技能、道具、关卡、服务器配置等）
- 自动生成对应的 C# 数据类代码（分部类）
- 导出序列化配置数据文件（用于运行时加载）

这是典型的 **数据驱动设计（Data-Driven Design）** 模式，将策划/运维的 Excel 配置直接转换为程序可用的类型安全数据。

## Excel 文件命名约定

项目中 Excel 文件使用特定后缀标识作用范围：

| 后缀 | 说明 | 示例 |
|------|------|------|
| `@s` | 服务器端专用 | `StartMachineConfig@s.xlsx` |
| `@c` | 客户端专用 | `UIConfig@c.xlsx` |
| `@cs` | 客户端+服务器共用 | `ItemConfig@cs.xlsx` |
| （无后缀）| 默认配置 | `UnitConfig.xlsx` |

各包的 Excel 文件存放在 `Excel/` 子目录中：
```
cn.etetet.startconfig/Excel/StartConfig/Example/
├── StartMachineConfig@s.xlsx   # 机器配置（服务器）
├── StartProcessConfig@s.xlsx   # 进程配置（服务器）
├── StartSceneConfig@s.xlsx     # 场景配置（服务器）
└── StartZoneConfig@s.xlsx      # 区服配置（服务器）

cn.etetet.ai/Excel/
└── AIConfig.xlsx               # AI 行为配置

cn.etetet.statesync/Excel/
└── UnitConfig.xlsx             # 单位配置
```

## 生成代码模式

ET Excel 导出器使用 **分部类（partial class）** 模式：

### 导出器生成部分（自动生成，不可手动修改）
```csharp
// 自动生成的数据类（字段来自 Excel 列）
public partial class StartProcessConfig : IConfig
{
    public int Id { get; set; }
    public int MachineId { get; set; }
    public int Port { get; set; }
    // ... 其他 Excel 列对应字段
}

// 自动生成的配置管理类（单例）
public partial class StartProcessConfigCategory : ConfigSingleton<StartProcessConfigCategory>
{
    // 提供 Get(int id) 查询方法
}
```

### 手动扩展部分（业务逻辑）
```csharp
// 开发者手动编写的扩展（cn.etetet.startconfig/Scripts/Model/Server/StartProcessConfig.cs）
public partial class StartProcessConfig
{
    // 聚合导航：通过 MachineId 关联到 StartMachineConfig
    public StartMachineConfig StartMachineConfig =>
        StartMachineConfigCategory.Instance.Get(this.MachineId);

    // 计算属性：从关联对象获取网络地址
    public string InnerIP => this.StartMachineConfig.InnerIP;
    public string OuterIP => this.StartMachineConfig.OuterIP;

    // 计算属性：将 IP+Port 合并为 IPEndPoint
    private IPEndPoint ipEndPoint;
    public IPEndPoint IPEndPoint
    {
        get
        {
            if (ipEndPoint == null)
                this.ipEndPoint = NetworkHelper.ToIPEndPoint(this.InnerIP, this.Port);
            return this.ipEndPoint;
        }
    }

    // 生命周期钩子（所有 IConfig 实现类都有）
    public override void EndInit() { }
}
```

## 配置访问模式

运行时通过 ConfigSingleton 访问配置数据：

```csharp
// 通过 ID 获取单条配置
StartMachineConfig machine = StartMachineConfigCategory.Instance.Get(machineId);

// 通过 Category 遍历所有配置
foreach (var config in StartSceneConfigCategory.Instance.GetAll())
{
    // 处理每条配置...
}
```

## 依赖关系

| 依赖项 | 说明 |
|--------|------|
| 无运行时依赖 | 该包不依赖其他 ET 包 |

## 与其他包的关系

| 相关包 | 关系说明 |
|--------|----------|
| `cn.etetet.yiuiluban` | 替代方案：使用 Luban 作为 Excel 导出工具，功能更强 |
| `cn.etetet.yiuilubangen` | Luban 代码生成工具，与本包功能互补 |
| `cn.etetet.startconfig` | 消费者：包含服务器启动配置的 Excel 文件和生成的 partial 类 |
| `cn.etetet.ai` | 消费者：包含 AIConfig.xlsx |
| `cn.etetet.statesync` | 消费者：包含 UnitConfig.xlsx |

## 实现原理

作为工具包（Tool Package），`cn.etetet.excel` 的实际导出逻辑不在客户端运行时，而是在编辑器或 CI 构建阶段执行：

```
Excel 文件（*.xlsx）
    ↓ [excelexporter 工具]
    ├── C# 分部类（自动生成，编译进游戏）
    │   ├── {ConfigName}.cs         ← 数据字段
    │   └── {ConfigName}Category.cs ← 单例管理类
    └── 二进制/JSON 配置文件（打包为 AssetBundle，运行时加载）
```

编译产物位于：`obj/Debug/ET.Excel.Editor.dll`（Unity 编辑器集成）

## Round 2 补充说明

**第一轮理解的修正：**
- 该包与 Luban 不是并列替代关系，而是**更早期的 ET 原生方案**。实际项目中两者可能并存。
- ET.Excel.Editor.dll 的存在表明该包**确有 Unity 编辑器集成代码**，但代码不在 Packages 目录中（可能在 Assets/Scripts/Editor 或通过 submodule 引入）。
- 分部类模式是 ET 框架的核心设计：**导出器生成数据结构，开发者编写业务逻辑**，两者通过 partial class 无缝结合。

## 备注

- 该包内容极简，仅包含包描述文件，无可执行代码
- 实际的 excelexporter 工具位于 ET 框架的工具链目录中
- 在 ET 框架的包管理系统中，该包 ID 为 13
- 与 Luban（`cn.etetet.yiuiluban`）相比，这是更早期的 ET 内置导出方案
- Excel 文件的 `@s`/`@c`/`@cs` 后缀约定是 ET 框架的标准配置分发机制
