# cn.etetet.ui

## 概述

ET框架实现的一个简单UI管理系统。版本 3.0.0，基于Unity 2022.3。提供UI的创建、销毁、分层管理，并通过属性标记+反射机制实现UI类型的注册与驱动。

---

## 目录结构

```
cn.etetet.ui/
├── package.json                          # 包元数据
├── Scripts/
│   ├── Loader/Client/
│   │   ├── CanvasConfig.cs               # Canvas配置MonoBehaviour
│   │   └── UILayerScript.cs              # UILayer枚举 + MonoBehaviour
│   ├── ModelView/Client/
│   │   ├── AUIEvent.cs                   # UI事件抽象基类
│   │   ├── LayerNames.cs                 # Unity Layer名称常量
│   │   ├── UI.cs                         # UI实体类 + UISystem
│   │   ├── UIComponent.cs                # Scene级UI管理组件
│   │   ├── UIEventAttribute.cs           # UI类型注册属性
│   │   ├── UIEventComponent.cs           # UI事件注册单例
│   │   └── UIGlobalComponent.cs          # 全局UI层级管理组件
│   └── HotfixView/Client/
│       ├── UIComponentSystem.cs          # UIComponent热更逻辑
│       └── UIGlobalComponentSystem.cs    # UIGlobalComponent热更逻辑
```

---

## 核心类/接口

### UILayer（枚举）
定义UI渲染层级，值越大越靠前（渲染在上层）：
```csharp
public enum UILayer
{
    Hidden = 0,   // 隐藏层（不可见）
    Low    = 10,  // 低优先级，如游戏背景UI
    Mid    = 20,  // 中优先级，如主HUD
    High   = 30,  // 高优先级，如弹窗、提示
}
```

### LayerNames（静态类）
Unity Layer名称字符串常量，供 `LayerMask` 使用：
- `UI`、`Unit`、`Map`、`Default`、`Hidden`
- 工具方法：`GetLayerInt(name)` / `GetLayerStr(int)`

### UI（Entity）
UI实体节点，对应一个Unity `GameObject`，支持嵌套子UI：
| 成员 | 类型 | 说明 |
|------|------|------|
| `GameObject` | `GameObject` | 关联的Unity对象 |
| `Name` | `string` | 节点名称 |
| `nameChildren` | `Dictionary<string, EntityRef<UI>>` | 子UI节点缓存 |

关键方法（在 `UISystem` 中）：
- `Awake(name, gameObject)` — 初始化，设置 UI Layer
- `Destroy()` — 递归Dispose所有子节点，并销毁GameObject
- `Add(UI)` / `Remove(name)` / `Get(name)` — 子节点管理（Get时懒加载：先查缓存，再从GameObject.transform.Find）

### UIComponent（Entity组件）
挂载在Scene上，管理当前场景所有根级UI：
| 成员 | 说明 |
|------|------|
| `UIs` | `Dictionary<string, EntityRef<UI>>`，按类型名索引 |
| `UIGlobalComponent` | 引用全局UI层级组件 |

### UIGlobalComponent（Entity组件）
挂载在Scene根上，持有各Layer的 `Transform` 引用：
| 成员 | 说明 |
|------|------|
| `UILayers` | `Dictionary<int, Transform>`，key=UILayer枚举int值 |

初始化时，从 `GameObject.Find("/Global/UI")` 的 `ReferenceCollector` 中按枚举名（"Hidden","Low","Mid","High"）获取各层Transform。

### UIEventComponent（单例）
`[CodeProcess]` 标记，程序启动时通过 `CodeTypes` 反射收集所有带 `[UIEventAttribute]` 的类型，建立 `UIEvents` 字典（key=UIType字符串，value=AUIEvent实例）。

### AUIEvent（抽象类）
所有UI类型的事件驱动基类：
```csharp
public abstract class AUIEvent : HandlerObject
{
    public abstract ETTask<UI> OnCreate(UIComponent uiComponent, UILayer uiLayer);
    public abstract void OnRemove(UIComponent uiComponent);
}
```

### UIEventAttribute（Attribute）
用于标记AUIEvent子类，指定对应的UI类型名称：
```csharp
[UIEvent("UILogin")]
public class UILoginEvent : AUIEvent { ... }
```

### CanvasConfig（MonoBehaviour）
附加在Canvas GameObject上的配置组件，记录 `CanvasName`。

### UILayerScript（MonoBehaviour）
附加在层级GameObject上，记录该节点的 `UILayer` 值，供编辑器或运行时识别。

---

## 实现原理

### UI创建流程

```
调用方
  └─► UIComponent.Create(uiType, uiLayer)
        └─► UIGlobalComponent.OnCreate(uiComponent, uiType, uiLayer)
              └─► UIEventComponent.Instance.UIEvents[uiType].OnCreate(uiComponent, uiLayer)
                    └─► [具体UI实现] 加载预制体，创建 UI Entity，返回 UI
        └─► UIComponent.UIs[uiType] = ui
```

### UI销毁流程

```
调用方
  └─► UIComponent.Remove(uiType)
        ├─► UIGlobalComponent.OnRemove(uiComponent, uiType)
        │     └─► UIEventComponent.Instance.UIEvents[uiType].OnRemove(uiComponent)
        │           └─► [具体UI实现] 执行清理逻辑
        └─► ui.Dispose()
              └─► UISystem.Destroy → 递归销毁子UI + UnityEngine.Object.Destroy(GameObject)
```

### 注册机制

启动时 `UIEventComponent.Awake()` 通过 `CodeTypes.Instance.GetTypes(typeof(UIEventAttribute))` 反射收集所有带属性的类，实例化后存入字典，后续所有Create/Remove调用均通过类型名字符串路由到对应的AUIEvent实现。

---

## 关键数据流

```
[Scene初始化]
  UIGlobalComponent.Awake()
    → 从 /Global/UI 节点读取 ReferenceCollector
    → 建立 UILayers: {0→Hidden, 10→Low, 20→Mid, 30→High}

[UI操作]
  UIComponent.Create/Remove
    → UIGlobalComponent (找Layer/分发事件)
    → UIEventComponent (路由到具体AUIEvent)
    → 具体AUIEvent实现 (加载资源、创建/销毁Entity+GameObject)
```

---

## 依赖关系

| 依赖 | 用途 |
|------|------|
| `cn.etetet.core` | `Entity`, `EntitySystem`, `Singleton`, `CodeTypes`, `HandlerObject`, `EntityRef`, `ETTask` |
| `cn.etetet.referencecollector` | `ReferenceCollector`，在UIGlobalComponent初始化时获取各层Transform |
| Unity | `GameObject`, `Transform`, `LayerMask`, `MonoBehaviour` |

---

## 补充说明（Round 2）

- `UIComponent.Awake()` 中有注释掉的初始化行（`//self.UIGlobalComponent = ...`），说明 `UIGlobalComponent` 的引用需要外部（如YIUI框架层）在初始化时手动注入，而不是在Awake中自动获取。
- `UI.Get(name)` 实现了**懒加载**：先查内部缓存字典，未命中时通过 `GameObject.transform.Find(name)` 从Unity场景树中查找并创建子UI Entity，这样对预制体嵌套的子节点无需提前注册。
- `UI` 实体通过 `[ChildOf()]` 标记，可作为任意Entity的子实体；`UIComponent` 通过 `[ComponentOf]` 标记，作为Scene组件；`UIGlobalComponent` 通过 `[ComponentOf(typeof(Scene))]` 限定只能挂在Scene上。
- 本包为基础UI框架，不包含具体UI页面实现，具体UI由 `cn.etetet.yiuiframework`（YIUI）等扩展包实现，通过 `AUIEvent` 接入本包的管理流程。
- 异常处理：`OnCreate` 和 `OnRemove` 均包含 try-catch，捕获后重新抛出带有 UIType 名称的包装异常，方便定位问题。
