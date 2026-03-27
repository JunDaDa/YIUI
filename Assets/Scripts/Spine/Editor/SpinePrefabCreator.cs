using System.Collections.Generic;
using System.IO;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器工具：选中 Spine 文件的父级目录，自动在 Assets/Arts/Prefabs/Role/ 下创建同名预制体。
/// 支持多选目录，批量创建。
/// 菜单路径：Assets/Spine/从选中目录创建角色预制体
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

                // 3e. 保存为预制体
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
