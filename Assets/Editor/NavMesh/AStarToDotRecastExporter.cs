using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using Pathfinding;
using Pathfinding.Graphs.Navmesh;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Path = System.IO.Path;
using File = System.IO.File;
using Directory = System.IO.Directory;
using FileStream = System.IO.FileStream;
using FileMode = System.IO.FileMode;
using FileInfo = System.IO.FileInfo;
using MemoryStream = System.IO.MemoryStream;
using BinaryWriter = System.IO.BinaryWriter;

namespace ET.Editor
{
    /// <summary>
    /// 将 A* Pathfinding Project 的 RecastGraph 扫描结果导出为 DotRecast 二进制 navmesh。
    ///
    /// 工作流：
    /// 1. 场景中配置 AstarPath + RecastGraph
    /// 2. 用 GraphUpdateScene 标记阻挡区域
    /// 3. A* Inspector 点 Scan（或本工具自动 Scan）
    /// 4. 菜单 ET/NavMesh/Export A* to DotRecast
    /// 5. 服务端 PathfindingComponent 用 Read32Bit 加载
    /// </summary>
    public static class AStarToDotRecastExporter
    {
        private const string OutputDir = "Packages/cn.etetet.statesync/Config/Recast";
        private const int MaxVertPerPoly = 6; // 与 DotRecast 加载时 Read32Bit(br, 6) 保持一致

        [MenuItem("ET/NavMesh/Export A* to DotRecast")]
        public static void Export()
        {
            var astar = AstarPath.active;
            if (astar == null)
            {
                EditorUtility.DisplayDialog("Export Failed", "场景中没有找到 AstarPath 组件", "OK");
                return;
            }

            var recastGraph = astar.data.recastGraph;
            if (recastGraph == null)
            {
                EditorUtility.DisplayDialog("Export Failed", "AstarPath 中没有找到 RecastGraph", "OK");
                return;
            }

            // 自动 Scan
            if (!recastGraph.isScanned)
            {
                Debug.Log("[NavMeshExport] Graph 未 Scan，正在自动 Scan...");
                AstarPath.active.Scan();
            }

            // 提取 tile 数据
            TileMeshes tileMeshes = recastGraph.ToTileMeshes();
            if (tileMeshes.tileMeshes == null || tileMeshes.tileMeshes.Length == 0)
            {
                EditorUtility.DisplayDialog("Export Failed", "Scan 结果为空，没有 navmesh 数据", "OK");
                return;
            }

            // 从 RecastGraph 读取配置
            float cs = recastGraph.cellSize;
            float ch = cs; // A* RecastGraph 使用相同的 cellSize 作为 cellHeight
            float walkableHeight = recastGraph.walkableHeight;
            float walkableRadius = recastGraph.characterRadius;
            float walkableClimb = recastGraph.walkableClimb;
            float tileWorldSizeX = recastGraph.TileWorldSizeX;
            float tileWorldSizeZ = recastGraph.TileWorldSizeZ;

            int tileRectWidth = tileMeshes.tileRect.Width;
            int tileRectHeight = tileMeshes.tileRect.Height;
            int totalTiles = tileRectWidth * tileRectHeight;

            // 计算全局 bounds（用于 DtNavMesh 初始化）
            float originX = tileMeshes.tileRect.xmin * tileWorldSizeX;
            float originZ = tileMeshes.tileRect.ymin * tileWorldSizeZ;

            // 创建 DtNavMeshParams
            var navMeshParams = new DtNavMeshParams();
            // 注意：X 取反以匹配现有 DotRecast 坐标系约定
            navMeshParams.orig = new RcVec3f(-originX - tileWorldSizeX * tileRectWidth, 0, originZ);
            navMeshParams.tileWidth = tileWorldSizeX;
            navMeshParams.tileHeight = tileWorldSizeZ;
            navMeshParams.maxTiles = totalTiles;
            navMeshParams.maxPolys = 1 << 14; // 16384 polys per tile 上限

            var navMesh = new DtNavMesh(navMeshParams, MaxVertPerPoly);

            int addedTiles = 0;

            for (int tz = 0; tz < tileRectHeight; tz++)
            {
                for (int tx = 0; tx < tileRectWidth; tx++)
                {
                    int idx = tx + tz * tileRectWidth;
                    var tile = tileMeshes.tileMeshes[idx];

                    if (tile.verticesInTileSpace == null || tile.verticesInTileSpace.Length == 0)
                        continue;
                    if (tile.triangles == null || tile.triangles.Length == 0)
                        continue;

                    int vertCount = tile.verticesInTileSpace.Length;
                    int triCount = tile.triangles.Length / 3;

                    // tile 在世界坐标中的偏移
                    float tileOffsetX = (tileMeshes.tileRect.xmin + tx) * tileWorldSizeX;
                    float tileOffsetZ = (tileMeshes.tileRect.ymin + tz) * tileWorldSizeZ;

                    // 计算 tile 的 bmin/bmax
                    float bminX = float.MaxValue, bminY = float.MaxValue, bminZ = float.MaxValue;
                    float bmaxX = float.MinValue, bmaxY = float.MinValue, bmaxZ = float.MinValue;

                    // 转换顶点：A* Int3 (tile space) → world float → 取反 X
                    var worldVerts = new float[vertCount * 3];
                    for (int i = 0; i < vertCount; i++)
                    {
                        Int3 v = tile.verticesInTileSpace[i];
                        float wx = -(v.x * Int3.PrecisionFactor + tileOffsetX); // X 取反
                        float wy = v.y * Int3.PrecisionFactor;
                        float wz = v.z * Int3.PrecisionFactor + tileOffsetZ;

                        worldVerts[i * 3 + 0] = wx;
                        worldVerts[i * 3 + 1] = wy;
                        worldVerts[i * 3 + 2] = wz;

                        if (wx < bminX) bminX = wx;
                        if (wy < bminY) bminY = wy;
                        if (wz < bminZ) bminZ = wz;
                        if (wx > bmaxX) bmaxX = wx;
                        if (wy > bmaxY) bmaxY = wy;
                        if (wz > bmaxZ) bmaxZ = wz;
                    }

                    var bmin = new RcVec3f(bminX, bminY, bminZ);
                    var bmax = new RcVec3f(bmaxX, bmaxY, bmaxZ);

                    // 量化顶点为 DtNavMeshCreateParams 格式 (int, 相对于 bmin, 除以 cs/ch)
                    var quantVerts = new int[vertCount * 3];
                    for (int i = 0; i < vertCount; i++)
                    {
                        quantVerts[i * 3 + 0] = (int)Mathf.Round((worldVerts[i * 3 + 0] - bminX) / cs);
                        quantVerts[i * 3 + 1] = (int)Mathf.Round((worldVerts[i * 3 + 1] - bminY) / ch);
                        quantVerts[i * 3 + 2] = (int)Mathf.Round((worldVerts[i * 3 + 2] - bminZ) / cs);
                    }

                    // 构建多边形数组：每个三角形 = 1 个 poly, nvp=MaxVertPerPoly
                    // polys 数组大小 = polyCount * 2 * nvp
                    // 前半：顶点索引（不足 nvp 的填 0xffff）
                    // 后半：邻接多边形（全填 0xffff 表示无邻接）
                    var polys = new int[triCount * 2 * MaxVertPerPoly];
                    var polyFlags = new int[triCount];
                    var polyAreas = new int[triCount];

                    for (int i = 0; i < triCount; i++)
                    {
                        int baseIdx = i * 2 * MaxVertPerPoly;

                        // 顶点索引（三角形有 3 个顶点）
                        polys[baseIdx + 0] = tile.triangles[i * 3 + 0];
                        polys[baseIdx + 1] = tile.triangles[i * 3 + 1];
                        polys[baseIdx + 2] = tile.triangles[i * 3 + 2];

                        // 填充剩余顶点位为 0xffff
                        for (int j = 3; j < MaxVertPerPoly; j++)
                            polys[baseIdx + j] = 0xffff;

                        // 邻接信息全部设为 0xffff（由 DtNavMeshBuilder 内部计算邻接）
                        for (int j = 0; j < MaxVertPerPoly; j++)
                            polys[baseIdx + MaxVertPerPoly + j] = 0xffff;

                        polyFlags[i] = 1; // 默认 flag = 1 (walkable)
                        polyAreas[i] = 0; // 默认 area = 0
                    }

                    // 构建 DtNavMeshCreateParams
                    var createParams = new DtNavMeshCreateParams
                    {
                        verts = quantVerts,
                        vertCount = vertCount,
                        polys = polys,
                        polyFlags = polyFlags,
                        polyAreas = polyAreas,
                        polyCount = triCount,
                        nvp = MaxVertPerPoly,
                        tileX = tileMeshes.tileRect.xmin + tx,
                        tileZ = tileMeshes.tileRect.ymin + tz,
                        tileLayer = 0,
                        bmin = bmin,
                        bmax = bmax,
                        walkableHeight = walkableHeight,
                        walkableRadius = walkableRadius,
                        walkableClimb = walkableClimb,
                        cs = cs,
                        ch = ch,
                        buildBvTree = true
                    };

                    DtMeshData meshData = DtNavMeshBuilder.CreateNavMeshData(createParams);
                    if (meshData == null)
                    {
                        Debug.LogWarning($"[NavMeshExport] Tile ({tx},{tz}) CreateNavMeshData 返回 null，跳过");
                        continue;
                    }

                    navMesh.AddTile(meshData, 0, 0);
                    addedTiles++;
                }
            }

            if (addedTiles == 0)
            {
                EditorUtility.DisplayDialog("Export Failed", "没有有效的 tile 数据可导出", "OK");
                return;
            }

            // 推断地图名：使用当前场景名
            string mapName = SceneManager.GetActiveScene().name;

            // 确保输出目录存在
            string fullOutputDir = Path.GetFullPath(OutputDir);
            if (!Directory.Exists(fullOutputDir))
                Directory.CreateDirectory(fullOutputDir);

            string outputPath = Path.Combine(fullOutputDir, mapName);

            // 写入 DotRecast 二进制格式
            using (var fs = new FileStream(outputPath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                var writer = new DtMeshSetWriter();
                writer.Write(bw, navMesh, RcByteOrder.LITTLE_ENDIAN, false); // RECAST4J 格式，maxVertPerPoly 写入文件头
            }

            // 创建 .meta 文件让 Unity 识别
            string metaPath = outputPath + ".meta";
            if (!File.Exists(metaPath))
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"[NavMeshExport] 导出成功！\n" +
                      $"  地图: {mapName}\n" +
                      $"  Tiles: {addedTiles}/{totalTiles}\n" +
                      $"  CellSize: {cs}, WalkableHeight: {walkableHeight}, WalkableRadius: {walkableRadius}\n" +
                      $"  文件: {outputPath}\n" +
                      $"  大小: {new FileInfo(outputPath).Length} bytes");

            EditorUtility.DisplayDialog("Export Success",
                $"NavMesh 导出成功！\n\n" +
                $"地图: {mapName}\n" +
                $"Tiles: {addedTiles}\n" +
                $"文件: {outputPath}",
                "OK");
        }
    }
}
