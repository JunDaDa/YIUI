using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ET.Editor
{
    /// <summary>
    /// 将 SAM 3 分割出的物件图片批量导入场景。
    /// 读取输出目录中的 PNG + 同名 .txt（像素坐标），
    /// 按底图尺寸映射到世界坐标，放置到 Foreground/Decoration 层。
    /// </summary>
    public static class SegmentImporter
    {
        /// <summary>
        /// 从分割输出目录导入所有物件到当前场景。
        /// </summary>
        /// <param name="segmentOutputDir">分割脚本输出目录（包含 PNG + .txt）</param>
        /// <param name="targetAssetDir">Sprite 资源保存目录（Assets/ 下）</param>
        /// <param name="backgroundSprite">底图 Sprite（用于计算世界尺寸映射）</param>
        /// <param name="parentNode">场景中的父节点</param>
        /// <param name="sortingLayer">目标 Sorting Layer</param>
        public static int Import(
            string segmentOutputDir,
            string targetAssetDir,
            Sprite backgroundSprite,
            Transform parentNode,
            string sortingLayer = "Foreground")
        {
            if (!Directory.Exists(segmentOutputDir))
            {
                Debug.LogError($"[SegmentImporter] Directory not found: {segmentOutputDir}");
                return 0;
            }

            // 底图世界尺寸
            float bgWorldW = backgroundSprite.rect.width / backgroundSprite.pixelsPerUnit;
            float bgWorldH = backgroundSprite.rect.height / backgroundSprite.pixelsPerUnit;
            float bgPixelW = backgroundSprite.rect.width;
            float bgPixelH = backgroundSprite.rect.height;

            // 确保资源目录存在
            if (!AssetDatabase.IsValidFolder(targetAssetDir))
            {
                CreateFolderRecursive(targetAssetDir);
            }

            string[] pngFiles = Directory.GetFiles(segmentOutputDir, "*.png");
            int imported = 0;

            for (int i = 0; i < pngFiles.Length; i++)
            {
                string pngPath = pngFiles[i];
                string baseName = Path.GetFileNameWithoutExtension(pngPath);

                // 跳过预览图
                if (baseName.EndsWith("_preview")) continue;

                // 读取位置信息
                string infoPath = Path.Combine(segmentOutputDir, baseName + ".txt");
                if (!File.Exists(infoPath))
                {
                    Debug.LogWarning($"[SegmentImporter] No position info for: {baseName}");
                    continue;
                }

                SegmentInfo info = ParseInfo(infoPath);
                if (info == null) continue;

                // 复制 PNG 到 Assets
                string assetPath = $"{targetAssetDir}/{baseName}.png";
                string fullAssetPath = Path.GetFullPath(assetPath);
                File.Copy(pngPath, fullAssetPath, true);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                // 设置为 Sprite
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = backgroundSprite.pixelsPerUnit;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.SaveAndReimport();
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[SegmentImporter] Failed to load sprite: {assetPath}");
                    continue;
                }

                // 计算世界坐标
                // 像素坐标 (px, py) 是图片左上角原点
                // Unity XZ 平面：X 对应图片 X，Z 对应图片 Y（取反，因为 Z+ 是远离相机）
                // 物件中心 = 像素左上角 + 尺寸/2
                float centerPx = info.X + info.W * 0.5f;
                float centerPy = info.Y + info.H * 0.5f;

                // 映射到世界坐标（底图中心为原点）
                float worldX = (centerPx / bgPixelW - 0.5f) * bgWorldW;
                float worldZ = -(centerPy / bgPixelH - 0.5f) * bgWorldH; // Y 翻转

                // 创建 GameObject
                var go = new GameObject(baseName);
                go.transform.SetParent(parentNode, false);
                go.transform.position = new Vector3(worldX, 0f, worldZ);
                // 旋转使 Sprite 平铺在 XZ 平面上对着相机
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = sortingLayer;
                // Z 轴排序：Z 越小 = 越靠近相机 = sortingOrder 越大
                sr.sortingOrder = Mathf.RoundToInt(-worldZ * 100f);

                Undo.RegisterCreatedObjectUndo(go, $"Import Segment {baseName}");
                imported++;

                EditorUtility.DisplayProgressBar(
                    "Importing Segments",
                    $"{baseName} ({imported}/{pngFiles.Length})",
                    (float)i / pngFiles.Length);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            Debug.Log($"[SegmentImporter] Imported {imported} segments to {parentNode.name}");
            return imported;
        }

        private static SegmentInfo ParseInfo(string infoPath)
        {
            string line = File.ReadAllLines(infoPath)[0].Trim();
            // Format: x=100,y=200,w=300,h=400 or x=100,y=200,w=300,h=400,area=12345
            var info = new SegmentInfo();
            string[] parts = line.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string[] kv = parts[i].Split('=');
                if (kv.Length != 2) continue;

                string key = kv[0].Trim();
                if (!int.TryParse(kv[1].Trim(), out int val)) continue;

                switch (key)
                {
                    case "x": info.X = val; break;
                    case "y": info.Y = val; break;
                    case "w": info.W = val; break;
                    case "h": info.H = val; break;
                }
            }
            return info;
        }

        private static void CreateFolderRecursive(string path)
        {
            string[] parts = path.Split('/');
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

        private class SegmentInfo
        {
            public int X;
            public int Y;
            public int W;
            public int H;
        }
    }
}
