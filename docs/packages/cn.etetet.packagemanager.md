# cn.etetet.packagemanager

## 概述

ET框架的 **编辑器专用包管理工具**，版本 1.0.7。基于 Odin Inspector 构建可视化界面，提供 ET 包的版本查询、一键更新、包库浏览、新包创建等功能。所有功能均为 Editor-Only，不参与运行时逻辑。

> **依赖**：需要 `ODIN_INSPECTOR` 宏（Sirenix.OdinInspector）才能启用全部功能。无 Odin 时提供简化版 EditorWindow。

---

## 目录结构

```
cn.etetet.packagemanager/
├── package.json
└── Editor/
    ├── Assets/                              # 持久化 ScriptableObject 资产
    │   ├── PackageInfoAsset.asset           # 包最新版本缓存 + 禁用列表
    │   └── PackageVersionAsset.asset        # 当前项目所有包版本快照
    ├── Base/                                # 基础框架类
    │   ├── BasePackageToolModule.cs         # 所有功能模块的基类
    │   ├── BaseCreateModule.cs              # 创建类模块基类（含 Initialize/OnDestroy）
    │   ├── BaseTreeMenuItem.cs              # OdinMenuTree 菜单项基类
    │   ├── BaseTemplate.cs                  # 模板代码基类
    │   ├── TemplateEngine.cs                # 文本模板渲染引擎
    │   ├── EditorHelper.cs                  # Editor 通用工具（路径转换等）
    │   └── ScriptableLoader.cs              # ScriptableObject 加载器
    ├── Helper/                              # 核心业务辅助类
    │   ├── PackageHelper.cs                 # 包版本查询/禁用管理（核心）
    │   ├── PackageInfoAsset.cs              # PackageInfoAsset + PackageLastVersionData 定义
    │   ├── PackageRequestAdd.cs             # 异步 UPM 安装包请求（单个）
    │   ├── PackageRequestRemove.cs          # 异步 UPM 移除包请求
    │   ├── PackageRequestAddAndRemove.cs    # 批量先移除后安装（批量覆盖更新）
    │   ├── PackageRequestTarget.cs          # 查询单个包最新版本请求
    │   └── PackageExecuteMenuItemHelper.cs  # 执行 ET 标准化菜单操作序列
    ├── Tools/                               # 编辑器工具类
    │   ├── Prefs.cs                         # EditorPrefs 封装（BoolPrefs/StringPrefs/EnumPrefs）
    │   ├── SbPool.cs                        # StringBuilder 对象池
    │   ├── StrUtil.cs / StrUtil_StrChunk.cs # 字符串工具
    │   ├── StrConv.cs / TypeExt.cs          # 类型扩展工具
    │   └── UnityTipsHelper.cs               # 弹窗/提示辅助
    ├── UnityWindow/                         # 独立 Unity 窗口（非 Odin 降级方案）
    │   ├── ETPackageUpdateWindow.cs         # 包更新提示窗口（无 Odin 降级）
    │   └── CreateWindow/ETPackageCreateWindow.cs # 包创建向导窗口（无 Odin 降级）
    └── Window/                              # Odin 主工具窗口及模块
        ├── ETPackageAutoTool.cs             # 主窗口（OdinMenuEditorWindow）
        ├── Attribute/ETPackageMenuAttribute.cs # 模块菜单注册 Attribute
        ├── VersionModule/                   # 版本管理模块
        │   ├── ETPackageVersionModule.cs    # 主类：筛选/展示/同步生成
        │   ├── ETPackageVersionModule_UpdateAll.cs # partial：批量更新
        │   ├── Extend/ETPackageVersionModule_Sync.cs
        │   ├── Helper/PackageVersionHelper.cs # PackageVersionAsset 加载/管理
        │   └── Assets/                      # 数据模型
        │       ├── PackageVersionAsset.cs   # 所有包版本快照 ScriptableObject
        │       ├── PackageVersionData.cs    # 单个包版本数据（含操作按钮）
        │       └── DependencyInfo.cs        # 依赖项数据
        ├── HubModule/                       # 包库浏览模块
        │   ├── ETPackageHubModule.cs        # 包库主模块
        │   ├── Helper/PackageHubHelper.cs   # Hub 数据加载
        │   ├── Assets/PackageHubAsset.cs    # Hub 包数据资产
        │   └── Hub/PackageCategoryModule.cs # 分类模块
        ├── UpdateModule/ETPackageUpdateModule.cs # 更新文档模块
        ├── CreateModule/                    # 一键创建包模块
        └── Document/ETPackageDocumentModule.cs # 文档链接模块
```

---

## 核心类与接口

### 主窗口

#### `ETPackageAutoTool` (`OdinMenuEditorWindow`)

ET包管理自动化工具主窗口，通过菜单 `ET/ETPackage 包管理自动化工具` 打开（需 `ODIN_INSPECTOR`）。

**菜单构建机制**：
- 扫描 `ET.PackageManager.Editor` 程序集中所有标记 `[ETPackageMenuAttribute]` 的类
- 按 `Order` 字段排序后，通过反射构建泛型 `TreeMenuItem<T>` 菜单项
- 记忆上次选中菜单（`ETPackageAutoTool_LastSelectMenu` EditorPref）

| 静态方法 | 说明 |
|---|---|
| `OpenWindow()` | 打开工具窗口 |
| `CloseWindow()` | 关闭工具窗口 |
| `CloseWindowRefresh()` | 关闭并刷新 AssetDatabase（执行 `Assets/Refresh`）|
| `UnloadAllAssets()` | 卸载所有缓存资产（PackageHelper + PackageVersionHelper）|
| `ReLoadAllAssets()` | 重新加载所有缓存资产 |

---

### 核心辅助类

#### `PackageHelper` (静态类)

包版本查询与管理的核心类，所有模块共用。

**主要职责**：
- 维护 `m_CurrentRegisteredPackages`：当前 Unity 项目中已注册的所有 `cn.etetet.*` 包
- 维护 `PackageInfoAsset`：包含远端最新版本缓存和用户禁用包列表
- 通过 `Client.List()`（UPM API）批量请求所有包的最新版本

**关键方法**：

| 方法 | 说明 |
|---|---|
| `CheckUpdateAll(callback)` | 批量请求所有已注册包的最新版本（有时间间隔限制 `UpdateInterval`，避免频繁请求）|
| `CheckUpdateTarget(name, callback)` | 查询单个包的最新版本（优先从缓存读取；禁用包直接跳过）|
| `GetVersionToLong(version)` | 将 `A.B.C` 版本号转成 15 位 long 数便于比较（每段 D5 格式，如 `1.2.3` → `000010000200003`）|
| `GetPackageCurrentVersion(name)` | 获取当前已安装版本 |
| `GetPackageLastVersion(name)` | 获取缓存的远端最新版本 |
| `GetPackageInfo(name)` | 获取 Unity `PackageInfo` 对象 |
| `IsBanPackage(name)` | 判断包是否被禁止自动更新 |
| `BanPackage(name)` / `ReBanPackage(name)` | 禁用/解禁包的自动更新检测（持久化到 PackageInfoAsset）|
| `LoadAsset()` | 加载 PackageInfoAsset，不存在时自动创建；加载后调用 `GetAllRegisteredPackages()` |
| `Unload()` | 清除缓存引用（通常在关闭窗口后调用）|

**版本号规则**：格式必须为 `A.B.C`，每段最多 5 位数字（最大 99999）。

**时间间隔控制**（`CheckUpdateAll` 内部逻辑）：
```
currentTime - LastUpdateTime < UpdateInterval(3600s) → 跳过网络请求，直接返回 true
否则 → Client.List() → EditorApplication.update 轮询完成 → 更新缓存
```

---

#### `PackageVersionHelper` (静态类，需 `ODIN_INSPECTOR`)

管理 `PackageVersionAsset` 的加载与数据初始化。

**关键职责**：
- 加载或创建 `PackageVersionAsset.asset`
- 在 `LoadAllPackageInfoData()` 中遍历所有已注册 ET 包，构建 `PackageVersionData` 字典
- **构建反向依赖（DependenciesSelf）**：在正向依赖构建完后，反向注册"谁依赖了我"

```csharp
// 反向依赖构建示例逻辑：
// packageA 依赖 packageB → packageB.DependenciesSelf 中记录一条 packageA 的 DependencyInfo
```

| 方法 | 说明 |
|---|---|
| `LoadAsset()` | 加载/创建 PackageVersionAsset，并初始化所有包数据 |
| `Unload()` | 清除静态缓存 |
| `SaveAsset()` | SetDirty（标记资产已修改）|
| `GetPackageVersionData(name)` | 从 AllPackageVersionData 获取指定包数据 |

---

#### `PackageExecuteMenuItemHelper` (静态类)

封装安装/更新包后必须执行的标准化操作序列。

**`ETAll()` 完整调用链（有序）**：
```
ET_Init_RepairDependencies()         → ET/Init/RepairDependencies
ET_Loader_ReGenerateProjectFiles()   → ET/Loader/ReGenerateProjectFiles
ET_Loader_ReGenerateProjectAssemblyReference() → ET/Loader/ReGenerateProjectAssemblyReference
ET_Loader_UpdateScriptsReferences()  → ET/Loader/UpdateScriptsReferences
ET_Excel_ExcelExporter()             → (已关闭，注释掉，手动执行)
ET_Proto_Proto2CS()                  → ET/Proto/Proto2CS
```

> 每个方法内部都有 `try/catch`，执行失败只打日志不抛出异常，保证链不中断。

---

### 功能模块（`BasePackageToolModule` 的子类）

所有模块通过 `[ETPackageMenuAttribute(menuName, order)]` 注册到主窗口菜单。

#### `ETPackageVersionModule` - 版本管理（Order: 1000）

**核心功能**：
- 展示项目中所有 ET 包的当前版本 vs 最新版本，支持多维度筛选
- 单包更新（`PackageVersionData.CheckUpdateVersion()`）
- 批量更新所有可更新包（`UpdateAll()`，在筛选为 Update 时显示）
- 同步生成：将版本号和依赖写回各包 `package.json`

**筛选系统**：

| 枚举值 | 说明 |
|---|---|
| `EPackagesFilterType.All` | 显示所有包 |
| `EPackagesFilterType.ET` | 仅 ET 官方包（`cn.etetet.*`）|
| `EPackagesFilterType.Update` | 有可用更新的包 |
| `EPackagesFilterType.Req` | 正在请求版本中的包 |
| `EPackagesFilterType.Ban` | 已禁用自动检测的包 |
| `EPackagesFilterType.ReBan` | 已禁用但可解禁的包 |

`EPackagesFilterOperationType`：支持 **唯一**（单选）、**或**（OR）、**与**（AND）三种筛选组合模式。

**版本比较规则**（`CheckVersion`）：
- 大版本号（A）必须完全一致
- 中版本号（B）必须完全一致
- 小版本号（C）当前版本必须 ≥ 依赖版本

**同步生成流程**（`SyncPackages` → `UpdatePackagesInfo`）：
1. 遍历所有 `PackageVersionData`，调用 `CheckPackageChange()` 检测版本或依赖是否有变化
2. 有变化则读取 `Packages/{name}/package.json`（Newtonsoft.Json 解析）
3. 修改 `version` 字段和 `dependencies` 对象，写回文件
4. 调用 `PackageExecuteMenuItemHelper.ET_Init_RepairDependencies()` 修复依赖
5. 关闭窗口并刷新 AssetDatabase

**批量更新流程**（`UpdateAll` → `RequestUpdateAll`）：
1. 对每个可更新包调用 `UpdateDependencies()`：删除本地目录 + 更新反向依赖的版本记录
2. 收集所有包名到 `allName` 列表
3. 使用 `PackageRequestAddAndRemove(allName, ...)` 批量重装
4. 完成后执行 `ETAll()` 并刷新

#### `ETPackageHubModule` - 包库（`[ETPackageMenu("库")]`）

**核心功能**：浏览所有可用的 ET 包，按包的 `category` 字段分类展示，支持安装/更新操作。

**初始化流程**：
```
PackageHelper.CheckUpdateAll() → 获取所有包最新版本
    → PackageHubHelper.CheckUpdate() → 获取 Hub 包数据
        → CreateCategory() → 构建 OdinMenuTree 分类
```

**分类层级**：从包的 category 字段按 `/` 分割，构建多级 OdinMenuTree 菜单。特殊分类：
- `All`：显示所有包（始终排第一）
- `Other`：未分类包（排在最后）

#### `ETPackageCreateModule` - 创建包（`[ETPackageMenu("创建")]`）

**核心功能**：一键创建符合 ET 规范的新包脚手架。

**创建参数**：

| 参数 | 说明 |
|---|---|
| `PackageAuthor` | 作者名 |
| `PackageName` | 包名（自动规范化：小写 + 非字母字符移除，最终路径 `Packages/cn.etetet.{name}`）|
| `PackageId` | 模块 ID（1000 以下为 ET 官方保留）|
| `DisplayName` | 显示名称（推荐 `ET.{name}`）|
| `AssemblyName` | Runtime 程序集名称 |
| `Description` | 包描述 |
| `PackageCreateType` | 生成类型（`EPackageCreateType`：Runtime/Hotfix/Model/Editor/HotfixView/ModelView）|
| `RuntimeRefType` | Runtime 引用类型（`EPackageRuntimeRefType`）|
| `FolderType` | CodeMode 文件夹类型（`EPackageCreateFolderType`：Client/Server/Share）|

**生成内容**（由 `ETPackageCreateHelper.CreatePackage()` 驱动）：

| 生成物 | 条件 |
|---|---|
| `*.asmdef`（Runtime）| PackageCreateType 含 Runtime |
| `*.asmdef`（Editor）| PackageCreateType 含 Editor |
| `Scripts/Hotfix/{Client\|Server\|Share}/` 目录 | PackageCreateType 含 Hotfix |
| `Scripts/HotfixView/Client/` 目录 | PackageCreateType 含 HotfixView |
| `Scripts/Model/{Client\|Server\|Share}/` 目录 | PackageCreateType 含 Model |
| `Scripts/ModelView/Client/` 目录 | PackageCreateType 含 ModelView |
| `package.json` | 始终生成 |
| `packagegit.json` | 始终生成（Git 配置）|
| `.ignoreasmdef`（忽略用）| 始终生成 |

创建完成后调用 `PackageExecuteMenuItemHelper.ETAll()` 并刷新资源数据库。

#### `ETPackageUpdateModule` - 更新（`[ETPackageMenu("更新")]`）

展示更新说明文档链接的简单占位模块（实际更新在 `ETPackageVersionModule` 中进行）。

---

### 数据模型

#### `PackageVersionData`（Odin 序列化）

单个包版本信息的核心数据类，同时包含 Odin Inspector 按钮操作。

| 字段/属性 | 说明 |
|---|---|
| `Name` | 包名（只读）|
| `Version` | 当前版本（set 时自动计算 `VersionLong` 和 `VersionValue`，并过滤非数字字符）|
| `VersionLong` | 版本 long 值（用于快速大小比较）|
| `VersionValue` | 版本各段 `int[]`（用于增减版本号按钮）|
| `LastVersion` | 远端最新版本（set 时自动计算 `LastVersionLong`）|
| `LastVersionLong` | 最新版本 long 值 |
| `CanUpdateVersion` | `LastVersionLong > VersionLong` 即为 true |
| `IsETPackage` | 包名含 `cn.etetet.` |
| `IsBan` | 是否禁用更新检测 |
| `Dependencies` | 本包依赖的包列表（`List<DependencyInfo>`）|
| `DependenciesSelf` | 依赖本包的包列表（反向依赖，`List<DependencyInfo>`）|

**版本操作按钮**（Odin Inspector 中直接显示）：
- `↑大/↓大`（A 段）、`↑中/↓中`（B 段）、`↑小/↓小`（C 段）
- `重置`：从 `PackageVersionHelper.GetPackageVersionData()` 取原始版本恢复
- `请求`：手动触发 `ReqCheckUpdate()`，向 UPM 查询最新版本
- `禁/解`：禁用或解禁该包的自动更新检测

**单包更新流程**（`UpdateDependencies()` 私有方法）：
```
1. Directory.Delete(packagePath, true) 删除本地包目录
2. 遍历 DependenciesSelf（依赖我的包）：
   - 将其 Dependencies 中对应本包的版本号更新为 LastVersion
3. ETPackageAutoTool.CloseWindow()
4. new PackageRequestAdd(Name, callback) → UPM Client.Add()
5. callback: UnloadAllAssets → ETAll() → SaveAssets → Refresh
```

#### `DependencyInfo`

```csharp
public string SelfName;         // 所属包名
public string Name;             // 依赖目标包名
public string Version;          // 依赖版本
public bool DependenciesSelf;   // true = 反向依赖（"依赖我"），false = 正向依赖（"我依赖"）
```

#### `PackageInfoAsset` (ScriptableObject)

持久化存储（`PackageManager/Editor/Assets/PackageInfoAsset.asset`）：

| 字段 | 说明 |
|---|---|
| `AllLastPackageInfo` | `PackageLastVersionData[]` — 包名 + 最新版本的序列化数组 |
| `AllLastPackageInfoDic` | 同上的字典形式（内存访问，不序列化）|
| `BanPackageInfo` | 禁用包名数组（序列化）|
| `BanPackageInfoHash` | 禁用包名 HashSet（内存访问，不序列化）|
| `LastUpdateTime` | 上次请求 UTC 时间戳（Unix 秒）|
| `UpdateInterval` | 请求冷却间隔，默认 3600 秒 |

#### `PackageVersionAsset` (`SerializedScriptableObject`，需 Odin)

持久化存储（`PackageManager/Editor/Assets/PackageVersionAsset.asset`）：

| 字段 | 说明 |
|---|---|
| `AllPackageVersionData` | `Dictionary<string, PackageVersionData>` — 项目当前所有包的版本快照 |
| `LastUpdateTime` | 上次更新时间戳 |
| `UpdateInterval` | 更新间隔 |

---

## 关键流程

### 版本检查流程

```
打开 ETPackageAutoTool 窗口
        ↓
ETPackageVersionModule.Initialize()
        ↓
PackageHelper.CheckUpdateAll()
    │  检查距上次请求是否超过 UpdateInterval (3600s)
    │  未超过 → 直接回调 true（使用缓存）
    │  超过   → Client.List() → EditorApplication.update 轮询
        ↓
检查完成 → LoadAllPackageInfoData()
    │  从 PackageVersionAsset 深拷贝（SerializationUtility.CreateCopy）所有包数据
    │  验证依赖版本匹配性（大中版本号一致，小版本号 ≥ 依赖要求）
    │  不匹配时 Debug.LogError 警告
        ↓
LoadFilterPackageInfoData()
    │  按筛选条件 + 搜索正则过滤，填充 m_FilterPackageInfoDataList
        ↓
界面展示包列表（Odin TableList）
```

### 单包安装/更新流程

```
用户点击"有最新版本可更新"按钮
        ↓
UnityTipsHelper.CallBack 确认弹窗
        ↓
Directory.Delete(本地包目录)
        ↓
更新反向依赖方（DependenciesSelf）中对本包的版本记录
        ↓
PackageRequestAdd（UPM Client.Add）异步安装
        ↓
完成 → UnloadAllAssets → PackageExecuteMenuItemHelper.ETAll()
     → AssetDatabase.SaveAssets / Assets/Refresh
```

### 批量更新流程

```
筛选为 "Update" 时，"更新当前所有" 按钮出现
        ↓
确认弹窗
        ↓
遍历 m_FilterPackageInfoDataList：
    每个包 Directory.Delete + 更新反向依赖版本记录
        ↓
PackageRequestAddAndRemove(allNames, callback)
    先批量 Remove，再批量 Add（通过 UPM Client）
        ↓
完成 → UnloadAllAssets → ETAll() → SaveAssets → Refresh
```

### 创建新包流程

```
填写 PackageAuthor, PackageName, PackageId, DisplayName, AssemblyName, ...
        ↓
ETPackageCreateHelper.GetPackagePath() 规范化包名（全小写，移除非字母字符）
        ↓
EditorHelper.GetProjPath() 获取绝对路径
        ↓
CreateDirectory() 创建根目录（ForceCreate=true 时先删除旧目录）
        ↓
根据 PackageCreateType flags 逐个创建：
    Runtime → ETPackageCreatePackageAsmdefCode (模板渲染)
    Editor  → ETPackageCreatePackageAsmdefEditorCode
    Hotfix/Model → 创建 Client/Server/Share 子目录
    HotfixView/ModelView → 创建 Client 子目录
        ↓
生成 package.json、packagegit.json、.ignoreasmdef
        ↓
PackageExecuteMenuItemHelper.ETAll()
        ↓
关闭窗口，刷新资源
```

---

## 无 Odin 降级方案

当项目未安装 Odin Inspector 时，通过 `#if !ODIN_INSPECTOR` 注册以下简化窗口：

| 菜单项 | 类 | 功能 |
|---|---|---|
| `ET/ETPackage 更新检查` | `ETPackageUpdateWindow` | 展示可更新包列表（富文本标绿最新版本号）|
| `ET/ETPackage 创建` | `ETPackageCreateWindow` | 基于 EditorGUILayout 的包创建表单 |

两个窗口均调用同样的 `PackageHelper` 和 `ETPackageCreateHelper`，功能等价，仅 UI 较简陋。

---

## 依赖关系

| 依赖方向 | 说明 |
|---|---|
| → Odin Inspector（Sirenix）| 主窗口及大部分模块依赖 OdinInspector（`#if ODIN_INSPECTOR`）|
| → UnityEditor.PackageManager | 调用 `Client.List()`、`Client.Add()` 等 UPM API |
| → Newtonsoft.Json | 解析/修改 `package.json`（`JObject`）|
| 无运行时依赖 | 全部为 Editor-Only 代码，Runtime asmdef 为空 |

**被其他包依赖**：本包作为 ET 包管理基础设施，其他 ET 包不直接依赖本包代码，但发布流程（创建/更新包）都通过本包工具完成。

---

## 注意事项

1. **Odin Inspector 是强依赖**：未安装时仅有简化版窗口，大量功能不可用（包括版本管理模块）
2. **版本格式强制要求 A.B.C**：每段最多 5 位数字，否则 `GetVersionToLong()` 返回 0 且报错
3. **网络请求有冷却时间**：由 `UpdateInterval`（默认 3600s）控制，强制更新可手动将 `LastUpdateTime` 设为 0
4. **更新为覆盖更新**：`Directory.Delete` 后重新 UPM 拉取，本地修改会丢失，需提前处理
5. **批量更新顺序**：`PackageRequestAddAndRemove` 先全部 Remove 再全部 Add，中途不可操作 Unity
6. **包 ID 1000 以下为 ET 官方保留**：自定义包请向 ET 官方申请或使用 1000+
7. **菜单执行防护**：`PackageExecuteMenuItemHelper` 每个方法都用 try/catch 包裹，防止某个菜单不存在导致整个后续流程中断
8. **`PackageVersionAsset` 使用 Odin 序列化**：`SerializedScriptableObject` + `OdinSerialize`，不能用普通 `[SerializeField]` 访问其数据
