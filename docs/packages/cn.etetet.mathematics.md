# cn.etetet.mathematics

## 概述

`cn.etetet.mathematics` 是一个薄层包装包，目的是让 `Unity.Mathematics`（Unity 官方数学库）能够同时在 **Unity 客户端**和 **服务端 .NET 8** 环境下使用。

它本身不包含任何业务逻辑，只是一个跨平台适配桥接层。

---

## 版本信息

- **版本**: 0.0.2
- **Unity 最低版本**: 2022.3
- **目标框架（服务端）**: .NET 8 (net8.0)
- **Assembly 名称**: `ET.Mathematics`
- **根命名空间**: `ET`
- **C# 语言版本**: 12
- **作者**: tanghai（ET 框架作者）

---

## 目录结构

```
cn.etetet.mathematics/
├── package.json                          # Unity 包定义，依赖 com.unity.mathematics@1.2.6
├── DotNet~/                              # 服务端 .NET 项目（~ 后缀在 Unity 中被忽略）
│   ├── ET.Mathematics.csproj             # .NET 8 项目文件
│   └── Unity.Mathematics/
│       └── PropertyAttribute.cs         # UnityEngine.PropertyAttribute 桩类（服务端兼容）
```

---

## 实现原理

### 双平台支持策略

| 平台 | 数学库来源 | 说明 |
|------|-----------|------|
| Unity 客户端 | `com.unity.mathematics@1.2.6`（UPM 包） | 直接使用 Unity 官方包 |
| 服务端 .NET 8 | `ET.Mathematics.csproj` 编译 | 通过 MSBuild `<Compile Include>` 直接引用 PackageCache 中的 .cs 源码 |

### 服务端项目关键配置

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>disable</Nullable>
        <LangVersion>12</LangVersion>
        <RootNamespace>ET</RootNamespace>
        <AssemblyName>ET.Mathematics</AssemblyName>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>

    <ItemGroup>
        <!-- 直接编译 Unity PackageCache 中 com.unity.mathematics 的所有 .cs 文件 -->
        <Compile Include="..\..\..\Library\PackageCache\com.unity.mathematics*\Unity.Mathematics\**\*.cs">
            <Link>...</Link>
        </Compile>
    </ItemGroup>
</Project>
```

**编译时定义符号**（Debug 和 Release 均相同）：
- `DOTNET` — 标识当前为服务端 .NET 环境
- `UNITY_DOTSPLAYER` — 让 Unity.Mathematics 源码走服务端兼容路径（跳过 UnityEngine 特有特性）

**被抑制的警告**：
- `0169` — 私有字段未使用
- `0649` — 字段从未赋值
- `3021` — 过时属性
- `8981` — C# 预览功能
- `NU1903` — NuGet 包漏洞

> **Round 2 补充**：`Link` 路径表达式使用了 MSBuild 内联 C# 函数对路径进行处理，保证文件虚拟链接路径以 `Unity.Mathematics/` 为根。`AllowUnsafeBlocks=true` 是因为 Unity.Mathematics 内部大量使用 unsafe 代码进行 SIMD 操作。

### PropertyAttribute 桩类

服务端环境没有 `UnityEngine.dll`，而 `Unity.Mathematics` 源码中某些数学类型的属性（如 `[Inspectable]`）依赖 `UnityEngine.PropertyAttribute`。

`DotNet~/Unity.Mathematics/PropertyAttribute.cs` 提供了一个空桩类：

```csharp
namespace Unity.Mathematics.UnityEngine
{
    public class PropertyAttribute { }
}
```

这使得服务端编译可以通过，而无需引入 UnityEngine 依赖。

---

## 提供的数学能力

通过透传 Unity.Mathematics，整个项目（客户端+服务端）共享以下数学类型：

| 类型 | 说明 |
|------|------|
| `float2/3/4` | SIMD 向量类型 |
| `int2/3/4` | 整数向量类型 |
| `quaternion` | 四元数 |
| `float3x3/4x4` | 矩阵类型 |
| `math` | 静态数学函数（sin/cos/sqrt/lerp/clamp 等） |
| `Random` | 确定性随机数生成器 |
| `AABB` | 轴对齐包围盒 |

### 典型使用示例（服务端）

```csharp
using Unity.Mathematics;

// 向量运算
float3 pos = new float3(1f, 0f, 2f);
float3 dir = math.normalize(pos);
float dist = math.length(pos);

// 四元数旋转
quaternion rot = quaternion.AxisAngle(math.up(), math.PI / 4f);
float3 rotated = math.rotate(rot, pos);

// 确定性随机（服务端帧同步安全）
var rng = new Random(12345u);
float val = rng.NextFloat(0f, 1f);
```

---

## 编译产物

- **输出路径**: `$(SolutionDir)Bin`（与解决方案同级的 Bin 目录）
- **产物**: `ET.Mathematics.dll`
- 同时输出 Debug 符号文件（.pdb）

---

## 依赖关系

### 依赖的包

| 包 | 版本 | 用途 |
|----|------|------|
| `com.unity.mathematics` | 1.2.6 | Unity 官方数学库（UPM） |

### 被哪些包使用

- `cn.etetet.move` — 移动计算（位置、速度向量）
- `cn.etetet.aoi` — AOI 范围计算
- `cn.etetet.recast` — 寻路网格计算
- 所有需要跨端共享数学类型的 package 均可依赖

---

## 注意事项

1. **`DotNet~` 目录**：Unity 对 `~` 结尾目录不导入资产，所以服务端专用代码放在此目录，不影响 Unity 编译
2. **PackageCache 路径依赖**：服务端 `.csproj` 用通配符 `com.unity.mathematics*` 匹配 PackageCache，需确保 Unity 已缓存该包（即在 Unity Editor 中打开过项目）
3. **无业务逻辑**：此包零业务逻辑，仅做数学库的跨平台可用性保障
4. **unsafe 代码**：启用了 `AllowUnsafeBlocks`，Unity.Mathematics 内部 SIMD 实现依赖 unsafe 指针操作
5. **`Nullable=disable`**：项目未启用可空引用类型检查，与 ET 框架整体风格一致
