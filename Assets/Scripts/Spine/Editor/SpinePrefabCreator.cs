using System.Collections.Generic;
using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 编辑器工具：选中 Spine 文件的父级目录，自动在 Assets/Arts/Prefabs/Role/ 下创建同名预制体。
/// 支持多选目录，批量创建。
/// 自动优化性能：关闭阴影、光照探针、反射探针、动态遮挡、运动向量等。
/// 菜单路径：Assets/CreateSpinePrefab
/// </summary>
public static class SpinePrefabCreator
{
    private const string k_OutputFolder = "Assets/Arts/Prefabs/Role";
    private const string k_SkeletonDataSuffix = "_SkeletonData.asset";
    private const string k_MenuPath = "Assets/CreateSpinePrefab";

    [MenuItem(k_MenuPath, priority = 100)]
    private static void CreatePrefabsFromSelection()
    {
        // 1. 收集选中的所有目录
        Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("错误", "请先在 Project 窗口中选中一个或多个 Spine 资源目录。", "确定");
            return;
        }

        List<string> folderPaths = new List<string>();
        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(path))
            {
                folderPaths.Add(path);
            }
        }

        if (folderPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "选中的对象中没有目录。请选中包含 Spine 文件的文件夹。", "确定");
            return;
        }

        // 2. 确保输出目录存在
        EnsureDirectoryExists(k_OutputFolder);

        // 3. 逐目录处理
        int successCount = 0;
        int skipCount = 0;
        List<string> errors = new List<string>();

        foreach (string folderPath in folderPaths)
        {
            string folderName = Path.GetFileName(folderPath);

            // 3a. 在目录中查找 *_SkeletonData.asset
            string[] guids = AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { folderPath });
            if (guids.Length == 0)
            {
                errors.Add($"[{folderName}] 目录下未找到 SkeletonDataAsset，已跳过。");
                continue;
            }

            if (guids.Length > 1)
            {
                errors.Add($"[{folderName}] 目录下找到多个 SkeletonDataAsset，无法确定使用哪个，已跳过。");
                continue;
            }

            string skeletonDataPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            SkeletonDataAsset skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(skeletonDataPath);
            if (skeletonDataAsset == null)
            {
                errors.Add($"[{folderName}] 加载 SkeletonDataAsset 失败: {skeletonDataPath}");
                continue;
            }

            // 3b. 验证 SkeletonData 是否可用
            if (skeletonDataAsset.GetSkeletonData(false) == null)
            {
                errors.Add($"[{folderName}] SkeletonDataAsset 数据无效（可能缺少 atlas 或 json/skel 文件）: {skeletonDataPath}");
                continue;
            }

            // 3c. 检查输出路径是否已存在
            string prefabPath = $"{k_OutputFolder}/{folderName}.prefab";
            if (File.Exists(prefabPath))
            {
                errors.Add($"[{folderName}] 预制体已存在: {prefabPath}，已跳过。如需覆盖请先手动删除。");
                skipCount++;
                continue;
            }

            // 3d. 创建 SkeletonAnimation GameObject
            GameObject go = new GameObject(folderName);
            try
            {
                SkeletonAnimation skeletonAnimation = SkeletonAnimation.AddToGameObject(go, skeletonDataAsset);
                skeletonAnimation.Initialize(false);

                if (skeletonAnimation.Skeleton == null)
                {
                    errors.Add($"[{folderName}] SkeletonAnimation 初始化失败，Skeleton 为 null。");
                    continue;
                }

                // 3e. 性能优化
                OptimizeRenderers(go);

                // 3f. 保存为预制体
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out bool savedSuccessfully);
                if (!savedSuccessfully || prefab == null)
                {
                    errors.Add($"[{folderName}] 保存预制体失败: {prefabPath}");
                    continue;
                }

                successCount++;
                Debug.Log($"[SpinePrefabCreator] 已创建预制体: {prefabPath}");
            }
            finally
            {
                // 清理临时 GameObject
                Object.DestroyImmediate(go);
            }
        }

        // 4. 汇总结果
        AssetDatabase.Refresh();

        string summary = $"处理完成：成功 {successCount} 个";
        if (skipCount > 0)
        {
            summary += $"，跳过 {skipCount} 个（已存在）";
        }

        if (errors.Count > 0)
        {
            summary += $"，错误 {errors.Count} 个：\n\n";
            foreach (string error in errors)
            {
                summary += error + "\n";
            }
        }

        if (errors.Count > 0)
        {
            EditorUtility.DisplayDialog("Spine 预制体创建结果", summary, "确定");
        }
        else if (successCount > 0)
        {
            EditorUtility.DisplayDialog("成功", summary, "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "没有需要创建的预制体。", "确定");
        }
    }

    /// <summary>
    /// 对已存在的 Role 预制体批量执行性能优化（不重新创建）。
    /// </summary>
    [MenuItem("ET/Tools/优化所有 Role 预制体", priority = 200)]
    private static void OptimizeAllRolePrefabs()
    {
        if (!AssetDatabase.IsValidFolder(k_OutputFolder))
        {
            EditorUtility.DisplayDialog("提示", $"目录不存在: {k_OutputFolder}", "确定");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { k_OutputFolder });
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // 打开预制体进行编辑
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject instance = PrefabUtility.LoadPrefabContents(assetPath);
            if (instance == null) continue;

            OptimizeRenderers(instance);
            PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            PrefabUtility.UnloadPrefabContents(instance);
            count++;
        }

        Debug.Log($"[SpinePrefabCreator] 已优化 {count} 个 Role 预制体");
        EditorUtility.DisplayDialog("完成", $"已优化 {count} 个 Role 预制体", "确定");
    }

    /// <summary>
    /// 对 GameObject 及其所有子物体的 Renderer 执行性能优化。
    /// 2D Spine 角色不需要 3D 渲染管线的大部分特性。
    /// </summary>
    private static void OptimizeRenderers(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            // --- 阴影 ---
            r.shadowCastingMode = ShadowCastingMode.Off;    // 不投射阴影
            r.receiveShadows = false;                        // 不接收阴影

            // --- 光照探针 ---
            r.lightProbeUsage = LightProbeUsage.Off;         // 2D 角色不需要光照探针

            // --- 反射探针 ---
            r.reflectionProbeUsage = ReflectionProbeUsage.Off; // 2D 角色不需要反射探针

            // --- 运动向量 ---
            r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion; // 不需要运动模糊

            // --- 动态遮挡剔除 ---
            r.allowOcclusionWhenDynamic = false;             // 2D 精灵不适合遮挡剔除

            // --- Sorting Layer ---
            r.sortingLayerName = "Character";                // 统一使用 Character 排序层
        }

        // --- SkeletonAnimation 优化 ---
        SkeletonAnimation[] skeletons = go.GetComponentsInChildren<SkeletonAnimation>(true);
        for (int i = 0; i < skeletons.Length; i++)
        {
            SkeletonAnimation sa = skeletons[i];

            // 关闭不需要的更新模式
            sa.updateWhenInvisible = UpdateMode.Nothing;     // 不可见时完全停止更新
        }

        Debug.Log($"[SpinePrefabCreator] 已优化 {renderers.Length} 个 Renderer, {skeletons.Length} 个 SkeletonAnimation");
    }

    [MenuItem(k_MenuPath, validate = true)]
    private static bool CreatePrefabsFromSelection_Validate()
    {
        // 至少选中了一个资源
        Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        return selectedObjects != null && selectedObjects.Length > 0;
    }

    private static void EnsureDirectoryExists(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        // 逐级创建目录
        string[] parts = assetPath.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
