using System.Collections.Generic;
using Pathfinding;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace ET.Editor
{
    /// <summary>
    /// 将选中 GameObject 及其子节点中所有 ProBuilderMesh 合并为一个 Mesh，
    /// 导出为 .asset 文件，用作 A* NavMeshGraph 的 Source Mesh。
    /// </summary>
    public static class ProBuilderMeshExporter
    {
        [MenuItem("ET/NavMesh/Export ProBuilder Meshes")]
        public static void ExportSelected()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Export", "请先在 Hierarchy 中选中一个 GameObject", "OK");
                return;
            }

            var pbMeshes = selected.GetComponentsInChildren<ProBuilderMesh>(true);
            if (pbMeshes.Length == 0)
            {
                EditorUtility.DisplayDialog("Export",
                    $"'{selected.name}' 及其子节点中没有找到 ProBuilderMesh 组件", "OK");
                return;
            }

            // 用 CombineInstance 收集每个 mesh + 世界矩阵
            var combineList = new List<CombineInstance>();
            int meshCount = 0;

            foreach (var pb in pbMeshes)
            {
                var mf = pb.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    continue;

                // 先复制一份 mesh，避免多个 ProBuilder 共享同一 mesh 实例的问题
                var meshCopy = UnityEngine.Object.Instantiate(mf.sharedMesh);

                var ci = new CombineInstance
                {
                    mesh = meshCopy,
                    transform = mf.transform.localToWorldMatrix,
                    subMeshIndex = 0
                };
                combineList.Add(ci);

                meshCount++;
                Debug.Log($"[ProBuilderExport] 收集: {pb.gameObject.name} " +
                          $"(verts={meshCopy.vertexCount}, tris={meshCopy.triangles.Length / 3})");
            }

            if (meshCount == 0)
            {
                EditorUtility.DisplayDialog("Export", "没有有效的 ProBuilder mesh 可导出", "OK");
                return;
            }

            // 合并所有 mesh
            string sceneName = SceneManager.GetActiveScene().name;
            var combinedMesh = new Mesh();
            combinedMesh.name = sceneName + "_NavMesh";
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combineList.ToArray(), true, true);
            combinedMesh.RecalculateNormals();
            combinedMesh.RecalculateBounds();

            // 清理临时复制的 mesh
            foreach (var ci in combineList)
            {
                UnityEngine.Object.DestroyImmediate(ci.mesh);
            }

            Debug.Log($"[ProBuilderExport] 合并结果: verts={combinedMesh.vertexCount}, tris={combinedMesh.triangles.Length / 3}");

            // 确保输出目录存在
            string outputDir = "Assets/Resources/Scenes";
            if (!AssetDatabase.IsValidFolder(outputDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Scenes");
            }

            // 先保存统计信息（保存 asset 后 combinedMesh 引用可能失效）
            int totalVerts = combinedMesh.vertexCount;
            int totalTris = combinedMesh.triangles.Length / 3;

            // 保存为 .asset
            string assetPath = $"{outputDir}/{combinedMesh.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(combinedMesh, existing);
                UnityEngine.Object.DestroyImmediate(combinedMesh);
                Debug.Log($"[ProBuilderExport] 更新: {assetPath}");
            }
            else
            {
                AssetDatabase.CreateAsset(combinedMesh, assetPath);
                Debug.Log($"[ProBuilderExport] 创建: {assetPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 查找场景中的 AstarPath，设置 sourceMesh 并执行 Scan
            bool astarUpdated = false;
            var astarPath = UnityEngine.Object.FindFirstObjectByType<AstarPath>();
            if (astarPath != null)
            {
                var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (savedMesh != null && astarPath.data.navmesh != null)
                {
                    astarPath.data.navmesh.sourceMesh = savedMesh;
                    EditorUtility.SetDirty(astarPath);
                    astarPath.Scan();
                    astarUpdated = true;
                    Debug.Log($"[ProBuilderExport] A* NavmeshGraph.sourceMesh 已更新，Scan 完成");
                }
            }

            string astarInfo = astarUpdated
                ? "A* 已自动更新并 Scan"
                : "未找到 A* 或 NavmeshGraph，请手动设置";

            EditorUtility.DisplayDialog("Export",
                $"合并导出完成！\n\n" +
                $"根节点: {selected.name}\n" +
                $"ProBuilder 物体数: {meshCount}\n" +
                $"总顶点数: {totalVerts}\n" +
                $"总三角形数: {totalTris}\n" +
                $"文件: {assetPath}\n" +
                $"A*: {astarInfo}",
                "OK");
        }

        [MenuItem("ET/NavMesh/Top-Down Orthographic View")]
        public static void TopDownOrthoView()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return;

            sceneView.orthographic = true;
            sceneView.rotation = Quaternion.Euler(90f, 0f, 0f);
            sceneView.Repaint();
        }
    }
}
