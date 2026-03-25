# cn.etetet.yiuigm — YIUI GM命令系统

**版本**: 3.0.4
**分类**: UI/YIUI
**依赖**: cn.etetet.core
**文档**: https://lib9kmxvq7k.feishu.cn/wiki/NYADwMydliVmQ7kWXOuc0yxGn7p

---

## 概述

`cn.etetet.yiuigm` 是一个运行时 GM（Game Master）调试命令系统，集成在 YIUI 框架中。它提供了一个可拖拽的悬浮按钮入口，点击后展开 GM 面板，允许开发者/测试人员在运行时按分类浏览、配置参数并执行自定义 GM 命令。

核心特性：
- **属性驱动注册**：通过 `[GM]` 特性标记任意类即可自动注册为 GM 命令，无需手动配置
- **类型化参数**：支持 6 种参数类型（String/Bool/Float/Int/Long/Enum），在 UI 上自动渲染对应输入控件
- **分类管理**：通过 `EGMType` 静态类定义命令分组，支持多项目扩展（`partial` 关键字）
- **异步执行**：命令执行为 `ETTask<bool>` 异步模式，返回值控制是否关闭 GM 面板
- **可配置开关**：通过 `YIUIConstHelper.Const.CloseGMCommand` 全局关闭 GM 功能

---

## 目录结构

```
cn.etetet.yiuigm/
├── Assets/GameRes/YIUI/GM/Prefabs/        # UI 预制体
│   ├── GMPanel.prefab                     # 悬浮按钮面板（永久存活）
│   ├── GMView.prefab                      # GM 主视图（命令列表）
│   ├── GMCommandItem.prefab               # 单条命令项（含参数列表）
│   ├── GMParamItem.prefab                 # 单个参数项（输入控件）
│   └── GMTypeItem.prefab                  # 命令分类标签项
├── Scripts/
│   ├── ModelView/Client/GM/               # 数据模型层
│   │   ├── GMAttribute.cs                 # [GM] 注册特性 + [GMGroup] 分类特性
│   │   ├── IGMCommand.cs                  # GM 命令接口
│   │   ├── GMCommandComponent.cs          # Scene 级 GM 管理组件 + OnGMEventClose 事件
│   │   ├── GMCommandInfo.cs               # 命令元数据（运行时）
│   │   ├── GMParamInfo.cs                 # 参数元数据
│   │   ├── EGMParamType.cs                # 参数类型枚举 + TryToValue 转换扩展
│   │   ├── EGMType_GM.cs                  # 内置命令分类定义（Common/Test）
│   │   ├── GMKeyHelper.cs                 # 反射获取分类键值辅助类（带缓存）
│   │   └── GM_Command_Test.cs             # 内置测试命令
│   ├── ModelView/Client/YIUIComponent/GM/ # YIUI UI 组件数据层
│   │   ├── GMPanelComponent.cs            # 悬浮按钮面板（IUpdate，持久化位置）
│   │   ├── GMViewComponent.cs             # GM 主视图（双LoopScroll，持久化选中索引）
│   │   ├── GMCommandItemComponent.cs      # 命令列表项（内嵌参数LoopScroll）
│   │   ├── GMParamItemComponent.cs        # 参数项（OptionList/OptionDic 用于枚举）
│   │   └── GMTypeItemComponent.cs         # 分类标签组件（空数据体）
│   ├── HotfixView/Client/GM/              # 热更新逻辑层（命令注册 + 执行）
│   │   ├── GMCommandComponentSystem.cs    # Awake反射初始化 + Run异步执行
│   │   ├── YIUIEventInitializeAfterGMHandler.cs  # YIUI初始化完成后创建GMCommandComponent
│   │   └── GM_Command_Test.cs             # 测试命令实现（展示6种参数类型用法）
│   └── HotfixView/Client/YIUISystem/GM/   # UI 组件系统层
│       ├── GMPanelComponentSystem.cs      # 拖拽逻辑 + 键盘快捷键
│       ├── GMViewComponentSystem.cs       # 分类/命令列表渲染联动
│       ├── GMCommandItemComponentSystem.cs # 命令项渲染 + 执行触发
│       ├── GMParamItemComponentSystem.cs  # 参数UI动态切换 + 枚举Dropdown构建
│       └── GMTypeItemComponentSystem.cs   # 分类标签显示名+选中态
```

---

## 核心类/接口

### `IGMCommand` — GM 命令接口

```csharp
public interface IGMCommand
{
    List<GMParamInfo> GetParams();
    ETTask<bool> Run(Scene clientScene, ParamVo paramVo);
}
```
- `GetParams()`: 返回命令所需的参数列表（定义 UI 输入控件类型和默认值）
- `Run()`: 异步执行命令逻辑，返回 `true` 表示执行后关闭 GM 面板，`false` 表示不关闭

---

### `GMAttribute` — 注册特性

```csharp
[GM(EGMType.Test, 1, "命令名称", "可选描述")]
public class MyGMCommand : IGMCommand { ... }
```

| 参数 | 说明 |
|------|------|
| `gmType` | 命令分类（对应 `EGMType` 中的 int 常量） |
| `gmLevel` | 命令等级（预留权限校验，**当前未实现**，源码含 TODO） |
| `gmName` | UI 显示名称 |
| `gmDesc` | UI 显示描述（可选，为空则不显示描述区域） |

另有 `GMGroupAttribute`，标记在 `EGMType` 字段上，指定该分类的 UI 显示名称：
```csharp
[GMGroup("通用")]
public const int Common = PackageType.YIUI * 1000 + 1;
```

---

### `GMCommandComponent` — 核心管理组件

```csharp
[ComponentOf(typeof(Scene))]
public class GMCommandComponent : Entity, IAwake, IDestroy
{
    public Dictionary<int, List<GMCommandInfo>> AllCommandInfo { get; set; }
}

public struct OnGMEventClose { }
```

- 挂载于 `Scene`，在 `Awake` 时通过反射扫描所有带 `[GMAttribute]` 的类，自动构建命令字典
- `AllCommandInfo`: key = `EGMType` 值，value = 该分类下的命令列表（按添加顺序）
- 还在同文件定义了 `OnGMEventClose` 事件结构体，由命令执行后触发（通知 GMView 关闭）
- 执行命令期间调用 `BanLayerOptionForever()`/`RecoverLayerOptionForever()` 禁止/恢复 UI 交互层操作

---

### `GMCommandInfo` — 命令元数据

```csharp
[EnableClass]
public class GMCommandInfo
{
    public int               GMType;        // 命令分类 int
    public string            GMTypeName;    // 分类显示名（从 GMKeyHelper.GetDesc 获取）
    public int               GMLevel;       // 命令等级
    public string            GMName;        // 命令名称
    public string            GMDesc;        // 命令描述
    public List<GMParamInfo> ParamInfoList; // 参数列表
    public IGMCommand        Command;       // 命令实例（Activator.CreateInstance 创建）
}
```

---

### `GMParamInfo` — 参数元数据

```csharp
[EnableClass]
public class GMParamInfo
{
    public EGMParamType ParamType;    // 参数类型
    public string       Desc;         // UI 描述文字
    public string       Value;        // 当前值（字符串存储，执行时转换）
    public string       EnumFullName; // 枚举时使用，如 "ET.Client.EGMParamType"
}
```

**注意**: `Value` 始终以字符串存储，执行时 `TryToValue()` 按类型转换。
Bool 类型 toggle 写入 `"1"` 或 `"0"`（非 `"true"`/`"false"`），但 `TryToValue()` 的 Bool 分支同时处理两种形式。

---

### `EGMParamType` — 参数类型枚举

| 值 | 说明 | UI 控件 | `u_DataTypeValue` |
|----|------|---------|-------------------|
| `String` | 字符串 | InputField (Standard) | 1 |
| `Bool` | 布尔 | Toggle | 2 |
| `Float` | 小数 | InputField (DecimalNumber) | 1 |
| `Int` | 整数 | InputField (IntegerNumber) | 1 |
| `Long` | 64位整数 | InputField (IntegerNumber) | 1 |
| `Enum` | 枚举 | Dropdown（反射填充枚举值） | 3 |

`u_DataTypeValue` 是预制体中的数据绑定字段，控制哪种 UI 控件可见（0=未知/隐藏，1=InputField，2=Toggle，3=Dropdown）。

扩展方法 `TryToValue()` 将字符串 `Value` 转换为对应 C# 类型对象：
- Enum 使用 `CodeTypes.Instance.GetType(enumFullName)` + `Enum.Parse()`
- 转换失败时 `Debug.LogError` 提示，不抛异常

---

### `EGMType` — 命令分类定义

```csharp
[UniqueId]
public static partial class EGMType
{
    [GMGroup("通用")]
    public const int Common = PackageType.YIUI * 1000 + 1;

    [GMGroup("测试")]
    public const int Test = PackageType.YIUI * 1000 + 2;
}
```
- 使用 `partial` 关键字，各业务模块可在自己包内扩展分类
- `[UniqueId]` 保证 ID 唯一性
- ID 设计：`PackageType.{包} * 1000 + N`，按包类型分区间，避免冲突
- 各分类在 UI 中按 ID 从小到大排序（`GMKeyHelper.GetKeys()` 调用了 `Sort()`）

---

### `GMKeyHelper` — 分类反射辅助

```csharp
public static class GMKeyHelper
{
    [StaticField] private static List<int> m_AllGMKey;
    [StaticField] private static Dictionary<int, string> m_GMKeyDesc;

    public static List<int>  GetKeys(bool force = false);  // 带缓存，force=true 强制刷新
    public static string     GetDesc(int key);             // 获取分类的 GMGroup 名称
    public static string     GetDisplayDesc(int key);      // 返回 "{key}_{名称}" 格式
}
```

- `GetKeys()` 通过 `AssemblyHelper.GetAssembly("ET.ModelView")` 加载程序集，反射 `EGMType` 的所有 `int` 常量
- 使用 `HashSet<int>` 去重，重复 key 输出 `Log.Error` 提示
- `[StaticField]` 标记表示这些字段在热重载时需要重置
- **重要**: 反射基于编译后的 DLL，新增分类或命令后必须重新编译 HotfixView/ModelView 程序集才能生效

---

## UI 组件系统

### `GMPanelComponent` — 悬浮按钮面板

```csharp
public partial class GMPanelComponent : Entity, IUpdate
{
    public KeyCode    _OpenGMViewKey;      // 配置的键盘快捷键
    public FloatPrefs _GMBtn_Pos_X;        // PlayerPrefs 持久化 X 坐标
    public FloatPrefs _GMBtn_Pos_Y;        // PlayerPrefs 持久化 Y 坐标
    public Vector2    _Offset;             // 拖拽时的偏移量
    public Vector2    _LimitSize;          // 面板可移动范围限制
}
```

- **层级**: `EPanelLayer.Top`，优先级 99999（始终在最顶层）
- **面板选项**: `ForeverCache | DisClose | IgnoreBack | IgnoreClose`（永久存活，不可被关闭）
- **拖拽**：`BeginDrag` 记录偏移量，`Drag` 更新位置，`EndDrag` 夹紧到限制范围并写入 `FloatPrefs`
- **键盘快捷键**: `Update` 中监听 `_OpenGMViewKey`，按键时切换 GMView 的显示/关闭
- 初始 Y 为 0 时自动设置为 `_LimitSize.y`（即屏幕上方）

---

### `GMViewComponent` — GM 主视图

```csharp
public partial class GMViewComponent : Entity, IDynamicEvent<OnGMEventClose>
{
    public bool                           Opened;
    public List<int>                      GMTypeData;            // 分类 ID 列表
    public EntityRef<YIUILoopScrollChild> m_GMTypeLoop;          // 左侧分类列表
    public EntityRef<YIUILoopScrollChild> m_GMCommandLoop;       // 右侧命令列表
    public EntityRef<GMCommandComponent>  m_CommandComponent;    // 引用管理器
    public IntPrefs                       m_GMTypeIndex;         // 持久化选中分类索引
}
```

- 左侧 `m_GMTypeLoop`：类型 `GMTypeItemComponent`，事件 `"u_EventSelect"`
- 右侧 `m_GMCommandLoop`：类型 `GMCommandItemComponent`，无选中事件
- 打开时若配置 `OpenGMViewFirstType=true` 则始终选第 0 个分类，否则恢复上次选中索引
- 监听 `OnGMEventClose` → 调用 `UIView.CloseAsync()` 关闭视图

---

### `GMCommandItemComponent` — 命令列表项

```csharp
public partial class GMCommandItemComponent : Entity
{
    public EntityRef<GMCommandComponent>  m_CommandComponent; // 引用执行器
    public GMCommandInfo                  Info;               // 当前命令元数据
    public EntityRef<YIUILoopScrollChild> m_GMParamLoop;      // 参数列表
}
```

- 显示命令名称 (`u_DataName`)、描述 (`u_DataDesc`)
- 参数数量 >= 1 时显示参数列表区域 (`u_DataShowParamLoop`)
- 点击执行按钮 → `OnEventRunInvoke` → `GMCommandComponent.Run(Info)`
- `ResetItem()` 先设置数据再异步刷新参数列表（`WaitRefresh`/`SetDataRefresh`）

---

### `GMParamItemComponent` — 参数项

```csharp
public partial class GMParamItemComponent : Entity
{
    public GMParamInfo                   ParamInfo;   // 当前参数元数据（Value 会被实时写回）
    public List<Dropdown.OptionData>     OptionList;  // 枚举类型时构建的下拉选项
    public Dictionary<string, string>    OptionDic;   // 显示名 → 字段名 映射
}
```

**枚举 Dropdown 构建流程**：
1. `RefreshDropdownInfo()` 通过 `CodeTypes.Instance.GetType(enumFullName)` 反射枚举类型
2. 遍历所有 `public static` 字段，读取 `[LabelText]` 特性获取显示名
3. 构建 `OptionList`（Dropdown 显示用）和 `OptionDic`（显示名 → 字段名）
4. 默认选中与 `info.Value` 字段名匹配的项

**三种事件处理**：
- `OnEventInputInvoke(string)` → 直接写回 `ParamInfo.Value`
- `OnEventToggleInvoke(bool)` → 写入 `"1"` 或 `"0"`
- `OnEventDropdownInvoke(int)` → 通过 `OptionDic` 查找字段名，写回 `ParamInfo.Value`

---

### `GMTypeItemComponent` — 分类标签项

数据体为空，所有逻辑在 System 中：
- `ResetItem(int data)` → `u_DataTypeName.SetValue(GMKeyHelper.GetDesc(data))`
- `SelectItem(bool value)` → `u_DataSelect.SetValue(value)`
- `OnEventSelectInvoke` → 空实现（选中逻辑由 LoopScroll 的点击回调 `YIUILoopOnClick` 处理）

---

## 关键流程

### 初始化流程

```
YIUIEventInitializeAfter 事件触发（YIUI框架初始化完成后）
  → YIUIEventInitializeAfterGMHandler.Run()
    → 检查 YIUIConstHelper.Const.CloseGMCommand 开关
    → scene.AddComponent<GMCommandComponent>()
      → GMCommandComponentSystem.Awake()
        → self.AllCommandInfo = new Dictionary<int, List<GMCommandInfo>>()
        → GMKeyHelper.GetKeys()       // 反射预热分类缓存
        → self.Init()                 // 反射扫描 [GMAttribute] 类，构建命令字典
        → YIUIRoot().OpenPanelAsync<GMPanelComponent>()  // 打开悬浮按钮面板
```

### 命令注册流程（Init 内部）

```
CodeTypes.Instance.GetTypes(typeof(GMAttribute))
  → 遍历每个带 [GMAttribute] 的类型
    → type.GetCustomAttribute<GMAttribute>()  // 读取元数据
    → Activator.CreateInstance(type)          // 创建 IGMCommand 实例
    → obj.GetParams()                         // 获取参数列表（null 则用空列表）
    → 构建 GMCommandInfo（含 GMTypeName = GMKeyHelper.GetDesc(gmType)）
    → AllCommandInfo[GMType].Add(info)        // 按分类存储
```

### 命令执行流程

```
用户点击命令项的执行按钮
  → GMCommandItemComponent.OnEventRunInvoke()
    → GMCommandComponent.Run(info)
      → 遍历 ParamInfoList
          → paramInfo.ParamType.TryToValue(paramInfo)  // 字符串 → 强类型对象
      → EntityRef<GMCommandComponent> selfRef = self    // 保存引用（await后self可能失效）
      → self.YIUIMgr().BanLayerOptionForever()          // 禁止 UI 交互，返回 banCode
      → try:
          → paramVo = ParamVo.Get(objData)             // 从对象池获取参数容器
          → closeGM = await info.Command.Run(scene, paramVo)  // 执行实际命令
          → ParamVo.Put(paramVo)                        // 归还对象池
          → if closeGM: DynamicEvent(new OnGMEventClose()) → GMViewComponent 关闭
      → finally:
          → self.YIUIMgr().RecoverLayerOptionForever(banCode)  // 恢复 UI 交互
```

### 枚举参数转换流程

```
TryToValue(EGMParamType.Enum, paramInfo)
  → CodeTypes.Instance.GetType(info.EnumFullName)   // 热更兼容的类型查找
  → Enum.Parse(enumType, info.Value)                 // 字段名 → 枚举值
  → 失败时 Debug.LogError，返回 null
```

---

## 依赖关系

```
cn.etetet.yiuigm
├── cn.etetet.core           (package.json 声明依赖)
│   └── CodeTypes, ETTask, Entity/Component, Scene, Log, StaticField
├── YIUIFramework
│   └── YIUILoopScrollChild, YIUIPanelComponent, YIUIWindowComponent
│   └── ParamVo, YIUIConstHelper, AssemblyHelper
│   └── FloatPrefs, IntPrefs（PlayerPrefs封装）
├── ET Framework
│   └── EntityRef, IUpdate, IAwake, IDestroy
│   └── DynamicEvent, IDynamicEvent<T>
├── Sirenix.OdinInspector
│   └── LabelTextAttribute（枚举字段显示名）
│   └── LabelText（EGMParamType 枚举成员标签）
└── UnityEngine.UI
    └── InputField, Toggle, Dropdown（Legacy UI）
```

### 被其他 package 使用的方式
- 业务包通过 `partial class EGMType` 扩展分类
- 业务包实现 `IGMCommand` 接口并标注 `[GM]` 特性注册命令
- GM 系统完全通过反射发现命令，不需要业务包主动注册

---

## 如何创建自定义 GM 命令

```csharp
// 1. 在业务包中扩展命令分类（可选，使用已有分类则跳过）
public static partial class EGMType
{
    [GMGroup("我的功能")]
    public const int MyFeature = PackageType.MyPackage * 1000 + 1;
}

// 2. 实现命令类，标注 [GM] 特性
[GM(EGMType.MyFeature, 1, "给金币", "给玩家指定数量金币")]
public class GM_AddGold : IGMCommand
{
    public List<GMParamInfo> GetParams()
    {
        return new()
        {
            new GMParamInfo(EGMParamType.Int, "金币数量", "1000"),
            new GMParamInfo(EGMParamType.Enum, "货币类型", "Gold", "ET.Client.ECurrencyType"),
        };
    }

    public async ETTask<bool> Run(Scene clientScene, ParamVo paramVo)
    {
        var amount       = paramVo.Get<int>(0);
        var currencyType = paramVo.Get<ECurrencyType>(1);
        // 执行业务逻辑...
        await ETTask.CompletedTask;
        return true; // true = 执行后关闭GM面板
    }
}
```

---

## 注意事项与边界情况

1. **热更新兼容**: `GMKeyHelper.GetKeys()` 读取的是 `ET.ModelView` 编译后的 DLL，新增分类或命令后必须重新编译程序集才能生效。
2. **命令等级预留**: `GMLevel` 字段预留但当前**未实现**权限校验（源码中有 `TODO` 注释，计划根据玩家等级过滤命令）。
3. **枚举参数全名**: 枚举类型参数必须提供完整类名（含命名空间），如 `"ET.Client.EGMParamType"`，否则 `CodeTypes.Instance.GetType` 返回 null，Dropdown 为空。
4. **全局关闭**: `YIUIConstHelper.Const.CloseGMCommand = true` 可完全禁用 GM 系统，适合正式发布包。
5. **面板不可关闭**: `GMPanelComponent` 优先级 99999 且 `DisClose|IgnoreClose`，无法被其他系统关闭或遮挡。
6. **Bool 存储格式**: Toggle 写回 `"1"`/`"0"`，但 `TryToValue` 同时支持 `"true"`/`"false"` 和 `"1"`/`"0"` 两种形式。
7. **参数值实时回写**: `GMParamInfo.Value` 在用户交互时被直接修改（通过事件），这意味着同一个命令实例的参数值在多次执行间会保留上次输入的值。
8. **await 后 self 失效**: `GMCommandComponent.Run()` 使用 `EntityRef<GMCommandComponent> selfRef = self` 保存引用，在 await 后重新赋值，防止实体被销毁后空指针。
9. **ParamVo 对象池**: 参数容器通过 `ParamVo.Get()`/`ParamVo.Put()` 使用对象池，`try-finally` 保证即使命令抛异常也会执行 `RecoverLayerOptionForever`，但 `ParamVo.Put` 只在 `try` 块内（若命令抛异常 ParamVo 不会归还）。
10. **分类 ID 重复**: `GMKeyHelper.GetKeys()` 使用 `HashSet` 检测重复 ID，重复时 `Log.Error` 并跳过，但不会阻止程序运行。
