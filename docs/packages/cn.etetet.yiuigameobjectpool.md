# cn.etetet.yiuigameobjectpool

**版本**: 2.0.2
**分类**: UI/YIUI
**描述**: 游戏对象缓存池，支持异步加载、自动回收、超时管理和容量控制
**依赖**: 无直接 package 依赖（运行时依赖 YIUIFramework、ET EventSystem）

---

## 目录结构

```
cn.etetet.yiuigameobjectpool/
├── Runtime/
│   └── GameObjectPool/
│       ├── YIUIGameObjectPool.cs              # 主单例，对外暴露 Get/Put API
│       ├── YIUIAutoRecycleAsyncObjectPool.cs  # 内部池实现，管理使用/缓存状态
│       ├── YIUIGameObjectPoolAutoRelease.cs   # MonoBehaviour，GameObject 销毁时自动通知池
│       ├── YIUIGameObjectPoolInfo.cs          # MonoBehaviour，Inspector 配置池参数
│       └── YIUIGameObjectPoolTrigger.cs       # MonoBehaviour，绑定 OnEnable/OnDisable 自动借还
├── Scripts/
│   ├── Core/Share/
│   │   └── IYIUIGameObjectPoolSettingsConfig.cs  # 配置接口（统一组件与 Luban 配置表）
│   └── HotfixView/Client/
│       └── YIUIInvokeGetGameObjectPoolSettingsHandler.cs  # Invoke 处理器，查询 Luban 配置
└── CodeMode/Model/ClientServer|Client/
    ├── ConfigExtend/
    │   └── GameObjectPoolSettingsConfig_Extend.cs  # Luban 生成类的扩展，实现 IYIUIGameObjectPoolSettingsConfig
    └── LubanGen/Config/
        ├── GameObjectPoolSettingsConfig.cs          # Luban 生成：单条配置
        └── GameObjectPoolSettingsConfigCategory.cs  # Luban 生成：配置表（按 ResName 索引）
```

---

## 核心类/接口

### `YIUIGameObjectPool`（ET namespace）
- **类型**: `YIUIMonoSingleton<YIUIGameObjectPool>`，实现 `ITimeProvider`
- **标注**: `[YIUISingleton]`
- **职责**: 整个池系统的门面，跨场景保持（DontDestroyOnLoad），在 Update 中驱动各子池的超时检查与空闲回收

| 方法/字段 | 说明 |
|-----------|------|
| `async ETTask<GameObject> Get(string resName, Transform parent = null)` | 异步获取对象；parent 为 null 时跳过 SetParent |
| `bool Put(GameObject obj)` | 归还对象，还原 parent 到 PoolRoot；应用退出时直接返回 true |
| `void Update()` | 每帧累加 `Time`，遍历子池调用 Update，每帧最多回收 1 个空闲子池（break 优化）|
| `IYIUIGameObjectPoolSettingsConfig GetSettings(string resName)` | 通过 EventSystem Invoke 查询 Luban 配置 |
| `internal void DestroyRemove(GameObject obj)` | 对象被直接 Destroy 时调用，仅清理字典引用，不销毁 |
| `float Time { get; private set; }` | 单调递增计时器（`ITimeProvider` 实现），场景重载时重置为 0 |

**关键私有字段**:
- `Dictionary<string, YIUIAutoRecycleAsyncObjectPool> m_Pools` — 按资源名维护的子池字典
- `Dictionary<GameObject, string> m_GameObjectToResName` — 反向映射，归还/销毁时定位子池
- `const bool m_SceneLoadedResetPool = true` — 硬编码，场景切换强制清空
- `m_PoolRoot` — 隐藏的根节点（`hideFlags = HideFlags.HideInHierarchy`，`SetActive(false)`，`DontDestroyOnLoad`）

**内部消息结构体**（定义在同文件，ET namespace）:
```csharp
public struct YIUIInvokeGetGameObjectPoolSettings { public string ResName; }
```

---

### `YIUIAutoRecycleAsyncObjectPool`（YIUIFramework namespace，internal）
- **类型**: 实现 `IRefPool`，通过 `RefPool` 自身也被对象池化
- **职责**: 单个资源名的池，管理"使用中"、"缓存中"、"超量备用"三个集合

**三个集合**:

| 集合 | 类型 | 说明 |
|------|------|------|
| `m_Uses` | `Dictionary<GameObject, PoolVo>` | 当前被借出使用的对象 |
| `m_Pools` | `List<PoolVo>` | 归还后等待复用的对象（正常资源） |
| `m_MaxPools` | `List<PoolVo>` | 超过 MaxCacheCount 时返回的替代对象 |

**配置参数**（来自 `IYIUIGameObjectPoolSettingsConfig`，仅在首次实例化时初始化，`m_InitPoolInfo` 守卫）:

| 参数 | 字段默认值 | 说明 |
|------|-----------|------|
| `m_Timeout` | `0` | 对象最大显示时间（秒）；`<=0` 永不过期 |
| `m_MaxCacheCount` | `int.MaxValue` | 同时使用数量上限；若配置 `>0` 但无 `MaxCacheCountNewResName` 则强制回退为无限 |
| `m_CacheTime` | `0` | 对象在缓存池中的保留时间（秒）；`<=0` 永久保留 |
| `m_MinCacheCount` | `int.MaxValue` | 无超时场景下 Put 时允许保留的最大缓存数；超出则直接 Destroy |
| `m_MaxCacheCountNewResName` | `null` | 超量时使用的替代资源名 |
| `m_HaveUpdate` | `false` | `= m_Timeout > 0 || m_CacheTime > 0`，控制每帧是否执行检查 |

> ⚠️ **重要**：`m_MinCacheCount` 默认为 `int.MaxValue`（不限制缓存数量）。只有配置表中 `MinCacheCount >= 0` 才会覆盖。当 `m_CacheTime <= 0` 且 `m_MinCacheCount < int.MaxValue` 时，Put 时超过 `m_MinCacheCount` 的对象直接销毁而不缓存。

**嵌套私有类 `PoolVo : IRefPool`**:
```csharp
private class PoolVo : IRefPool
{
    public float EndTime;   // 超时时间点（Timeout 或 CacheTime）
    public GameObject Value;
    public bool IsMax;      // true = 来自 MaxCacheCountNewResName
    public void Recycle() { EndTime = 0; Value = null; IsMax = false; }
}
```

**空闲判定**（`IsIdle()`）:
- 条件：`m_Uses.Count < 1 && m_Pools.Count < 1`（**不含 m_MaxPools**）
- 连续空闲超过 `m_IdleTimeout = 60` 秒后返回 `true`
- 由主单例在 `Update()` 中检测并调用 `RefPool.Put(pool)` 回收子池本身

**Update 检查逻辑**（每帧最多回收 1 个对象，break 策略）:
- `CheckTimeout()` — 遍历 `m_Uses`，`Time >= EndTime` 时自动 Put 归还
- `CheckCacheTime()` — 遍历 `m_Pools`，超时且超过 MinCacheCount 时销毁
- `CheckMaxPoolCacheTime()` — 遍历 `m_MaxPools`，超时时销毁（同样使用 `m_CacheTime` 判定）

---

### `YIUIGameObjectPoolAutoRelease`（YIUIFramework namespace）
- **类型**: `MonoBehaviour`
- **添加时机**: `InstantiateGameObjectAsync` 时自动 `AddComponent`（不在菜单中显示）
- **公开字段**: `public EntityRef<Entity> m_EntityRef` — 在 `InstantiateGameObjectAsync` 中赋值
- **职责**: `OnDestroy` 时通知池清理 `m_Uses` 引用，并通过 `YIUIInvokeEntity_ReleaseInstantiate` 触发资产释放

```csharp
private void OnDestroy()
{
    if (YIUISingletonHelper.IsQuitting) return;
    YIUIGameObjectPool.Inst?.DestroyRemove(this.gameObject);  // 清引用，不销毁
    if (m_EntityRef.Entity == null || m_EntityRef.Entity.IsDisposed) return;
    EventSystem.Instance?.YIUIInvokeEntitySync(m_EntityRef,
        new YIUIInvokeEntity_ReleaseInstantiate { obj = this.gameObject });  // 释放 Addressable 资产
}
```

---

### `YIUIGameObjectPoolInfo`（YIUIFramework namespace）
- **类型**: `SerializedMonoBehaviour`，实现 `IYIUIGameObjectPoolSettingsConfig`
- **添加方式**: 设计时手动挂载到预制体根节点
- **职责**: 在预制体上直接配置池参数（Inspector 可视化），作为配置表的后备（优先级低于 Luban 配置）

---

### `YIUIGameObjectPoolTrigger`（YIUIFramework namespace）
- **类型**: `SerializedMonoBehaviour`
- **职责**: 绑定 GameObject 的 OnEnable/OnDisable 生命周期自动借还对象池
- **使用场景**: 配合 `UIDataBindActive`（YIUI 数据绑定系统）使用，节点激活时加载，关闭时回收
- **AddComponentMenu**: `"YIUIBind/Data/对象池触发器 【PoolTrigger】 YIUIDataGameObjectPoolTriggerYIUIBind"`

**关键运行时字段**（`[NonSerialized]`）:
- `bool m_Loading` — 正在加载的锁，防止重入
- `GameObject m_GameObject` — 当前持有的对象引用
- `ETCancellationToken m_CancelToken` — 用于取消进行中的加载协程

**Inspector 序列化字段**:
- `string m_ResName` — 资源名称（只读显示）
- `bool m_DelayTrigger` / `int m_DelayTime` — 延迟触发（毫秒）
- `bool m_Offset` / `Vector3 m_PositionOffset/RotationOffset/ScaleOffset` — 偏移设置

**Load() 流程**:
```
OnEnable()
  → 获取 CoroutineLock（同一 GetHashCode() 防并发重入）
  → 可选 WaitAsync(m_DelayTime ms)
  → YIUIGameObjectPool.Inst.Get(m_ResName, transform)
  → 检查 m_CancelToken（已取消则 Put 归还）
  → 应用 Offset（若启用）
```

**OnDisable() 流程**:
```
OnDisable()
  → m_CancelToken.Cancel()（取消进行中的加载）
  → DisablePut(m_GameObject)
      → 获取 CoroutineLock
      → 等待 1 帧（WaitFrameAsync，防止同帧禁用/启用的闪烁）
      → YIUIGameObjectPool.Inst.Put(obj)
```

**Editor 专属功能**:
- `GameObject m_SourceObject` — 预制体拖拽槽，OnValueChanged 时自动提取 ResName
- `GetSourceObject()` — 通过 AssetDatabase 查找预制体，可检测重名资源
- `LoadObject()` / `DestroyObject()` — 调试用，实例化/删除预制体
- `SyncOffset()` — 将当前 GameObject Transform 同步为偏移值

---

### `IYIUIGameObjectPoolSettingsConfig`（YIUIFramework namespace）
- **类型**: interface
- **实现类**: `YIUIGameObjectPoolInfo`（MonoBehaviour）、`GameObjectPoolSettingsConfig`（Luban 生成 + Extend）
- **属性**:
  ```csharp
  float Timeout { get; }
  int MaxCacheCount { get; }
  float CacheTime { get; }
  int MinCacheCount { get; }
  string MaxCacheCountNewResName { get; }
  ```

---

### `YIUIInvokeGetGameObjectPoolSettingsHandler`（ET.Client namespace）
- **标注**: `[Invoke]`
- **职责**: 响应 `YIUIInvokeGetGameObjectPoolSettings` 消息，从 `GameObjectPoolSettingsConfigCategory.Instance` 按 ResName 查询配置，未找到时返回 null

---

## 实现原理

### 配置优先级
```
Luban 配置表（GameObjectPoolSettingsConfigCategory）> null
    → 若 null，则取 obj.GetComponent<YIUIGameObjectPoolInfo>()
    → 若仍 null，使用默认值（无限制）
配置仅在首次实例化时读取（m_InitPoolInfo 守卫，之后不再更新）
```

### Get 流程
```csharp
// 伪代码
YIUIGameObjectPool.Get(resName, parent)
  → pool = GetAutoRecycleAsyncObjectPool(resName)  // 如不存在则从 RefPool 取并初始化
  → go = await pool.Get()
      → m_Uses.Count >= m_MaxCacheCount ?
          GetByMaxPool()  // 从 m_MaxPools.Pop() 或 Instantiate(MaxCacheCountNewResName)
        : GetByPool()     // 从 m_Pools.Pop() 或 Instantiate(resName)
      → 首次 Instantiate：AddComponent<YIUIGameObjectPoolAutoRelease>()，读取配置
      → result.EndTime = m_Timer.Time + m_Timeout（若 Timeout <= 0 则 EndTime = 0）
      → m_Uses.Add(go, poolVo)
  → if (parent != null) { go.SetParent(parent); localPos/Rot/Scale = identity }
  → m_GameObjectToResName.Add(go, resName)
  → return go
```

### Put 流程
```csharp
YIUIGameObjectPool.Put(obj)
  → if IsQuitting → return true（跳过）
  → m_GameObjectToResName.Remove(obj, out resName)
  → pool.Put(obj)
      → m_Uses.Remove(obj) → 取出 poolVo
      → if (m_CacheTime <= 0 && m_MinCacheCount >= 0):
          if m_Pools.Count >= m_MinCacheCount → DestroyRemove(poolVo); return false
      → poolVo.EndTime = m_Timer.Time + m_CacheTime（若 CacheTime <= 0 则 EndTime = 0）
      → poolVo.IsMax ? m_MaxPools.Add(poolVo) : m_Pools.Add(poolVo)
  → if result: obj.transform.SetParent(m_PoolRootTransform)
```

### 场景切换处理
```
OnSceneLoadedHandler()
  → ClearPool()
      → foreach pool: RefPool.Put(pool)（注：Clear 前未 Destroy GameObject，由子池 Clear 处理）
      → Destroy(m_PoolRoot); m_PoolRoot = null; Time = 0; 字典.Clear()
  → InitPool()
      → Time = 0; 重建 m_PoolRoot（HideInHierarchy, DontDestroyOnLoad）
```

---

## 关键流程图

### 对象借出时序
```
调用方                    YIUIGameObjectPool          YIUIAutoRecycleAsyncObjectPool
  |--Get(resName,parent)->|                            |
  |                       |--GetAutoRecyclePool--------|
  |                       |--pool.Get()--------------->|
  |                       |                            |--m_Uses.Count < MaxCacheCount?
  |                       |                            |    YES → GetByPool() (m_Pools.Pop 或 Instantiate)
  |                       |                            |    NO  → GetByMaxPool() (m_MaxPools.Pop 或 Instantiate MaxRes)
  |                       |                            |--首次Instantiate：AddComponent<AutoRelease>，读取配置
  |                       |                            |--设 EndTime，加入 m_Uses
  |                       |<--返回 GameObject---------|
  |--SetParent(parent)----|
  |--m_GameObjectToResName.Add(go, resName)
  |<--返回 go-------------|
```

### 超时自动回收时序
```
每帧 Update()
  YIUIGameObjectPool:
    Time += deltaTime
    foreach pool in m_Pools.Values:
      pool.Update():
        if HaveUpdate:
          CheckTimeout():  遍历 m_Uses，Time >= EndTime → Put 归还（每帧最多 1 个）
          CheckCacheTime(): 遍历 m_Pools，Time >= EndTime && Count > MinCacheCount → Destroy
          CheckMaxPoolCacheTime(): 遍历 m_MaxPools，Time >= EndTime → Destroy
      if pool.IsIdle(): (m_Uses<1 && m_Pools<1 且空闲超60s)
        m_Pools.Remove; RefPool.Put(pool); break  (每帧最多回收 1 个子池)
```

---

## 代码示例

### 手动借还（代码层）
```csharp
// 借出
var go = await YIUIGameObjectPool.Inst.Get("EffectFire", transform);
if (go == null) return;  // MaxCacheCount 超出且无替代资源时可能返回 null

// 使用 go ...

// 归还
YIUIGameObjectPool.Inst.Put(go);
```

### 零代码自动借还（组件层）
1. 在空节点挂载 `YIUIGameObjectPoolTrigger`
2. Editor 中拖入预制体到 m_SourceObject 槽，自动填写 m_ResName
3. 可选设置延迟触发、偏移
4. 节点 `SetActive(true)` → 自动 Get；`SetActive(false)` → 等 1 帧后自动 Put

### 配置（Luban 表）
```
GameObjectPoolSettings.xlsx 字段：
  ResName              资源名（主键）
  Timeout              显示超时（秒），0=不限
  MaxCacheCount        最大同时使用数，0=不限；>0 必须填 MaxCacheCountNewResName
  MaxCacheCountNewResName 超量时的替代资源名
  CacheTime            缓存保留时间（秒），0=永久
  MinCacheCount        无超时时缓存上限，-1=不限
```

---

## 依赖关系

### 依赖的 Package / 系统
| 依赖 | 用途 |
|------|------|
| `cn.etetet.yiuiinvoke` | `YIUIInvokeEntity_InstantiateGameObject`（异步实例化）、`YIUIInvokeEntity_ReleaseInstantiate`（Addressable 释放）、`YIUIInvokeEntity_CoroutineLock`（协程锁）、`YIUIInvokeEntity_WaitAsync/WaitFrameAsync` |
| `cn.etetet.yiuiframework` | `YIUIMonoSingleton`、`RefPool`、`IRefPool`、`YIUISingletonHelper`、`ITimeProvider` |
| `cn.etetet.core` / ET | `Entity`、`EntityRef`、`EventSystem`、`ETTask`、`ETCancellationToken` |
| `cn.etetet.yiuiluban` | `GameObjectPoolSettingsConfigCategory`（Luban 配置表读取） |
| Sirenix Odin Inspector | Inspector 序列化和 UI 展示（`SerializedMonoBehaviour`、`[BoxGroup]` 等） |

### 被依赖情况
- 任何需要 GameObject 缓存的系统均可通过 `YIUIGameObjectPool.Inst.Get/Put` 使用
- `YIUIGameObjectPoolTrigger` 设计为零代码集成（纯组件配置，通常配合 UIDataBindActive 数据绑定）

---

## 设计亮点与注意事项

1. **子池本身被池化**：`YIUIAutoRecycleAsyncObjectPool` 实现 `IRefPool`，空闲后通过 `RefPool.Put` 回收，减少 GC 压力

2. **超量替代资源机制**：MaxCacheCount 超出时使用轻量替代预制体（保留碰撞等逻辑，去除渲染），适用于技能特效等场景；**必须配置 MaxCacheCountNewResName，否则强制无上限**

3. **帧级别优化**：所有 Check 方法每帧最多回收 1 个对象（break 策略），主单例 Update 每帧最多回收 1 个空闲子池

4. **配置双来源**：运行时从 Luban 表（热更支持）或预制体组件获取配置，Luban 优先，且**仅首次实例化时读取**（之后配置修改不生效）

5. **PoolRoot 对 Inspector 不可见**：`hideFlags = HideFlags.HideInHierarchy`，在 Hierarchy 窗口中隐藏，避免误操作

6. **Put 逻辑中的 MinCacheCount 语义**：当 `CacheTime <= 0` 时，Put 会检查 `m_Pools.Count >= m_MinCacheCount`；若超出，该对象直接 Destroy 而不进入缓存池。这意味着 MinCacheCount 在无超时配置下实际充当"最大缓存数"

7. **IsIdle() 不计 m_MaxPools**：只要 m_MaxPools 有对象但 m_Uses 和 m_Pools 均空，仍会计入空闲计时，60 秒后子池被回收（m_MaxPools 对象一同销毁）

8. **注意**：`m_SceneLoadedResetPool` 硬编码为 `true`，场景切换强制清空所有池，确保使用方在场景切换前完成回收或可接受对象丢失

9. **AutoRelease 的 EntityRef**：`m_EntityRef` 是 `public` 字段，在 `InstantiateGameObjectAsync` 中直接赋值，用于跨场景追踪 Addressable 资产所有者
