# cn.etetet.demores

## 概述

**ET.DemoRes** 是 ET 框架自带 Demo 的美术资源包，版本 3.0.0，适配 Unity 2022.3。
该包不含任何 C# 代码，仅提供场景、角色模型、动画、贴图等运行时资产，供 Demo 功能演示使用。

- **作者**: tanghai (egametang/ET)
- **包 ID**: cn.etetet.demores (packagegit ID=8, Name=DB)
- **依赖**: 无外部依赖

---

## 目录结构

```
cn.etetet.demores/
├── Scenes/
│   ├── Map1.unity              # 演示地图1（含烘焙光照）
│   ├── Map1Settings.lighting   # Map1 光照设置
│   ├── Map1/
│   │   ├── LightingData.asset
│   │   ├── Lightmap-0_comp_dir.png
│   │   ├── Lightmap-0_comp_light.exr
│   │   └── ReflectionProbe-0.exr
│   ├── Map2.unity              # 演示地图2（含烘焙光照）
│   ├── Map2Settings.lighting
│   ├── Map2/
│   │   ├── LightingData.asset
│   │   ├── Lightmap-0_comp_dir.png
│   │   ├── Lightmap-0_comp_light.exr
│   │   └── ReflectionProbe-0.exr
│   └── Mat/
│       └── Urp.mat             # URP 材质
├── Unit/
│   └── Skeleton/
│       ├── Ani/                # 动画 FBX 文件
│       │   ├── Skeleton@Attack.FBX
│       │   ├── Skeleton@Damage.FBX
│       │   ├── Skeleton@Death.FBX
│       │   ├── Skeleton@Idle.FBX
│       │   ├── Skeleton@Knockback.FBX
│       │   ├── Skeleton@Run.FBX
│       │   ├── Skeleton@Skill.FBX
│       │   ├── Skeleton@Stand.FBX
│       │   ├── Skeleton@Walk.FBX
│       │   └── Materials/
│       │       └── skeleton_D.mat
│       ├── Character/          # 骨骼蒙皮模型
│       │   ├── Skeleton@Skin.FBX
│       │   └── Materials/
│       │       └── skeleton_D.mat
│       ├── Texture/
│       │   └── Skeleton_D.tif  # 骨骼怪漫反射贴图
│       ├── Skeleton.prefab     # 骨骼怪预制体
│       └── SkeletonController.controller  # 动画状态机
├── package.json
└── packagegit.json
```

---

## 资源说明

### 场景资源

| 资源 | 说明 |
|------|------|
| Map1.unity | 演示场景1，包含完整的烘焙光照（Lightmap + ReflectionProbe） |
| Map2.unity | 演示场景2，包含完整的烘焙光照 |
| Urp.mat | URP 渲染管线的基础材质 |

两个场景均预先烘焙了：
- **方向性光照贴图** (`Lightmap-0_comp_dir.png`)
- **光照强度贴图** (`Lightmap-0_comp_light.exr`)
- **反射探针** (`ReflectionProbe-0.exr`)

### 角色资源：骨骼怪（Skeleton）

| 动画状态 | FBX 文件 | 说明 |
|----------|----------|------|
| Idle | Skeleton@Idle.FBX | 待机 |
| Stand | Skeleton@Stand.FBX | 站立 |
| Walk | Skeleton@Walk.FBX | 行走 |
| Run | Skeleton@Run.FBX | 奔跑 |
| Attack | Skeleton@Attack.FBX | 普通攻击 |
| Skill | Skeleton@Skill.FBX | 技能释放 |
| Damage | Skeleton@Damage.FBX | 受伤 |
| Knockback | Skeleton@Knockback.FBX | 击退 |
| Death | Skeleton@Death.FBX | 死亡 |

**SkeletonController.controller** 是对应的 Unity Animator Controller，管理上述 9 个动画状态之间的切换逻辑。

**Skeleton.prefab** 是完整的骨骼怪预制体，挂载了 Animator 组件（引用 SkeletonController）和蒙皮网格渲染器（引用 skeleton_D 材质）。

---

## 依赖关系

| 依赖方向 | 说明 |
|----------|------|
| 无代码依赖 | 该包不含 C# 脚本，无 assembly 引用 |
| 被 cn.etetet.demores 引用 | 该包的资源被 Demo 场景（如 cn.etetet.unit、cn.etetet.ui 等业务包的 Demo 流程）加载和使用 |
| 渲染管线 | 使用 URP（Universal Render Pipeline）材质 |

---

## 实现原理

该包是纯资产包（Asset-only Package），遵循 Unity Package Manager 规范：

1. **场景加载**: Demo 场景（Map1/Map2）通过 YooAssets（cn.etetet.yooassets）的热更新资产管理系统按需加载
2. **角色实例化**: Skeleton.prefab 由 cn.etetet.yiuigameobjectpool（对象池）管理，避免频繁的 Instantiate/Destroy 开销
3. **动画驱动**: SkeletonController 的状态切换由 cn.etetet.unit 模块的 Unit 状态机触发，通过 Animator.SetTrigger/SetBool 参数控制

---

## 使用场景

- **单元测试场景**: Map1/Map2 作为服务器同步、移动、AOI 等功能的演示场景
- **角色系统演示**: Skeleton 作为 Demo 中的怪物单位，验证战斗逻辑（攻击、受伤、死亡流程）
- **渲染效果验证**: 预烘焙光照确保 Demo 画质一致性，不依赖实时光照计算

---

## 备注

- 该包无运行时代码，升级风险极低
- packagegit ID=8 (Name="DB") 对应内部版本管理中的资产库分类
- 实际项目中可替换为正式美术资产，该包仅供开发期演示

---

## Round 2 补充说明

### 确认与修正

**Round 1 理解准确**：该包确为纯资产包（无 `.cs` 文件），`package.json` 中 `relatedPackages` 为空，无显式代码依赖。`relatedPackages` 字段留空而非列出依赖，表明此包是被动引用方。

### 资产加载方式（代码示例）

虽然该包自身无 C# 代码，以下是其他模块引用这些资产的典型方式：

**场景加载**（由 cn.etetet.loader 发起）：
```csharp
// 通过 YooAssets 加载 Map1 场景
var handle = YooAssets.LoadSceneAsync("Map1", LoadSceneMode.Single);
await handle.ToUniTask();
```

**Skeleton Prefab 实例化**（由 cn.etetet.unit 发起）：
```csharp
// 通过对象池获取骨骼怪实例
var go = await GameObjectPoolComponent.Instance.GetAsync("Skeleton");
var animator = go.GetComponent<Animator>();
animator.SetTrigger("Attack"); // 驱动 SkeletonController 状态机
```

### 动画状态机结构

SkeletonController.controller 的状态转换逻辑（根据 FBX 动画推断）：

```
[Idle/Stand] ──移动指令──► [Walk/Run]
[Any State]  ──攻击指令──► [Attack] ──完成──► [Idle]
[Any State]  ──技能指令──► [Skill]  ──完成──► [Idle]
[Any State]  ──受伤事件──► [Damage] ──完成──► [Idle]
[Any State]  ──击退事件──► [Knockback] ──完成──► [Idle]
[Any State]  ──死亡事件──► [Death]  （终态）
```

### 资产命名规范

ET 框架的动画资产命名遵循 `{Model}@{State}.FBX` 约定：
- `@` 符号分隔模型名和动画状态名
- Unity 自动识别此命名约定并提取动画片段（AnimationClip）

### 与其他包的实际集成点

| 调用包 | 使用资产 | 具体用途 |
|--------|----------|----------|
| cn.etetet.unit | Skeleton.prefab, SkeletonController | 怪物单位实体创建和动画控制 |
| cn.etetet.loader | Map1.unity, Map2.unity | Demo 场景热加载 |
| cn.etetet.yiuigameobjectpool | Skeleton.prefab | 对象池复用怪物实体 |
| cn.etetet.demores（自身） | Urp.mat | 场景内 URP 材质渲染 |
