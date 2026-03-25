# cn.etetet.proto

## 概述

`cn.etetet.proto` 是 ET MMO 框架的协议代码生成工具包（v3.0.2）。它提供了一套完整的 `.proto` 文件到 C# 代码的转换流程，同时包含所有游戏网络消息的定义（由 proto 文件生成的 C# 类）。这是整个游戏网络通信的基础协议层。

**核心职责**：
- Proto 文件解析与 C# 代码生成（DotNet~ 工具）
- Unity Editor 集成，方便开发者触发代码生成
- 管理网络消息 Opcode（消息操作码），确保全局唯一性
- 提供所有客户端/服务端之间通信的消息类定义

---

## 目录结构

```
cn.etetet.proto/
├── CodeMode/                    # 生成的消息 C# 文件（按部署目标分类）
│   └── Model/
│       ├── Client/              # 仅客户端使用的消息
│       │   ├── LoginOuter_C_1000.cs
│       │   ├── RouterProto_C_1100.cs
│       │   └── StateSyncOuter_C_11001.cs
│       ├── ClientServer/        # 客户端和服务端共同使用的消息
│       │   ├── LoginOuter_C_1000.cs
│       │   ├── LoginInner_S_20001.cs
│       │   ├── RouterProto_C_1100.cs
│       │   ├── StateSyncOuter_C_11001.cs
│       │   ├── StateSyncInner_S_21001.cs
│       │   └── ActorLocation_S_20100.cs
│       └── Server/              # 仅服务端使用的消息
│           ├── LoginOuter_C_1000.cs
│           ├── LoginInner_S_20001.cs
│           ├── RouterProto_C_1100.cs
│           ├── StateSyncOuter_C_11001.cs
│           ├── StateSyncInner_S_21001.cs
│           └── ActorLocation_S_20100.cs
├── DotNet~/                     # .NET 8 独立工具，用于 proto → C# 转换
│   ├── Init.cs                  # 程序入口点（调用 NoCut.Run() + Proto2CS.Export()）
│   ├── Proto2CS.cs              # 核心转换逻辑
│   ├── Template.txt             # 配置表代码模板（供 Excel 包使用）
│   ├── ET.Proto2CS.csproj       # .NET 项目文件
│   └── Exe/
│       └── ET.Proto2CS.dll      # 编译好的工具 DLL
│       └── ET.Proto2CS          # 可执行文件
└── Editor/                      # Unity Editor 集成
    ├── ProtoEditor.cs           # 菜单命令：ET/Proto/Proto2CS（包初始化时自动触发）
    └── ProtoViewWindow.cs       # Proto 可视化管理窗口
```

---

## Proto 文件命名规范

Proto 文件必须遵循固定命名规则：

```
{ProtoName}_{C 或 S}_{起始Opcode}.proto
```

- `{ProtoName}` — 协议组名（如 `LoginOuter`、`StateSyncOuter`）
- `C` — 表示该 proto 生成**客户端**消息（同时也生成到服务端和 ClientServer）
- `S` — 表示该 proto 生成**服务端**消息（只生成到服务端和 ClientServer）
- `{起始Opcode}` — 该文件第一个消息的 opcode 起始值（每条消息依次 +1）

**生成规则**：
- `_C_` 文件 → 生成到 `Client/`、`Server/`、`ClientServer/`
- `_S_` 文件 → 生成到 `Server/`、`ClientServer/`

**命名校验（ProtoViewWindow）**：
- 必须包含 `_C_` 或 `_S_` 之一（两者都有或都没有 → errorCode=1，红色报错）
- Opcode 数字段必须能 `int.TryParse` 解析（失败 → errorCode=3，红色报错）
- 同一 Opcode 数值不能在多个文件中重复（重复 → errorCode=2，黄色警告）

---

## 核心类/接口

### DotNet~ 工具（代码生成器）

#### `Init`（内部静态类）
**文件**: `DotNet~/Init.cs`

程序入口，依次执行：
1. `NoCut.Run()` — 强制引用 MongoHelper，防止 .NET 8 发布时裁剪 MongoDB 库
2. `Proto2CS.Export()` — 触发完整转换流程
3. 输出 `"proto2cs ok!"` 确认完成

```csharp
internal static class Init
{
    private static int Main(string[] args)
    {
        try
        {
            NoCut.Run();        // 防 tree-shaking
            Proto2CS.Export();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        Console.WriteLine("proto2cs ok!");
        return 1;
    }
}
```

#### `Proto2CS`（公共静态类）
**文件**: `DotNet~/Proto2CS.cs`

对外暴露的入口类，单一方法委托给 `InnerProto2CS`：
```csharp
public static class Proto2CS
{
    public static void Export()  // 触发完整的 proto → C# 转换流程
    {
        InnerProto2CS.Proto2CS();
    }
}
```

#### `InnerProto2CS`（内部静态分部类）
**文件**: `DotNet~/Proto2CS.cs`

核心转换引擎，包含所有解析和代码生成逻辑。

**静态字段**：
| 字段 | 类型 | 说明 |
|------|------|------|
| `clientMessagePath` | `string` | 输出路径：`CodeMode/Model/Client` |
| `serverMessagePath` | `string` | 输出路径：`CodeMode/Model/Server` |
| `clientServerMessagePath` | `string` | 输出路径：`CodeMode/Model/ClientServer` |
| `splitChars` | `char[]` | 字段分割符：`[' ', '\t']` |
| `msgOpcode` | `List<OpcodeInfo>` | 当前文件的 opcode 列表（每文件清空一次） |
| `opcodeList` | `List<int>` | 全局 opcode 列表（用于检测重复，每次 Export 清空） |

**方法**：
| 方法 | 说明 |
|------|------|
| `Proto2CS()` | 主流程：先调用 `MongoHelper.ToJson(1)` 防裁剪，然后扫描所有包的 Proto 目录，逐文件调用转换 |
| `ProtoFile2CS(path, module, protoName, cs, startOpcode)` | 将单个 .proto 文件转换为 C# 代码字符串 |
| `GenerateCS(result, path, proto)` | 将生成的 C# 代码写入目标路径（`FileMode.Create` 覆盖写） |
| `Map(sb, newline, sbDispose)` | 处理 proto 中的 `map<K,V>` 字段，生成 Dictionary + BsonDictionaryOptions |
| `Repeated(sb, newline, sbDispose)` | 处理 proto 中的 `repeated` 字段（生成 `List<T> = new()`） |
| `Members(sb, newline, sbDispose)` | 处理 proto 中的普通字段（生成属性 + Dispose 重置） |
| `ConvertType(type)` | proto 类型 → C# 类型映射 |
| `ResponseTypeRegex()` | Source-Generated Regex：匹配 `// ResponseType` 注释 |

**注意**：`MongoHelper.ToJson(1)` 在 `Proto2CS()` 方法顶部被调用（不是在 Init 中），作用是强制使 MongoDB.Bson 库被 JIT 引用，防止 .NET 8 AOT/裁剪时将其移除。

#### `OpcodeInfo`（内部类）
```csharp
internal class OpcodeInfo
{
    public string Name;    // 消息类名
    public int Opcode;     // 对应的 opcode 整数值
}
```

---

### Unity Editor 集成

#### `ProtoEditor`（静态类）
**文件**: `Editor/ProtoEditor.cs`

| 成员 | 说明 |
|------|------|
| `[MenuItem("ET/Proto/Proto2CS")] Run()` | 执行 DotNet 工具进行代码生成（`ProcessHelper.DotNet` 同步等待） |
| `[MenuItem("Assets/Create/ET/Create Proto")] GenerateProto()` | 在 Project 窗口快速创建空 .proto 模板文件 |
| `Init()` | **包初始化钩子**：每次 Unity 打开/编译都会自动调用一次 `Run()`，确保生成代码始终是最新的 |

`Init()` 是包加载钩子（通过 ET 框架的包系统自动调用），意味着 **每次编辑器启动都会自动重新生成所有协议**，无需手动触发。

#### `ProtoViewWindow`（EditorWindow）
**文件**: `Editor/ProtoViewWindow.cs`（命名空间：`ET.Editor`）

菜单：`ET/Proto/ProtoView`

**字段**：
| 字段 | 类型 | 说明 |
|------|------|------|
| `togSort` / `lastTogSort` | `bool` | 是否按 opcode 排序显示 |
| `scrollViewPos` | `Vector2` | 滚动列表位置 |
| `vsCodePath` / `newVSCodePath` | `string` | VSCode 可执行文件路径（点击标签栏选择） |
| `togOpenFolder` | `bool` | 打开 VSCode 时是否同时在文件管理器中显示临时目录 |
| `protoItems` | `List<ItemData>` | 扫描到的所有 .proto 文件列表 |
| `delDataPath` | `string` | 项目根路径（用于路径处理） |
| `protoConbinePath` | `string` | 临时合并目录：`{ProjectRoot}/Temp` |

**核心方法**：
| 方法 | 说明 |
|------|------|
| `RefreshDataList()` | 扫描 Packages 目录下所有 `.proto` 文件，校验命名，设置 errorCode |
| `CombineProtoOpenByVSCode()` | 将所有 proto 复制到 `Temp/TmpProto/`，用 VSCode 打开整个目录 |
| `SeprateProtoAndSave()` | 将编辑后的 `Temp/TmpProto/` 中的文件逐一拷贝回原始分散位置 |
| `FindProtoFilesInPackagesDirectory()` | 递归搜索 `Packages/` 下所有 `*.proto` 文件 |
| `ProtoStyleIsError(name)` | 检查命名格式：必须**恰好含有** `_C_` 或 `_S_` 之一 |
| `OpenInVSCode(dirPath)` | 启动 VSCode 进程，传入目录路径作为参数 |

**ItemData 内部类**：
```csharp
class ItemData
{
    public string name;        // proto 文件名（不含扩展名）
    public int num;            // 解析出的起始 opcode 数字
    public string simpPath;    // 仅文件名（含扩展名）
    public string fullPath;    // 完整绝对路径
    public short errorCode;    // 0=正常, 1=命名格式错误, 2=opcode重复, 3=数字解析失败
}
```

**GUI 颜色编码**：
- `errorCode=0` → 白色（正常）
- `errorCode=1` → 红色（命名格式错误：缺少 _C_ 或 _S_）
- `errorCode=2` → 黄色（opcode 数值重复）
- `errorCode=3` → 红色（opcode 数字段无法解析）

---

## 消息类生成规则

每个 proto `message` 块在生成的 C# 类中包含以下固定结构：

```csharp
[MemoryPackable]                                    // MemoryPack 序列化标记
[Message(ProtoName.MessageName)]                    // 消息 opcode 绑定（ushort 常量）
[ResponseType(nameof(ResponseClass))]               // （可选）标注对应的响应类
public partial class MessageName : MessageObject, IXxx
{
    // 工厂方法（支持对象池）
    public static MessageName Create(bool isFromPool = false)
    {
        return ObjectPool.Fetch<MessageName>(isFromPool);
    }

    [MemoryPackOrder(n-1)]                          // 序列化字段顺序（proto编号-1，从0开始）
    public Type FieldName { get; set; }

    // 自动生成的 Dispose（回收到对象池）
    public override void Dispose()
    {
        if (!this.IsFromPool) return;
        // 重置所有值类型字段 = default
        // 清空 List/Dictionary（不设 null，避免重新分配）
        // byte[] 字段不重置
        ObjectPool.Recycle(this);
    }
}
```

**Dispose 策略细节**：
- 值类型字段（int/long/float3 等）→ `this.X = default;`
- `repeated`（List）字段 → `this.X.Clear();`（保留 List 对象复用）
- `map`（Dictionary）字段 → `this.X.Clear();`（保留 Dictionary 对象复用）
- `bytes`（byte[]）字段 → **不重置**（避免频繁分配大数组）
- 添加 `// no dispose` 注释到消息的 `}` 行 → 跳过自动生成，需手动实现

**Opcode 常量生成**：每个 .proto 文件末尾生成一个同名 `public static class {ProtoName}`，其中每条消息对应一个 `const ushort` 常量：
```csharp
public static class StateSyncOuter
{
    public const ushort RouterSync = 11002;
    public const ushort C2M_TestRequest = 11003;
    // ...
}
```

---

## 消息接口继承关系

生成的消息类继承 `MessageObject` 并实现以下接口之一：

| 接口 | 用途 | 标准字段 |
|------|------|---------|
| `IMessage` | 普通单向消息（服务器→客户端广播） | 无额外字段 |
| `IRequest` / `IResponse` | Actor 内部 RPC 请求/响应 | RpcId; Error, Message |
| `ISessionRequest` / `ISessionResponse` | Session 直连 RPC 请求/响应 | RpcId; Error, Message |
| `ISessionMessage` | Session 单向消息 | 无额外字段 |
| `ILocationRequest` / `ILocationResponse` | Actor Location（位置感知）RPC | RpcId; Error, Message |
| `ILocationMessage` | Actor Location 单向消息 | RpcId |
| `IActorMessage` / `IActorRequest` / `IActorResponse` | Actor 消息 | 视具体情况 |

> **规律**：所有 Response/Response 类型都会有 `RpcId`（用于请求配对）、`Error`（错误码，0表示成功）、`Message`（错误描述文本）三个标准字段。

---

## 当前已定义消息（完整字段说明）

### LoginOuter（Client 协议，起始 opcode 1000）

| Opcode | 消息名 | 接口 | 字段 |
|--------|--------|------|------|
| 1001 | Main2NetClient_Login | IRequest | RpcId, OwnerFiberId(int), Address(string), Account(string), Password(string) |
| 1002 | NetClient2Main_Login | IResponse | RpcId, Error, Message, PlayerId(long) |
| 1003 | C2G_Ping | ISessionRequest | RpcId |
| 1004 | G2C_Ping | ISessionResponse | RpcId, Error, Message, Time(long) — 服务器时间戳 |
| 1005 | C2R_Login | ISessionRequest | RpcId, Account(string), Password(string) |
| 1006 | R2C_Login | ISessionResponse | RpcId, Error, Message, Address(string), Key(long), GateId(long) |
| 1007 | C2G_LoginGate | ISessionRequest | RpcId, Key(long), GateId(long) |
| 1008 | G2C_LoginGate | ISessionResponse | RpcId, Error, Message, PlayerId(long) |

> **修正**：`Main2NetClient_Login` 是 Fiber 内部 IPC（不经过网络），额外字段 `OwnerFiberId`（发送方 Fiber ID）和 `Address`（目标服务器地址）是内部路由信息，Round 1 文档省略了这两个字段。

### RouterProto（Client 协议，起始 opcode 1100）

| Opcode | 消息名 | 接口 | 字段 |
|--------|--------|------|------|
| 1101 | HttpGetRouterResponse | （无） | Realms(List\<string\>), Routers(List\<string\>) |

### StateSyncOuter（Client 协议，起始 opcode 11001）

| Opcode | 消息名 | 接口 | 关键字段 |
|--------|--------|------|---------|
| 11002 | RouterSync | （无） | ConnectId(uint), Address(string) |
| 11003 | C2M_TestRequest | ILocationRequest | RpcId, request(string) |
| 11004 | M2C_TestResponse | IResponse | RpcId, Error, Message, response(string) |
| 11005 | C2G_EnterMap | ISessionRequest | RpcId |
| 11006 | G2C_EnterMap | ISessionResponse | RpcId, Error, Message, **MyId(long)** — 玩家 UnitId |
| 11007 | MoveInfo | （无） | Points(List\<float3\>), Rotation(quaternion), TurnSpeed(int) |
| 11008 | UnitInfo | （无） | UnitId(long), ConfigId(int), Type(int), Position(float3), Forward(float3), KV(Dictionary\<int,long\>), MoveInfo(MoveInfo) |
| 11009 | M2C_CreateUnits | IMessage | Units(List\<UnitInfo\>) |
| 11010 | M2C_CreateMyUnit | IMessage | Unit(UnitInfo) |
| 11011 | M2C_StartSceneChange | IMessage | SceneInstanceId(long), SceneName(string) |
| 11012 | M2C_RemoveUnits | IMessage | Units(List\<long\>) — 单位 ID 列表 |
| 11013 | C2M_PathfindingResult | ILocationMessage | RpcId, Position(float3) — 目标点 |
| 11014 | C2M_Stop | ILocationMessage | RpcId |
| 11015 | M2C_PathfindingResult | IMessage | Id(long), Position(float3), Points(List\<float3\>) — 完整路径 |
| 11016 | M2C_Stop | IMessage | **Error(int)**, Id(long), Position(float3), Rotation(quaternion) |
| 11017 | G2C_Test | ISessionMessage | （无字段） |
| 11018 | C2M_Reload | **ISessionRequest** | RpcId, Account(string), Password(string) |
| 11019 | M2C_Reload | ISessionResponse | RpcId, Error, Message |
| 11020 | G2C_TestHotfixMessage | ISessionMessage | Info(string) |
| 11021 | C2M_TestRobotCase | ILocationRequest | RpcId, N(int) — 压测次数 |
| 11022 | M2C_TestRobotCase | ILocationResponse | RpcId, Error, Message, N(int) |
| 11023 | C2M_TestRobotCase2 | ILocationMessage | RpcId, N(int) |
| 11024 | M2C_TestRobotCase2 | ILocationMessage | RpcId, N(int) |
| 11025 | C2M_TransferMap | ILocationRequest | RpcId |
| 11026 | M2C_TransferMap | ILocationResponse | RpcId, Error, Message |
| 11027 | C2G_Benchmark | ISessionRequest | RpcId |
| 11028 | G2C_Benchmark | ISessionResponse | RpcId, Error, Message |

> **修正**：
> - `M2C_Stop` 有 `Error(int)` 字段，Round 1 文档遗漏了此字段
> - `C2M_Reload` 接口是 **ISessionRequest**（不是 ILocationRequest），说明热重载请求直接通过 Gate Session 传递，无需 Actor Location 路由

### LoginInner（Server 协议，起始 opcode 20001）

| Opcode | 消息名 | 接口 | 字段 |
|--------|--------|------|------|
| 20002 | R2G_GetLoginKey | IRequest | RpcId, Account(string) |
| 20003 | G2R_GetLoginKey | IResponse | RpcId, Error, Message, Key(long), GateId(long) |
| 20004 | G2M_SessionDisconnect | ILocationMessage | RpcId |

### ActorLocation（Server 协议，起始 opcode 20100）

| Opcode | 消息名 | 接口 | 字段 |
|--------|--------|------|------|
| 20101 | ObjectAddRequest | IRequest | RpcId, Type(int), Key(long), ActorId(long) |
| 20102 | ObjectAddResponse | IResponse | RpcId, Error, Message |
| 20103 | ObjectLockRequest | IRequest | RpcId, Type(int), Key(long), Time(long) |
| 20104 | ObjectLockResponse | IResponse | RpcId, Error, Message |
| 20105 | ObjectUnLockRequest | IRequest | RpcId, Type(int), Key(long), OldActorId(long), NewActorId(long) |
| 20106 | ObjectUnLockResponse | IResponse | RpcId, Error, Message |
| 20107 | ObjectRemoveRequest | IRequest | RpcId, Type(int), Key(long) |
| 20108 | ObjectRemoveResponse | IResponse | RpcId, Error, Message |
| 20109 | ObjectGetRequest | IRequest | RpcId, Type(int), Key(long) |
| 20110 | ObjectGetResponse | IResponse | RpcId, Error, Message, ActorId(long) |

### StateSyncInner（Server 协议，起始 opcode 21001）

| Opcode | 消息名 | 接口 | 字段 |
|--------|--------|------|------|
| 21002 | M2A_Reload | IRequest | RpcId |
| 21003 | A2M_Reload | IResponse | RpcId, Error, Message |
| 21004 | M2M_UnitTransferRequest | IRequest | RpcId, UnitId(long), Units(byte[]), Entitys(byte[]) |
| 21005 | M2M_UnitTransferResponse | IResponse | RpcId, Error, Message |

> **补充**：`M2M_UnitTransferRequest` 的 `Units` 和 `Entitys` 字段是 **byte[]** 类型，这意味着 Map 间单位转移通过序列化后的字节流传输完整 Entity 数据（可能是 MemoryPack 序列化结果）。

---

## 实现原理

### Proto2CS 转换流程

```
1. 初始化（防止 tree-shaking）：MongoHelper.ToJson(1)
2. 读取 PackagesLock → 解析各包路径
3. 扫描所有包的 Proto/ 目录，收集 .proto 文件 + 模块名
4. 按文件名解析：ProtoName_C/S_StartOpcode.proto
5. 逐文件调用 ProtoFile2CS：
   a. 清空 msgOpcode 列表（每文件独立）
   b. 生成文件头：using MemoryPack; + namespace ET {
   c. 逐行扫描 proto：
      - // ResponseType XXX → 缓存 responseType 变量
      - ///注释 → XML <summary> 注释（放在类级别）
      - //注释 → 单行注释
      - message XXX // Interface → 生成 partial class + attributes
      - { → 清空 sbDispose，生成类体 + static Create() 方法
      - 字段行 → 检测 map/repeated/普通字段，生成属性 + Dispose 重置代码
      - } → 插入 Dispose 方法（除非含 // no dispose），关闭类
   d. 追加 Opcode 常量静态类
   e. 关闭 namespace
6. 根据 C/S 写入目标目录（C→三目录，S→两目录）
7. 全局 opcodeList 中检测重复（Console.WriteLine 警告）
```

### 类型映射表（proto → C#）

| Proto 类型 | C# 类型 |
|-----------|---------|
| int16 | short |
| int32 | int |
| bytes | byte[] |
| uint32 | uint |
| long / int64 | long |
| uint64 | ulong |
| uint16 | ushort |
| float3 | Unity.Mathematics.float3 |
| quaternion | Unity.Mathematics.quaternion |
| 其他（含自定义类型） | 原样保留 |

### 字段处理规则

**普通字段**：
```
int32 RpcId = 1;
→  [MemoryPackOrder(0)] public int RpcId { get; set; }
   Dispose: this.RpcId = default;
```

**repeated 字段**：
```
repeated UnitInfo Units = 1;
→  [MemoryPackOrder(0)] public List<UnitInfo> Units { get; set; } = new();
   Dispose: this.Units.Clear();
```

**map 字段**：
```
map<int32, long> KV = 1;
→  [MongoDB.Bson.Serialization.Attributes.BsonDictionaryOptions(...ArrayOfArrays)]
   [MemoryPackOrder(0)]
   public Dictionary<int, long> KV { get; set; } = new();
   Dispose: this.KV.Clear();
```

**bytes 字段**：
```
bytes Data = 1;
→  [MemoryPackOrder(0)] public byte[] Data { get; set; }
   Dispose: （无重置代码）
```

**注意**：`MemoryPackOrder` 的值是 `(proto字段编号 - 1)`，即从0开始。

### sbDispose StringBuilder 机制

转换引擎在处理消息体时，用一个辅助 `StringBuilder sbDispose` 累积每个字段的重置代码片段，最后在遇到消息结束的 `}` 时，将所有累积的重置语句嵌入 Dispose 方法体中：

```csharp
// 类似这种最终插入效果：
public override void Dispose()
{
    if (!this.IsFromPool) { return; }

    this.RpcId = default;     // 来自 sbDispose
    this.Error = default;     // 来自 sbDispose
    this.Units.Clear();       // 来自 sbDispose

    ObjectPool.Recycle(this);
}
```

---

## 关键设计模式

### 对象池模式

所有消息类支持对象池复用，避免 GC 压力：

```csharp
// 从池中获取（isFromPool=true 时会在 Dispose 时自动回收）
var msg = C2R_Login.Create(isFromPool: true);
msg.Account = "user123";
msg.Password = "pass456";

// 通过网络发送（框架内部处理后自动调用 Dispose）
await session.Call(msg);

// 手动回收（不经过框架自动回收时）
msg.Dispose();  // 字段重置 + 回收到 ObjectPool
```

### Source-Generated Regex

`Proto2CS.cs` 使用 C# 的 Source Generators 自动生成高效正则表达式：

```csharp
[GeneratedRegex(@"//\s*ResponseType")]
private static partial Regex ResponseTypeRegex();
```

这避免了运行时正则表达式编译开销，是 .NET 7+ 的最佳实践。

### 自动初始化模式

`ProtoEditor.Init()` 被 ET 包管理系统注册为初始化钩子，实现"零配置"：
- 开发者添加包后，无需任何手动操作，下次打开 Unity 时自动生成协议代码
- 修改 proto 文件后，重新编译时自动触发更新

---

## Opcode 组织规则

| 范围 | 协议组 | 说明 |
|------|--------|------|
| 1000~1099 | LoginOuter | 客户端登录外部协议 |
| 1100~1199 | RouterProto | 路由器外部协议 |
| 11001~11999 | StateSyncOuter | 客户端状态同步外部协议 |
| 20001~20099 | LoginInner | 服务端登录内部协议 |
| 20100~20199 | ActorLocation | Actor 位置管理内部协议 |
| 21001~21999 | StateSyncInner | 服务端状态同步内部协议 |

---

## 依赖关系

### 本包依赖的其他包

| 依赖 | 用途 |
|------|------|
| `cn.etetet.core` | `MessageObject`、`ObjectPool`、消息接口（IRequest/IResponse 等）、`PackageHelper`、`PackagesLock` |
| `cn.etetet.memorypack` | MemoryPack 序列化（`[MemoryPackable]`、`[MemoryPackOrder]`） |
| `cn.etetet.mathematics` | `Unity.Mathematics.float3`、`quaternion`（在 MoveInfo/UnitInfo 中使用） |

### 依赖本包的其他包

| 依赖方包 | 原因 |
|---------|------|
| `cn.etetet.login` | 使用 LoginOuter、LoginInner 消息 |
| `cn.etetet.statesync` | 使用 StateSyncOuter、StateSyncInner 消息 |
| `cn.etetet.router` | 使用 RouterProto 消息 |
| `cn.etetet.actorlocation` | 使用 ActorLocation 消息 |
| 几乎所有游戏逻辑包 | 消息协议是跨层通信的唯一载体 |

---

## 开发工作流

### 添加新协议的步骤

1. **定义消息**：在对应包的 `Proto/` 目录下创建 `.proto` 文件（按命名规范）

   ```
   // 文件：Packages/cn.etetet.xxx/Proto/XxxMsg_C_15000.proto
   syntax = "proto3";
   package ET;

   // 请求进入副本
   // ResponseType EnterDungeon_Response
   message EnterDungeon_Request // ISessionRequest
   {
       int32 RpcId = 1;
       int32 DungeonId = 2;
   }

   message EnterDungeon_Response // ISessionResponse
   {
       int32 RpcId = 1;
       int32 Error = 2;
       string Message = 3;
       int64 InstanceId = 4;
   }
   ```

2. **触发生成**：`ET → Proto → Proto2CS` 菜单，或使用 `ProtoViewWindow` 的 "Proto2CS" 按钮

3. **查看结果**：生成的 C# 文件出现在 `CodeMode/Model/{Client|Server|ClientServer}/` 中

4. **编写处理器**：在对应的 Handler 包中实现消息处理逻辑（`[MessageHandler]` attribute）

### 多文件编辑工作流（ProtoViewWindow）

1. 点击 "open in vscode" → 所有 proto 合并到 `Temp/TmpProto/` → VSCode 打开该目录
2. 在 VSCode 中统一编辑多个 proto 文件
3. 点击 "save" → 将编辑后的文件逐一散回原始包目录
4. 点击 "Proto2CS" → 触发代码生成

---

## 注意事项

- Opcode 在全局范围内必须唯一，`ProtoViewWindow` 会以黄色高亮显示重复的 opcode
- Proto 文件名必须严格遵守 `Name_C/S_Number.proto` 格式，否则 `ProtoViewWindow` 会红色报错
- `// no dispose` 注释放在消息的 `}` 上，可阻止自动生成 Dispose 方法（需手动实现）
- `map<>` 字段自动添加 MongoDB `BsonDictionaryOptions` 注解（兼容持久化）
- `Template.txt` 是配置表（LuBan/Excel）的代码模板，与 proto 消息模板无关
- `bytes` 类型字段在 Dispose 中**不会重置**（设计决策：避免频繁分配）
- `MemoryPackOrder` 的值 = proto 字段编号 - 1（从0开始），删除中间字段时要注意顺序不能改变
- 生成的所有文件都会被 `\t` 替换为4个空格，行尾统一为 `\r\n`
