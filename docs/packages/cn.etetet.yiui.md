# cn.etetet.yiui

## 概述

**YIUI 框架主包（配置/入口包）**

`cn.etetet.yiui` 是 YIUI UI 框架的顶层元包（meta-package），作为框架的入口和配置中心。本包本身不包含运行时 C# 代码，而是：

1. 提供全局 Sprite Atlas 编辑器配置
2. 定义 `Ignore.ET.YIUI` Assembly Definition（仅在 `IGNORE` 宏定义时编译，实际上永不编译）
3. 作为包管理系统中 YIUI 框架的注册锚点（Id: 1002, Name: "YIUI.YIUI"）
4. 作为模板包存在于 `cn.etetet.yiuiframework` 的 `.Template` 目录中（Round 2 补充）

实际的框架运行时代码分布在以下相关包中：
- `cn.etetet.yiuiframework` — 框架核心逻辑
- `cn.etetet.yiuiinvoke` — 框架 Invoke 扩展
- `cn.etetet.yiuiyooassets` — YooAssets 集成

---

## 目录结构

```
cn.etetet.yiui/
├── package.json                         # 包元数据
├── packagegit.json                      # 包管理 ID (Id:1002, Name:"YIUI.YIUI")
├── README.md                            # 框架简介和链接
├── Ignore.ET.YIUI.asmdef               # 忽略编译的 asmdef（需要 IGNORE 宏）
└── Assets/
    └── Editor/
        └── YIUI/
            └── GlobalSpriteAtlasSettings.txt  # 全局 Sprite Atlas 配置
```

---

## 核心文件说明

### package.json

| 字段 | 值 |
|------|-----|
| name | cn.etetet.yiui |
| displayName | ET.YIUI |
| version | 0.0.0 |
| unity | 2022.3 |
| category | UI/YIUI |
| dependencies | 无直接声明依赖 |

### Ignore.ET.YIUI.asmdef

Assembly Definition 的完整配置：

```json
{
    "name": "Ignore.ET.YIUI",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": ["IGNORE"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

关键设置：
- `defineConstraints: ["IGNORE"]` — 仅在定义了 `IGNORE` 宏时才编译，这是 ET 框架惯用的"永不实际编译"模式
- `autoReferenced: true` — 若此程序集编译则自动被其他程序集引用（但由于 IGNORE 约束，实际不会）
- `references: []` — 无程序集引用，这是一个空壳
- `noEngineReferences: false` — 允许引用 Unity 引擎程序集（UnityEngine.dll 等）

### GlobalSpriteAtlasSettings.txt

全局 Sprite Atlas 编辑器配置，供 `YIUIFramework.Editor.UIAtlasModule` 使用。

**公共设置（SpriteAtlasSettings）：**

| 字段 | 值 | 说明 |
|------|-----|------|
| AtlasType | 0 | Master Atlas（主图集）|
| IncludeInBuild | true | 包含在构建中 |
| BlockOffset | 1 | 块偏移 |
| EnableRotation | false | 禁用旋转（保持 UV 一致性）|
| EnableTightPacking | false | 禁用紧凑打包 |
| Padding | 4 | 4px 边距（防止纹理渗色）|
| GenerateMipMaps | false | 不生成 Mip（UI 通常不需要）|
| Readable | false | 非可读（节省内存）|
| sRGB | true | 使用 sRGB 色彩空间 |
| FilterMode | 1 | Bilinear 过滤 |

**平台设置：**

| 平台 | BuildTargetName | 最大尺寸 | Format | 说明 |
|------|----------------|----------|--------|------|
| Default | DefaultTexturePlatform | 2048 | `UInt64.MaxValue` (自动) | Crunch 质量50 |
| PC | Standalone | 2048 | 29 (DXT5/BC3) | Crunch 质量50 |
| Android | Android | 2048 | 50 (ETC2_RGBA8) | Crunch 质量50 |
| iPhone | iPhone | 2048 | 48 (ASTC_6x6) | Crunch 质量50 |

> **Round 2 补充 - Format 枚举值含义：**
> - `18446744073709551615` = `UInt64.MaxValue` 表示"自动选择"（Unity 内部处理）
> - `29` = `TextureFormat.DXT5`（PC 端 BC3 压缩，支持 Alpha 通道）
> - `50` = `TextureFormat.ETC2_RGBA8`（Android 端 ETC2，支持 Alpha 通道）
> - `48` = `TextureFormat.ASTC_6x6`（iOS 端 ASTC，高质量自适应压缩）

---

## 实现原理

### 包的定位

`cn.etetet.yiui` 采用"壳包"模式：

```
cn.etetet.yiui（入口/配置）
    ├── cn.etetet.yiuiframework（框架核心）
    ├── cn.etetet.yiuiinvoke（Invoke 驱动层）
    ├── cn.etetet.yiuiyooassets（资源加载集成）
    ├── cn.etetet.yiuilocalizationpro（本地化）
    ├── cn.etetet.yiuigm（GM 工具）
    ├── cn.etetet.yiuireddot（红点系统）
    ├── cn.etetet.yiuieffect（特效）
    └── cn.etetet.yiui3ddisplay（3D 展示）
```

### 模板关系（Round 2 补充）

`cn.etetet.yiui` 本身是由 `cn.etetet.yiuiframework` 的 `.Template/cn.etetet.yiui/` 目录生成的模板包。模板目录包含与本包相同的文件：
- `Ignore.ET.YIUI.asmdef`
- `package.json`
- `packagegit.json`
- `README.md`

这意味着当框架需要更新或重新生成包时，`cn.etetet.yiuiframework` 内的 Package Manager 工具可以从模板重新创建此包。这是 ET 框架包管理系统的标准机制。

### Sprite Atlas 全局配置机制

编辑器工具 `UIAtlasModule`（位于 `cn.etetet.yiuiframework`）读取此配置文件，为整个项目的所有 UI Sprite Atlas 提供统一的打包参数，避免每个 Atlas 单独配置造成的不一致。

```csharp
// UIAtlasModule 使用的配置类（位于 cn.etetet.yiuiframework）
namespace YIUIFramework.Editor
{
    public class UIAtlasModule
    {
        [Serializable]
        public class GlobalSpriteAtlasSettings
        {
            public UISpriteAtlasSettings SpriteAtlasSettings;
            public UIPlatformSettings Default;
            public UIPlatformSettings PC;
            public UIPlatformSettings Android;
            public UIPlatformSettings iPhone;
        }
    }
}
```

配置文件通过 Unity 的 JSON 序列化格式（含 `$id`/`$type` 多态引用）读取，`$type` 使用 `"index|FullTypeName, AssemblyName"` 格式实现类型注册。

---

## 依赖关系

### 直接依赖
本包 `package.json` 中无声明依赖。

### 被依赖关系
作为 YIUI 框架的顶层包，其他业务模块通过 `cn.etetet.yiuiframework` 等子包间接使用本包提供的配置。

### 模板来源
`cn.etetet.yiuiframework/.Template/cn.etetet.yiui/` 提供了本包的标准模板。

---

## 关键流程

### 编辑器 Sprite Atlas 生成流程

```
Unity Editor 触发 Atlas 生成
    → UIAtlasModule 读取 GlobalSpriteAtlasSettings.txt
    → 解析 JSON（含多态 $type 引用）
    → 应用公共设置（AtlasType=Master, Padding=4, EnableRotation=false...）
    → 按构建目标平台（DefaultTexturePlatform/Standalone/Android/iPhone）应用对应压缩格式
    → 对每个平台：设置 MaxTextureSize=2048, CrunchedCompression=true, Quality=50
    → 生成 .spriteatlas 资源文件并写入磁盘
```

### 包管理注册流程

```
PackageManager 工具初始化
    → 扫描所有 packagegit.json 文件
    → 发现 Id=1002, Name="YIUI.YIUI"（来自本包）
    → 将 YIUI 框架注册到包版本管理系统
    → 支持版本检查和更新操作
```

---

## 注意事项

1. **此包不含运行时代码**，仅作为配置和入口包存在
2. Atlas 配置中 `EnableRotation: false` 是关键设置，开启后可能导致 UI 图集 UV 错误
3. Crunch 压缩（`CrunchedCompression: true`）会增加打包时间，但显著减小包体；如遇到构建性能问题可调整 `compressionQuality`（当前为50）
4. `packagegit.json` 中的 Id(1002) 由 `cn.etetet.packagemanager` 用于包版本管理
5. `Ignore.ET.YIUI.asmdef` 中的 `autoReferenced: true` 在实际环境中无效（因为 IGNORE 宏不会被定义），不会影响编译
6. Default 平台的 Format 值 `18446744073709551615`（UInt64.MaxValue）表示 Unity 自动选择压缩格式，而非实际的纹理格式枚举值

---

## 参考资料

- [YIUI 框架官方文档](https://lib9kmxvq7k.feishu.cn/wiki/ES7Gwz4EAiVGKSkotY5cRbTznuh)
- [YIUI-ET9.0 GitHub](https://github.com/LiShengYang-yiyi/YIUI/tree/YIUI-ET9.0)
- [ET-Packages 组织](https://github.com/ET-Packages/cn.etetet.yiui)
