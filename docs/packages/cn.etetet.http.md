# cn.etetet.http

## 概述

简单的 HTTP 服务器库，基于 .NET 内置 `HttpListener` 实现。专为服务端（Server）场景设计，提供 HTTP 请求监听、分发和处理能力。版本 3.0.0。

## 目录结构

```
cn.etetet.http/
├── package.json
├── Scripts/
│   ├── Model/Server/
│   │   ├── HttpComponent.cs         # HTTP 监听组件（Entity）
│   │   ├── HttpDispatcher.cs        # HTTP 请求路由分发器（Singleton）
│   │   ├── HttpHandlerAttribute.cs  # 路由注册 Attribute
│   │   └── IHttpHandler.cs          # Handler 接口
│   └── Hotfix/Server/
│       └── HttpComponentSystem.cs   # HttpComponent 生命周期系统
```

## 核心类/接口

### `HttpComponent` (Model)
- 继承 `Entity`，挂载于 `Scene`
- 实现 `IAwake<string>`、`IDestroy`
- 字段：`HttpListener Listener` — .NET 原生 HTTP 监听器

### `HttpComponentSystem` (Hotfix)
- `[EntitySystemOf(typeof(HttpComponent))]` 静态分部类
- `Awake(string address)`：解析地址列表（分号分隔），启动 `HttpListener`，异步开始循环接受请求
- `Destroy()`：停止并关闭监听器
- `Accept()`：异步循环等待请求（`GetContextAsync()`），逐个派发处理
- `Handle(HttpListenerContext)`：从 `HttpDispatcher` 取出对应 `IHttpHandler`，调用 `Handle()`，`finally` 中关闭响应

### `HttpDispatcher` (Model)
- `[CodeProcess]` 单例，实现 `ISingletonAwake`
- 内部字典：`Dictionary<string /*path*/, Dictionary<int /*sceneType*/, IHttpHandler>>`
- `Awake()`：通过反射扫描所有带 `[HttpHandlerAttribute]` 的类型，实例化并注册到分发表
- `Get(int sceneType, string path)`：根据 SceneType + Path 查询对应 Handler

### `IHttpHandler` (Model)
```csharp
public interface IHttpHandler
{
    ETTask Handle(Scene scene, HttpListenerContext context);
}
```
- 所有 HTTP 路由处理器必须实现的接口

### `HttpHandlerAttribute` (Model)
```csharp
[HttpHandler(sceneType: SceneType.Realm, path: "/login")]
```
- 继承 `BaseAttribute`
- 参数：`SceneType`（整型场景类型）、`Path`（URL 绝对路径字符串）

## 实现原理

```
启动流程：
HttpComponent.Awake(address)
  └─ HttpListener.Start()
  └─ Accept() [异步循环]
       └─ GetContextAsync() → HttpListenerContext
       └─ Handle(context) [异步，NoContext]
            └─ HttpDispatcher.Get(sceneType, path)
            └─ IHttpHandler.Handle(scene, context)
            └─ context.Response.Close()

注册流程（进程启动时）：
HttpDispatcher.Awake()
  └─ CodeTypes.GetTypes(HttpHandlerAttribute)
  └─ 反射实例化 → 注册到 dispatcher[path][sceneType]
```

## 关键设计点

1. **SceneType 隔离**：同一 URL Path 可在不同 SceneType（如 Realm/Gate/Map）注册不同 Handler，实现多场景 HTTP 服务复用同一端口分发逻辑
2. **异步非阻塞**：`Accept` 循环使用 `ETTask` + `NoContext`，不阻塞主线程
3. **InstanceId 保护**：`Accept` 循环通过 `instanceId == self.InstanceId` 检测组件是否已销毁，安全退出循环
4. **ObjectDisposedException 静默忽略**：Listener 关闭后 GetContextAsync 会抛出此异常，属正常销毁流程，静默处理

## 依赖关系

- 依赖 ET 核心框架（`Entity`、`Scene`、`Singleton`、`ETTask`、`CodeTypes`、`BaseAttribute`）
- 依赖 `cn.etetet.core`（实体系统、单例系统）
- 依赖 .NET `System.Net.HttpListener`（无第三方 HTTP 库）
- 无其他 package 依赖

## 使用方式

```csharp
// 在 Scene 初始化时添加 HttpComponent
scene.AddComponent<HttpComponent, string>("http://+:8080/;http://+:8081/");

// 实现 Handler
[HttpHandler(SceneType.Realm, "/login")]
public class LoginHttpHandler : IHttpHandler
{
    public async ETTask Handle(Scene scene, HttpListenerContext context)
    {
        // 读取请求、写入响应
        var response = context.Response;
        // ...
    }
}
```

## 注意事项

- Windows 下非管理员运行需要提前执行：`netsh http add urlacl url=<address> user=Everyone`
- 仅用于服务端（`ET.Server` 命名空间），客户端不使用此 package

---

## Round 2 补充说明

### 代码精读修正

**1. SceneType 获取方式**

Round 1 文档描述正确，但细节需补充：`Handle()` 中通过 `self.IScene.SceneType` 获取场景类型（而非 `self.Scene().SceneType`），这是 ET 框架访问所属 Scene 类型的接口方式。

**2. 未注册路由的错误处理**

`HttpDispatcher.Get()` 直接使用字典索引访问，**不做 null 检查**：
```csharp
public IHttpHandler Get(int sceneType, string path)
{
    return this.dispatcher[path][sceneType]; // 若未注册，抛 KeyNotFoundException
}
```
这意味着：若收到未注册的路径请求，会抛出 `KeyNotFoundException`，被 `Handle()` 的外层 `catch (Exception e)` 捕获并 `Log.Error`，响应最终在 `finally` 中被 `Close()`（客户端收到空响应）。**这是一个潜在的静默失败点**，建议业务层做 404 响应处理。

**3. `HttpComponent` 的组件约束**

```csharp
[ComponentOf(typeof(Scene))]
public class HttpComponent: Entity, IAwake<string>, IDestroy
```
`[ComponentOf(typeof(Scene))]` 限制了该组件只能挂载到 `Scene` 实体上，ET 框架在 Debug 模式下会做运行时断言检查。

**4. 多地址监听实现**

Awake 中以分号 `;` 分割地址字符串，空字符串跳过：
```csharp
foreach (string s in address.Split(';'))
{
    if (s.Trim() == "") continue;
    self.Listener.Prefixes.Add(s);
}
```
因此可以同时监听多个端口：`"http://+:8080/;http://+:8081/"`

**5. 并发模型**

```
Accept() 循环          Handle() 并发
─────────────          ────────────────
GetContextAsync()  →   Handle(ctx).NoContext()  ← 并发，不等待
GetContextAsync()  →   Handle(ctx).NoContext()  ← 并发，不等待
...
```
每个请求的 `Handle()` 通过 `.NoContext()` 独立异步执行，`Accept()` 循环立即继续等待下一个请求，实现请求并发处理。

### 边界情况分析

| 场景 | 处理方式 | 风险 |
|------|----------|------|
| 未注册路径 | KeyNotFoundException → Log.Error → 空响应 | 客户端超时或无法感知错误 |
| HttpListener 已关闭时接收请求 | ObjectDisposedException 静默忽略 | 正常行为，无风险 |
| Handler 内部异常 | Log.Error，Response.Close() 仍执行 | 低风险，响应资源不泄漏 |
| 地址权限不足 | HttpListenerException 转换为更友好的提示异常 | 需要管理员预先授权 |

### 与 cn.etetet.login 的集成

`cn.etetet.login` 包通常会实现 `IHttpHandler` 处理登录 HTTP 接口（Realm 场景），是 `cn.etetet.http` 的主要业务消费者：

```csharp
// cn.etetet.login 中的典型用法
[HttpHandler(SceneType.Realm, "/account/login")]
public class AccountLoginHttpHandler: IHttpHandler
{
    public async ETTask Handle(Scene scene, HttpListenerContext context)
    {
        // 解析请求体 → 验证账号密码 → 返回 token
    }
}
```
