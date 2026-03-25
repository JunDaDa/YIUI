# cn.etetet.yiuireddot — YIUI 红点系统

**版本**: 3.0.0
**分类**: UI/YIUI
**依赖**: cn.etetet.core
**文档**: https://lib9kmxvq7k.feishu.cn/wiki/XzyawmryHitNVNk9QVtcDAftn5O

---

## 概述

`cn.etetet.yiuireddot` 是一个完整的运行时红点（消息提示）管理系统，支持树形层级关联、异步脏标更新、玩家提示开关持久化和编辑器调试堆栈。

核心特性：
- **树形传播**：叶节点数量变化自动向上递归累加到所有父节点
- **双模式更新**：支持同步实时刷新或异步脏标（每秒2次）批量刷新，防止高频调用
- **玩家开关**：每个红点可配置是否允许玩家手动关闭，关闭状态持久化到 PlayerPrefs
- **UGUI 绑定**：`RedDotBind` MonoBehaviour 组件，拖到 GameObject 即可绑定，自动管理监听生命周期
- **调试堆栈**：编辑器下（或开启 `YIUIMACRO_REDDOT_STACK` 宏）记录每次变更的调用堆栈及前后值
- **可视化编辑器**：配套 Inspector 工具，DAG 视图验证父子关系防循环引用，可视化配置父子关联
- **Key 描述系统**：运行时可通过 `GetKeyDes()` 获取红点 key 的中文描述（仅 Editor 或 STACK 宏开启时加载）

---

## 目录结构

```
cn.etetet.yiuireddot/
├── Runtime/
│   ├── Bind/
│   │   ├── RedDotBind.cs              # 通用 UGUI 绑定组件（显隐控制）
│   │   ├── RedDotTextBind.cs          # 带文字显示的绑定组件（UGUI.Text）
│   │   └── RedDotTmpBind.cs           # TextMeshPro 版本（#if TextMeshPro 条件编译）
│   ├── Config/
│   │   ├── RedDotConfigAsset.cs       # 配置资产（ScriptableObject）
│   │   └── RedDotConfigData.cs        # 单个红点配置数据（Key/父级列表/开关）
│   ├── Data/
│   │   ├── RedDotData.cs              # 红点运行时数据（核心）+ SetTips/AddOnChanged
│   │   ├── RedDotData_SetDirty.cs     # partial 类，脏标记相关字段和方法
│   │   ├── RedDotStack.cs             # 调试堆栈记录（单条记录）
│   │   ├── RedDotStackHelper.cs       # 堆栈辅助
│   │   ├── ERedDotOSType.cs           # 操作类型枚举（Count=0/Tips=1）
│   │   └── FirstRedDotChangeData.cs   # 变更溯源数据（传播链中第一个触发节点信息）
│   ├── Key/
│   │   ├── RedDotKeyAsset.cs          # Key 资产（ScriptableObject，Editor/STACK宏下加载）
│   │   ├── RedDotKeyData.cs           # Key 数据（Id + Des 描述字段）
│   │   └── RedDotKeyHelper.cs         # 反射扫描 ERedDotKeyType，提供编辑器下拉列表
│   └── Mgr/
│       ├── RedDotMgr.cs               # 单例管理器主体（初始化/配置加载/父子关联）
│       ├── RedDotMgr_API.cs           # 公开 API（GetData/AddChanged/SetCount/GetCount/SetTips）
│       ├── RedDotMgr_Dirty.cs         # 脏标异步刷新（CountDown 定时器，每0.5秒触发）
│       └── RedDotMgr_KeyAsset.cs      # Key 资产加载 + GetKeyDes() API
├── Scripts/
│   ├── Model/Share/
│   │   ├── ERedDotKeyType.cs          # 红点 Key 枚举定义（由 YIUI 工具生成，ET.Model 程序集）
│   │   └── RedDotEventType.cs         # ET 事件结构体 Event_RedDot_Change{RedDotId, Count}
│   ├── ModelView/Client/
│   │   ├── GM/EGMType_RedDot.cs       # 红点相关 GM 分类定义（EGMType.RedDot）
│   │   └── YIUIComponent/RedDot/      # YIUI UI 组件（Panel/DataItem/StackItem）
│   └── HotfixView/Client/
│       ├── GM/GM_Command_RedDot.cs    # GM 命令：打开红点调试面板（RedDotPanelComponent）
│       └── System/On_Event_RedDot_Change_Handler.cs  # ET 事件处理：转发到 RedDotMgr.SetCount
└── Editor/
    └── RedDot/
        ├── Config/                    # 配置编辑器（DAG 视图、配置界面）
        └── Key/                       # Key 编辑器（生成 ERedDotKeyType 代码）
```

---

## 核心类详解

### `RedDotMgr` — 单例管理器（4个 partial 文件）

```csharp
[YIUISingleton(1000)]
public partial class RedDotMgr : YIUISingleton<RedDotMgr>
```

**初始化流程**（`MgrAsyncInit`）:
1. `LoadConfigAsset()` — 异步加载 `RedDotConfigAsset`（资产名 `"RedDotConfigAsset"`）
2. `InitNewAllData()` — 遍历 `RedDotKeyHelper.GetKeys(force=true)` 所有 key，new RedDotData(config)
3. `InitLinkData()` — 根据 config.ParentList 建立父子双向引用
4. 加载完后立即 **Release** 资产（资产只用于初始化，不长期持有）
5. `#if UNITY_EDITOR || YIUIMACRO_REDDOT_STACK` — 额外加载 `RedDotKeyAsset`（用于描述查询）
6. `SyncSetCount=false`（默认）时，调用 `InitAsyncDirty()` 启动定时刷新

**4 个 partial 文件职责对照**：

| 文件 | 核心字段 | 核心方法 |
|------|---------|---------|
| `RedDotMgr.cs` | `m_AllRedDotData`, `m_RedDotConfigAsset` | `InitNewAllData`, `InitLinkData`, `MgrAsyncInit` |
| `RedDotMgr_API.cs` | — | `GetData`, `AddChanged`, `RemoveChanged`, `SetCount`, `GetCount`, `SetTips`, `DeletePlayerTipsPrefs` |
| `RedDotMgr_Dirty.cs` | `m_DirtyData(HashSet)`, `m_RedDotDirtyFrame=2` | `InitAsyncDirty`, `TryDirtySetCount`, `RefreshDirtyCount`, `DisposeDirty` |
| `RedDotMgr_KeyAsset.cs` | `m_AllRedDotKeyData(Dict)`, `m_RedDotKeyAsset` | `LoadKeyAsset`, `InitKeyData`, `GetKeyDes(int)` |

---

### `RedDotData` — 单个红点运行时数据（2个 partial 文件）

```csharp
public partial class RedDotData
{
    public int Key { get; }                           // = Config.Key
    public RedDotConfigData Config { get; }           // 静态配置
    public int RealCount { get; private set; }        // 真实计数（不受 Tips 影响）
    public int Count => Tips ? RealCount : 0;         // 外部可见计数（受 Tips 影响）
    public bool Tips { get; private set; }            // 当前是否允许显示
    public HashSet<RedDotData> ParentList { get; }    // 父节点集合
    public HashSet<RedDotData> ChildList { get; }     // 子节点集合
    public List<RedDotStack> StackList { get; }       // 调试堆栈（仅 Editor/宏开启时非null）

    // RedDotData_SetDirty.cs 的 partial 字段
    private bool m_ChangeDirty;                       // 是否有脏标
    private int m_DirtyCount = -1;                    // 待应用的值
    private RedDotStack m_DirtyStack;                 // 脏标时记录的堆栈（对象复用）
    private FirstRedDotChangeData m_DirtyFirstRedDotChangeData;  // 脏标时记录的溯源数据
}
```

**完整方法列表**：

| 方法 | 可见性 | 说明 |
|------|--------|------|
| `RedDotData(RedDotConfigData)` | internal | 构造，初始化父子集合，InitTips |
| `InitTips()` | private | 根据 SwitchTips 决定是否从 PlayerPrefs 读取初始 Tips 值 |
| `DeletePlayerTipsPrefs()` | internal | 删除 PlayerPrefs 中的 Tips 持久化数据 |
| `AddParent(RedDotData)` | internal | 双向绑定：this.ParentList.Add(data)，data.ChildList.Add(this) |
| `AddChild(RedDotData)` | private | ChildList.Add |
| `TrySetCount(int)` | internal | 同步设置（检查是否叶节点） |
| `SetCount(int, RedDotStack)` | private | 真正执行值变更，触发 NotifyChange |
| `NotifyChange(RedDotStack)` | private | InvokeOnChanged + 递归通知所有父节点 |
| `ChildChanged(RedDotStack)` | private | 重新累加所有子 Count，调 SetCount |
| `SetTips(bool, RedDotStack)` | internal | 修改 Tips，写 PlayerPrefs，触发 NotifyChange |
| `AddOnChanged(Action<int>)` | internal | 注册监听（立即回调一次） |
| `RemoveChanged(Action<int>)` | internal | 移除监听 |
| `InvokeOnChanged()` | private | try-catch 调用 m_OnChangedAction(Count) |
| `TryDirtySetCount(int)` | internal | 记录脏标（SetDirtyOS 捕获堆栈） |
| `RefreshDirtyCount()` | internal | 执行脏标中缓存的变更，调 ResetDirty |
| `SetDirtyOS(int)` | private | 编辑器下创建/复用 RedDotStack 和 FirstData |
| `ResetDirty()` | private | 清除脏标字段（m_ChangeDirty=false, m_DirtyCount=-1, stack/first=null） |

**重要细节**：
- `SetDirtyOS` 中使用对象复用（`??=`），多次调用只分配一次 `RedDotStack` 和 `FirstRedDotChangeData`
- 堆栈在调用 `TryDirtySetCount`（SetCount调用时机）时捕获，而非定时器触发时，保证堆栈准确
- `SetCount(count, stack)` 中 stack 非 null 时直接 `AddStack(stack)` 插入，不重新创建（父节点传播链共享同一 stack）

---

### `RedDotConfigData` — 配置数据

```csharp
[Serializable]
public class RedDotConfigData
{
    [OdinSerialize] public int Key { get; set; }
    [OdinSerialize] public List<int> ParentList { get; set; } = new List<int>();
    // true = 玩家可开关; false = 不可开关（永久提示）
    [OdinSerialize] public bool SwitchTips { get; set; } = true;
}
```

---

### `ERedDotKeyType` — 当前项目 Key 定义

```csharp
[UniqueId]
public static class ERedDotKeyType  // namespace: ET，程序集: ET.Model
{
    [LabelText("无")]   public const int None = 0;
    [LabelText("主")]   public const int Key1 = 1;
    [LabelText("商店")] public const int Key2 = 2;
    [LabelText("钻石")] public const int Key3 = 3;
    [LabelText("金币")] public const int Key4 = 4;
    [LabelText("装备")] public const int Key5 = 5;
    [LabelText("强化")] public const int Key6 = 6;
    [LabelText("升级")] public const int Key7 = 7;
}
```
- 由 YIUI 编辑器工具自动生成，**不可手动编辑**
- `[UniqueId]` 特性可能供代码分析工具（`cn.etetet.yiuicodeanalysis`）检测重复 ID

---

### `RedDotKeyHelper` — Key 反射辅助（完整 API）

```csharp
// 运行时获取所有 key（缓存，force=true 强制刷新）
List<int> GetKeys(bool force = false)

// O(1) 查询 key 是否存在
bool ContainsKey(int key)

// 仅 Editor：获取 key 的 LabelText 描述（如 "商店"）
string GetDesc(int key)

// 仅 Editor：格式化 "key_描述"（如 "2_商店"）
string GetDisplayDesc(int key)

// 仅 Editor：超过 10 个 key 时自动分组（递归，每组 10 个）
ValueDropdownList<int> SubDisplayValueDropdownList(ValueDropdownList<int> keys)
```

**反射实现**：
```csharp
var assembly = AssemblyHelper.GetAssembly("ET.Model");
Type redDotKeyType = assembly.GetType("ET.ERedDotKeyType");
FieldInfo[] fields = redDotKeyType.GetFields(BindingFlags.Public | BindingFlags.Static);
// 过滤 IsLiteral && FieldType == typeof(int)
```

---

### `RedDotKeyData` — Key 数据（运行时描述）

```csharp
public class RedDotKeyData
{
    [OdinSerialize] public int Id { get; internal set; } = 1;
    [OdinSerialize] public string Des { get; internal set; }  // 中文描述
}
```
- 由 `RedDotKeyAsset`（ScriptableObject）存储，运行时通过 `RedDotMgr.GetKeyDes(key)` 查询
- 仅在 `#UNITY_EDITOR || YIUIMACRO_REDDOT_STACK` 条件下加载

---

### `RedDotStack` — 调试堆栈单条记录

```csharp
public class RedDotStack
{
    public int Id { get; internal set; }                 // 递增 ID（从1开始，新的插在前面）
    public DateTime DataTime { get; internal set; }      // 操作时间
    public StackTrace StackTrace { get; internal set; }  // C# 调用堆栈
    public ERedDotOSType RedDotOSType { get; internal set; }  // Count=0 或 Tips=1
    public int OriginalCount { get; internal set; }      // 变更前的值
    public int ChangeCount { get; internal set; }        // 变更后的值
    public bool ChangeTips { get; internal set; }        // 当前 Tips 状态
    public FirstRedDotChangeData FirstData { get; internal set; }  // 传播链溯源
}
```

---

### `ERedDotOSType` — 操作类型枚举

```csharp
public enum ERedDotOSType
{
    [LabelText("改变数量")] Count = 0,
    [LabelText("改变提示")] Tips  = 1,
}
```

---

### `Event_RedDot_Change` — ET 事件结构体

```csharp
namespace ET
{
    public struct Event_RedDot_Change
    {
        public int RedDotId;  // 对应 ERedDotKeyType 中的 key
        public int Count;     // 目标数量
    }
}
```

---

## 脏标更新机制（完整实现）

```
// 调用时机（业务代码）：
RedDotMgr.Inst.SetCount(ERedDotKeyType.Key3, 5)
  → TryDirtySetCount(data[Key3], 5)          // RedDotMgr_Dirty.cs
    → m_DirtyData.Remove(data)               // 先移除旧的（防止重复）
    → data.TryDirtySetCount(5)               // RedDotData_SetDirty.cs
      → 检查叶节点，m_ChangeDirty=true, m_DirtyCount=5
      → SetDirtyOS(5)：此时捕获 StackTrace（编辑器下）
    → m_DirtyData.Add(data)                  // 重新加入（保证只有最新值）

// 定时器每 0.5 秒触发（YIUIInvokeEntity_CountDownAdd, Forever=true）：
RedDotUpdateRefresh() → RefreshDirtyCount()
  → foreach data in m_DirtyData:
    → data.RefreshDirtyCount()
      → SetCount(m_DirtyCount=5, m_DirtyStack)  // 执行变更
      → ResetDirty()                              // 重置：m_ChangeDirty=false, m_DirtyCount=-1
  → m_DirtyData.Clear()
```

**关键设计**：
- 同一帧多次 `SetCount(key, ...)` → 前一次被 Remove 丢弃，只保留最后一次值
- 堆栈在 `SetCount` 调用时捕获（非定时器触发时），保证调用位置准确
- 脏标 `RedDotStack`/`FirstRedDotChangeData` 对象复用（`??=`），减少 GC

**销毁时清理**（`DisposeDirty`）：
```csharp
ET.EventSystem.Instance?.YIUIInvokeEntitySyncSafety(Entity,
    new YIUIInvokeEntity_CountDownRemove { TimerCallback = RedDotUpdateRefresh });
```

---

## UGUI 绑定组件（完整实现）

### `RedDotBind` — 通用绑定

```csharp
[AddComponentMenu("YIUIFramework/红点/红点通用绑定 【RedDotBind】")]
public class RedDotBind : MonoBehaviour
{
    [SerializeField] private int m_Key;   // Inspector 下拉选择
    public bool Show { get; private set; }
    public int Count { get; private set; }
}
```

**生命周期**：
```csharp
void Awake()
{
    if (m_Key <= 0) { Show=false; Count=0; Refresh(); return; }
    OnDestroy();   // 先移除旧监听（防止重复订阅，如果 Awake 被调用多次）
    RedDotMgr.Inst?.AddChanged(m_Key, OnRedDotChangeHandler);
}

void OnDestroy()
{
    if (m_Key <= 0) return;
    if (YIUISingletonHelper.Disposing) return;  // 全局销毁时跳过，防止空引用
    RedDotMgr.Inst?.RemoveChanged(m_Key, OnRedDotChangeHandler);
}

void OnRedDotChangeHandler(int count)
{
    Show = count >= 1;
    Count = count;
    Refresh();  // gameObject.SetActive(Show); ChangeText();
}
```

**动态换绑**：
```csharp
// force=false 时，已有绑定的 key 不允许更改（报错）
// force=true 时，强制更换（先移除旧监听，再注册新监听）
public void ChangeBind(int key, bool force = false)
```

**编辑器增强**（`#if UNITY_EDITOR`）：
- 下拉菜单由 `RedDotKeyHelper.SubDisplayValueDropdownList` 生成，超10项自动分组
- 支持 `InputChangeKey` 手动输入（适用于 key 太多时）
- `OnValueChanged` 双向同步 m_Key 和 InputChangeKey

### `RedDotTextBind` / `RedDotTmpBind`

```csharp
// UGUI.Text 版
public class RedDotTextBind : RedDotBind
{
    [SerializeField] private Text m_Text;
    protected override void ChangeText() => m_Text.text = Count.ToString();
}

// TextMeshPro 版（#if TextMeshPro 条件编译）
public class RedDotTmpBind : RedDotBind
{
    [SerializeField] private TextMeshProUGUI m_Text;
    protected override void ChangeText() => m_Text.text = Count.ToString();
}
```
两者均在红点 count >= 1 时显示数量文字，count == 0 时 gameObject 隐藏（由基类 Refresh 控制）。

---

## ET 事件集成（完整流程）

```csharp
// 1. 业务层发布事件（无需直接引用 YIUIFramework）
ET.EventSystem.Instance.Publish(clientScene, new Event_RedDot_Change
{
    RedDotId = ERedDotKeyType.Key3,  // 钻石红点
    Count = 5
});

// 2. 事件处理器路由（SceneType.All）
[Event(SceneType.All)]
public class On_Event_RedDot_Change_Handler : AEvent<Scene, Event_RedDot_Change>
{
    protected override async ETTask Run(Scene currentScene, Event_RedDot_Change args)
    {
        RedDotMgr.Inst.SetCount(args.RedDotId, args.Count);
        await ETTask.CompletedTask;
    }
}
```

**解耦设计**：业务逻辑（Model/HotfixModel 层）只依赖 `ET.Event_RedDot_Change` 结构体，无需引用 `YIUIFramework`。

---

## GM 调试

```csharp
[GM(EGMType.RedDot, 1, "打开红点调试界面")]
public class GM_OpenRedDotPanel : IGMCommand
{
    public async ETTask<bool> Run(Scene clientScene, ParamVo paramVo)
    {
        // 打开 RedDotPanelComponent 可视化调试面板
        await clientScene.YIUIRoot().OpenPanelAsync<RedDotPanelComponent>();
        return true;
    }
}
```
GM 命令打开的面板（`RedDotPanelComponent`）可查看所有红点状态，包含 `RedDotDataItem`（单条数据展示）和 `RedDotStackItem`（堆栈条目展示）子组件。

---

## 调试堆栈（编辑器 / `YIUIMACRO_REDDOT_STACK` 宏）

**触发条件**：编译时定义 `YIUIMACRO_REDDOT_STACK` 宏，或在 Unity Editor 中运行

**信息内容**：
- `Id`：本数据的第 N 次操作（新操作插入到 List 头部）
- `DataTime`：操作发生时间
- `StackTrace`：C# 调用堆栈（捕获时机是 SetCount 调用时，而非定时器触发时）
- `RedDotOSType`：改变数量(Count=0) 或 改变提示(Tips=1)
- `OriginalCount / ChangeCount`：变更前后值
- `ChangeTips`：变更时的 Tips 状态
- `FirstData`：传播链溯源，记录最初触发这次级联变化的叶节点信息

**查看方式**：`RedDotMgr.AllRedDotData[key].StackList`（在 Inspector 中查看）

---

## 关键流程

### 初始化

```
YIUISingleton 初始化 (优先级 1000)
  └─ RedDotMgr.MgrAsyncInit()
     ├─ LoadConfigAsset()：YIUI Invoke 异步加载 "RedDotConfigAsset"
     │   ├─ InitNewAllData()：遍历 ERedDotKeyType，new RedDotData(config)
     │   ├─ InitLinkData()：根据 ParentList 建立父子双向引用
     │   └─ Release ConfigAsset（不长期持有）
     ├─ [Editor/STACK宏] LoadKeyAsset()：加载 "RedDotKeyAsset"，初始化 m_AllRedDotKeyData
     └─ [!SyncSetCount] InitAsyncDirty()：启动 CountDown 定时器（间隔0.5秒，Forever=true）
```

### 红点更新传播（叶节点 → 根节点）

```
业务代码: RedDotMgr.Inst.SetCount(ERedDotKeyType.Key3 /*钻石*/, 5)
  → [异步模式] TryDirtySetCount(data[Key3], 5)
  → [0.5秒后定时器触发] data[Key3].RefreshDirtyCount()
    → SetCount(5, dirtyStack)
      → RealCount = 5
      → NotifyChange(stack)
        → InvokeOnChanged() → RedDotBind.OnRedDotChangeHandler(5) → UI 显示数字5
        → foreach parent in ParentList (e.g., Key2=商店)
          → parent.ChildChanged(stack)
            → count = sum(child.Count for child in ChildList)  // 注意用 Count 而非 RealCount
            → parent.SetCount(count, stack)  // 共享同一 stack 对象
              → ... 递归向上直到根节点 Key1=主
```

### UI 绑定订阅

```
RedDotBind.Awake()  [m_Key=2, 商店]
  → OnDestroy()：移除旧监听（防重复）
  → RedDotMgr.Inst.AddChanged(2, OnRedDotChangeHandler)
    → data[Key2].AddOnChanged(action)
      → m_OnChangedAction += action
      → InvokeOnChanged(Count=0)  // 立即回调，UI 初始化为隐藏状态
```

---

## 依赖关系

### package.json 声明依赖
- `cn.etetet.core`

### 运行时隐式依赖

```
cn.etetet.yiuireddot
├── YIUIFramework                   （YIUISingleton, BoolPrefs, AssemblyHelper, YIUIInvoke 等）
├── ET Framework                    （ETTask, AEvent, EventSystem, Scene 等）
├── Sirenix.OdinInspector           （Inspector 编辑器 UI，ValueDropdown 等）
└── Sirenix.Serialization           （OdinSerialize，用于 RedDotConfigData 序列化）
```

### 被其他模块使用
- **业务逻辑层** → `ET.Event_RedDot_Change` 事件（松耦合，无需直接引用 YIUIFramework）
- **UI 层** → `RedDotBind` / `RedDotTextBind` / `RedDotTmpBind` 组件（纯 Unity 组件挂载）
- **GM 调试** → `GM_Command_RedDot`（打开可视化调试面板）

---

## 当前项目红点树结构

根据 `ERedDotKeyType` 和配置，当前项目的红点树为：

```
Key1 (主)
└─ Key2 (商店)
   ├─ Key3 (钻石)    ← 叶节点，可 SetCount
   ├─ Key4 (金币)    ← 叶节点，可 SetCount
   ├─ Key5 (装备)    ← 叶节点，可 SetCount
   │   └─ Key6 (强化) ← 叶节点，可 SetCount（根据配置决定是否为装备的子节点）
   └─ Key7 (升级)    ← 叶节点，可 SetCount
```
*注：具体父子关系由 `RedDotConfigAsset.asset` 中的配置决定，以上为推测，以实际配置为准*

---

## 注意事项与最佳实践

1. **只有叶节点可 SetCount**：非叶节点（有 ChildList 的节点）由子节点自动累加，直接 SetCount 会 `LogError` 并返回 false。

2. **Tips 仅影响 Count，不影响 RealCount**：父节点累加用的是 `Count`（受 Tips 影响），关闭提示的子节点贡献 0 给父节点。

3. **编译依赖**：`ERedDotKeyType` 位于 `ET.Model` 程序集，新增 key 后必须重新 ET 编译才能在运行时生效（`RedDotKeyHelper.GetKeys()` 通过反射读取）。

4. **AddChanged 立即回调**：注册监听时会以当前值立即回调一次，UI 初始化无需额外刷新逻辑。

5. **脏标模式同值优化**：若 `SetCount(key, sameValue)` 与当前 `RealCount` 相同，`TryDirtySetCount` 会调用 `ResetDirty()` 并返回 false，不加入脏集合，避免无效刷新。

6. **ChangeBind 注意事项**：默认 `force=false` 时，已绑定的组件不允许更换 key（需手动传 `force=true`），防止意外覆盖。

7. **RedDotTmpBind 条件编译**：需要项目定义 `TextMeshPro` 宏（通常由 TMP 包自动添加），否则类不参与编译。

8. **GM 命令弱依赖**：`GM_Command_RedDot.cs` 注释说明若项目未引用 GM 包则删除此文件，属于可选集成。

9. **脏标只保留最新值**：同一帧对同一 key 多次调用 `SetCount`，只有最后一次生效（HashSet 先 Remove 再 Add）。

10. **定时器管理**：使用 `YIUIInvokeEntity_CountDownAdd`（`Forever=true`）而非 Unity Update，跟随 Entity 生命周期，`OnDispose` 时通过 `YIUIInvokeEntity_CountDownRemove` 安全清理。
