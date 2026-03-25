# cn.etetet.recast

## 概述

DotRecast 是 Recast Navigation 库的 C# 移植版，原版由 Mikko Mononen 开发（C++），经过 recast4j（Java，Piotr Piastucki）再到 DotRecast（C#，Choi Ikpil ikpil@naver.com）。该包提供完整的导航网格生成和查询功能，是 MMOGame 寻路系统的底层基础，供服务端（通过 ET.Recast.csproj）使用。

**命名空间前缀：** `DotRecast.*`
**目标框架：** .NET 8.0
**许可：** Zlib License（开源）

---

## 目录结构

```
Runtime/
├── Core/                    # 基础工具库（数学、数据结构、线程安全、压缩）
├── Recast/                  # NavMesh 生成管线（体素化→过滤→分区→轮廓→多边形网格）
│   └── Geom/                # 输入几何体接口和实现
├── Detour/                  # NavMesh 运行时查询（A* 寻路、射线检测、离网连接）
│   └── Io/                  # NavMesh 序列化/反序列化
├── Detour.Crowd/            # 多 Agent 群体导航（局部避让、速度规划）
│   └── Tracking/            # 调试信息数据结构
├── Detour.Dynamic/          # 动态 NavMesh（运行时添加/移除碰撞体）
│   ├── Colliders/           # 各种碰撞体类型
│   └── Io/                  # 体素文件序列化
├── Detour.Extras/           # 扩展工具（跳跃链接构建、BVTree、OBJ 导出）
│   └── Jumplink/            # 跳跃链接相关类
└── Detour.TileCache/        # 瓦片缓存（压缩层缓存+动态障碍物）
DotNet~/
└── ET.Recast.csproj         # 服务端编译项目（引用 Runtime/**/*.cs）
```

---

## 核心类与接口

### Core 层

| 类/接口 | 说明 |
|--------|------|
| `RcVec3f` | 3D 浮点向量，所有空间计算基础类型 |
| `RcVec2f` | 2D 浮点向量 |
| `RcMatrix4X4` | 4×4 矩阵，用于空间变换 |
| `RcMath` | 静态数学工具（Clamp、Lerp、Vdot、Vcross 等） |
| `RcImmutableArray<T>` | 不可变数组，多文件分部实现（Minimal/Listable/Enumerable） |
| `RcSortedQueue<T>` | 优先队列，供 Detour 寻路使用 |
| `RcAtomicInteger/Long/Float/Boolean` | 无锁原子类型，用于多线程安全计数 |
| `RcByteBuffer` | 字节缓冲区，NavMesh 序列化/反序列化 |
| `RcByteOrder` | 字节序枚举（BIG_ENDIAN / LITTLE_ENDIAN） |
| `RcTelemetry` | 性能计时器，统计各阶段耗时 |
| `RcTelemetryTick` | 计时单位，记录每个标签的累计时间 |
| `RcTimerLabel` | 枚举：各阶段计时器标签（RC_TIMER_TOTAL 等） |
| `IRcCompressor` | 压缩接口 |
| `FastLZ` | FastLZ 压缩算法实现（TileCache 数据压缩） |
| `IRcRand` | 随机数接口（FindRandomPoint 等用） |
| `FRand` | 默认随机数实现 |
| `Intersections` | 几何相交测试工具 |
| `RcConvexUtils` | 凸包工具 |
| `RcArrayUtils` | 数组工具（Fill、Copy 等） |
| `RcHashCodes` | 哈希码工具 |
| `RcFrequency` | 频率计数工具 |
| `RcSegmentVert` | 线段顶点数据结构 |
| `RcEdge` | 边数据结构 |
| `CollectionExtensions` | 集合扩展方法 |
| `RcAnonymousDisposable` | 匿名 IDisposable，用于 RAII 模式 |

### Recast 层（NavMesh 生成）

| 类/接口 | 说明 |
|--------|------|
| `RcConfig` | NavMesh 生成配置（格子尺寸、Agent 参数、分区方式等，所有字段 readonly） |
| `RecastBuilder` | 核心构建器，驱动整个生成管线，支持多线程/单线程/异步瓦片构建 |
| `RecastBuilderConfig` | 单个瓦片的构建配置（含瓦片坐标 tileX/tileZ） |
| `RecastBuilderResult` | 构建结果容器（Heightfield、CompactHeightfield、ContourSet、PolyMesh、PolyMeshDetail） |
| `IRecastBuilderProgressListener` | 构建进度回调接口（可用于显示进度条） |
| `IInputGeomProvider` | 输入几何体接口，提供顶点/三角形数据和包围盒 |
| `SimpleInputGeomProvider` | 标准输入几何体实现 |
| `RcTriMesh` | 三角形网格数据（顶点+索引） |
| `RcChunkyTriMesh` | 空间分块三角形网格，加速几何查询（树形分割） |
| `RcChunkyTriMeshNode` | 分块节点（包围盒+三角形范围） |
| `RcOffMeshConnection` | 离网连接输入数据（起止点、半径、双向、Area/Flags） |
| `BoundsItem` | 包围盒项，用于排序（BoundsItemX/Y/ZComparer） |
| `RcHeightfield` | 稀疏高度场（体素化阶段产物，每列存储 RcSpan 链） |
| `RcSpan` | 高度场中的体素跨度（smin/smax/area） |
| `RcCompactHeightfield` | 紧凑高度场（过滤后可行走区域，存储 RcCompactCell+RcCompactSpan） |
| `RcCompactCell` | 紧凑高度场的格子（存储 span 起始索引+span 数量） |
| `RcCompactSpan` | 紧凑高度场中的 span（y/reg/con/h） |
| `RcHeightfieldLayer` | 高度场层（LAYERS 分区产物） |
| `RcHeightfieldLayerSet` | 高度场层集合 |
| `RcContourSet` | 轮廓集合（多边形轮廓提取结果） |
| `RcContour` | 单个轮廓（顶点列表+区域 ID） |
| `RcContourHole` | 轮廓孔洞 |
| `RcContourRegion` | 轮廓区域 |
| `RcPolyMesh` | 多边形网格（最终 NavMesh 数据基础） |
| `RcPolyMeshDetail` | 详细多边形网格（高度细节，可选） |
| `RcConvexVolume` | 凸包体积（用于 MarkConvexPolyArea，标记自定义区域） |
| `RcAreaModification` | 面积修改操作（masking + value） |
| `RcRegion` | 区域数据（分区阶段中间结果） |
| `RcLayerRegion` | 层区域数据 |
| `RcSweepSpan` | 扫描线 span（单调分区用） |
| `RcHeightPatch` | 高度补丁（详细网格构建用） |
| `RcPotentialDiagonal` | 潜在对角线（多边形三角化用） |
| `RcConstants` | 常量集合（RC_SPAN_MAX_HEIGHT 等） |
| `RcPartitionType` | 枚举：WATERSHED(0) / MONOTONE(1) / LAYERS(2) |
| `RecastVoxelization` | 步骤1：三角形→体素化（RasterizeTriangle 等） |
| `RecastRasterization` | 光栅化工具函数 |
| `RecastFilledVolumeRasterization` | 填充体素化（实心几何体用） |
| `RecastFilter` | 步骤2：过滤不可行走区域（低悬挂、台阶、低高度） |
| `RecastCompact` | 步骤3：紧凑高度场构建 |
| `RecastArea` | 面积侵蚀和标记（ErodeWalkableArea、MarkConvexPolyArea） |
| `RecastRegion` | 步骤4：分区（Watershed/Monotone/Layer） |
| `RecastContour` | 步骤5：轮廓提取 |
| `RecastMesh` | 步骤6：多边形网格构建 |
| `RecastMeshDetail` | 步骤7：详细网格构建 |
| `RecastCommon` | 公共工具函数（GetHeightFieldSpanCount 等） |
| `RecastLayers` | LAYERS 分区实现 |
| `ObjImporter` | OBJ 文件导入器 |
| `ObjImporterContext` | OBJ 导入中间状态 |
| `PolyUtils` | Recast 多边形工具函数（Recast 命名空间，区别于 Extras） |

### Detour 层（NavMesh 查询）

| 类/接口 | 说明 |
|--------|------|
| `DtNavMesh` | 导航网格容器，管理所有瓦片，支持动态增删 |
| `DtNavMeshQuery` | 寻路查询引擎：FindPath、FindStraightPath、Raycast、FindRandomPoint 等 |
| `DtNavMeshBuilder` | 从 RcPolyMesh 创建 DtNavMesh |
| `DtNavMeshCreateParams` | NavMesh 创建参数 |
| `DtNavMeshParams` | NavMesh 参数（原点、瓦片尺寸、最大瓦片/多边形数） |
| `DtMeshTile` | 单个导航网格瓦片 |
| `DtMeshData` | 瓦片数据（含 header/verts/polys/links 等） |
| `DtMeshHeader` | 瓦片头（魔数、版本、多边形/顶点数量等） |
| `DtPoly` | 多边形数据（顶点索引、标志、Area） |
| `DtPolyDetail` | 多边形细节（三角剖分，高度采样） |
| `DtPolyPoint` | 多边形上的点（polyRef + 位置） |
| `DtLink` | 多边形间连接 |
| `DtOffMeshConnection` | 离网连接（跳跃点、传送门） |
| `IDtQueryFilter` | 寻路过滤器接口（PassFilter + GetCost） |
| `DtQueryDefaultFilter` | 默认过滤器（includeFlags/excludeFlags + areaCost 数组，每个 Area 独立代价） |
| `DtQueryEmptyFilter` | 空过滤器（拒绝所有） |
| `DtQueryNoOpFilter` | 无操作过滤器（接受所有，代价=0） |
| `DtFindPathOption` | 寻路选项结构体（NoOption/AnyAngle/ZeroScale，包含 heuristic/options/raycastLimit） |
| `IQueryHeuristic` | A* 启发函数接口 |
| `DefaultQueryHeuristic` | 默认启发函数（欧几里得距离乘以 heuristicScale） |
| `DtNodePool` | A* 节点池，复用节点避免 GC |
| `DtNodeQueue` | A* 优先队列 |
| `DtNode` | A* 节点（pos/cost/total/pidx/flags/id） |
| `DtQueryData` | Sliced 寻路状态（分帧 A* 中间状态） |
| `DtStatus` | **readonly struct** 状态码，按位组合（SUCCESS/FAILURE/IN_PROGRESS + detail bits） |
| `DtRaycastHit` | 射线检测结果（t/normal/path） |
| `DtBVNode` | BVH 树节点，加速多边形查找 |
| `BVItem` | BVH 构建用中间项（BVItemX/Y/ZComparer 排序） |
| `IDtPolyQuery` | 多边形查询回调接口 |
| `IDtPolygonByCircleConstraint` | 圆形约束接口（约束随机点在圆内） |
| `DtNoOpDtPolygonByCircleConstraint` | 无约束实现（Shared 单例）— 圆心周围 |
| `DtStrictDtPolygonByCircleConstraint` | 严格约束实现（Shared 单例）— 严格在圆内 |
| `DetourBuilder` | 低层构建工具 |
| `DtConnectPoly` | 多边形连接辅助 |
| `ConvexConvexIntersection` | 凸体相交计算 |
| `Intersection` | 相交工具 |
| `InFlag` | 内部标志枚举 |
| `DtUtils` | Detour 数学工具函数（TriArea2D、RandomPointInConvexPoly 等） |
| `PathUtils` | 路径辅助工具（MergeCorridorStart/End/StartMoved/EndMoved） |
| `StraightPathItem` | 直线路径上的点（位置+标志+多边形 ref） |
| `DtSegInterval` | 线段区间 |
| `DtStraightPathOption` | 直线路径选项标志（AREA_CROSSINGS/ALL_CROSSINGS） |
| IO 类 | DtMeshDataReader/Writer、DtMeshSetReader/Writer、DtNavMeshParamReader/Writer 序列化 |

### Detour.Crowd 层

| 类/接口 | 说明 |
|--------|------|
| `DtCrowd` | 群体管理器，每帧更新所有 Agent 的位置和速度 |
| `DtCrowdAgent` | 单个 Agent 状态（位置、速度、路径走廊、邻居等） |
| `DtCrowdAgentParams` | Agent 配置（maxSpeed、maxAcceleration、radius、height、updateFlags、obstacleAvoidanceType） |
| `DtCrowdAgentState` | Agent 状态枚举（DT_CROWDAGENT_STATE_INVALID/WALKING/OFFMESH） |
| `DtCrowdAgentAnimation` | Agent 离网连接动画状态 |
| `DtMoveRequestState` | 移动请求状态枚举 |
| `DtCrowdConfig` | Crowd 全局配置（maxAgents/maxAgentRadius） |
| `DtCrowdNeighbour` | 邻居 Agent 数据（idx + dist） |
| `DtObstacleAvoidanceQuery` | 局部障碍物避让计算（ORCA 近似） |
| `DtObstacleAvoidanceParams` | 避让参数（velocityBias/weightDesVel/weightCurVel/weightSide/weightToi/horizTime/gridSize/adaptiveDivs 等） |
| `DtObstacleCircle` | 圆形障碍物（Agent） |
| `DtObstacleSegment` | 线段障碍物（边界） |
| `DtPathCorridor` | 路径走廊，维护当前路径段并支持局部更新 |
| `DtPathQuery` | 单次路径查询请求 |
| `DtPathQueryResult` | 路径查询结果 |
| `DtPathQueue` | 异步路径查找队列 |
| `DtSegment` | 线段（起点+终点） |
| `DtLocalBoundary` | 局部边界缓存，供避让使用 |
| `DtProximityGrid` | 邻近网格，快速查找周围 Agent |
| `DtCrowdTelemetry` | Crowd 性能统计 |
| `DtCrowdTimerLabel` | Crowd 计时器标签枚举 |
| `DtCrowdAgentDebugInfo` | Agent 调试信息（来自 Tracking 子目录） |
| `DtObstacleAvoidanceDebugData` | 障碍物避让调试数据 |

### Detour.Dynamic 层

| 类/接口 | 说明 |
|--------|------|
| `DynamicNavMesh` | 支持运行时动态修改的 NavMesh，基于 VoxelFile |
| `DynamicNavMeshConfig` | 动态 NavMesh 配置 |
| `DynamicTile` | 动态瓦片，包含碰撞体列表和重建逻辑 |
| `DynamicTileCheckpoint` | 瓦片检查点（快照/回滚用） |
| `ICollider` | 碰撞体接口（Bounds + Voxelize） |
| `AbstractCollider` | 碰撞体抽象基类 |
| `BoxCollider` | 盒型碰撞体 |
| `SphereCollider` | 球型碰撞体 |
| `CapsuleCollider` | 胶囊碰撞体 |
| `CylinderCollider` | 圆柱碰撞体 |
| `TrimeshCollider` | 三角形网格碰撞体 |
| `ConvexTrimeshCollider` | 凸三角形网格碰撞体 |
| `CompositeCollider` | 组合碰撞体（多个 ICollider 组合） |
| `VoxelFile` / `VoxelTile` | 体素文件格式（持久化动态 NavMesh） |
| `VoxelFileReader/Writer` | 体素文件序列化 |
| `VoxelQuery` | 体素空间查询 |
| `IUpdateQueueItem` | 更新队列项接口 |
| `AddColliderQueueItem` | 添加碰撞体队列项 |
| `RemoveColliderQueueItem` | 移除碰撞体队列项 |
| IO/`ByteUtils` | 字节工具（体素文件 IO 用） |

### Detour.Extras 层

| 类/接口 | 说明 |
|--------|------|
| `JumpLinkBuilder` | 跳跃链接构建器（分析 NavMesh 边缘生成跳跃/攀爬链接） |
| `JumpLinkBuilderConfig` | 跳跃链接配置 |
| `JumpLink` / `JumpEdge` / `JumpSegment` | 跳跃链接数据结构 |
| `JumpLinkType` | 跳跃类型枚举（JUMP / CLIMB） |
| `EdgeExtractor` | NavMesh 边缘提取器 |
| `EdgeSampler` / `EdgeSamplerFactory` | 边缘采样器 |
| `TrajectorySampler` | 轨迹采样（模拟跳跃弹道） |
| `JumpTrajectory` / `ClimbTrajectory` | 跳跃/攀爬轨迹（继承 Trajectory） |
| `Trajectory` | 轨迹基类 |
| `GroundSample` | 地面采样点 |
| `GroundSegment` | 地面线段 |
| `IGroundSampler` / `AbstractGroundSampler` | 地面采样接口和抽象基类 |
| `NavMeshGroundSampler` | 基于 NavMesh 的地面采样 |
| `PolyQueryInvoker` | 多边形查询触发器（JumpLink 用） |
| `BVTreeBuilder` | BVH 树构建工具 |
| `ObjExporter` | NavMesh 导出为 OBJ 格式 |
| `PolyUtils` | 多边形工具函数（Extras 命名空间） |

### Detour.TileCache 层

| 类/接口 | 说明 |
|--------|------|
| `DtTileCache` | 瓦片缓存，支持压缩层存储和运行时障碍物添加/移除 |
| `DtTileCacheBuilder` | 缓存层构建器 |
| `DtTileCacheLayer` | 单个缓存层数据 |
| `DtTileCacheObstacle` | 动态障碍物（Box/Cylinder/Oriented Box） |
| `TileCacheObstacleType` | 障碍物类型枚举 |
| `DtCompressedTile` | 压缩后的瓦片数据 |
| `IDtTileCacheMeshProcess` | 网格处理接口（添加离网连接等） |
| `DtTileCacheFastLzCompressor` | FastLZ 压缩器实现 |
| `DtTileCacheParams` / `DtTileCacheStorageParams` | 缓存参数 |
| IO 类 | DtTileCacheReader/Writer 序列化 |

---

## 实现原理

### NavMesh 生成管线（Recast 部分）

```
输入几何体 (IInputGeomProvider)
    ↓ Step 1: RecastVoxelization.BuildSolidHeightfield()
              RcChunkyTriMesh 加速三角形与格子的相交查询
RcHeightfield（稀疏高度场，每列 RcSpan 链）
    ↓ Step 2: RecastFilter（过滤不可行走区域）
              FilterLowHangingWalkableObstacles / FilterLedgeSpans / FilterWalkableLowHeightSpans
RcHeightfield（已过滤）
    ↓ Step 3: RecastCompact.BuildCompactHeightfield()
              RecastArea.ErodeWalkableArea()
              RecastArea.MarkConvexPolyArea() [可选，标记 RcConvexVolume]
RcCompactHeightfield（紧凑高度场，RcCompactCell + RcCompactSpan 数组）
    ↓ Step 4: RecastRegion（分区，三种方式选一）
              - WATERSHED: BuildDistanceField() + BuildRegions()
              - MONOTONE:  BuildRegionsMonotone()
              - LAYERS:    BuildLayerRegions() → RcHeightfieldLayerSet
RcCompactHeightfield（带区域 ID）
    ↓ Step 5: RecastContour.BuildContours() → RcContourSet
    ↓ Step 6: RecastMesh.BuildPolyMesh() → RcPolyMesh
    ↓ Step 7: RecastMeshDetail.BuildPolyMeshDetail() [可选] → RcPolyMeshDetail
    ↓ DtNavMeshBuilder → DtNavMesh（运行时可查询）
```

### 寻路流程（Detour 部分）

```
目标点
    ↓ DtNavMeshQuery.FindNearestPoly()       → 找起点/终点多边形
    ↓ DtNavMeshQuery.FindPath()              → A* 多边形路径（polygon refs）
    ↓ DtNavMeshQuery.FindStraightPath()      → 漏斗算法拉直路径点
    ↓ 可选: DtNavMeshQuery.Raycast()         → 视线检测
输出: StraightPathItem 列表（含位置+标志+polyRef）
```

**分帧寻路（Sliced FindPath）：** 避免单帧卡顿，适用于长距离路径
```
InitSlicedFindPath() → 初始化 A* 状态到 DtQueryData
UpdateSlicedFindPath(maxIter) → 每帧执行有限次迭代，返回 DT_IN_PROGRESS / DT_SUCCESS
FinalizeSlicedFindPath() → 提取最终路径
```

### 多 Agent 群体导航（Crowd 部分）

```
每帧 DtCrowd.Update(dt):
    1. CheckPathValidity       - 检查路径有效性（NavMesh 是否有变化）
    2. UpdateMoveRequest       - 处理移动请求（A* 异步，DtPathQueue）
    3. UpdateTopologyOptimization - 优化路径拓扑
    4. BuildProximityGrid      - 构建 DtProximityGrid（查找邻居 Agent）
    5. FindCorners             - 找下一个转角（DtPathCorridor.FindCorners）
    6. TriggerOffMeshConnections - 触发离网连接
    7. CalcSteering            - 计算方向（目标 + 分离力）
    8. DtObstacleAvoidanceQuery - 局部 ORCA 避让（可配置多个 DtObstacleAvoidanceParams）
    9. Integrate               - 速度积分，更新位置
   10. HandleCollisions        - Agent 间碰撞处理（基于分离力）
   11. MoveAgents              - 在 NavMesh 上约束位置（DtNavMeshQuery.MoveAlongSurface）
   12. UpdateOffMeshConnections - 更新离网连接动画（DtCrowdAgentAnimation）
```

---

## 关键流程图

### 动态 NavMesh 更新流程

```
AddCollider(collider)
    → currentColliderId++ → AddColliderQueueItem 入队

Update()/UpdateAsync()
    → 处理 updateQueue 中的 AddCollider/RemoveCollider 请求
    → 标记受影响 DynamicTile 为 dirty
    → 重新体素化 dirty 的瓦片（RecastBuilder）
    → 重建 DtNavMesh 瓦片

查询时使用最新 DtNavMesh
```

### TileCache 障碍物更新流程

```
AddObstacle() → 返回 ObstacleRef（long 类型句柄）
    → 标记相关瓦片为 dirty

Update()
    → 解压缩 dirty 瓦片的压缩层（FastLZ 解压）
    → 标记障碍物区域（对每个 DtTileCacheObstacle）
    → 重新构建多边形网格（DtTileCacheBuilder）
    → 更新 DtNavMesh 对应瓦片
```

---

## 代码示例

### 基础寻路

```csharp
// 1. 从 OBJ 构建 NavMesh
var geom = ObjImporter.Load("map.obj");
var cfg = new RcConfig(
    useTiles: true, tileSizeX: 48, tileSizeZ: 48,
    partition: RcPartitionType.WATERSHED.Value,
    cellSize: 0.3f, cellHeight: 0.2f,
    walkableSlopeAngle: 45f, walkableHeight: 2.0f,
    walkableClimb: 0.9f, walkableRadius: 0.6f,
    maxEdgeLen: 12, maxSimplificationError: 1.3f,
    minRegionArea: 8, mergeRegionArea: 20,
    maxVertsPerPoly: 6, detailSampleDist: 6f, detailSampleMaxError: 1f
);
var builder = new RecastBuilder();
var results = builder.BuildTiles(geom, cfg, new TaskFactory()); // 多线程
// 2. 创建 DtNavMesh（多瓦片）
var navMesh = new DtNavMesh(...);
foreach (var result in results) {
    DtNavMeshBuilder.CreateNavMeshData(CreateParams(result), out var meshData);
    navMesh.AddTile(meshData, 0, 0, out _);
}
// 3. 查询寻路
var query = new DtNavMeshQuery(navMesh);
var filter = new DtQueryDefaultFilter(
    includeFlags: 0xFFFF, excludeFlags: 0,
    areaCost: new float[64] // 每 area 代价
);
var halfExtents = new RcVec3f(2f, 4f, 2f);
query.FindNearestPoly(startPos, halfExtents, filter, out long startRef, out _, out _);
query.FindNearestPoly(endPos,   halfExtents, filter, out long endRef,   out _, out _);
var path = new List<long>();
query.FindPath(startRef, endRef, startPos, endPos, filter, ref path, DtFindPathOption.NoOption);
var straightPath = new List<StraightPathItem>();
query.FindStraightPath(startPos, endPos, path, ref straightPath, 256, 0);
// straightPath[i].pos 即路径点坐标
```

### DtStatus 的正确使用

```csharp
// DtStatus 是 readonly struct，通过按位 OR 组合状态
DtStatus status = query.FindPath(...);
if (status.Failed())        // 包含 DT_FAILURE 或 DT_INVALID_PARAM
    return;
if (status.IsPartial())     // DT_PARTIAL_RESULT：找到部分路径（未到终点）
    HandlePartialPath();
if (status.Succeeded())     // 包含 DT_SUCCSESS 或 DT_PARTIAL_RESULT
    UseResult();
// 组合状态码：
var err = DtStatus.DT_FAILURE | DtStatus.DT_INVALID_PARAM;
```

### DtQueryDefaultFilter 自定义代价

```csharp
// 地形类型：0=普通, 1=泥地(慢), 2=水路(禁止), 3=传送(快)
float[] areaCost = new float[64];
areaCost[0] = 1.0f;  // 普通
areaCost[1] = 3.0f;  // 泥地，代价3倍
areaCost[3] = 0.1f;  // 传送，代价0.1倍（快）
var filter = new DtQueryDefaultFilter(
    includeFlags: 0b0001 | 0b0010 | 0b1000, // 包含普通/泥地/传送，排除水路
    excludeFlags: 0b0100,
    areaCost: areaCost
);
```

### DtCrowd 群体导航

```csharp
var crowd = new DtCrowd(new DtCrowdConfig { maxAgents = 100, maxAgentRadius = 0.6f }, navMesh);
var ap = new DtCrowdAgentParams {
    radius = 0.6f, height = 2.0f,
    maxAcceleration = 8f, maxSpeed = 3.5f,
    collisionQueryRange = 12f,
    pathOptimizationRange = 30f,
    updateFlags = DtCrowdAgentParams.DT_CROWD_ANTICIPATE_TURNS
                | DtCrowdAgentParams.DT_CROWD_OBSTACLE_AVOIDANCE
                | DtCrowdAgentParams.DT_CROWD_OPTIMIZE_VIS
                | DtCrowdAgentParams.DT_CROWD_OPTIMIZE_TOPO,
    obstacleAvoidanceType = 3, // 使用第3套避让参数（高质量）
    separationWeight = 2.0f
};
int agentIdx = crowd.AddAgent(startPos, ap);
crowd.RequestMoveTarget(agentIdx, endPolyRef, endPos);
// 每帧
crowd.Update(deltaTime, null); // 第二参数为 debug info
var agent = crowd.GetAgent(agentIdx);
// agent.npos = 当前位置, agent.vel = 当前速度
```

---

## 关键常量

| 常量 | 值 | 说明 |
|------|-----|------|
| `DtNavMesh.DT_MAX_AREAS` | 64 | 最大区域类型数 |
| `DtNavMesh.DT_NULL_LINK` | 0xFFFFFFFF | 无效连接标记 |
| `DtNavMesh.DT_EXT_LINK` | 0x8000 | 外部连接标记 |
| `DtNavMesh.DT_POLY_BITS` | 20 | 多边形索引位数 |
| `DtNavMesh.DT_TILE_BITS` | 28 | 瓦片索引位数 |
| `DynamicNavMesh.MAX_VERTS_PER_POLY` | 6 | 每个多边形最大顶点数 |
| `DtNavMeshQuery.DT_FINDPATH_ANY_ANGLE` | 0x02 | 允许任意角度寻路（射线优化） |
| `DtNavMeshQuery.DT_STRAIGHTPATH_START` | 0x01 | 直线路径起点标志 |
| `DtNavMeshQuery.DT_STRAIGHTPATH_END` | 0x02 | 直线路径终点标志 |
| `DtNavMeshQuery.DT_STRAIGHTPATH_OFFMESH_CONNECTION` | 0x04 | 离网连接点标志 |
| `DtNavMeshQuery.DT_STRAIGHTPATH_AREA_CROSSINGS` | 0x01 | 每个 Area 变化点加顶点 |
| `DtNavMeshQuery.DT_STRAIGHTPATH_ALL_CROSSINGS` | 0x02 | 每个多边形边界加顶点 |
| `DtCrowdAgentParams.DT_CROWD_ANTICIPATE_TURNS` | 1 | 预判转向 |
| `DtCrowdAgentParams.DT_CROWD_OBSTACLE_AVOIDANCE` | 2 | 启用障碍物避让 |
| `DtCrowdAgentParams.DT_CROWD_SEPARATION` | 4 | 启用分离力 |
| `DtCrowdAgentParams.DT_CROWD_OPTIMIZE_VIS` | 8 | 可见性优化路径 |
| `DtCrowdAgentParams.DT_CROWD_OPTIMIZE_TOPO` | 16 | 拓扑优化路径 |

---

## 依赖关系

### 包内依赖

```
Core ← Recast ← Detour ← Detour.Crowd
                       ↑
              Detour.Dynamic
                       ↑
              Detour.Extras
                       ↑
              Detour.TileCache
```

### 与其他 Package 的关系

- **cn.etetet.move**：服务端移动系统，使用 `DtNavMeshQuery` 做路径规划，使用 `DtCrowd` 管理群体 Agent
- **cn.etetet.aoi**：AOI 系统可能配合 NavMesh 做视野/阻挡判断
- **cn.etetet.referencecollector**：场景几何体收集后传入 `IInputGeomProvider` 生成 NavMesh
- 该包本身不依赖项目中其他 package，是纯算法库

---

## 第一轮修正

1. **DtStatus 类型**：第一轮误描述为"状态码类/枚举"，实际是 `readonly struct`，通过 `|` 和 `&` 运算符组合，`Succeeded()` 方法同时接受 DT_SUCCSESS 和 DT_PARTIAL_RESULT（注意原始拼写 `DT_SUCCSESS` 不是 `DT_SUCCESS`）
2. **DtFindPathOption**：第一轮未记录此结构体，实际寻路时应传入此项而非裸整数 options
3. **RecastBuilder 异步支持**：除 `BuildTiles` 外还有 `BuildTilesAsync`（带 CancellationToken）
4. **Sliced FindPath**：第一轮未提，实际服务端可用此分帧机制避免单帧过长
5. **DtQueryDefaultFilter 构造**：支持三参数构造（includeFlags + excludeFlags + areaCost 数组），每个 Area 可设独立代价

---

## 性能考量

1. **多线程瓦片构建**：`RecastBuilder.BuildTiles()` 支持 `TaskFactory` 并行构建多个瓦片，大地图必用
2. **节点池复用**：`DtNodePool` 避免 A* 过程中频繁 GC，寻路高频调用时关键
3. **Crowd 限制**：官方建议单 Crowd 最多 20-30 个 Agent（每帧 0.5ms），超过需要分群
4. **TileCache vs DynamicNavMesh**：
   - TileCache：预先生成压缩层，运行时仅处理障碍物（快）
   - DynamicNavMesh：运行时完整重建受影响瓦片（慢但灵活）
5. **BVH 树**：DtNavMesh 内部使用 BVH 树加速多边形查找，大地图查询性能保障
6. **AllowUnsafeBlocks**：编译配置开启不安全代码，部分热路径使用 unsafe 指针操作
7. **RcChunkyTriMesh**：输入几何体预处理时构建，空间分块加速射线-三角形检测，大地图必备
8. **分帧寻路**：`InitSlicedFindPath/UpdateSlicedFindPath` 可将 A* 分散到多帧，避免服务端卡帧
