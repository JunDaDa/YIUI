# cn.etetet.yiuicodeanalysis

**版本**: 0.0.1
**分类**: UI/YIUI
**命名空间**: 无（纯 DLL 依赖包）
**依赖**: 无
**Unity 最低版本**: 2022.3
**作者**: YIYI (https://github.com/LiShengYang-yiyi/YIUI/tree/YIUI-ET9.0)

## 概述

YIUI 代码分析工具包，是一个**纯 Editor 依赖库包**，不包含任何 .cs 源代码。本包的作用是向 Unity Editor 层提供以下第三方 DLL 依赖，供 YIUI 代码生成工具和 Excel 配置表工具使用：

1. **Roslyn C# 代码分析 API**：用于解析、分析 C# 源代码（代码格式化、语法树生成等）
2. **NPOI**：用于读写 Excel 文件（.xls/.xlsx）

---

## 目录结构

```
cn.etetet.yiuicodeanalysis/
├── Editor/
│   ├── ET.YIUI.CodeAnalysis.Editor.asmdef   # Editor 程序集定义
│   ├── CodeAnalysis/                         # Roslyn 代码分析 DLL
│   │   ├── Microsoft.CodeAnalysis.dll           # Roslyn 核心 API
│   │   ├── Microsoft.CodeAnalysis.CSharp.dll    # Roslyn C# 语言支持
│   │   ├── System.Collections.Immutable.dll     # 不可变集合（Roslyn 依赖）
│   │   └── System.Reflection.Metadata.dll       # 反射元数据（Roslyn 依赖）
│   └── NPOI/
│       ├── NPOI.2.5.6/
│       │   ├── NPOI.dll                         # NPOI 核心
│       │   ├── NPOI.OOXML.dll                   # OOXML 格式支持（.xlsx）
│       │   ├── NPOI.OpenXml4Net.dll             # OpenXml 解析
│       │   └── NPOI.OpenXmlFormats.dll          # OpenXml 格式定义
│       ├── Portable.BouncyCastle.1.8.9/
│       │   └── BouncyCastle.Crypto.dll          # 加密库（NPOI 依赖，处理加密 Excel）
│       └── SharpZipLib.1.3.3/
│           └── ICSharpCode.SharpZipLib.dll      # ZIP 压缩库（NPOI 依赖）
└── Ignore.ET.YIUI.CodeAnalysis.asmdef          # Runtime 占位程序集（条件编译保护）
```

---

## 程序集配置

### Editor 程序集（ET.YIUI.CodeAnalysis.Editor）

```json
{
  "name": "ET.YIUI.CodeAnalysis.Editor",
  "includePlatforms": ["Editor"],
  "references": ["ET.Core", "ET.Core.Editor", "ET.Model", "ET.ModelView", "ET.Hotfix", "ET.HotfixView"],
  "allowUnsafeCode": true,
  "autoReferenced": true
}
```

引用了全套 ET 框架程序集（Core/Model/Hotfix/View），说明代码分析工具需要感知整个项目的类型结构。

### Runtime 占位程序集（Ignore.ET.YIUI.CodeAnalysis）

```json
{
  "name": "Ignore.ET.YIUI.CodeAnalysis",
  "defineConstraints": ["IGNORE"],
  "autoReferenced": true
}
```

**重要**：该程序集使用了 `defineConstraints: ["IGNORE"]`，这意味着它只有在项目定义了 `IGNORE` 宏时才会编译。由于正常项目不会定义该宏，所以此程序集实际上**永远不会被编译**。这是一种标准的 Unity Package 占位技术，目的是：
- 避免 Unity 报告"包没有 Runtime 程序集"的警告
- 在包管理器中正常显示包信息
- 不向运行时增加任何代码

---

## DLL 详情

### Roslyn CodeAnalysis（代码分析）

| DLL | 版本 | 用途 |
|-----|------|------|
| `Microsoft.CodeAnalysis.dll` | Roslyn | 核心 API：语法树、符号、诊断 |
| `Microsoft.CodeAnalysis.CSharp.dll` | Roslyn | C# 语言特定解析：SyntaxFactory、CSharpCompilation |
| `System.Collections.Immutable.dll` | - | 不可变集合，Roslyn 内部使用 |
| `System.Reflection.Metadata.dll` | - | PE/元数据读取，Roslyn 依赖 |

#### 实际使用示例（来自 cn.etetet.yiuinumeric）

```csharp
// 使用 Roslyn 格式化生成的 C# 代码
affectContent = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree
    .ParseText(affectContent)
    .GetRoot()
    .NormalizeWhitespace()
    .ToFullString();
```

这是 Roslyn 在本项目中最典型的使用模式：**代码格式化**。生成器先通过字符串拼接产生原始 C# 代码，再用 Roslyn 解析成语法树并重新格式化（`NormalizeWhitespace()`），输出整洁的代码文件。

### NPOI（Excel 读写）

| DLL | 版本 | 用途 |
|-----|------|------|
| `NPOI.dll` | 2.5.6 | NPOI 主库：IWorkbook、ISheet、IRow、ICell 等核心 API |
| `NPOI.OOXML.dll` | 2.5.6 | 支持 .xlsx 格式（OpenXML） |
| `NPOI.OpenXml4Net.dll` | 2.5.6 | OpenXML 包解析底层 |
| `NPOI.OpenXmlFormats.dll` | 2.5.6 | OpenXML 格式数据类 |
| `BouncyCastle.Crypto.dll` | 1.8.9 | 处理加密保护的 Excel 文件 |
| `ICSharpCode.SharpZipLib.dll` | 1.3.3 | ZIP 解压（.xlsx 本质是 ZIP 压缩包） |

#### 实际使用示例（来自 cn.etetet.yiuinumeric）

```csharp
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

// 打开 .xlsx 文件（只读模式，允许文件共享）
var book = new XSSFWorkbook(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
var sheet = book.GetSheetAt(0);
var rowCount = sheet.LastRowNum + 1;

// 遍历行（从第4行开始，前3行为标题/注释）
for (int i = 3; i < rowCount; i++)
{
    var row = sheet.GetRow(i);
    // 通过扩展方法获取单元格字符串值
    var cellValue = row.GetString(0);
}

// NPOIExtensions 扩展方法（定义在 cn.etetet.yiuinumeric）
public static class NPOIExtensions
{
    public static string GetString(this IRow self, int index) { ... }
}
```

---

## 与其他 Package 的关系

```
cn.etetet.yiuicodeanalysis
  └── 被以下 Editor 工具依赖（通过 ET.YIUI.CodeAnalysis.Editor 程序集）：
        cn.etetet.yiuiframework (Editor/YIUIAutoTool)  // YIUI 代码生成工具
        cn.etetet.excel (Editor)                       // Excel 配置表导入工具
        cn.etetet.yiuinumeric (Editor/Window)          // Numeric 生成工具（Roslyn+NPOI）
        cn.etetet.yiuilocalizationpro (Editor)         // 本地化工具（NPOI 读取 Excel）
```

### 依赖关系图

```
[cn.etetet.yiuicodeanalysis] ─→ [Microsoft.CodeAnalysis.CSharp]
                              ─→ [NPOI 2.5.6]
                                    └─→ [BouncyCastle.Crypto]
                                    └─→ [ICSharpCode.SharpZipLib]

[cn.etetet.yiuiframework Editor] ──→ [cn.etetet.yiuicodeanalysis]
[cn.etetet.excel Editor]         ──→ [cn.etetet.yiuicodeanalysis]
[cn.etetet.yiuinumeric Editor]   ──→ [cn.etetet.yiuicodeanalysis]
[cn.etetet.yiuilocalizationpro Editor] ─→ [cn.etetet.yiuicodeanalysis]
```

---

## 核心功能说明

### 1. Roslyn 的使用模式（代码格式化）

本项目中 Roslyn 主要用于**代码格式化**而非完整的语义分析：
1. 代码生成器通过字符串拼接产生原始 C# 代码
2. `CSharpSyntaxTree.ParseText()` 解析为语法树
3. `.GetRoot().NormalizeWhitespace()` 整理缩进和空白
4. `.ToFullString()` 转回格式化后的字符串写入文件

### 2. NPOI 的使用模式（Excel 解析）

Excel 文件解析流程：
1. 通过 `FileStream` 打开 .xlsx 文件（FileShare.ReadWrite 避免锁文件）
2. 创建 `XSSFWorkbook`（OOXML 格式）
3. `GetSheetAt(0)` 获取第一个工作表
4. 按行遍历，通常前3行为头部（标题/说明/类型），从第4行（index=3）开始读数据
5. 各包自行实现 `NPOIExtensions` 扩展方法简化单元格读取

---

## 注意事项

- **仅 Editor 有效**：所有 DLL 都在 `Editor/` 目录下，不会打包到游戏包体
- **运行时不可用**：游戏运行时无法使用 Roslyn 或 NPOI，仅限编辑器工具使用
- **无版本锁定**：`package.json` 中 `dependencies` 为空，DLL 直接打入包中
- **文件共享模式**：读取 Excel 时使用 `FileShare.ReadWrite`，支持 Excel 文件被其他程序同时打开

---

## 修订历史

- **Round 1**：初始文档，描述包结构和 DLL 清单
- **Round 2**：补充 `Ignore.ET.YIUI.CodeAnalysis.asmdef` 的 `IGNORE` 约束机制说明；添加 Roslyn 和 NPOI 在项目中的实际代码示例；明确 Roslyn 在本项目中以代码格式化为主要用途；补充 cn.etetet.yiuilocalizationpro 和 cn.etetet.yiuinumeric 为依赖方；确认 asmdef `overrideReferences: false` 使 DLL 通过目录自动引用（无需在 precompiledReferences 中显式列出）
