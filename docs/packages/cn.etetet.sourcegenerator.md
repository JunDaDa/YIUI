# cn.etetet.sourcegenerator

## 概述

ET框架的 **Roslyn Source Generator（源代码生成器）** 和 **Diagnostic Analyzer（诊断分析器）** 包。版本 3.0.1，依托 .NET Roslyn 编译器平台，在编译期自动生成样板代码，并通过静态分析强制执行 ET 框架的 ECS 架构约束。

---

## 目录结构

```
cn.etetet.sourcegenerator/
├── package.json                         # 包元数据
├── ET.SourceGenerator.dll.meta          # 预编译好的分析器 DLL（Unity 引用入口）
├── Runtime/                             # 运行时 Attribute 定义（Unity 程序集）
│   ├── ChildOfAttribute.cs              # 子实体父级类型约束
│   ├── ComponentOfAttribute.cs          # 组件父级类型约束
│   ├── DisableNewAttribute.cs           # 禁止 new 构造
│   ├── EnableAccessEntiyChildAttribute.cs # 允许访问 Entity Child
│   ├── EnableClassAttribute.cs          # Model 程序集中允许声明的普通类
│   ├── EnableMethodAttribute.cs         # 允许的方法
│   ├── EntitySystemOf.cs                # EntitySystemOfAttribute / LSEntitySystemOfAttribute
│   ├── FriendOfAttribute.cs             # 友元类
│   ├── StaticFieldAttribute.cs          # 静态字段标记
│   └── UniqueIdAttribute.cs             # 唯一 Id 约束
└── DotNet~/
    ├── ET.SourceGeneratorAttribute/     # 独立 .NET 项目：NoCut 属性
    └── ET.SourceGenerator/              # 核心分析器+生成器项目
        ├── Config/
        │   ├── AnalyzeAssembly.cs       # 目标程序集白名单
        │   ├── Definition.cs            # 全局常量（类型名、属性名、接口名）
        │   ├── DiagnosticCategories.cs  # 诊断分类
        │   ├── DiagnosticIds.cs         # 诊断编号 ET0001–ET1001
        │   └── DiagnosticRules.cs       # 所有诊断规则描述（Rule static class）
        ├── Generator/
        │   ├── ETSystemGenerator/
        │   │   ├── ETSystemGenerator.cs # 主生成器：EntitySystem/Event/MessageHandler
        │   │   └── AttributeTemplate.cs # 代码生成模板字典（5种Attribute→模板字符串）
        │   ├── ETGetComponentGenerator.cs           # GetXxx 扩展方法生成（当前Initialize已注释，实际未激活）
        │   └── ETEntitySerializeFormatterGenerator.cs # MemoryPack 序列化生成
        ├── Analyzer/                    # 20+ 个诊断分析器（见下表）
        ├── CodeFixer/                   # 代码修复提供者
        ├── AnalyzerGlobalSetting.cs     # 分析器全局开关（EnableAnalyzer）
        ├── AnalyzerHelper.cs            # 公共辅助扩展方法（25+个方法）
        └── StringHashHelper.cs          # 字符串 Hash 工具
```

---

## 核心类与接口

### Generator（代码生成器）

#### `ETSystemGenerator` (`ISourceGenerator`)

**作用**：扫描带有以下 Attribute 的方法，自动在同一静态分部类中生成对应的系统/处理器类。

| 目标 Attribute | 生成的类名格式 | 生成的类基类 |
|---|---|---|
| `[EntitySystem]` | `{ArgsTypesUnderLine}_{MethodName}System` | `{MethodName}System<TEntity,...>` |
| `[LSEntitySystem]` | `{ArgsTypesUnderLine}_{MethodName}System` | `{MethodName}System<TLSEntity,...>` |
| `[MessageHandler]` | `{ClassName}_{MethodName}_Handler` | `MessageHandler<TMessage>` |
| `[ActorMessageHandler]` | `{ClassName}_{MethodName}_Handler` | `ActorMessageHandler<TActor, TMessage>` |
| `[ActorMessageLocationHandler]` | `{ClassName}_{MethodName}_Handler` | `ActorMessageLocationHandler<TActor, TMessage>` |
| `[Event]` | `{ArgsTypes2}_{MethodName}` | `AEvent<TScene, TEvent>` |

> **注意**：`{ArgsTypesUnderLine}` 是所有参数类型名用下划线拼接的字符串（将 `.`, `<`, `>` 等符号替换为 `_`）。Event 的类名中 `{ArgsTypes2}` 取方法第3个参数（index 2）的类型名。

**生成流程**：
1. `SyntaxContextReceiver.OnVisitSyntaxNode` — 遍历语法树，收集含目标 Attribute 的方法，以所属 `ClassDeclarationSyntax` 为 key 汇总成 `Dictionary<ClassDeclarationSyntax, HashSet<MethodDeclarationSyntax>>`
2. `Execute` → `GenerateCSFiles` — 校验类是否为 `static partial`，否则报 ET1001；解析完整命名空间
3. `GenerateSystemCodeByTemplate` — 遍历方法，解析参数列表，生成多组 `argsTypes/argsVars/argsTypesVars` 变量列表；从 `AttributeTemplate` 字典取模板；替换12+个占位符；追加到 StringBuilder
4. `Execute` 后处理 — 合并同一静态类的所有方法代码，写入 `{namespace}.{className}.EntitySystems.g.cs`

**特殊处理**（`SpeicalProcessForArgs`）：当方法为 `EntitySystem/LSEntitySystem` 且名为 `GetComponentSys` 时，`$argsTypes$` 只取第一个类型（截断多余参数）。

#### `AttributeTemplate`

所有代码模板以字符串字典形式硬编码在 `AttributeTemplate` 构造函数中，使用 `$占位符$` 语法：

```csharp
// EntitySystem 模板（完整）：
$attribute$
public class $argsTypesUnderLine$_$methodName$System: $methodName$System<$argsTypes$>
{
    protected override $returnType$ $methodName$($argsTypesVars$)
    {
        $return$$argsVars0$.$methodName$($argsVarsWithout0$);
    }
}

// MessageHandler 模板：
$attribute$
public class $className$_$methodName$_Handler: MessageHandler<$argsTypesWithout0$>
{
    protected override async ETTask Run($argsTypesVars$)
    {
        await $className$.$methodName$($argsVars$);
    }
}

// Event 模板：
$attribute$
public class $argsTypes2$_$methodName$: AEvent<$argsTypes$>
{
    protected override async ETTask Run($argsTypesVars$)
    {
        await $className$.$methodName$($argsVars$);
    }
}
```

**占位符说明**：

| 占位符 | 含义 |
|---|---|
| `$attribute$` | 原始 Attribute 字符串，如 `[EntitySystem]` |
| `$methodName$` | 方法名 |
| `$className$` | 所属静态类名 |
| `$entityType$` | 第一个参数类型名（即 Entity 类型） |
| `$argsTypes$` | 所有参数类型逗号列表（如 `Unit, int`） |
| `$argsTypesUnderLine$` | 所有参数类型用 `_` 拼接（用于生成唯一类名） |
| `$argsTypesVars$` | 所有参数类型+名称（如 `Unit self, int level`） |
| `$argsVars$` | 所有参数名（如 `self, level`） |
| `$argsTypesWithout0$` | 除第0个外的参数类型 |
| `$argsVarsWithout0$` | 除第0个外的参数名 |
| `$argsTypesVarsWithout0$` | 除第0个外的参数类型+名 |
| `$argsTypes{i}$` | 第 i 个参数类型（0-indexed） |
| `$argsVars{i}$` | 第 i 个参数名 |
| `$returnType$` | 返回类型（void 或具体类型） |
| `$return$` | 若有返回值则为 `return `，否则为空 |

#### `ETGetComponentGenerator` (`ISourceGenerator`)

> **Round 2 修正**：`Initialize` 方法中 `context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create)` **已被注释掉**，导致 `Execute` 中 `context.SyntaxContextReceiver` 永远为 null，Generator 实际上**未激活/未生效**。`GetXxx()` 扩展方法生成功能目前处于禁用状态。

若激活（取消注释），其行为：
- 扫描带 `[ComponentOf(typeof(ParentEntity))]` 的类
- 按命名空间分组，为每个命名空间生成一个扩展类 `{AssemblyName}ETGetComponentExtension`
- 为每个 ComponentOf 关系生成：`public static ComponentName GetComponentName(this ParentEntityName self) => self.GetComponent<ComponentName>()`
- 输出文件：`ETGetComponentGenerator.{nameSpace}.g.cs`

#### `ETEntitySerializeFormatterGenerator` (`ISourceGenerator`)

**作用**：扫描所有继承 `ET.Entity` 或 `ET.LSEntity` 且带 `[MemoryPackable]` 的实体类，生成统一的 `ETEntitySerializeFormatter`（MemoryPack 多态序列化路由器）。

- 使用类名的 `long` Hash 作为 tag（`StringHashHelper.GetLongHashCode`）
- 生成 `Serialize` / `Deserialize` switch 分支，支持所有 Entity 子类型的多态序列化
- 同时生成 `EntitySerializeRegister.Register()` 静态注册入口

---

### `AnalyzerHelper`（公共辅助方法，25+ 扩展）

所有方法均为静态扩展，供各分析器复用：

| 方法 | 说明 |
|---|---|
| `GetFirstChild<T>(SyntaxNode)` | 获取直接子节点中第一个 T 类型节点 |
| `GetLastChild<T>(SyntaxNode)` | 获取直接子节点中最后一个 T 类型节点 |
| `GetParentClassDeclaration(SyntaxNode)` | 向上遍历祖先，找到最近的 ClassDeclarationSyntax |
| `HasAttribute(ITypeSymbol, string)` | 判断类型是否有指定全名的 Attribute |
| `HasAttributeInTypeAndBaseTyes(ITypeSymbol, string)` | 同上，同时检查所有基类 |
| `BaseTypes(ITypeSymbol)` | 枚举所有基类（yield return） |
| `HasAttribute(INamedTypeSymbol, string)` | 支持 Attribute 继承链检查 |
| `GetFirstAttribute(INamedTypeSymbol, string)` | 获取第一个指定名称的 AttributeData |
| `HasInterface(INamedTypeSymbol, string)` | 检查是否实现了指定接口（AllInterfaces） |
| `IsInterface(INamedTypeSymbol, string)` | 判断接口名是否匹配 `{namespace}.{name}` |
| `IsAssemblyNeedAnalyze(string, string[])` | 判断程序集名是否在分析白名单中 |
| `GetMemberAccessSyntaxParentType(MemberAccessExpression, SemanticModel)` | 获取成员访问表达式的宿主对象类型（支持局部变量/参数/属性/方法/字段/事件） |
| `GetNeareastAncestor<T>(SyntaxNode)` | 找最近祖先 T 类型节点 |
| `HasParameterType(IMethodSymbol, string, out IParameterSymbol)` | 方法是否有指定类型的参数 |
| `DescendantNodes<T>(SyntaxNode)` | 枚举所有后代中 T 类型节点 |
| `PreviousNode(SyntaxNode)` | 同层级的上一个兄弟节点 |
| `NextNode(SyntaxNode)` | 同层级的下一个兄弟节点 |
| `GetAwaitStatementControlFlowBlock(...)` | 构建控制流图，找 await 语句所在的 BasicBlock（用于 ET0016 检测） |
| `IsPartial(ClassDeclarationSyntax)` | 判断类是否有 partial 修饰符 |
| `GetNameSpace(INamedTypeSymbol)` | 获取完整命名空间字符串 |
| `IsSemanticModelNeedAnalyze(SemanticModel, string[])` | 按文件路径过滤是否需要分析 |
| `HasMethodWithParams(INamedTypeSymbol, string, ITypeSymbol[])` | 类型是否有指定名称+参数的方法 |
| `HasMethodWithParams(INamedTypeSymbol, string, string[])` | 同上，参数为字符串类型名 |
| `IsETEntity(ITypeSymbol)` | 类型是否为 `ET.Entity` 或其直接子类 / `ET.LSEntity` 的直接子类 |
| `IsEntityRefOrEntityWeakRef(ITypeSymbol)` | 类型是否为 `EntityRef<T>` 或 `EntityWeakRef<T>` |

---

### `Definition`（全局常量）

集中定义所有分析器/生成器使用的类型名、属性名、方法名常量：

```csharp
// 基础类型
EntityType = "ET.Entity"
LSEntityType = "ET.LSEntity"
ETTask = "ETTask"
ETTaskFullName = "ET.ETTask"
EntityRefType = "EntityRef"
EntityWeakRefType = "EntityWeakRef"

// 方法名列表
AddChildMethods = ["AddChild", "AddChildWithId"]
ComponentMethod = ["AddComponent", "GetComponent"]

// 生命周期接口+方法名对（全量）
IAwakeInterface = "ET.IAwake"          → AwakeMethod = "Awake"
IUpdateInterface = "ET.IUpdate"        → UpdateMethod = "Update"
IDestroyInterface = "ET.IDestroy"      → DestroyMethod = "Destroy"
IAddComponentInterface = "ET.IAddComponent"  → AddComponentMethod = "AddComponent"
IDeserializeInterface = "ET.IDeserialize"    → DeserializeMethod = "Deserialize"
IGetComponentInterface = "ET.IGetComponentSys" → GetComponentMethod = "GetComponentSys"
ILoadInterface = "ET.ILoad"            → LoadMethod = "Load"
ILateUpdateInterface = "ET.ILateUpdate" → LateUpdateMethod = "LateUpdate"
ISerializeInterface = "ET.ISerialize"  → SerializeMethod = "Serialize"
// LS 帧同步专用
ILSRollbackInterface = "ET.ILSRollback" → LSRollbackMethod = "LSRollback"
ILSUpdateInterface = "ET.ILSUpdate"    → LSUpdateMethod = "LSUpdate"

// 友元访问 Attribute（3个）
FriendAttributes = [FriendOfAttribute, EntitySystemOfAttribute, LSEntitySystemOfAttribute]

// 特殊约束
ETCancellationToken = "ET.ETCancellationToken"
ETClientNameSpace = "ET.Client"
ClientDirInServer = @"Unity\Assets\Scripts\Hotfix\Client\"
```

---

### Runtime Attributes（运行时属性）

| Attribute | 使用目标 | 说明 |
|---|---|---|
| `ChildOfAttribute(Type)` | `class` | 标记子实体允许的父级类型；分析器 ET0001 校验 AddChild 调用 |
| `ComponentOfAttribute(Type)` | `class` | 标记组件允许的父级实体；分析器 ET0007 校验 AddComponent/GetComponent |
| `EntitySystemOfAttribute(Type)` | `class` | 标记 System 静态类对应的 Entity 类型；触发 ETSystemGenerator |
| `LSEntitySystemOfAttribute(Type)` | `class` | 同上，针对 LSEntity（帧同步） |
| `DisableNewAttribute` | `class` | 禁止使用 `new` 构造该类；分析器 ET0031 报错 |
| `EnableClassAttribute` | `class` | Model 程序集中允许声明该非 Entity 类；规避分析器 ET0032 |
| `EnableMethodAttribute` | `method` | 允许该方法通过方法声明限制检查 |
| `EnableAccessEntiyChildAttribute` | `class` | 允许在 Entity 类中直接访问 Child/Component |
| `FriendOfAttribute(Type)` | `class` | 友元访问，等同于 EntitySystemOf / LSEntitySystemOf 的访问权限 |
| `StaticFieldAttribute` | `field` | 静态字段必须标记此属性才能绕过 ET0015 |
| `UniqueIdAttribute(min, max)` | `class` | 标记 Id 字段需在 [min, max] 区间内且不重复；分析器 ET0011/ET0012 |

---

### Diagnostic Analyzers（诊断分析器）

| 编号 | 分析器类 | 检查内容 | 级别 |
|---|---|---|---|
| ET0001 | `AddChildTypeAnalyzer` | AddChild/AddChildWithId 的类型必须有 `[ChildOf(typeof(parent))]` | Error |
| ET0002 | `EntityFiledAccessAnalyzer` | 禁止在 Entity 类内直接访问 Child/Component（需用 System） | Error |
| ET0003 | `EntityClassDeclarationAnalyzer` | Entity 类只能直接继承 `Entity` / `LSEntity`，禁止多层继承 | Error |
| ET0004 | `HotfixProjectFieldDeclarationAnalyzer` | Hotfix 程序集字段声明限制 | Error |
| ET0005 | `ClassDeclarationInHotfixAnalyzer` | Hotfix 程序集类声明限制 | Error |
| ET0006 | `EntityMemberDeclarationAnalyzer` | Entity 成员声明限制（方法约束）| Error |
| ET0007 | `EntityComponentAnalyzer` | AddComponent/GetComponent 类型必须有 `[ComponentOf(typeof(parent))]` | Error |
| ET0008 | `ETTaskAnalyzer` | 同步方法内 ETTask 调用必须加 `.Coroutine()` | Error |
| ET0009 | `ETTaskAnalyzer` | 异步方法内 ETTask 调用必须加 `await` 或 `.Coroutine()` | Error |
| ET0010 | `EntityMemberDeclarationAnalyzer` | Entity 类禁止声明委托字段或属性 | Error |
| ET0011 | `UniqueIdAnalyzer` | UniqueId 字段值必须在约束区间内 | Error |
| ET0012 | `UniqueIdAnalyzer` | UniqueId 字段值禁止重复 | Error |
| ET0013 | `StaticClassCircularDependencyAnalyzer` | 静态类间循环依赖检测 | Error |
| ET0014 | `EntityFiledAccessAnalyzer` | 禁止在 Entity 类中直接调用 Child 和 Component | Error |
| ET0015 | `StaticFieldDeclarationAnalyzer` | 静态字段声明必须标记 `[StaticField]` | Error |
| ET0016 | `ETCancellationTokenAnalyzer` | await 后必须判断 `CancelToken.IsCancel`（利用控制流图分析）| Error |
| ET0017 | `ETCancellationTokenAnalyzer` | await 调用必须传入相同 CancelToken | Error |
| ET0018 | `ETCancellationTokenAnalyzer` | 异步方法 ETCancelToken 参数禁止声明默认值 | Error |
| ET0019 | `ETCancellationTokenAnalyzer` | 调用处 ETCancelToken 参数禁止传 null | Error |
| ET0020 | `EntityMemberDeclarationAnalyzer` | Entity 类禁止声明实体字段（应用 EntityRef） | Error |
| ET0021 | `AsyncMethodReturnTypeAnalyzer` | 禁止声明返回值为 void 的异步方法 | Error |
| ET0022 | `ClientClassInServerAnalyzer` | Server 程序集禁止引用 ET.Client 命名空间 | Error |
| ET0023 | `EntityMemberDeclarationAnalyzer` | LSEntity 禁止声明浮点数字段（帧同步确定性约束） | Error |
| ET0024 | `EntitySystemAnalyzer` | Entity 类实现了生命周期接口但 System 类缺少对应方法 | Error |
| ET0025 | `EntitySystemAnalyzer` | `[EntitySystem]` 方法必须在有 `[EntitySystemOf]` 的类中 | Error |
| ET0026 | `EntityMethodDeclarationAnalyzer` | Entity 内/含实体参数的方法内必须用 Fiber 输出日志（禁止用 `ET.Log`） | Error |
| ET0027 | `EntityHashCodeAnalyzer` | Entity 类名 Hash 不能重复 | Error |
| ET0028 | `EntityComponentAnalyzer` | Entity 类不能同时标记 `[ComponentOf]` 和 `[ChildOf]` | Error |
| ET0029 | `EntityClassDeclarationAnalyzer` | 禁止声明泛型 Entity 类 | Error |
| ET0030 | `NetMessageAnalyzer` | 消息类禁止声明实体字段 | Error |
| ET0031 | `DiableNewAnalyzer` | 禁止使用 new 构造带 `[DisableNew]` 的类 | Error |
| ET0032 | `DisableNormalClassDeclaratonInModelAssemblyAnalyzer` | Model/ModelView 程序集禁止声明非 Entity 类（除非加 `[EnableClass]`） | Error |
| ET1001 | `ETSystemGenerator`（生成器内） | `[EntitySystem]` 方法所在类必须是 `static partial` 类 | Error |

### CodeFixer（代码修复）

| 修复器 | 对应诊断 | 行为 |
|---|---|---|
| `EntityFiledAccessCodeFixProvider` | ET0002/ET0014 | 自动将 Entity 内访问 Child/Component 的代码迁移到 System 类 |
| `EntitySystemCodeFixProvider` | ET0024 | 为缺失的生命周期方法自动生成 `[EntitySystem]` 方法骨架 |

---

## 实现原理

### Source Generator 工作流

```
编译开始
    │
    ▼
ISyntaxContextReceiver.OnVisitSyntaxNode()
    │  遍历所有语法节点，按规则收集目标符号
    │  ETSystemGenerator: 收集含目标 Attribute 的方法 → 所属静态类
    ▼
ISourceGenerator.Execute()
    │  分析收集结果，渲染代码模板
    │  ETSystemGenerator: 校验 static partial → 解析命名空间 → 替换占位符
    ▼
context.AddSource(filename, code)
    │  注入生成的 .g.cs 文件到编译上下文
    ▼
生成代码参与后续编译
```

### Analyzer 工作流

```
编译器调用 DiagnosticAnalyzer.Initialize()
    │  注册感兴趣的语法节点/符号类型
    ▼
针对每个匹配的节点/符号触发回调
    │  执行规则检查逻辑
    │  复杂检查（如 ET0016）使用 ControlFlowGraph.Create() 进行数据流分析
    ▼
context.ReportDiagnostic(diagnostic)
    │  报告错误/警告
    ▼
IDE 或构建输出显示诊断信息
（部分支持 CodeFixer 自动修复）
```

### 程序集分组（AnalyzeAssembly）

分析器按目标程序集分组执行，避免误报：

| 分组常量 | 包含程序集 |
|---|---|
| `AllHotfix` | ET.Hotfix, ET.HotfixView |
| `AllModel` | ET.Model, ET.ModelView |
| `AllModelHotfix` | ET.Model, ET.Hotfix, ET.ModelView, ET.HotfixView |
| `All` | ET.Core + AllModelHotfix |
| `AllLogicModel` | ET.Model（仅用于序列化生成器）|

---

## 关键流程

### 生命周期方法自动生成流程（含完整模板替换）

```
开发者编写：
[EntitySystemOf(typeof(Unit))]
public static partial class UnitSystem
{
    [EntitySystem]
    private static void Awake(Unit self) { ... }
}

↓ ETSystemGenerator 执行：
  1. OnVisitSyntaxNode: 收集 {UnitSystem → [Awake方法]}
  2. GenerateCSFiles: 验证 UnitSystem 是 static partial ✓
  3. GenerateSystemCodeByTemplate:
     - methodName = "Awake"
     - componentName = "Unit"
     - argsTypes = "Unit"
     - argsTypesUnderLine = "Unit"
     - argsVars0 = "self"
     - argsVarsWithout0 = "" (无其他参数)
     - returnType = "void"
     - 替换模板 → 生成：

public static partial class UnitSystem
{
    [EntitySystem]
    public class Unit_AwakeSystem : AwakeSystem<Unit>
    {
        protected override void Awake(Unit self)
        {
            self.Awake();
        }
    }
}

↓ 写入 ET.Unit.UnitSystem.EntitySystems.g.cs
```

### MessageHandler 生成流程

```
开发者编写：
public static partial class LoginMessageHandler
{
    [MessageHandler]
    private static async ETTask HandleC2G_Login(Scene scene, C2G_Login msg) { ... }
}

↓ 生成：
public class LoginMessageHandler_HandleC2G_Login_Handler : MessageHandler<C2G_Login>
{
    protected override async ETTask Run(Scene scene, C2G_Login msg)
    {
        await LoginMessageHandler.HandleC2G_Login(scene, msg);
    }
}
```

### ETCancelToken 控制流检测（ET0016）

```
分析器收集含 ETCancelToken 参数的异步方法
    ↓
找到方法内所有 await 表达式
    ↓
对每个 await 调用 ControlFlowGraph.Create(methodSyntax, semanticModel)
    ↓
找到 await 所在的 BasicBlock（通过 block.Operations 匹配）
    ↓
检查该 BasicBlock 的后续节点（PreviousNode / NextNode）中是否有 cancelToken.IsCancel 判断
    ↓
若没有 → 报告 ET0016 Error
```

### Entity 序列化注册流程

```
ETEntitySerializeFormatterGenerator 扫描所有 [MemoryPackable] 实体类
        ↓
生成 ETEntitySerializeFormatter（多态路由器）
以类名 LongHashCode 作为 tag 区分类型
        ↓
生成 EntitySerializeRegister.Register() 注册到 MemoryPackFormatterProvider
        ↓
序列化时：tag = __typeToTag[value.GetType()] → switch 分支调用对应类型的 MemoryPack 序列化
反序列化时：读取 tag → switch 分支 Fetch<T>() + ReadPackable
```

---

## 依赖关系

| 依赖方向 | 说明 |
|---|---|
| → `cn.etetet.core` | 使用 Entity、LSEntity 基类定义（用于分析器类型判断） |
| → `cn.etetet.memorypack` | ETEntitySerializeFormatterGenerator 生成的代码依赖 MemoryPack API |
| → Roslyn API | Microsoft.CodeAnalysis (ISourceGenerator, DiagnosticAnalyzer, ControlFlowGraph) |
| 被所有含 Entity/System 代码的包依赖 | 通过 Unity Analyzer DLL 引用机制，所有程序集编译时均经过此分析器 |

---

## 注意事项与最佳实践

1. **EntitySystemOf 必须配对使用**：静态类必须同时声明 `[EntitySystemOf(typeof(XxxEntity))]`，否则 ET0025 报错
2. **禁止多层 Entity 继承**：Entity 类只能直接继承 `Entity`，不能继承 `Entity` 的子类（ET0003）
3. **Entity 内禁止持有 Entity 引用**：应使用 `EntityRef<T>` 替代（ET0020）；`EntityWeakRef<T>` 也是允许的弱引用形式
4. **LSEntity 不得含浮点数**：保证帧同步确定性（ET0023）
5. **ETCancelToken 传递链**：异步函数中 await 后必须立即判断 `cancelToken.IsCancel()`（ET0016）；不能传入 null（ET0019）；不能有默认值（ET0018）
6. **Entity Hash 唯一性**：类名的 `long` Hash 不能碰撞，若碰撞需修改类名（ET0027）
7. **分析器可以全局禁用**：通过 `AnalyzerGlobalSetting.EnableAnalyzer = false`（用于特殊构建场景）
8. **ETGetComponentGenerator 当前未激活**：`Initialize` 中的 `RegisterForSyntaxNotifications` 被注释，GetXxx 扩展方法不会自动生成，需手动写或取消注释
9. **所有分析器全部为 Error 级别**：没有 Warning，违规即编译失败，是强制性约束
10. **生成文件命名规则**：ETSystemGenerator 输出 `{namespace}.{className}.EntitySystems.g.cs`；ETGetComponentGenerator 输出 `ETGetComponentGenerator.{namespace}.g.cs`
