# cn.etetet.yiuieffect

## 概述

**版本**: 3.0.0
**分类**: UI/YIUI
**描述**: YIUI 特效系统，集成了两个第三方 Unity UI 特效库（Coffee.UIEffect 和 Coffee.UIParticle），并提供 YIUI 数据绑定扩展。

该包为 YIUI 框架提供 UI 特效能力，包含：
1. **UIEffect** - 对 UI 图形元素应用色调/颜色/采样/过渡/渐变/阴影等着色器效果
2. **UIParticle** - 在 UI Canvas 内渲染粒子效果（无需 Camera 或 RenderTexture）
3. **YIUIBind 数据绑定** - 通过数据绑定系统驱动 UIEffect（如置灰效果）

---

## 目录结构

```
cn.etetet.yiuieffect/
├── package.json
├── Ignore.ET.YIUI.Effect.asmdef          # 主程序集定义
├── Editor/
│   ├── UIEffect/                          # UIEffect 编辑器工具
│   │   ├── UIEffectEditor.cs              # UIEffect Inspector 编辑器
│   │   ├── UIEffectProjectSettingsEditor.cs
│   │   ├── UIEffectReplicaEditor.cs       # Replica 编辑器
│   │   └── UIEffectTweenerEditor.cs       # Tweener 编辑器
│   ├── UIParticleEditor/                  # UIParticle 编辑器工具
│   │   ├── UIParticleEditor.cs
│   │   ├── UIParticleMenu.cs
│   │   └── AnimatablePropertyEditor.cs
│   └── YIUIAssemblyReference.asmref
├── Plugins/
│   ├── UIEffect/
│   │   ├── Runtime/                       # UIEffect 运行时核心
│   │   │   ├── UIEffect.cs                # 主组件（具体实现）
│   │   │   ├── UIEffectBase.cs            # 抽象基类
│   │   │   ├── UIEffectContext.cs         # 效果参数上下文
│   │   │   ├── UIEffectReplica.cs         # 效果复制组件
│   │   │   ├── UIEffectTweener.cs         # 补间动画组件
│   │   │   ├── UIEffectProjectSettings.cs # 项目级设置
│   │   │   ├── Enums.cs                   # 所有效果枚举定义
│   │   │   ├── Internal/                  # 内部工具（材质缓存、对象池等）
│   │   │   └── Utilities/                 # 代理和辅助工具
│   │   └── Shaders/
│   │       ├── UIEffect.shader            # 主着色器
│   │       └── UIEffect.cginc             # 着色器公共头文件
│   └── UIParticle/
│       ├── UIParticle.cs                  # 粒子 UI 主组件
│       ├── UIParticleRenderer.cs          # 粒子渲染器
│       ├── UIParticleUpdater.cs           # 粒子更新器（PlayerLoop）
│       ├── UIParticleAttractor.cs         # 粒子吸引器
│       ├── UIParticleProjectSettings.cs   # 粒子项目设置
│       ├── AnimatableProperty.cs          # 可动画属性
│       └── Internal/ + Utilities/
└── Runtime/
    └── YIUIBind/
        └── Data/
            └── UIDataBindGray.cs          # YIUI 数据绑定置灰组件
```

---

## 核心类说明

### UIEffectBase（抽象基类）

**命名空间**: `Coffee.UIEffects`
**继承**: `UIBehaviour`, `IMeshModifier`, `IMaterialModifier`, `ICanvasRaycastFilter`
**标注**: `[ExecuteAlways]`, `[DisallowMultipleComponent]`

核心职责：通过 Unity UI 的 Mesh 修改接口和 Material 修改接口，向 `Graphic` 组件注入着色器效果。

#### 关键字段
```csharp
// 静态对象池，所有 UIEffectBase 实例共享
private static readonly InternalObjectPool<UIEffectContext> s_ContextPool;

private Graphic _graphic;       // 缓存的 Graphic 引用
private Material _material;     // 当前生效的修改材质（由 MaterialRepository 管理）
private UIEffectContext _context; // 从对象池 Rent 的效果上下文
private Action _setVerticesDirtyIfVisible; // 延迟 Mesh 刷新回调
```

#### 核心属性
| 属性 | 类型 | 说明 |
|------|------|------|
| `graphic` | `Graphic` | 懒加载的 GetComponent<Graphic>() |
| `effectId` | `uint` | 虚属性，默认返回 GetInstanceID()，UIEffectReplica 复用 target.effectId |
| `actualSamplingScale` | `float` | 虚属性，默认返回 1，UIEffect/UIEffectReplica 各自实现 |
| `canModifyShape` | `bool` | 是否允许修改 Mesh 形状，默认 true |
| `context` | `UIEffectContext` | 懒加载，首次访问时从对象池 Rent 并调用 UpdateContext |
| `transitionRoot` | `RectTransform` | 过渡效果的根变换，默认为自身，UIEffectReplica 可复用 target 的 |

#### 关键方法
```csharp
// 生命周期：OnEnable → UpdateContext → SetMaterialDirty + SetVerticesDirty
protected override void OnEnable();

// OnDisable：从 UIExtraCallbacks 解除注册 + 释放材质实例
protected override void OnDisable();

// OnDestroy：清空引用并将 context 归还对象池
protected override void OnDestroy();

// IMeshModifier 实现：调用 context.ModifyMesh() 写入顶点 UV 数据
public virtual void ModifyMesh(VertexHelper vh);

// IMaterialModifier 实现：通过 MaterialRepository 获取/创建带 UIEffect 着色器的材质
public virtual Material GetModifiedMaterial(Material baseMaterial);

// 将 context 参数应用到 material 的 Shader 属性
public virtual void ApplyContextToMaterial(Material material);

// 子类必须实现：将序列化参数写入 UIEffectContext
protected abstract void UpdateContext(UIEffectContext c);

// 子类必须实现：由 UIEffectTweener 驱动，设置指定 cullingMask 对应的参数比率
public abstract void SetRate(float rate, UIEffectTweener.CullingMask cullingMask);

// 子类必须实现：ICanvasRaycastFilter 射线过滤
public abstract bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera);
```

#### Mesh 修改延迟机制
当 `transitionRoot` 与 `transform` 不同时（如过渡根节点缩放为零），会跳过本次修改，并将 `SetVerticesDirtyIfVisible` 注册到 `UIExtraCallbacks.onBeforeCanvasRebuild`，等待下一帧 Canvas 重建前重试。

---

### UIEffect（具体效果组件）

**命名空间**: `Coffee.UIEffects`
**继承**: `UIEffectBase`
**标注**: `[ExecuteAlways]`, `[DisallowMultipleComponent]`, `[Icon(...)]`

#### 序列化参数完整列表

**色调（Tone）**
```csharp
ToneFilter m_ToneFilter = ToneFilter.None;
float m_ToneIntensity = 1;           // [Range(0,1)]
Vector4 m_ToneParams;                // 额外参数（如 Posterize 的色阶数）
```

**颜色（Color）**
```csharp
ColorFilter m_ColorFilter = ColorFilter.None;
float m_ColorIntensity = 1;          // [Range(0,1)]
Color m_Color = Color.white;
bool m_ColorGlow = false;
```

**采样（Sampling）**
```csharp
SamplingFilter m_SamplingFilter = SamplingFilter.None;
float m_SamplingIntensity = 0.5f;    // [Range(0,1)]
float m_SamplingWidth = 1;           // [Range(0.5f,10f)]
float m_SamplingScale = 1f;          // [PowerRange(0.01f,100f,10f)]
```

**过渡（Transition）**
```csharp
TransitionFilter m_TransitionFilter = TransitionFilter.None;
float m_TransitionRate = 0.5f;       // [Range(0,1)]
bool m_TransitionReverse;
Texture m_TransitionTex;
Vector2 m_TransitionTexScale;
Vector2 m_TransitionTexOffset;
float m_TransitionRotation;          // [Range(0,360)]
bool m_TransitionKeepAspectRatio;
float m_TransitionWidth = 0.2f;      // [Range(0,1)]
float m_TransitionSoftness = 0.2f;   // [Range(0,1)]
MinMax01 m_TransitionRange;          // 过渡范围限制
ColorFilter m_TransitionColorFilter;
Color m_TransitionColor;
bool m_TransitionColorGlow;
bool m_TransitionPatternReverse;
float m_TransitionAutoPlaySpeed;     // [-5,5] 自动播放速度
```

**目标颜色（Target）**
```csharp
TargetMode m_TargetMode = TargetMode.None;
Color m_TargetColor = Color.white;
float m_TargetRange = 0.1f;          // [Range(0,1)]
float m_TargetSoftness = 0.5f;       // [Range(0,1)]
```

**混合（Blend）**
```csharp
BlendType m_BlendType = BlendType.AlphaBlend;
BlendMode m_SrcBlendMode = BlendMode.One;
BlendMode m_DstBlendMode = BlendMode.OneMinusSrcAlpha;
```

**阴影（Shadow）**
```csharp
ShadowMode m_ShadowMode = ShadowMode.None;
Vector2 m_ShadowDistance = new Vector2(1f, -1f);
int m_ShadowIteration = 1;           // [Range(1,5)]
float m_ShadowFade = 0.9f;           // [Range(0,1)]
float m_ShadowMirrorScale = 0.5f;    // [Range(0,2)]
float m_ShadowBlurIntensity = 1;     // [Range(0,1)]
ColorFilter m_ShadowColorFilter = ColorFilter.Replace;
Color m_ShadowColor = Color.white;
bool m_ShadowColorGlow;
```

**渐变（Gradation）**
```csharp
GradationMode m_GradationMode = GradationMode.None;
Color m_GradationColor1/2/3/4;      // 四角颜色
Gradient m_GradationGradient;        // 渐变
float m_GradationOffset;             // [-1,1]
float m_GradationScale = 1;          // [PowerRange(0.01f,10,10)]
float m_GradationRotation;           // [Range(0,360)]
```

**边缘（Edge）**
```csharp
EdgeMode m_EdgeMode = EdgeMode.None;
float m_EdgeWidth = 0.5f;            // [Range(0,1)]
ColorFilter m_EdgeColorFilter;
Color m_EdgeColor = Color.white;
float m_EdgeShinyRate = 0.5f;        // [Range(0,1)]
float m_EdgeShinyWidth = 0.5f;       // [Range(0,1)]
float m_EdgeShinyAutoPlaySpeed = 1f; // [-5,5]
PatternArea m_PatternArea = PatternArea.Inner;
bool m_AllowToModifyMeshShape = true;
```

#### Replica 双向引用机制
```csharp
// UIEffect 维护所有引用它的 UIEffectReplica 列表
public List<UIEffectReplica> replicas => _replicas ??= InternalListPool<UIEffectReplica>.Rent();

// 当 UIEffect 刷新时，同步通知所有 replicas
// SetVerticesDirty() → replicas.ForEach(c => c.SetVerticesDirty())
// SetMaterialDirty() → replicas.ForEach(c => c.SetMaterialDirty())
// SetRate()         → replicas.ForEach(c => c.SetRate(...))
```

---

### UIEffectContext（效果参数容器）

**职责**: 作为 UIEffect 参数的运行时快照，负责将参数写入顶点数据和 Shader 属性。

关键设计：
- 通过静态对象池（`s_ContextPool`）管理，避免 GC 压力
- `willModifyMaterial` 属性根据所有参数判断是否需要替换材质（任意效果非 None 则为 true）
- `ModifyMesh()` 将过渡参数编码到顶点 UV2/UV3，供 Shader 解码使用
- `ApplyToMaterial()` 设置所有 Shader 属性（`_ToneFilter`、`_ToneIntensity`、`_TransitionRate` 等）

---

### UIEffectReplica（效果复制组件）

**继承**: `UIEffectBase`
**用途**: 引用另一个 UIEffect，共享其效果上下文，节省材质实例，实现多元素同步效果。

#### 核心实现细节
```csharp
// 复用目标 UIEffect 的 effectId，使材质 hash 相同，从 MaterialRepository 获取同一材质
public override uint effectId => target ? target.effectId : 0;

// 直接返回目标的 context（不维护自己的 context）
public override UIEffectContext context => target && target.isActiveAndEnabled ? target.context : null;

// 可选：使用目标的 transitionRoot（同步过渡变换）
public override RectTransform transitionRoot => useTargetTransform && target
    ? target.transitionRoot
    : transform as RectTransform;
```

#### 双向注册机制
```csharp
// OnEnable → RefreshTarget(target)
private void RefreshTarget(UIEffect newTarget)
{
    _currentTarget?.replicas.Remove(this);   // 从旧 target 注销
    _currentTarget = newTarget;
    _currentTarget?.replicas.Add(this);      // 向新 target 注册
}

// 每帧（在 Canvas 重建前）检测 target 变换变化，必要时标脏顶点
private void SetVerticesDirtyIfTransformChanged();
```

---

### UIEffectTweener（补间动画组件）

**继承**: `MonoBehaviour`
**标注**: `[ExecuteAlways]`, `[RequireComponent(typeof(UIEffectBase))]`

#### 完整枚举定义
```csharp
[Flags] public enum CullingMask {
    Tone = 1 << 0,
    Color = 1 << 1,
    Sampling = 1 << 2,
    Transition = 1 << 3,
    GradiationOffset = 1 << 5,
    GradiationRotation = 1 << 6,
    EdgeShiny = 1 << 8
}

public enum UpdateMode { Normal, Unscaled, Manual }
public enum WrapMode { Once, Loop, PingPongOnce, PingPongLoop }
public enum Direction { Forward, Reverse }
public enum PlayOnEnable { None, Forward, Reverse, KeepDirection }
```

#### 关键字段和属性
```csharp
private CullingMask m_CullingMask = (CullingMask)(-1); // 默认全部位
private float m_Duration = 1;    // 动画时长 [0.05f,10]
private float m_Delay;           // 延迟时间 [0,10]
private float m_Interval;        // 循环间隔 [0,10]
private AnimationCurve m_Curve = AnimationCurve.Linear(0, 0, 1, 1); // 缓动曲线
private UnityEvent m_OnComplete; // 完成回调

// 运行时
private float _rate = -1;   // 当前比率（-1 表示未初始化）
private float _time;        // 当前时间
private UIEffectBase _target; // 懒加载，GetComponent<UIEffectBase>()
```

#### 核心属性
```csharp
// rate setter 触发 target.SetRate(m_Curve.Evaluate(rate), cullingMask)
public float rate { get; private set; }

// totalTime = delay + duration + interval
public float totalTime { get; }

// time 考虑 WrapMode：Once/PingPongOnce 用 Clamp，Loop/PingPongLoop 用 Repeat
public float time { get; }
```

---

### UIDataBindGray（数据绑定置灰）

**命名空间**: `YIUIFramework`
**继承**: `UIDataBindBool`
**标注**: `[LabelText("置灰")]`, `[RequireComponent(typeof(UIEffect))]`

```csharp
[AddComponentMenu("YIUIBind/Data/置灰 【Gray】 UIDataBindGray")]
public sealed class UIDataBindGray : UIDataBindBool
{
    [Range(0,1)] float m_EnabledGray = 1;   // 绑定为 true 时的灰度强度
    [Range(0,1)] float m_DisabledGray = 0;  // 绑定为 false 时的灰度强度
    [ReadOnly][Required] UIEffect m_Grayscale;  // 自动获取的 UIEffect 引用

    // 初始化时确保 toneFilter = Grayscale（强制设置过滤模式）
    protected override void OnRefreshData()
    {
        base.OnRefreshData();
        m_Grayscale ??= GetComponent<UIEffect>();
        m_Grayscale.toneFilter = ToneFilter.Grayscale;  // 关键：强制为 Grayscale 模式
    }

    // 数据变化时只调整强度，不改变 filter 类型
    protected override void OnValueChanged()
    {
        m_Grayscale.toneIntensity = GetResult() ? m_EnabledGray : m_DisabledGray;
    }
}
```

**注意**: 第一轮文档遗漏了 `OnRefreshData()` 会强制设置 `toneFilter = ToneFilter.Grayscale` 这一重要行为。即使 UIEffect 上设置了其他 ToneFilter，挂载 UIDataBindGray 后会覆盖为 Grayscale。

---

## 枚举（`Enums.cs`）

| 枚举 | 可选值 |
|------|--------|
| `ToneFilter` | None, Grayscale, Sepia, Negative, Retro, Posterize |
| `ColorFilter` | None, Multiply, Additive, Subtractive, Replace, MultiplyLuminance, MultiplyAdditive, HsvModifier, Contrast |
| `SamplingFilter` | None, BlurFast, BlurMedium, BlurDetail, Pixelation, RgbShift, EdgeLuminance, EdgeAlpha |
| `TransitionFilter` | None, Fade, Cutoff, Dissolve, Shiny, Mask, Melt, Burn, Pattern |
| `BlendType` | Custom, AlphaBlend, Multiply, Additive, SoftAdditive, MultiplyAdditive |
| `TargetMode` | None, Hue, Luminance |
| `ShadowMode` | None, Shadow, Shadow3, Outline, Outline8, Mirror |
| `EdgeMode` | None, Plain, Shiny |
| `PatternArea` | All, Inner, Edge |
| `GradationMode` | None, Horizontal, HorizontalGradient, Vertical, VerticalGradient, RadialFast, RadialDetail, Diagonal, DiagonalToRightBottom, DiagonalToLeftBottom, Angle, AngleGradient |

---

## UIParticle（`Coffee.UIExtensions` 命名空间）

**UIParticle** - UI 粒子主组件（继承 `MaskableGraphic`）
- 将 `ParticleSystem` 渲染到 UI Canvas 中，支持 Mask、排序、缩放
- **AutoScalingMode**: `None`, `UIParticle`, `Transform`
- **MeshSharing**: `None`, `Auto`, `Primary`, `PrimarySimulator`, `Replica`（共享粒子 Mesh 用于多实例）
- **PositionMode**: `Relative`, `Absolute`
- 关键属性：`particles`（粒子系统列表）, `scale`, `animatableProperties`

**UIParticleRenderer** - 负责将单个粒子系统的 Mesh 提交到 CanvasRenderer

**UIParticleUpdater** - 通过 Unity PlayerLoop 在每帧更新所有活跃的 UIParticle

**UIParticleAttractor** - 将粒子吸引到目标 UI 元素位置（实现 UI 粒子吸附效果）

---

## 内部工具类（`Coffee.UIEffectInternal`）

| 类 | 说明 |
|----|------|
| `MaterialRepository` | 材质缓存仓库，基于 `Hash128(baseMaterialId, effectId, samplingScaleId, 0)` 复用材质实例 |
| `ObjectRepository<T>` | 通用哈希对象仓库 |
| `InternalObjectPool<T>` | 内部对象池，构造时传入 create/validate/reset 委托 |
| `InternalListPool<T>` | 列表对象池，UIEffect.replicas 使用 |
| `ShaderVariantRegistry` | 着色器变体注册表，查找 `Hidden/{0} (UIEffect)` 变体或回退到 `Hidden/UI/Default (UIEffect)` |
| `UIExtraCallbacks` | 静态事件 `onBeforeCanvasRebuild`，在 Canvas 重建前提供额外回调 |
| `GraphicProxy` | 代理模式，支持不同类型 Graphic（Image、TMP 等）的顶点处理 |
| `FrameCache` | 帧级缓存，在同一帧内复用计算结果 |
| `PreloadedProjectSettings<T>` | 项目设置基类，通过 `PreloadedAssets` 加载 |
| `TransformSensitivity` | 枚举，控制 UIEffectReplica 检测父变换变化的敏感度 |
| `MinMax01` | 过渡范围 [0,1] 的最小/最大值对 |
| `FastAction` | 高性能委托封装，减少 GC |

---

## 实现原理

### 材质修改流程

```
UIEffectBase.GetModifiedMaterial(baseMaterial)
    │
    ├─ 若 !isActiveAndEnabled 或 context 为空 或 !context.willModifyMaterial
    │   └─ 释放 _material，直接返回 baseMaterial（不修改）
    │
    ├─ 计算 samplingScaleId = (uint)(InverseLerp(0.01, 100, actualSamplingScale) * uint.MaxValue)
    ├─ hash = Hash128(baseMaterialId, effectId, samplingScaleId, 0)
    ├─ MaterialRepository.Valid(hash, _material) ? 直接复用 : 创建新材质
    │   └─ new Material(baseMaterial) { shader = FindOptionalShader(原shader, "(UIEffect)") }
    └─ ApplyContextToMaterial(_material)
        └─ context.ApplyToMaterial(material, actualSamplingScale)
```

**UIEffectReplica 共享材质的关键**：因为复用 `target.effectId`，所以 hash 相同，从 MaterialRepository 获取同一材质实例，不会额外创建材质。

### 顶点修改流程（Mesh Modifier）

```
UIEffectBase.ModifyMesh(VertexHelper vh)
    │
    ├─ CanModifyMesh() 检查 transitionRoot 是否可见（scale 非零）
    │   ├─ 可见 → context.ModifyMesh(graphic, transitionRoot, vh, canModifyShape)
    │   │         └─ 编码过渡范围/类型到 UV2/UV3 顶点数据
    │   └─ 不可见 → 注册 SetVerticesDirtyIfVisible 到 onBeforeCanvasRebuild 延迟重试
    └─
```

### 属性更新的脏标记传播

UIEffect 属性 setter 模式（所有属性统一实现方式）：
```csharp
// 以 toneFilter 为例：
set {
    if (m_ToneFilter == value) return;         // 1. 等值检查，避免无效刷新
    context.toneFilter = m_ToneFilter = value; // 2. 同步更新 context 和序列化字段
    SetMaterialDirty();                        // 3. 标记材质脏，触发 Canvas 重建
}
// 注：影响顶点形状的属性（如 transitionFilter）会额外调用 SetVerticesDirty()
```

---

## 关键流程图

### UIEffect 完整生命周期

```
Awake/OnEnable
    ├── context = s_ContextPool.Rent()
    ├── UpdateContext(context)           // 将所有序列化参数写入 context
    ├── SetMaterialDirty()               // → Canvas.WillRenderCanvases → GetModifiedMaterial()
    └── SetVerticesDirty()               // → Canvas.WillRenderCanvases → ModifyMesh()

OnDisable
    ├── 注销 UIExtraCallbacks 回调
    ├── MaterialRepository.Release(_material)  // 减少材质引用计数
    ├── SetMaterialDirty() → 恢复原始材质
    └── SetVerticesDirty() → 恢复原始 Mesh

OnDestroy
    └── s_ContextPool.Return(_context)   // 归还对象池
```

### UIEffectTweener 完整播放流程

```
Play(Direction.Forward)
    └── _isPaused = false, _time = 0

Update() [每帧]
    ├── deltaTime = UpdateMode 决定 (Time.deltaTime / unscaledDeltaTime)
    ├── _time += deltaTime * (direction == Forward ? 1 : -1)
    ├── rate = WrapMode.Evaluate(_time / totalTime)
    │   ├── Once:       Clamp01(t)
    │   ├── Loop:       Repeat(t, 1)
    │   ├── PingPongOnce: PingPong(Clamp(t,0,2), 1)
    │   └── PingPongLoop: PingPong(Repeat(t,2), 1)
    └── rate setter → target.SetRate(m_Curve.Evaluate(rate), cullingMask)
                    → UIEffect.SetRate() 更新对应参数
                    → SetVerticesDirty/SetMaterialDirty()
                    → replicas.ForEach(c => c.SetRate(...))

完成（rate 到达终点）→ OnComplete.Invoke()
```

### UIDataBindGray 数据驱动流程

```
初始化
    └── OnRefreshData()
        ├── m_Grayscale ??= GetComponent<UIEffect>()
        └── m_Grayscale.toneFilter = ToneFilter.Grayscale  ← 强制设置！

数据源 bool 变化
    └── UIDataBindBool.OnValueChanged()
        └── UIDataBindGray.OnValueChanged()
            └── m_Grayscale.toneIntensity = GetResult() ? m_EnabledGray : m_DisabledGray
                └── context.toneIntensity = value
                    └── SetMaterialDirty() → 重新计算着色器参数
```

### UIEffectReplica 同步流程

```
UIEffect.SetMaterialDirty()
    ├── graphic.SetMaterialDirty()             // 自身 Graphic 刷新
    └── replicas.ForEach(r => r.SetMaterialDirty())  // 所有 Replica 同步刷新

UIEffectReplica.GetModifiedMaterial()
    └── MaterialRepository.Get(Hash128(baseMaterialId, target.effectId, ...))
        └── 命中缓存（与 UIEffect 共享同一材质实例）
```

---

## 代码示例

### 1. 运行时控制效果

```csharp
// 获取 UIEffect 并切换到扫光效果
var effect = button.GetComponent<UIEffect>();
effect.toneFilter = ToneFilter.Grayscale;
effect.toneIntensity = 0.8f;

// 切换为溶解过渡
effect.transitionFilter = TransitionFilter.Dissolve;
effect.transitionRate = 0f;

// 使用 UIEffectTweener 播放
var tweener = button.GetComponent<UIEffectTweener>();
tweener.cullingMask = UIEffectTweener.CullingMask.Transition;
tweener.duration = 0.5f;
tweener.wrapMode = UIEffectTweener.WrapMode.Once;
tweener.Play();
```

### 2. UIEffectReplica 共享效果

```csharp
// 主效果（在一个 UI 元素上）
var mainEffect = mainPanel.GetComponent<UIEffect>();
mainEffect.toneFilter = ToneFilter.Grayscale;

// 多个 Replica 共享同一效果，节省材质实例
foreach (var item in itemList)
{
    var replica = item.AddComponent<UIEffectReplica>();
    replica.target = mainEffect;
    replica.useTargetTransform = false; // 各自独立位置
}
```

### 3. 数据绑定置灰

```csharp
// 在 Inspector 中：
// 1. 给按钮 Image 添加 UIEffect 组件
// 2. 再添加 UIDataBindGray 组件（会自动找 UIEffect）
// 3. 设置绑定键（如 "ButtonInteractable"）

// 代码驱动：
YIUIDataHelper.SetData("ButtonInteractable", false); // → 触发 toneIntensity = 1（置灰）
YIUIDataHelper.SetData("ButtonInteractable", true);  // → 触发 toneIntensity = 0（恢复）
```

---

## 第一轮修正与补充

1. **UIDataBindGray 的 OnRefreshData()** - 第一轮文档未提及该方法强制设置 `toneFilter = ToneFilter.Grayscale`，这意味着挂载该组件会覆盖 UIEffect 原有的 ToneFilter 设置。

2. **UIEffectReplica 的双向引用** - UIEffect 维护 `replicas` 列表，SetVerticesDirty/SetMaterialDirty/SetRate 都会传播到所有 Replica，实现真正的同步刷新。

3. **材质 Hash 的 samplingScaleId** - Hash 中的第三分量是 `actualSamplingScale` 映射到 `uint.MaxValue` 的值，说明相同采样缩放才会复用材质。

4. **InternalListPool** - UIEffect.replicas 使用 `InternalListPool<UIEffectReplica>.Rent()`，而非普通 `new List<>()`，同样遵循对象池设计。

5. **UIEffectTweener.rate setter** - 通过 `m_Curve.Evaluate(rate)` 应用缓动曲线后再传给 `target.SetRate()`，AnimationCurve 是可以自定义的。

---

## 依赖关系

```
cn.etetet.yiuieffect
    ├── UnityEngine.UI (Unity 内置)
    ├── Coffee.UIEffect (内嵌于 Plugins/UIEffect)
    ├── Coffee.UIParticle (内嵌于 Plugins/UIParticle)
    ├── Sirenix.OdinInspector ([LabelText], [ReadOnly], [Required] 等标签)
    └── YIUIFramework (UIDataBindBool 基类来自 cn.etetet.yiuiframework)
```

**package.json** 中 `dependencies` 为空，依赖通过程序集引用（asmref/asmdef）隐式声明。

---

## 与其他 Package 的关系

| Package | 关系 |
|---------|------|
| `cn.etetet.yiuiframework` | `UIDataBindGray` 继承自框架的 `UIDataBindBool`，数据绑定系统 |
| `cn.etetet.yiuiinvoke` | 无直接依赖，但 UIEffect 可通过 YIUI Invoke 系统调用 |
| `cn.etetet.yiui` | UI 视图层通过 YIUIBind 数据绑定使用置灰功能 |

---

## 典型使用场景

1. **按钮置灰**：给 Button 的 Graphic 添加 `UIEffect` + `UIDataBindGray`，绑定到"是否可交互"数据
2. **UI 过场效果**：给面板添加 `UIEffect`（TransitionFilter=Fade）+ `UIEffectTweener`，实现开关面板淡入淡出
3. **UI 粒子特效**：给 Canvas 内的特效节点添加 `UIParticle`，让粒子跟随 UI 层级渲染
4. **多对象同步效果**：使用 `UIEffectReplica` 引用同一 `UIEffect` 实例，统一控制一组 UI 的视觉效果
5. **溶解/扫光**：使用 TransitionFilter=Dissolve/Shiny + UIEffectTweener 实现解锁或高亮动画
6. **阴影/描边文字**：ShadowMode=Outline8 + 边缘色配置，对 TMP 文字或 Image 添加描边效果
