# cn.etetet.memorypack

## 概述

**版本**: 1.10.1
**显示名称**: ET.MemoryPack
**来源**: Copy From https://github.com/Cysharp/MemoryPack
**关键词**: Serialization
**许可**: MIT

MemoryPack 是一个极高性能的二进制序列化库，移植自 Cysharp/MemoryPack。它采用零拷贝（zero-copy）技术和内存直写策略，专为 .NET/Unity 环境下的高速序列化需求而设计。在 ET 框架中，主要用于网络消息（Proto 包）的序列化/反序列化。

---

## 目录结构

```
cn.etetet.memorypack/
├── package.json
├── Runtime/
│   ├── MemoryPack.Core/
│   │   ├── Attributes.cs                     # 序列化特性定义
│   │   ├── CustomFormatterAttributes.cs      # 自定义格式化器特性
│   │   ├── IMemoryPackable.cs                # 核心可序列化接口
│   │   ├── IMemoryPackFormatter.cs           # 格式化器接口和基类
│   │   ├── MemoryPackCode.cs                 # 协议常量 (null、空集合标记)
│   │   ├── MemoryPackFormatterProvider.cs    # 格式化器注册和查找中心
│   │   ├── MemoryPackFormatterProvider.WellknownTypes.cs  # 内置类型格式化器注册
│   │   ├── MemoryPackReader.cs               # 二进制读取器（ref struct）
│   │   ├── MemoryPackReader.Unmanaged.cs     # 非托管类型读取扩展
│   │   ├── MemoryPackReaderOptionalState.cs  # 读取器可选状态（选项/服务提供者）
│   │   ├── MemoryPackReaderWriter.VarInt.cs  # 可变长整数编解码
│   │   ├── MemoryPackSerializationException.cs  # 序列化异常
│   │   ├── MemoryPackSerializer.Serialize.cs    # 序列化入口（含ThreadStatic缓冲）
│   │   ├── MemoryPackSerializer.Deserialize.cs  # 反序列化入口
│   │   ├── MemoryPackSerializer.NonGenerics.cs  # 非泛型序列化接口
│   │   ├── MemoryPackSerializerOptions.cs    # 序列化选项（字符串编码等）
│   │   ├── MemoryPackWriter.cs               # 二进制写入器（ref struct，含深度检查）
│   │   ├── MemoryPackWriter.Unmanaged.cs     # 非托管类型写入扩展
│   │   ├── MemoryPackWriterOptionalState.cs  # 写入器可选状态
│   │   ├── Compression/
│   │   │   ├── BitPackFormatter.cs           # 位打包格式化器
│   │   │   ├── BrotliCompressor.cs           # Brotli 压缩器
│   │   │   ├── BrotliDecompressor.cs         # Brotli 解压器
│   │   │   └── BrotliFormatter.cs            # Brotli 压缩格式化器
│   │   ├── Formatters/
│   │   │   ├── ArrayFormatters.cs            # 数组类型格式化器
│   │   │   ├── CollectionFormatters.cs       # 集合类型格式化器（List, Dict等）
│   │   │   ├── DynamicUnionFormatter.cs      # 动态联合类型格式化器（运行时多态）
│   │   │   ├── GenericCollectionFormatters.cs # 泛型集合格式化器
│   │   │   ├── InterfaceCollectionFormatters.cs # IList/ICollection等接口格式化器
│   │   │   ├── NullableFormatter.cs          # Nullable<T> 格式化器
│   │   │   ├── StringFormatter.cs            # string 格式化器
│   │   │   ├── TupleFormatter.cs             # Tuple/ValueTuple 格式化器
│   │   │   ├── UnmanagedFormatter.cs         # 非托管类型直写格式化器（最高性能）
│   │   │   ├── BigIntegerFormatter.cs        # BigInteger 格式化器
│   │   │   ├── BitArrayFormatter.cs          # BitArray 格式化器
│   │   │   ├── CultureInfoFormatter.cs       # CultureInfo 格式化器
│   │   │   ├── KeyValuePairFormatter.cs      # KeyValuePair<K,V> 格式化器
│   │   │   ├── LazyFormatter.cs              # Lazy<T> 格式化器
│   │   │   ├── MemoryPackableFormatter.cs    # IMemoryPackable 包装格式化器
│   │   │   ├── MultiDimensionalArrayFormatters.cs # 2D/3D/4D 数组格式化器
│   │   │   ├── StringBuilderFormatter.cs     # StringBuilder 格式化器
│   │   │   ├── TimeZoneInfoFormatter.cs      # TimeZoneInfo 格式化器
│   │   │   ├── TypeFormatter.cs              # System.Type 格式化器
│   │   │   ├── UriFormatter.cs               # Uri 格式化器
│   │   │   └── VersionFormatter.cs           # Version 格式化器
│   │   └── Internal/
│   │       ├── EnumerableExtensions.cs       # IEnumerable 扩展方法
│   │       ├── FixedArrayBufferWriter.cs     # 固定长度缓冲区写入器（FixedSize优化）
│   │       ├── MathEx.cs                     # 数学辅助（数组容量增长算法）
│   │       ├── MemoryMarshalEx.cs            # .NET 5以下的 MemoryMarshal 兼容层
│   │       ├── PreserveAttribute.cs          # [Preserve] IL2CPP 保护特性
│   │       ├── ReusableLinkedArrayBufferWriter.cs  # 可复用链式缓冲区（ThreadStatic主缓冲）
│   │       ├── ReusableReadOnlySequenceBuilder.cs  # 可复用只读序列构建器（Stream反序列化）
│   │       └── TypeHelpers.cs                # 类型辅助：检测UnmanagedSZArray/FixedSize
│   └── MemoryPack.Unity/
│       ├── ProviderInitializer.cs            # Unity 类型格式化器注册（RuntimeInitializeOnLoadMethod）
│       └── UnityFormatters.cs               # Unity 特有类型格式化器
```

---

## 核心类与接口

### 接口

| 接口 | 说明 |
|------|------|
| `IMemoryPackable<T>` | 可序列化类型的核心接口，需实现静态 `Serialize` / `Deserialize` 方法（.NET 7+ 静态抽象接口） |
| `IMemoryPackFormatter<T>` | 泛型格式化器接口，定义 `Serialize(ref MemoryPackWriter, ref T?)` 和 `Deserialize(ref MemoryPackReader, ref T?)` |
| `IMemoryPackFormatter` | 非泛型格式化器接口，用于运行时动态类型处理（`object?` 参数） |
| `IMemoryPackFormatterRegister` | 格式化器自注册接口，生成代码通过此接口注册自身（`static RegisterFormatter()`） |
| `IFixedSizeMemoryPackable` | 固定大小可序列化接口，提供 `static int Size`（.NET 7+ 静态抽象） |

### 核心类

| 类 | 说明 |
|----|------|
| `MemoryPackSerializer` | 静态序列化入口（partial class），提供 `Serialize<T>` / `Deserialize<T>`，支持 `byte[]`、`IBufferWriter<byte>`、`Stream`、`ReadOnlySequence<byte>` |
| `MemoryPackWriter` | ref struct 二进制写入器，包装 `IBufferWriter<byte>`，带深度限制（1000层），提供 `WriteObjectHeader`、`WriteValue`、`WriteUnmanaged`、`WriteVarInt` 等 API |
| `MemoryPackReader` | ref struct 二进制读取器，包装 `ReadOnlySpan<byte>` / `ReadOnlySequence<byte>`，提供 `TryReadObjectHeader`、`ReadValue`、`ReadUnmanaged`、`ReadVarInt` 等 API |
| `MemoryPackFormatterProvider` | 全局格式化器注册表（静态），内含 `ConcurrentDictionary` 和泛型缓存 `Cache<T>` |
| `MemoryPackFormatter<T>` | 抽象格式化器基类，同时实现 `IMemoryPackFormatter<T>` 和非泛型 `IMemoryPackFormatter` |
| `MemoryPackSerializerOptions` | record 类型序列化选项，含 `StringEncoding`（Utf8/Utf16）和 `IServiceProvider` |
| `DynamicUnionFormatter<T>` | 运行时多态联合格式化器，通过 `Dictionary<Type, ushort>` 双向映射实现联合类型的动态序列化 |
| `UnmanagedFormatter<T>` | 非托管类型格式化器，直接调用 `Unsafe.WriteUnaligned`/`ReadUnaligned`，零开销 |
| `DangerousUnmanagedFormatter<T>` | 不要求 T:unmanaged 约束的版本，用于运行时检测为值类型的情况 |
| `SerializerWriterThreadStaticState` | ThreadStatic 内部状态，持有 `ReusableLinkedArrayBufferWriter` 和 `MemoryPackWriterOptionalState`，每线程仅创建一次 |

### 特性（Attributes）

| 特性 | 目标 | 说明 |
|------|------|------|
| `[MemoryPackable]` | 类/结构/接口 | 标记为可序列化，可指定 `GenerateType` 和 `SerializeLayout` |
| `[MemoryPackUnion(tag, type)]` | 类/接口 | 定义多态联合类型（继承序列化），tag 为 ushort，支持最多 249 个（WideTag机制支持更多） |
| `[MemoryPackOrder(n)]` | 字段/属性 | 显式指定序列化顺序（`SerializeLayout.Explicit` 模式） |
| `[MemoryPackIgnore]` | 字段/属性 | 排除序列化 |
| `[MemoryPackInclude]` | 字段/属性 | 强制包含私有成员序列化 |
| `[MemoryPackAllowSerialize]` | 字段/属性 | 允许序列化通常不允许的成员 |
| `[MemoryPackConstructor]` | 构造函数 | 指定反序列化使用的构造函数 |
| `[MemoryPackOnSerializing]` | 方法 | 序列化前回调 |
| `[MemoryPackOnSerialized]` | 方法 | 序列化后回调 |
| `[MemoryPackOnDeserializing]` | 方法 | 反序列化前回调 |
| `[MemoryPackOnDeserialized]` | 方法 | 反序列化后回调 |
| `[GenerateTypeScript]` | 类/接口 | 标记生成 TypeScript 类型定义 |
| `[Preserve]` | 任意 | IL2CPP 链接器保护，防止被裁剪（内部 PreserveAttribute） |

### 枚举

| 枚举 | 值 | 说明 |
|------|----|------|
| `GenerateType` | `Object`, `VersionTolerant`, `CircularReference`, `Collection`, `NoGenerate` | 代码生成策略 |
| `SerializeLayout` | `Sequential`, `Explicit` | 成员排列方式（顺序 vs 显式索引） |
| `StringEncoding` | `Utf16`, `Utf8` | 字符串编码方式，默认 Utf8 |

---

## 二进制协议格式

### 对象头（Object Header）
- `0x00~0xF9`（0~249）：成员数量或 Union Tag
- `0xFA`（250）：WideTag（Union 扩展标记，后跟 ushort）/ CircularReference 引用 ID 标记
- `0xFB~0xFE`（251~254）：保留
- `0xFF`（255）：Null 对象

### 集合头（Collection Header）
- 4 字节 int32：集合长度（元素数量），-1（`0xFFFFFFFF`）表示 null

### 字符串编码
- **Utf16 模式**：长度（int32，正数）+ UTF-16 字节序列（length × 2字节）
- **Utf8 模式**：`~utf8ByteCount`（int32，负数取反）+ utf16Length（int32）+ UTF-8 字节序列

### VarInt 编码（可变长整数）
针对小值节省空间，first byte 为类型码或直接值：

| 范围 | 编码 |
|------|------|
| -120 ~ 127 | 1字节 sbyte 直接编码 |
| byte 范围但超出上述 | TypeCode(-121) + 1字节 |
| sbyte 超出 -120 | TypeCode(-122) + 1字节 |
| ushort | TypeCode(-123) + 2字节 |
| short | TypeCode(-124) + 2字节 |
| uint | TypeCode(-125) + 4字节 |
| int | TypeCode(-126) + 4字节 |
| ulong | TypeCode(-127) + 8字节 |
| long | TypeCode(-128) + 8字节 |

---

## 实现原理

### 零拷贝序列化

MemoryPack 的核心设计目标是最大化内存效率：

1. **非托管类型直写**：对于 `unmanaged` 类型（int、float、struct等），直接调用 `Unsafe.WriteUnaligned` / `Unsafe.ReadUnaligned` 进行内存块拷贝，无任何格式化开销。

2. **线程静态缓冲区**：`MemoryPackSerializer` 使用 `[ThreadStatic]` 的 `SerializerWriterThreadStaticState`（含 `ReusableLinkedArrayBufferWriter`）作为临时写入缓冲，避免每次序列化时重新分配内存。初始化 `pinned: true` 使缓冲固定在内存中。

3. **`AllocateUninitializedArray`**：对非托管类型数组直接分配未初始化字节数组，跳过清零操作（GC.AllocateUninitializedArray）。

4. **ref 传值**：所有 Writer/Reader/Value 参数均通过 `ref` 传递，避免栈拷贝；Writer/Reader 本身为 `ref struct`，只能在栈上存在，彻底避免装箱。

5. **深度保护**：`MemoryPackWriter` 内置 `DepthLimit = 1000` 的递归深度检查，防止循环引用造成栈溢出。

### 格式化器解析流程

```
MemoryPackSerializer.Serialize<T>()
  │
  ├─ T is unmanaged? → Unsafe.WriteUnaligned 直写（最快路径）
  ├─ T[] of unmanaged? → 直接 CopyBlockUnaligned（memcpy）
  ├─ T implements IFixedSizeMemoryPackable? → FixedArrayBufferWriter（精确分配）
  └─ 通用路径:
       MemoryPackFormatterProvider.GetFormatter<T>()
         │
         ├─ Cache<T>.formatter（静态泛型缓存，首次访问时在静态构造函数中初始化，后续无锁）
         │    ├─ T implements IMemoryPackFormatterRegister? → 反射调用 RegisterFormatter()
         │    ├─ 匿名类型 → ErrorMemoryPackFormatter（抛出）
         │    └─ CreateGenericFormatter() 按优先级尝试：
         │         enum/unmanaged → DangerousUnmanagedFormatter
         │         Tuple → TupleFormatters
         │         KnownGeneric → KeyValuePair, Lazy, Nullable
         │         ArrayLike → Memory<T>, ReadOnlyMemory<T>等
         │         Collection → List, Dict等标准集合
         │         InterfaceCollection → IList, IDictionary等
         │         自定义泛型工厂 genericFormatterFactory
         └─ formatter.Serialize(ref writer, ref value)
```

### 反序列化流程

```
MemoryPackSerializer.Deserialize<T>(ReadOnlySpan<byte>)
  │
  ├─ T is unmanaged? → Unsafe.ReadUnaligned 直读（最快路径）
  └─ 通用路径:
       new MemoryPackReader(buffer, state)
       reader.ReadValue(ref value)
         → formatter.Deserialize(ref reader, ref value)
       reader.Dispose()（归还 rentBuffer 到 ArrayPool）
```

### 异步流反序列化流程

```
DeserializeAsync<T>(Stream stream)
  │
  ├─ MemoryStream with buffer → 直接 ReadOnlySpan 快路径
  └─ 通用 Stream:
       ReusableReadOnlySequenceBuilderPool.Rent()
       循环读取 64K 块 → builder.Add(buffer)
       结束时构建 ReadOnlySequence<byte>
       Deserialize<T>(sequence)
       builder.Reset()
```

### Union 多态序列化（DynamicUnionFormatter）

```csharp
// 序列化：运行时获取实际类型 → 查 typeToTag → 写 UnionHeader(tag) + 具体类型数据
// 反序列化：读 UnionHeader(tag) → 查 tagToType → 反射写入具体类型
```
`DynamicUnionFormatter<T>` 在运行时通过两个 `Dictionary`（typeToTag / tagToType）实现双向映射，适合需要在运行时动态注册联合类型的场景。

### Unity 类型注册

`ProviderInitializer.cs` 在模块加载时自动为 Unity 内置类型注册格式化器：
- Vector2/3/4, Quaternion, Color, Color32, Bounds, Rect, Matrix4x4, Plane, Ray, Ray2D, LayerMask, RangeInt → `UnmanagedFormatter<T>` (直写)
- AnimationCurve, Gradient, RectOffset → 自定义格式化器

---

## 关键流程图

### 序列化流程

```
用户调用
MemoryPackSerializer.Serialize<T>(value)
         │
         ▼
  IsReferenceOrContainsReferences<T> ?
    否 → Unsafe.WriteUnaligned (纯内存拷贝, 返回精确大小数组)
    是 ↓
  NET7+: 检查 TypeHelpers.TryGetUnmanagedSZArrayElementSizeOrMemoryPackableFixedSize
    UnmanagedSZArray → 直接 CopyBlockUnaligned (4+N bytes)
    FixedSizeMemoryPackable → FixedArrayBufferWriter 精确分配
    其他 ↓
  ThreadStatic SerializerWriterThreadStaticState 初始化/复用
         │
         ▼
  MemoryPackWriter 包装 ReusableLinkedArrayBufferWriter
         │
         ▼
  IMemoryPackFormatter<T>.Serialize(writer, value)
    (由 Source Generator 生成的 Serialize 实现)
         │
         ▼
  逐字段写入 (基本类型直写, 引用类型递归, depth++ depth--)
         │
         ▼
  writer.Flush() → BufferWriter.ToArrayAndReset()
         │
         ▼
  返回 byte[]
```

---

## 代码示例

### 定义可序列化消息

```csharp
[MemoryPackable]
public partial class C2G_LoginGate : IMessage
{
    public int RpcId { get; set; }
    public string Key { get; set; }
    public long GateSessionId { get; set; }
}

// Source Generator 会自动生成：
// static void Serialize(ref MemoryPackWriter writer, ref C2G_LoginGate? value)
// static void Deserialize(ref MemoryPackReader reader, ref C2G_LoginGate? value)
```

### 序列化与反序列化

```csharp
// 序列化
var msg = new C2G_LoginGate { Key = "abc", GateSessionId = 12345 };
byte[] bytes = MemoryPackSerializer.Serialize(msg);

// 反序列化
var result = MemoryPackSerializer.Deserialize<C2G_LoginGate>(bytes);

// 写入到 IBufferWriter（网络层常用）
MemoryPackSerializer.Serialize<C2G_LoginGate>(bufferWriter, msg);

// 从 ReadOnlySequence 反序列化（接收缓冲）
int consumed = MemoryPackSerializer.Deserialize<C2G_LoginGate>(sequence, ref msg);
```

### 多态 Union 类型

```csharp
[MemoryPackable]
[MemoryPackUnion(0, typeof(Cat))]
[MemoryPackUnion(1, typeof(Dog))]
public partial interface IAnimal { }

[MemoryPackable]
public partial class Cat : IAnimal { public string Name { get; set; } }

// 动态注册（运行时）
var formatter = new DynamicUnionFormatter<IAnimal>((0, typeof(Cat)), (1, typeof(Dog)));
MemoryPackFormatterProvider.Register(formatter);
```

### VarInt 使用（状态同步等）

```csharp
// Writer
writer.WriteVarInt(entityId);   // 小值(0-127): 仅1字节

// Reader
long entityId = reader.ReadVarIntInt64();
```

### 版本兼容模式

```csharp
[MemoryPackable(GenerateType.VersionTolerant)]
public partial class PlayerData
{
    [MemoryPackOrder(0)] public int Level { get; set; }
    [MemoryPackOrder(1)] public string Name { get; set; }
    // 新增字段时追加 Order=2,3...，旧数据仍可反序列化
}
```

---

## 依赖关系

### 被哪些包使用

- **cn.etetet.proto** — 网络消息类用 `[MemoryPackable]` 标注，生成序列化代码，通过 MemoryPackSerializer 进行消息的编解码
- **cn.etetet.sourcegenerator** — Source Generator 为标注了 `[MemoryPackable]` 的类型生成 `IMemoryPackable<T>` 的静态 Serialize/Deserialize 实现
- **cn.etetet.netinner** — 网络层在发包/收包时调用 MemoryPackSerializer，使用 `IBufferWriter<byte>` 写入 / `ReadOnlySequence<byte>` 读取
- **cn.etetet.statesync** — 状态同步消息序列化
- **cn.etetet.excel** — Excel 数据表序列化（配置文件的二进制缓存）

### 外部依赖

- `System.Buffers`（ArrayPool, IBufferWriter, ReadOnlySequence）
- `System.Runtime.CompilerServices`（Unsafe, RuntimeHelpers, MethodImpl）
- `System.Runtime.InteropServices`（MemoryMarshal, StructLayout）
- `System.Runtime.Serialization`（用于 DataContractSerializer 兼容）
- `UnityEngine`（Vector3, Quaternion 等，仅 Unity 运行时）
- **无其他 ET 包依赖**（基础工具库）

---

## 性能考量

1. **热路径零分配**：非托管类型走 `Unsafe.WriteUnaligned` 路径，无 GC 压力；对应的，所有 `ref struct` 的 Writer/Reader 均在栈上，不产生 GC
2. **ThreadStatic 复用**：`SerializerWriterThreadStaticState` 每线程只分配一次（含 `ReusableLinkedArrayBufferWriter`，pinned 内存），后续复用无分配
3. **静态泛型缓存**：`Cache<T>.formatter` 利用泛型类静态构造函数保证每类型只初始化一次（类型安全的懒加载，无锁，首次调用后为纯读操作）
4. **固定大小优化**：实现 `IFixedSizeMemoryPackable` 的类型可跳过动态缓冲区分配，使用 `FixedArrayBufferWriter` 精确预分配
5. **UnmanagedSZArray 快路径**：一维非托管数组直接 `memcpy`（`Unsafe.CopyBlockUnaligned`），是网络层 byte[] 传输的最优路径
6. **Utf16 vs Utf8**：Utf16 编码在 .NET 内部无需转换，适合纯 .NET 场景；Utf8 更紧凑，适合混合或持久化场景。ET 默认 Utf8
7. **深度限制保护**：递归深度超过 1000 层时抛出异常，防止恶意数据造成栈溢出

## 注意事项

- Unity 2021.2 以下版本不支持 `MemoryPackCustomFormatterAttribute<T>` 泛型特性（条件编译排除）
- `GenerateType.VersionTolerant` 和 `CircularReference` 模式强制使用 `SerializeLayout.Explicit`，需为每个成员手动指定 `[MemoryPackOrder]`
- 匿名类型无法序列化（`ErrorMemoryPackFormatter` 会在运行时抛出异常）
- `MemoryPackWriter` / `MemoryPackReader` 是 `ref struct`，不能捕获到闭包或 async 方法中
- `DangerousReadUnmanagedArray`/`DangerousReadUnmanagedSpan` 等 Dangerous 方法跳过类型检查，仅在确认 T 为非托管类型时使用
- `MemoryPackWriter.GetSpanReference` 返回的 `ref byte` 仅在下次 `GetSpanReference` 之前有效（多段缓冲区滑动窗口）
- `MemoryPackSerializerOptions` 是 record 类型，不可变，通过 `with` 表达式创建变体
- `MemoryPackFormatterProvider` 是全局静态注册表，格式化器注册应在程序启动时完成，避免并发注册
