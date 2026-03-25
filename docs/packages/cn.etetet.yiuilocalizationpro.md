# cn.etetet.yiuilocalizationpro

## 概述

**版本**: 3.0.5
**分类**: UI/YIUI
**描述**: YIUI 多语言本地化系统，基于 I2 Localization 2.8.20 f2 封装，深度集成 ET 框架的 YIUI UI框架。
**依赖**: 无外部 package 依赖（通过代码引用 ET、YIUIFramework、I2.Loc 等）

---

## 目录结构

```
cn.etetet.yiuilocalizationpro/
├── Runtime/
│   ├── I2LocalizeMgr.cs              # 核心单例管理器（YIUISingleton 1100）
│   ├── I2LocalizeHelper.cs           # 常量/Helper（资源名前缀 "I2_"）
│   ├── Manager/                      # LocalizationManager 静态类（分部文件）
│   │   ├── LocalizationManager.cs               # 初始化、版本、WebService
│   │   ├── LocalizationManager_Language.cs      # 语言切换、代码映射
│   │   ├── LocalizationManager_Translation.cs   # 翻译查询、LocalizeAll
│   │   ├── LocalizationManager_Sources.cs       # 数据源注册/移除
│   │   ├── LocalizationManager_Targets.cs       # 本地化目标管理（mLocalizeTargets）
│   │   ├── LocalizationManager_Parameters.cs    # 参数替换系统
│   │   ├── LocalizationManager_Format.cs        # 格式化工具
│   │   ├── LocalizationManager_RTL.cs           # 从右到左语言支持
│   │   └── LocalizationManager_SystemLanguage.cs # 系统语言检测
│   ├── LanguageSource/               # 语言数据源
│   │   ├── LanguageSourceData.cs             # 核心数据容器 + Awake/OnDestroy
│   │   ├── LanguageSourceAsset.cs            # Unity ScriptableObject 封装
│   │   ├── LanguageSource.cs                 # MonoBehaviour 组件
│   │   ├── LanguageSourceData_Terms.cs       # Term CRUD、字典同步、翻译查询、Fallback
│   │   ├── LanguageSourceData_Languages.cs   # 语言列表管理
│   │   ├── LanguageSourceData_Import_CSV.cs  # CSV 导入（Import_CSV）
│   │   ├── LanguageSourceData_Export_CSV.cs  # CSV 导出
│   │   ├── LanguageSourceData_Import_Google.cs # Google Sheet 导入
│   │   ├── LanguageSourceData_Export_Google.cs # Google Sheet 导出
│   │   ├── LanguageSourceData_Assets.cs      # Asset 引用管理
│   │   └── LanguageSourceData_Misc.cs        # 分类/Key工具方法
│   ├── YIUI/                         # YIUI 框架集成层
│   │   ├── I2/
│   │   │   ├── UIDataBindTextI2Base.cs   # 多语言文本绑定基类
│   │   │   ├── UIDataBindI2Text.cs       # Unity Text 组件绑定
│   │   │   ├── UIDataBindTextI2TMP.cs    # TextMeshPro 组件绑定（#if TextMeshPro）
│   │   │   └── UIDataBindImageI2.cs      # Image 多语言图片绑定（异步加载）
│   │   ├── Event/
│   │   │   └── EventView.cs              # EventView_ChangeLanguage 事件
│   │   └── YIUIConstAsset_I2.cs          # 多语言配置项（DefaultLanguage等）
│   ├── Targets/                      # 本地化目标适配器
│   │   ├── ILocalizeTarget.cs        # 抽象基类 ScriptableObject
│   │   ├── ILocalizeTargetDesc.cs    # 目标描述接口
│   │   ├── LocalizeTarget_UnityUI_Text.cs      # Unity UI.Text
│   │   ├── LocalizeTarget_UnityUI_Image.cs     # Unity UI.Image
│   │   ├── LocalizeTarget_UnityUI_RawImage.cs  # Unity UI.RawImage
│   │   ├── LocalizeTarget_TextMeshPro_UGUI.cs  # TMP UGUI
│   │   ├── LocalizeTarget_TextMeshPro_Label.cs # TMP 3D
│   │   ├── LocalizeTarget_UnityStandard_AudioSource.cs  # AudioSource
│   │   ├── LocalizeTarget_UnityStandard_Prefab.cs       # Prefab 替换
│   │   ├── LocalizeTarget_UnityStandard_Child.cs        # 子物体替换
│   │   ├── LocalizeTarget_NGUI_*.cs    # NGUI 适配（条件编译）
│   │   └── LocalizeTarget_2DToolKit_*.cs # 2DToolKit 适配（条件编译）
│   ├── Google/                       # Google Translate/Sheet 集成
│   │   ├── GoogleLanguages.cs        # 语言列表、代码转换
│   │   ├── GoogleTranslation.cs      # 翻译请求
│   │   └── TranslationJob_*.cs       # 翻译任务（GET/POST/WEB）
│   ├── Utils/                        # 工具类
│   │   ├── AutoChangeCultureInfo.cs  # 自动切换 CultureInfo
│   │   ├── CoroutineManager.cs       # 协程管理（LocalizeAll 使用）
│   │   ├── CustomLocalizeCallback.cs # 自定义回调
│   │   ├── HindiFixer.cs             # 印地语修复
│   │   ├── I2Utils.cs                # 通用工具
│   │   └── LocalizationParamsManager.cs # 参数管理 MonoBehaviour
│   ├── Configurables/
│   │   ├── PersistentStorage.cs      # 持久化存储（PlayerPrefs 封装）
│   │   └── SpecializationManager.cs  # 特化版本管理（[i2s_] 标签）
│   ├── Localize.cs                   # MonoBehaviour：驱动 UI 组件本地化
│   ├── LocalizeDropdown.cs           # Dropdown 本地化
│   ├── LocalizationReader.cs         # 本地化数据读取
│   ├── LanguageData.cs               # 语言数据结构（名称+代码+启用状态）
│   └── TermData.cs                   # Term 数据结构（Key+各语言翻译数组）
├── Editor/                           # 编辑器工具
│   ├── Localization/                 # I2 Localization 编辑器窗口（多分部文件）
│   │   ├── LocalizationEditor.cs                       # 主窗口
│   │   ├── LocalizationEditor_Languages.cs             # 语言管理页
│   │   ├── LocalizationEditor_Terms.cs                 # Term 管理页
│   │   ├── LocalizationEditor_Spreadsheet_Google.cs    # Google Sheet 页
│   │   ├── LocalizationEditor_Spreadsheet_Local.cs     # 本地 CSV 页
│   │   └── LocalizationEditor_Tools*.cs                # 工具页（分类/字符集/合并等）
│   ├── Inspectors/                   # Inspector 扩展
│   ├── YIUIAutoTool/UII2Localization/
│   │   ├── UII2LocalizationModule.cs       # YIUI 自动工具模块（多语言 Tab）
│   │   ├── YIUILocalizationCreate.cs       # Excel → I2Localization 生成
│   │   └── YIUILocalizationExport.cs       # LanguageSourceData → CSV 按语言拆分导出
│   ├── EditorTools.cs
│   ├── PostProcessBuild_ANDROID.cs   # Android 构建后处理
│   ├── PostProcessBuild_IOS.cs       # iOS 构建后处理
│   ├── PostProcessBuild_UnloadLanguages.cs # 构建时卸载非目标语言
│   ├── Toolbar/YIUILocalizationExcelToolBar.cs # 工具栏快捷入口
│   └── UpgradeManager.cs             # 升级管理（CreateLanguageSources）
├── Assets/
│   ├── Editor/I2Localization/        # 编辑器下全量数据
│   │   ├── I2Languages.asset         # LanguageSourceAsset（编辑器使用）
│   │   └── I2_AllSource.csv          # 全量 CSV（编辑器备份）
│   └── GameRes/I2Localization/       # 运行时资源（按语言拆分）
│       ├── I2_Chinese.csv
│       └── I2_English.csv
└── CodeMode/                         # Luban 生成代码（仅 .meta，实际代码由 Luban 生成到 HotfixView）
```

---

## 核心类与接口

### I2LocalizeMgr（主管理器）

- **命名空间**: `I2.Loc`
- **继承**: `YIUIMonoSingleton<I2LocalizeMgr>`, `IResourceManager_Bundles`
- **YIUISingleton 优先级**: 1100（确保在 UI 系统之前初始化）
- **RequireComponent**: `LanguageSource`（同一 GameObject 上必须有 LanguageSource）

| 成员 | 类型 | 说明 |
|------|------|------|
| `CurrentLanguage` | `string` 属性 | 当前语言名（只读） |
| `m_DefaultLanguage` | `string` | 默认语言（来自 YIUIConstHelper.Const.I2DefaultLanguage） |
| `m_AllLanguage` | `List<string>` | 已加载的语言列表（动态维护） |
| `m_UseRuntimeModule` | `bool` | 编辑器是否模拟运行时模式 |
| `MgrAsyncInit()` | `ETTask<bool>` | 异步初始化 |
| `LoadLanguage(string, bool)` | `ETTask` | 动态异步加载语言 CSV |
| `SetLanguage(string, bool)` | `bool` | 切换语言（派发 EventView_ChangeLanguage） |
| `SetLanguage(int)` | `bool` | 按索引切换语言 |
| `CheckLanguage(string)` | `bool` | 检查语言是否已加载 |
| `LoadFromBundle(string, Type)` | `Object` | IResourceManager_Bundles 实现，委托 YIUIInvoke |
| `GetHideAndDontSave()` | `bool` | 返回 false（不隐藏） |

**初始化流程（MgrAsyncInit）**:
1. `GetDefaultLanguage()` — partial 方法，外部注入逻辑（默认从 YIUIConstHelper 读）
2. 检查 `m_DefaultLanguage` 非空，否则报错返回 false
3. 获取同 GameObject 上的 `LanguageSource` 组件
4. **编辑器非运行时模式**（`!m_UseRuntimeModule`）：直接 `LocalizationManager.RegisterSourceInEditor()` + `UpdateAllLanguages()` + `SetLanguage(defaultLanguage)`
5. **运行时/模拟运行时**：`m_SourceData.Awake()` + `await LoadLanguage(defaultLanguage, true)`

**LoadLanguage 流程**:
```
LoadLanguage(language, setCurrent)
  ├── CheckLanguage() → 已存在则跳过（防重）
  ├── GetLanguageAssetName() → "I2_" + language（如 "I2_Chinese"）
  ├── YIUIInvokeEntity_Load<TextAsset> → 异步加载 CSV 文件
  ├── Import_CSV(text, Replace) → 解析填充 mTerms/mLanguages
  ├── setCurrent=true → SetLanguage(language)
  └── YIUIInvokeEntity_Release → 释放 TextAsset 资源
```

---

### LocalizationManager（静态分部类）

核心静态服务，I2 Localization 的核心翻译引擎。

| 方法 | 说明 |
|------|------|
| `GetTranslation(term, ...)` | 获取翻译字符串，支持 RTL/参数替换 |
| `TryGetTranslation(term, out string, ...)` | 尝试获取翻译（不抛异常，返回 bool） |
| `GetTermTranslation(term)` | `GetTranslation` 的别名 |
| `GetTranslatedObject<T>(assetName)` | 获取翻译对应的资源对象 |
| `SetLanguageAndCode(name, code)` | 底层语言切换：持久化存储、更新 IsRight2Left、触发 LocalizeAll |
| `LocalizeAll(Force)` | 批量刷新所有场景中的 `Localize` 组件（运行时通过协程延迟一帧） |
| `GetAllLanguages()` | 返回所有启用语言列表 |
| `HasLanguage(string)` | 检查语言是否存在（支持模糊匹配区域） |
| `SelectStartupLanguage()` | 启动时自动选择语言：已存档 → 系统语言 → 第一个可用 |
| `InitializeIfNeeded()` | 懒加载初始化（编辑器下注册 PlayMode 钩子） |
| `GetTermData(term)` | 获取 TermData 对象 |
| `GetTermsList(category)` | 获取指定分类的所有 Term Key |
| `GetCategories()` | 获取所有分类列表 |
| `PreviewLanguage(language)` | 编辑器预览语言（不持久化）|

**关键静态变量**:
```csharp
static string mCurrentLanguage;         // 当前语言名
static string mLanguageCode;            // 当前语言代码（如 "zh"）
static CultureInfo mCurrentCulture;     // 当前文化信息
public static bool IsRight2Left;        // RTL 标志
public static bool HasJoinedWords;      // 无空格语言标志（中日泰）
public static event OnLocalizeCallback OnLocalizeEvent;  // 全局本地化回调
public static List<ILocalizeTargetDesc> mLocalizeTargets; // 已注册的 Target 描述符
```

**翻译查询缓存链**:
```
GetTranslation(term)
  → TryGetTranslation(term)
    → 遍历 Sources[i].TryGetTranslation(term, overrideLanguage)
      → GetLanguageIndex(CurrentLanguage) → 获取语言索引 idx
      → GetTermData(term) → mDictionary[term] → TermData
      → TermData.GetTranslation(idx, specialization)
        → SpecializationManager.GetSpecializedText()
        → 去除 [i2nt] 标签
      → 缺失翻译处理（Fallback/ShowWarning/Empty/ShowTerm）
```

---

### LanguageSourceData（数据容器）

I2 Localization 核心数据容器，持有所有翻译数据。

**主要字段**:

| 字段 | 类型 | 说明 |
|------|------|------|
| `mTerms` | `List<TermData>` | 所有 Term（序列化存储） |
| `mDictionary` | `Dictionary<string, TermData>` | mTerms 的哈希索引（非序列化，运行时构建） |
| `mLanguages` | `List<LanguageData>` | 语言列表（名称+代码+是否启用） |
| `CaseInsensitiveTerms` | `bool` | Term Key 是否大小写不敏感 |
| `OnMissingTranslation` | `MissingTranslationAction` | 缺失翻译处理策略 |
| `mIsGlobalSource` | `bool` | 是否为全局数据源 |
| `Google_WebServiceURL` | `string` | Google Web Service URL |
| `Google_SpreadsheetKey` | `string` | Google Sheet Key |

**缺失翻译处理策略（MissingTranslationAction）**:
- `Empty`：返回空字符串
- `Fallback`：尝试同语系其他区域 → 若无则取第一个有翻译的语言
- `ShowWarning`：返回 `<!-Missing Translation [term]-!>` 并 LogWarning
- `ShowTerm`：直接返回 Term Key 字符串

**Fallback 逻辑**（`TryGetFallbackTranslation`）:
1. 获取当前语言的基础代码（去掉区域部分，如 "zh-TW" → "zh"）
2. 优先查找同语系其他区域（如 "zh-CN" 补 "zh-TW" 的缺失）
3. 若仍无，取第一个启用且有翻译的语言

---

### TermData（翻译条目）

```csharp
[Serializable]
public class TermData {
    public string   Term;          // 唯一 Key（支持 Category/Key 路径）
    public eTermType TermType;     // 类型（Text/Font/Sprite/AudioClip/GameObject 等）
    public string   Description;  // 描述（仅编辑器序列化）
    public string[] Languages;    // 各语言翻译数组，按 mLanguages 顺序索引
    public byte[]   Flags;        // 各语言翻译标志（Normal/AutoTranslated）
}
```

**eTermType 枚举**（决定 ILocalizeTarget 处理方式）:
```csharp
Text, Font, Texture, AudioClip, GameObject, Sprite, Material, Child, Mesh, Object, Video
// 条件编译追加：UIAtlas, UIFont(NGUI) / TK2dFont(TK2D) / TextMeshPFont(TMP) / SVGAsset(SVG)
```

**特化版本（Specialization）**:
- 同一个 Term 可有针对不同平台/设备的特化翻译
- 格式：`[i2s_iOS]特化内容[/i2s_iOS]通用内容`
- 由 `SpecializationManager.GetSpecializedText()` 根据当前平台选择
- `[i2nt]` 标签：标记"不翻译"区域（运行时自动移除标签）

---

### Localize（UI 本地化驱动组件）

挂载在 UI GameObject 上，自动响应语言切换并刷新 UI 组件。

**核心字段**:

| 字段 | 说明 |
|------|------|
| `mTerm` | 主 Term Key |
| `mTermSecondary` | 副 Term Key（通常用于字体） |
| `FinalTerm` / `FinalSecondaryTerm` | 实际使用的 Term（可能从组件文本推断） |
| `PrimaryTermModifier` | 主翻译文本修改（大写/小写/首字母大写/标题大小写） |
| `TermPrefix` / `TermSuffix` | 翻译前后附加字符 |
| `mLocalizeTarget` | 当前绑定的 ILocalizeTarget（运行时创建） |
| `LocalizeEvent` | Unity 事件回调（可在 Inspector 配置回调修改翻译） |
| `AlwaysForceLocalize` | 强制每次 OnEnable 都重新本地化 |
| `AddSpacesToJoinedLanguages` | 中日泰等语言自动插入空格（便于换行） |

**OnLocalize 流程**:
```
OnLocalize(Force)
  ├── 检查 gameObject 激活、语言非空
  ├── 语言未变 && 无回调 → 跳过（LastLocalizedLanguage 缓存）
  ├── GetFinalTerms() → 确定 FinalTerm/FinalSecondaryTerm
  ├── GetTranslation(FinalTerm) → MainTranslation
  ├── 执行 LocalizeCallBack + LocalizeEvent（允许外部修改翻译内容）
  ├── ApplyLocalizationParams() → 处理 {param} 占位符
  ├── FindTarget() → 确保 mLocalizeTarget 有效
  ├── 应用 TermModification（ToUpper/ToLower等）
  ├── 应用 RTL fix（如果 IsRight2Left && !IgnoreRTL）
  ├── 应用 AddSpacesToJoinedLanguages（中文等）
  └── mLocalizeTarget.DoLocalize(this, main, secondary) → 写入 UI 组件
```

---

### ILocalizeTarget（本地化目标适配器）

**抽象基类**（ScriptableObject）:
```csharp
public abstract class ILocalizeTarget : ScriptableObject {
    public abstract bool IsValid(Localize cmp);
    public abstract void GetFinalTerms(Localize cmp, string Main, string Secondary,
                                        out string primaryTerm, out string secondaryTerm);
    public abstract void DoLocalize(Localize cmp, string main, string secondary);
    public abstract bool CanUseSecondaryTerm();
    public abstract bool AllowMainTermToBeRTL();
    public abstract bool AllowSecondTermToBeRTL();
    public abstract eTermType GetPrimaryTermType(Localize cmp);
    public abstract eTermType GetSecondaryTermType(Localize cmp);
}
```

**泛型子类 LocalizeTarget\<T\>**：持有 `T mTarget`，`IsValid()` 自动从 GameObject 获取组件。

**已实现的 Target 类型**:
| 类 | 组件 | 主 Term | 副 Term |
|----|------|--------|--------|
| `LocalizeTarget_UnityUI_Text` | `UI.Text` | 文本内容 | 字体名 |
| `LocalizeTarget_UnityUI_Image` | `UI.Image` | Sprite 名 | — |
| `LocalizeTarget_TextMeshPro_UGUI` | `TextMeshProUGUI` | 文本内容 | 字体名 |
| `LocalizeTarget_UnityStandard_AudioSource` | `AudioSource` | AudioClip 名 | — |
| `LocalizeTarget_UnityStandard_Prefab` | `Transform` | Prefab 名 | — |

---

### UIDataBindTextI2Base（YIUI 数据绑定基类）

- **命名空间**: `YIUIFramework`
- **继承**: `UIDataBindSelectBase`
- **作用**: 结合 YIUI 数据绑定系统与 I2 多语言，当绑定数据变化时自动更新 UI 文本

**核心字段**:

| 字段 | 说明 |
|------|------|
| `m_I2Key` | 多语言 Term Key（延迟序列化 `[Delayed]`） |
| `m_JointFormat` | 格式化拼接模板（`{0}` 为多语言内容，`{1}` 起为动态数据） |
| `m_ChangeEnabled` | 是否联动 GameObject 的 Enabled 状态 |
| `m_NumberPrecision` | 是否启用数字精度格式化（float/double 用 `m_NumberPrecisionStr`） |
| `m_NumberPrecisionStr` | 数字格式化字符串（默认 "F1"） |
| `m_LastI2Key` / `m_I2Content` | 翻译内容缓存（Key 变化时才重查） |

**OnValueChanged 逻辑**:
```
无 m_I2Key → 直接用绑定数据值作为文本
有 m_I2Key → GetI2Content(key) 查翻译
  有 m_JointFormat → string.Format(jf, [i2Content, data0, data1, ...])
  无 m_JointFormat → string.Format(i2Content, [data0, data1, ...])
```

> ⚠️ **已知限制**: `GetI2Content` 仅在 `key != m_LastI2Key` 时重查，`//TODO 无法实施切换语言` 注释表明语言切换后若 Key 未变则缓存不失效。运行时语言切换依赖 `LocalizeAll()` 触发 `Localize` 组件，而非本类直接监听事件。

**派生类**:
- `UIDataBindI2Text`：绑定 Unity `Text`
- `UIDataBindTextI2TMP`：绑定 `TextMeshProUGUI`（`#if TextMeshPro`）
- `UIDataBindImageI2`：绑定 `Image`（Key → Sprite 资源名，异步加载）

---

### YIUILocalizationCreate / YIUILocalizationExport（编辑器工具流）

**Excel → I2 生成流程**（`CreateI2LocalizationByXlsx`）:
```
读取 I2_AllSource.xlsx（Luban/Config/Datas/）
  │
  ├── 解析第一行：Key / Type / Desc / Language0 / Language1 / ...
  ├── 逐行读取 Term 数据
  │   ├── 跳过 ## 注释行
  │   ├── 检查 Key 重复、空格（强制删除）
  │   ├── 补齐空白语言列
  │   └── 自动补齐（I2AutoComplete=true 时用注释语言填充空翻译）
  │
  ├── languageSourceData.Import_CSV() → 填充 LanguageSourceData
  │
  ├── YIUILocalizationExport.ExportAllCsv()
  │   ├── 导出 I2_AllSource.csv（Editor/I2Localization/）
  │   └── 按语言分别导出 I2_{Language}.csv（GameRes/I2Localization/）
  │
  ├── 生成 I2 Key 枚举/常量脚本（BuildScriptWithSelected）
  ├── 生成 LocalizationCheck.json（Luban 验证用）
  └── 触发 ET/Excel/ExcelExporter（Luban 代码生成）
```

**ExportAllCsv 流程**:
```
Export_CSV(data, null) → 全量 I2_AllSource.csv
清空 GameRes/I2Localization/ 目录
foreach language in data.mLanguages:
    Export_CSV(data, language.Name) → I2_{language}.csv（只含单语言列）
```

CSV 格式：`Key,Type,Desc,Language`（逗号分隔，含引号转义）

---

### EventView_ChangeLanguage（语言切换事件）

```csharp
namespace ET.Client {
    public struct EventView_ChangeLanguage {
        public string Language;
    }
}
```

通过 `ET.EventSystem.Instance?.YIUIInvokeEntitySync(Entity, new EventView_ChangeLanguage {...})` 派发，任何注册此事件的 ET View 可响应语言切换（如刷新动态文本、切换字体集等）。

---

### YIUIConstAsset（多语言配置项）

在 `YIUIConstAsset` partial 类中扩展（分组 `BoxGroup("多语言设置")`）：

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `I2UseRuntimeModule` | false | 编辑器是否模拟运行时（使用 CSV 加载而非 Asset） |
| `I2DefaultLanguage` | "Chinese" | 默认语言（必须配置，否则初始化失败） |
| `I2CodeCommentLanguage` | "Chinese" | 代码生成的注释语言 |
| `I2CloseSecondaryTranslation` | false | 关闭字体二级语言功能 |
| `I2NullTranslationError` | false | 空翻译是否报错（开发期检查） |
| `I2AutoComplete` | false | 未填写翻译自动补齐（格式见 I2AutoCompleteFormat） |
| `I2AutoCompleteFormat` | "{0}[Null]" | 自动补齐格式，{0} 为注释语言内容 |

---

### I2LocalizeHelper（常量）

```csharp
public const string I2ResAssetNamePrefix = "I2_";
// 运行时加载语言资源名: "I2_Chinese", "I2_English" 等
#if UNITY_EDITOR
public const string I2GlobalSourcesEditorPath = ".../I2Languages.asset";
#endif
```

---

## 实现原理

### 运行时语言加载（核心流程）

```
游戏启动
    │
    ▼
I2LocalizeMgr.MgrAsyncInit()
    │
    ├── GetDefaultLanguage() → 读取 YIUIConstHelper.Const.I2DefaultLanguage
    │
    ├── m_SourceData.Awake()
    │     ├── LocalizationManager.AddSource(this) → 注册数据源
    │     ├── UpdateDictionary() → 构建 mDictionary
    │     ├── UpdateAssetDictionary() → 构建资产字典
    │     └── LocalizeAll(true) → 刷新已有 Localize 组件
    │
    └── LoadLanguage("Chinese", setCurrent=true)
            ├── GetLanguageAssetName() → "I2_Chinese"
            ├── YIUIInvokeEntityAsync<Load, TextAsset> → 异步加载
            ├── Import_CSV(text, Replace) → 解析 CSV 填充数据
            ├── UpdateAllLanguages() → 更新 m_AllLanguage 列表
            ├── SetLanguage("Chinese")
            │     ├── LocalizationManager.CurrentLanguage = "Chinese"
            │     │     └── SetLanguageAndCode()
            │     │           ├── PersistentStorage.SetSetting_String("I2 Language", name)
            │     │           ├── 更新 IsRight2Left, HasJoinedWords
            │     │           └── LocalizeAll() → 协程下一帧刷新所有 Localize
            │     └── YIUIInvokeEntitySync(EventView_ChangeLanguage)
            │           └── 通知 ET 客户端事件系统
            └── YIUIInvokeEntitySync(Release) → 释放 TextAsset
```

### 翻译查询链

```
LocalizationManager.GetTranslation("UI/BtnOK")
    → InitializeIfNeeded()
    → foreach source in Sources:
        source.TryGetTranslation("UI/BtnOK", out translation)
            → GetLanguageIndex(CurrentLanguage="Chinese") → idx=0
            → mDictionary["UI/BtnOK"] → TermData
            → TermData.Languages[0] = "确定"
            → SpecializationManager.GetSpecializedText("确定", null) → "确定"
    → ApplyRTLfix（如 IsRight2Left）
    → return "确定"
```

### Term Key 命名规范

- 支持 Category/Key 路径：`UI/BtnOK`，`Items/Sword`
- `ValidateFullTerm()` 自动规范化：反斜杠→斜杠、去首尾空格、移除 EmptyCategory 前缀
- `GetCategoryFromFullTerm(term)` 提取分类，`GetKeyFromFullTerm(term)` 提取纯 Key
- Key 不得含空格（生成时强制删除并报错提示）

### 数据双轨制

| 特性 | 编辑器非运行时（`I2UseRuntimeModule=false`） | 运行时/模拟运行时 |
|------|------|------|
| 数据来源 | `I2Languages.asset`（Editor Asset，RegisterSourceInEditor） | CSV 文件（通过 YooAssets 动态加载） |
| 语言切换 | Inspector Dropdown 可实时切换 | 必须代码调用 `SetLanguage()` |
| 动态加载语言 | 不支持（调用 LoadLanguage 报错） | 支持 `await LoadLanguage()` |
| 初始化入口 | `RegisterSourceInEditor()` | `SourceData.Awake()` + CSV |
| 资源占用 | 全量 Asset 常驻 | 按需加载，加载后 TextAsset 立即释放 |

---

## 关键流程图

### 多语言文本数据绑定（UIDataBindTextI2Base）

```
绑定数据变更
    │
    ▼
OnValueChanged()
    ├── 无 DataSelectDic → SetText(""), SetEnabled(false)
    ├── 无 m_I2Key → 直接 SetText(dataValue.ToString())
    └── 有 m_I2Key → GetI2Content(m_I2Key)
            ├── key != m_LastI2Key → LocalizationManager.GetTranslation(key) 重查
            └── 有 m_JointFormat:
                    string.Format(jf, [i2Content, data0, data1, ...])
                无 m_JointFormat:
                    string.Format(i2Content, [data0, data1, ...])
                          │
                          ▼
                    SetText(result) → 写入 UI 组件
```

### 语言切换完整流程

```
I2LocalizeMgr.SetLanguage("English")
    ├── CheckLanguage("English") → 已加载检查
    ├── m_CurrentLanguage == "English" → 跳过（幂等）
    ├── LocalizationManager.CurrentLanguage = "English"
    │     └── SetLanguageAndCode("English", "en")
    │           ├── PersistentStorage 持久化
    │           ├── mCurrentCulture = CreateCultureForCode("en")
    │           ├── IsRight2Left = IsRTL("en") → false
    │           ├── HasJoinedWords = false
    │           └── LocalizeAll(false)
    │                 └── CoroutineManager.Start(Coroutine_LocalizeAll())
    │                       └── yield return null（下一帧）
    │                             └── DoLocalizeAll()
    │                                   ├── 遍历所有 Localize 组件
    │                                   │     └── localize.OnLocalize(false)
    │                                   └── OnLocalizeEvent?.Invoke()
    └── YIUIInvokeEntitySync(EventView_ChangeLanguage{Language="English"})
              └── ET 事件系统 → 通知客户端监听者
```

### Excel 到运行时 CSV 的工具链

```
策划编辑 I2_AllSource.xlsx
    │
    ▼ YIUI AutoTool → "多语言" → "生成"
    │
YIUILocalizationCreate.CreateI2LocalizationByXlsx()
    ├── 读取 xlsx → 解析 Terms + Languages
    ├── Import_CSV → 填充 LanguageSourceData
    ├── ExportAllCsv()
    │     ├── Editor/I2Localization/I2_AllSource.csv（全量）
    │     └── GameRes/I2Localization/
    │           ├── I2_Chinese.csv（仅中文列）
    │           └── I2_English.csv（仅英文列）
    ├── 生成 I2 Key 常量脚本（供代码安全引用）
    ├── 生成 LocalizationCheck.json（Luban 验证）
    └── 触发 Luban Excel 导出（配置表代码生成）
```

---

## 依赖关系

### 本包依赖

| 依赖 | 用途 |
|------|------|
| `cn.etetet.yiuiframework` | `YIUIMonoSingleton`, `UIDataBindSelectBase`, `YIUIConstHelper`, `YIUISingleton` |
| `cn.etetet.yiuiinvoke` | `YIUIInvokeEntity_Load`, `YIUIInvokeEntity_Release`, `YIUIInvokeEntity_LoadSprite`, `YIUIInvokeEntity_CoroutineLock` |
| `cn.etetet.core` (ET) | `ETTask`, `EventSystem.Instance`, `Entity`, `NoContext()` |
| Sirenix OdinInspector | Inspector 标注（`[ReadOnly]`, `[ValueDropdown]`, `[ShowInInspector]` 等） |
| TextMeshPro（可选） | `UIDataBindTextI2TMP`、`LocalizeTarget_TextMeshPro_UGUI`（`#if TextMeshPro`） |
| NPOI（Editor Only） | 读取 xlsx 文件（`YIUILocalizationCreate`） |

### 被其他包依赖

- 需要多语言功能的 UI 包可直接调用 `LocalizationManager.GetTranslation(key)`
- YIUI 绑定：在预制体上使用 `UIDataBindI2Text` / `UIDataBindTextI2TMP` / `UIDataBindImageI2`
- 所有挂有 `Localize` 组件的 UI GameObject 均会响应 `LocalizeAll()` 广播

---

## 资源规范

| 资源 | 命名 | 路径 | 用途 |
|------|------|------|------|
| 编辑器全量数据 Asset | `I2Languages.asset` | `Assets/Editor/I2Localization/` | I2 编辑器工具、编辑器模式初始化 |
| 编辑器全量 CSV | `I2_AllSource.csv` | `Assets/Editor/I2Localization/` | 版本管理备份 |
| 运行时语言 CSV | `I2_{语言名}.csv` | `Assets/GameRes/I2Localization/` | YooAssets 管理，按需加载 |
| 数据源 Excel | `I2_AllSource.xlsx` | `Luban/Config/Datas/` | 策划编辑，工具读取 |
| Luban 验证 JSON | `LocalizationCheck.json` | `Luban/Config/Datas/` | Luban 配置表用于校验 Key 合法性 |

---

## 注意事项与已知问题

1. **语言切换缓存问题**: `UIDataBindTextI2Base.GetI2Content()` 仅在 Key 变化时重新查询（有 `//TODO 无法实施切换语言` 注释），语言切换后若 Key 未变则缓存不失效。动态文本需额外监听 `EventView_ChangeLanguage` 手动刷新，或通过 `LocalizeAll` 广播中的 `Localize` 组件间接触发。

2. **初始语言加载 TODO**: 代码注释明确标注三个未实现功能：
   - 上次选择语言记忆（目前只读配置默认值）
   - 服务器拉取语言包逻辑
   - 语言设置界面即时加载缺失语言包

3. **运行时必须提前加载语言**: `SetLanguage()` 前必须确保对应语言的 CSV 已通过 `LoadLanguage()` 加载，否则 `CheckLanguage()` 失败报错。如需自动加载可传 `load=true` 参数。

4. **RTL 支持**: 内置 `RTLFixer`、`HindiFixer` 及 `IsRight2Left` 标志，实际效果取决于字体配置。`IgnoreRTL=true` 可对特定 `Localize` 组件关闭 RTL 处理。

5. **编辑器 RegisterSourceInEditor vs 运行时 SourceData.Awake()**: 两种模式初始化路径不同，切换时需注意 `m_UseRuntimeModule` 配置。运行时模式下 `SourceData.Awake()` 会触发 `LocalizationManager.AddSource()` + `LocalizeAll(true)`。

6. **Term Key 不允许空格**: `YIUILocalizationCreate` 在读取 xlsx 时会强制删除 Key 中的空格并报错，需手动修正源数据。

7. **特化版本系统**: `[i2s_iOS]...[/i2s_iOS]` 语法允许同一 Key 在不同平台/设备有不同翻译，由 `SpecializationManager` 在运行时选择正确版本。

8. **语言 CSV 导出按语言拆分**: 每个语言一个独立 CSV（如 `I2_Chinese.csv` 只含中文列），按需加载可避免加载所有语言数据。加载后 TextAsset 立即通过 `YIUIInvokeEntity_Release` 释放，节省内存。
