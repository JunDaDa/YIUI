# cn.etetet.referencecollector

## 概述

`ET.ReferenceCollector` 是一个 Unity MonoBehaviour 组件，用于在 Inspector 面板中以 key-value 方式管理 GameObject 及其他 UnityEngine.Object 引用。核心思想是将多个子对象引用集中存储在一个组件上，通过字符串 key 快速检索，避免在代码中反复使用 `Find` 或 `GetComponent` 查找对象。

- **版本**：3.0.0
- **Unity**：2022.3+
- **无外部依赖**（纯 Unity 内置）

---

## 目录结构

```
cn.etetet.referencecollector/
├── Runtime/
│   ├── ReferenceCollector.cs        # 核心 MonoBehaviour 组件
│   ├── GameObjectHelper.cs          # GameObject 扩展方法
│   └── ET.ReferenceCollector.asmdef
├── Editor/
│   ├── ReferenceCollectorEditor.cs  # 自定义 Inspector 编辑器
│   └── ET.ReferenceCollector.Editor.asmdef
└── package.json
```

---

## 核心类

### `ReferenceCollectorData`（可序列化数据结构）

```csharp
[Serializable]
public class ReferenceCollectorData
{
    public string key;
    public Object gameObject;  // UnityEngine.Object
}
```

以 `(key, UnityEngine.Object)` 键值对形式存储单条引用数据，支持 Unity 序列化。

---

### `ReferenceCollectorDataComparer`

实现 `IComparer<ReferenceCollectorData>`，使用 `StringComparison.Ordinal`（字节级字符串比较）对 `ReferenceCollectorData` 列表按 key 排序，性能优良。

---

### `ReferenceCollector : MonoBehaviour, ISerializationCallbackReceiver`

**核心组件**，挂载到 Prefab 或 GameObject 上。

#### 字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `data` | `List<ReferenceCollectorData>` | 可序列化列表（持久化到 Prefab/Scene 文件） |
| `dict` | `Dictionary<string, Object>` | 运行时查找字典（不序列化，由反序列化回调重建） |

#### 公开方法（Runtime）

| 方法 | 说明 |
|------|------|
| `T Get<T>(string key) where T : class` | 泛型获取，按 key 从 dict 返回对应引用，转换为 T 类型 |
| `Object GetObject(string key)` | 非泛型版本，返回原始 UnityEngine.Object |
| `OnAfterDeserialize()` | 反序列化后将 `data` List 重建为 `dict`，供运行时高效查找 |
| `OnBeforeSerialize()` | 空实现 |

#### Editor-Only 方法（`#if UNITY_EDITOR`）

| 方法 | 说明 |
|------|------|
| `Add(string key, Object obj)` | 通过 SerializedObject API 添加或更新 key-value 对 |
| `Remove(string key)` | 删除指定 key 的数据 |
| `Clear()` | 清空所有引用 |
| `Sort()` | 按 key 排序 data 列表 |

---

### `GameObjectHelper`（ET.Client 命名空间）

`GameObject` 的扩展方法类：

```csharp
public static T Get<T>(this GameObject gameObject, string key) where T : class
```

语法糖，等价于：
```csharp
gameObject.GetComponent<ReferenceCollector>().Get<T>(key)
```

失败时抛出带上下文信息（GameObject 名称 + key）的异常，方便调试。

---

### `ReferenceCollectorEditor : Editor`（仅 Editor）

为 `ReferenceCollector` 自定义的 `Inspector` 面板，提供以下功能：

| 功能 | 说明 |
|------|------|
| 添加引用 | 自动生成 GUID Hash 作为默认 key |
| 全部删除 | 清空所有引用 |
| 删除空引用 | 清理 null 对象引用 |
| 排序 | 按 key 字母序排序 |
| 搜索 key | 输入 key 可即时预览对应 Object |
| 拖拽支持 | 将 Asset/GameObject 拖入 Inspector 区域自动添加（以对象名为 key） |
| 逐条删除 | 每条引用旁有 X 按钮单独删除 |

---

## 实现原理

### 序列化与运行时字典分离

Unity 不能直接序列化 `Dictionary`，因此采用两层结构：
1. **序列化层**：`List<ReferenceCollectorData> data`——写入 `.prefab` / `.unity` 文件
2. **运行时层**：`Dictionary<string, Object> dict`——由 `OnAfterDeserialize()` 从 `data` 重建

```
Prefab 文件 (YAML)
    └── data: [{key: "Panel", gameObject: fileID:xxx}, ...]
         ↓ OnAfterDeserialize()
    dict: {"Panel" → GameObject, "Button" → Button, ...}
         ↓ 运行时
    referenceCollector.Get<Button>("Button")
```

### Editor 修改通过 SerializedObject API

所有编辑器修改（Add/Remove/Clear/Sort）都经过 `SerializedObject` + `SerializedProperty`，确保：
- 支持 Undo/Redo
- 正确标记 Prefab dirty，触发保存
- 与 Unity 序列化系统兼容

---

## 关键流程

### 运行时获取引用

```
游戏启动
  → Unity 反序列化 Prefab
  → OnAfterDeserialize() 重建 dict
  → 代码调用 referenceCollector.Get<Button>("CloseButton")
  → dict.TryGetValue("CloseButton") → 返回 Button 组件
```

### 编辑器拖拽添加引用

```
拖拽 GameObject/Asset 到 Inspector
  → DragAndDrop.AcceptDrag()
  → AddReference(dataProperty, o.name, o)
  → SerializedProperty 插入新元素
  → ApplyModifiedProperties() 写回 Prefab
```

---

## 依赖关系

```
cn.etetet.referencecollector
  ← 无外部 package 依赖
  ← 被 cn.etetet.ui 及所有 UI-related package 广泛使用
  ← 被 ET.Client 代码通过 GameObjectHelper 扩展方法调用
```

---

## Round 2 补充

### 方法签名完整列表

**ReferenceCollector.cs**
- `void Add(string key, Object obj)` [Editor Only]
- `void Remove(string key)` [Editor Only]
- `void Clear()` [Editor Only]
- `void Sort()` [Editor Only]
- `T Get<T>(string key) where T : class`
- `Object GetObject(string key)`
- `void OnBeforeSerialize()`
- `void OnAfterDeserialize()`

**GameObjectHelper.cs**
- `static T Get<T>(this GameObject gameObject, string key) where T : class`

**ReferenceCollectorEditor.cs**
- `void OnEnable()`
- `override void OnInspectorGUI()`
- `void DelNullReference()`
- `void AddReference(SerializedProperty dataProperty, string key, Object obj)`

### 代码示例

**UI 代码中获取子控件：**
```csharp
// 方式1：通过 ReferenceCollector 直接获取
var rc = go.GetComponent<ReferenceCollector>();
var closeBtn = rc.Get<Button>("CloseButton");

// 方式2：通过 GameObjectHelper 扩展方法（ET.Client 命名空间）
var closeBtn = go.Get<Button>("CloseButton");
```

**Editor 脚本中添加引用：**
```csharp
var rc = gameObject.GetComponent<ReferenceCollector>();
rc.Add("MyPanel", someGameObject);
```

### 注意事项

1. **key 唯一性**：`OnAfterDeserialize` 中遇到重复 key 会跳过后续项，保留第一个
2. **null 引用**：运行时 `dict` 可能包含 null 值（Object 被删除后），调用方需自行处理
3. **Editor Only 方法**：`Add/Remove/Clear/Sort` 在构建后不可用，仅用于编辑器工具脚本
4. **命名空间**：`GameObjectHelper` 在 `ET.Client` 命名空间中，需要 using 引入

### 性能考量

- `OnAfterDeserialize` 在每次 Prefab 实例化时执行，时间复杂度 O(n)，n 为引用数量
- 运行时 `Get<T>` 为 O(1) 字典查找，性能优秀
- 编辑器排序使用 `Ordinal` 字符串比较，比文化感知比较更快
