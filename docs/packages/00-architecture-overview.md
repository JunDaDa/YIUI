# MMOGame 项目整体架构概述

> 最后更新：Round 2 完成 (全部 47 个包已完成二轮分析)

---

## 项目技术栈

| 层次 | 技术 |
|------|------|
| 引擎 | Unity 2022.3 |
| 框架 | ET 框架 9.x (tanghai) |
| UI框架 | YIUI (作者: YIYI) |
| 热更新 | HybridCLR (code-philosophy) 7.8.1 |
| 序列化 | MongoDB.Bson (持久化) + MemoryPack (网络) |
| 网络协议 | KCP (外部) + TCP (内部) |
| 资产加载 | YooAssets |
| 数值配置 | LuBan 代码生成 |

---

## 包架构总览

项目共有 **47个 ET 包** + 若干工具包，分为以下几个层次：

### 第一层：框架核心（无依赖）
- **`cn.etetet.core`** — ET框架基础：Entity/Fiber/ETTask/Network/EventSystem/Actor
- **`cn.etetet.loader`** — 程序集加载器（Model/ModelView/Hotfix/HotfixView）
- **`cn.etetet.hybridclr`** — HybridCLR热更新支持

### 第二层：基础服务
- **`cn.etetet.yooassets`** — YooAssets 资产管理封装
- **`cn.etetet.sourcegenerator`** — Source Generator 代码生成（EntitySystem属性等）
- **`cn.etetet.numeric`** — 数值系统基础
- **`cn.etetet.proto`** — 网络协议定义
- **`cn.etetet.excel`** — Excel配置读取
- **`cn.etetet.memorypack`** — MemoryPack序列化支持
- **`cn.etetet.mathematics`** — 数学库
- **`cn.etetet.packagemanager`** — 包管理工具

### 第三层：游戏系统
- **`cn.etetet.startconfig`** — 启动配置
- **`cn.etetet.router`** — 路由系统（客户端连接路由服）
- **`cn.etetet.login`** — 登录流程
- **`cn.etetet.netinner`** — 内网通信（服务器间）
- **`cn.etetet.statesync`** — 状态同步
- **`cn.etetet.actorlocation`** — Actor位置系统（玩家跨服寻址）
- **`cn.etetet.unit`** — 游戏单位基础
- **`cn.etetet.move`** — 移动系统
- **`cn.etetet.aoi`** — 视野感知系统（Area of Interest）
- **`cn.etetet.ai`** — AI系统
- **`cn.etetet.recast`** — 寻路（Recast/Detour）
- **`cn.etetet.http`** — HTTP 服务

### 第四层：UI系统（YIUI）
- **`cn.etetet.yiuiframework`** — YIUI 核心框架（依赖 core + yiuiinvoke）
- **`cn.etetet.yiuiinvoke`** — YIUI Invoke接口定义
- **`cn.etetet.yiuiyooassets`** — YIUI + YooAssets 集成
- **`cn.etetet.yiui`** — YIUI 主包（Panel管理等）
- **`cn.etetet.yiuigm`** — YIUI GM工具
- **`cn.etetet.yiuireddot`** — 红点系统
- **`cn.etetet.yiuieffect`** — UI特效
- **`cn.etetet.yiui3ddisplay`** — UI中3D模型展示
- **`cn.etetet.yiuigameobjectpool`** — UI对象池
- **`cn.etetet.yiuiloopscrollrectasync`** — 异步循环列表
- **`cn.etetet.yiuisuperscroll`** — 超级滚动列表
- **`cn.etetet.yiuisuperscrolldemo`** — 滚动列表示例
- **`cn.etetet.yiuitips`** — Tips提示系统
- **`cn.etetet.yiuilocalizationpro`** — 多语言本地化
- **`cn.etetet.yiuiluban`** — YIUI + LuBan 配置集成
- **`cn.etetet.yiuilubangen`** — YIUI LuBan代码生成
- **`cn.etetet.yiuinumeric`** — YIUI 数值显示
- **`cn.etetet.yiuinumericconfig`** — YIUI 数值配置
- **`cn.etetet.yiuistatesync`** — YIUI 状态同步
- **`cn.etetet.yiuicodeanalysis`** — YIUI 代码分析工具

### 第五层：辅助工具
- **`cn.etetet.watcher`** — 进程守护/监视（拉起并监控所有服务端子进程）
- **`cn.etetet.console`** — 控制台命令
- **`cn.etetet.referencecollector`** — Unity引用收集器
- **`cn.etetet.demores`** — Demo资源
- **`cn.etetet.luban`** — LuBan配置系统
- **`cn.etetet.lubangen`** — LuBan代码生成
- **`cn.etetet.statesync`** — 状态同步

---

## ET框架核心架构

### Entity Component System (ECS变种)

ET框架实现了一种 ECS 的变体，与标准 ECS 的区别：

```
标准 ECS:    Entity(纯ID) + Component(纯数据) + System(纯逻辑)
ET ECS:      Entity = 数据容器 + 可持有组件和子Entity
             System = 分离的逻辑（通过 [EntitySystem] 扩展方法）
             一个对象可以既是 Entity 又是 Component
```

### Fiber 并发架构

```
Unity主线程
    │
    ▼
World (全局单例容器)
    │
    ├── FiberManager (管理所有Fiber)
    │       ├── MainThreadScheduler
    │       │       └── Fiber[Main] → Scene → Entity树
    │       ├── ThreadScheduler
    │       │       └── Fiber[Gate] / Fiber[Game] ...
    │       └── ThreadPoolScheduler
    │               └── Fiber[...] (临时任务)
    │
    └── 全局 Singletons (EventSystem, MessageDispatcher, ObjectPool, ...)
```

### 代码分层（热更新架构）

```
程序集分层：
  Core (不热更)  → 框架底层，Entity/Fiber/Network基础
  Model          → 数据定义，Entity字段（热更新）
  ModelView      → Unity视图数据（热更新）
  Hotfix         → 游戏逻辑System（热更新）
  HotfixView     → Unity视图逻辑（热更新）

热更新方案：HybridCLR (IL2CPP + 解释执行热更代码)
```

### 网络通信架构

```
客户端 ←──KCP──→ Gate服务器 ←──TCP(内网)──→ Game服务器
                                    ↕ 内网
                              其他服务器 (Login/Config...)

消息流：
  发送: Session.Call(request) → 序列化 → KChannel → 网络
  接收: 网络 → PacketParser → Session → MessageDispatcher → Handler
  RPC:  等待 ETTask<IResponse>, 通过 RpcId 匹配响应
```

### 事件通信架构

```
模块内精确调用:  Invoke<Args, Result>(type, args)
               必须有Handler，类似虚函数分发

跨模块广播:     Publish<Scene, Event>(scene, event)  (同步)
               PublishAsync<Scene, Event>(scene, event) (异步)
               可以无订阅者，按SceneType过滤
```

---

## 关键设计模式

1. **ECS + Actor 混合** — Entity作为数据容器，System提供行为，Fiber提供隔离性
2. **Fiber = 轻量Actor** — 每个Fiber单线程执行，通过Mailbox通信，避免共享状态
3. **代码热更新** — Model/Hotfix分离，HybridCLR支持运行时替换逻辑代码
4. **对象池** — Entity/ETTask/消息对象均使用池化，减少GC压力
5. **源码生成** — EntitySystem特性由SourceGenerator自动生成注册代码，减少手写样板

---

## 分析进度

| Round | 状态 | 完成包数 |
|-------|------|---------|
| Round 1 | ✅ 已完成 | 47/47 |
| Round 2 | ✅ 已完成 | 47/47 |
| Round 3 | 进行中 | 0/47 |
| Round 4 | 待开始 | - |

**Round 1 & Round 2 已完成所有包**：
cn.etetet.core, cn.etetet.loader, cn.etetet.yiuiframework, cn.etetet.yiuiinvoke, cn.etetet.yiuiyooassets, cn.etetet.yooassets, cn.etetet.yiuilocalizationpro, cn.etetet.yiuigm, cn.etetet.yiuireddot, cn.etetet.yiuieffect, cn.etetet.yiui3ddisplay, cn.etetet.yiuigameobjectpool, cn.etetet.yiuiloopscrollrectasync, cn.etetet.yiuisuperscroll, cn.etetet.yiuisuperscrolldemo, cn.etetet.yiuitips, cn.etetet.yiuicodeanalysis, cn.etetet.yiuinumeric, cn.etetet.yiuinumericconfig, cn.etetet.yiuistatesync, cn.etetet.yiuiluban, cn.etetet.yiuilubangen, cn.etetet.yiui, cn.etetet.numeric, cn.etetet.proto, cn.etetet.excel, cn.etetet.hybridclr, cn.etetet.sourcegenerator, cn.etetet.packagemanager, cn.etetet.actorlocation, cn.etetet.ai, cn.etetet.aoi, cn.etetet.console, cn.etetet.demores, cn.etetet.http, cn.etetet.login, cn.etetet.mathematics, cn.etetet.memorypack, cn.etetet.move, cn.etetet.netinner, cn.etetet.recast, cn.etetet.referencecollector, cn.etetet.router, cn.etetet.startconfig, cn.etetet.statesync, cn.etetet.ui, cn.etetet.unit, cn.etetet.watcher

**Round 2 主要发现**：
- 多个包的 `createScenes` 等预留参数在函数体中未实际使用，是后续功能扩展点
- `WatcherHelper.GetThisMachineConfig()` 是供外部调用的公共工具，包内 Awake 流程本身不使用
- ET框架的 package.json `relatedPackages` 字段普遍为空，实际依赖通过 asmdef 引用隐式满足
- Actor 消息系统 (IActorMessage/IActorRpcMessage) 依赖 `ObjectPoolComponent` 做消息对象复用，减少 GC

**Round 3 目标**：深化跨 package 交互分析，补充边界情况和异常处理
