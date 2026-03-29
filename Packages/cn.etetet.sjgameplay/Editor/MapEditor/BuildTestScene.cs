using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ET.Editor
{
    /// <summary>
    /// 一键构建测试地图场景：底图 + SAM 分割物件自动摆放。
    /// </summary>
    public static class BuildTestScene
    {
        private const string BgAssetPath = "Assets/Arts/Map/Bgs/XinShouCun.png";
        private const string SegmentDir = "Assets/Arts/Map/Segments";
        private const string SceneSavePath = "Assets/Scenes/XinShouCun.unity";
        private const float PixelsPerUnit = 100f;

        [MenuItem("ET/Build Test Map Scene")]
        private static void Execute()
        {
            // 确保底图是 Sprite
            SetupSpriteImporter(BgAssetPath);

            // 设置所有 Segment 为 Sprite
            string fullSegDir = Path.GetFullPath(SegmentDir).Replace('\\', '/');
            string[] segPngs = Directory.GetFiles(fullSegDir, "*.png");
            for (int i = 0; i < segPngs.Length; i++)
            {
                string assetPath = SegmentDir + "/" + Path.GetFileName(segPngs[i]);
                SetupSpriteImporter(assetPath);
            }

            AssetDatabase.Refresh();

            // 加载底图 Sprite 获取尺寸
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgAssetPath);
            if (bgSprite == null)
            {
                Debug.LogError($"[BuildTestScene] Cannot load background: {BgAssetPath}");
                return;
            }

            float bgPixelW = bgSprite.rect.width;
            float bgPixelH = bgSprite.rect.height;
            float bgWorldW = bgPixelW / PixelsPerUnit;
            float bgWorldH = bgPixelH / PixelsPerUnit;
            Debug.Log($"[BuildTestScene] BG: {bgPixelW}x{bgPixelH}px = {bgWorldW}x{bgWorldH} world units");

            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Directional Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Main Camera (正交)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.3f, 0.2f, 1f);
            // 斜向下看 XZ 平面
            camGo.transform.position = new Vector3(0f, 7f, -6f);
            camGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            camGo.AddComponent<CameraFollow>();

            // 层级结构
            var mapRoot = new GameObject("MapRoot");
            var bgNode = new GameObject("Background");
            bgNode.transform.SetParent(mapRoot.transform, false);
            var decoNode = new GameObject("Decorations");
            decoNode.transform.SetParent(mapRoot.transform, false);
            var fgNode = new GameObject("Foreground");
            fgNode.transform.SetParent(mapRoot.transform, false);
            var colNode = new GameObject("Colliders");
            colNode.transform.SetParent(mapRoot.transform, false);
            var spawnNode = new GameObject("SpawnPoints");
            spawnNode.transform.SetParent(mapRoot.transform, false);

            // 铺底图：旋转 90 度贴在 XZ 平面
            var bgSr = bgNode.AddComponent<SpriteRenderer>();
            bgSr.sprite = bgSprite;
            bgSr.sortingLayerName = "Background";
            bgSr.sortingOrder = 0;
            bgNode.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            bgNode.transform.position = Vector3.zero;

            // 放置分割物件
            int placed = 0;
            for (int i = 0; i < segPngs.Length; i++)
            {
                string pngFile = segPngs[i];
                string baseName = Path.GetFileNameWithoutExtension(pngFile);
                string infoFile = Path.Combine(fullSegDir, baseName + ".txt");
                if (!File.Exists(infoFile)) continue;

                // 解析坐标
                string line = File.ReadAllLines(infoFile)[0].Trim();
                int px = 0, py = 0, pw = 0, ph = 0;
                string[] parts = line.Split(',');
                for (int j = 0; j < parts.Length; j++)
                {
                    string[] kv = parts[j].Split('=');
                    if (kv.Length != 2) continue;
                    int.TryParse(kv[1], out int val);
                    switch (kv[0].Trim())
                    {
                        case "x": px = val; break;
                        case "y": py = val; break;
                        case "w": pw = val; break;
                        case "h": ph = val; break;
                    }
                }

                // 加载 Sprite
                string spriteAssetPath = SegmentDir + "/" + baseName + ".png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spriteAssetPath);
                if (sprite == null) continue;

                // 像素中心 → 世界坐标
                // 底图中心为世界原点，像素原点在左上角
                float centerPx = px + pw * 0.5f;
                float centerPy = py + ph * 0.5f;
                float worldX = (centerPx / bgPixelW - 0.5f) * bgWorldW;
                float worldZ = -(centerPy / bgPixelH - 0.5f) * bgWorldH;

                var go = new GameObject(baseName);
                go.transform.SetParent(fgNode.transform, false);
                go.transform.position = new Vector3(worldX, 0.01f, worldZ);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = "Foreground";
                sr.sortingOrder = Mathf.RoundToInt(-worldZ * 100f);

                placed++;
            }

            // 创建出生点
            var spawn = new GameObject("SpawnPoint_0");
            spawn.transform.SetParent(spawnNode.transform, false);
            spawn.transform.position = Vector3.zero;

            // 保存场景
            string sceneDir = Path.GetDirectoryName(SceneSavePath);
            if (!AssetDatabase.IsValidFolder(sceneDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            EditorSceneManager.SaveScene(scene, SceneSavePath);

            Debug.Log($"[BuildTestScene] Done! Placed {placed} segments. Scene: {SceneSavePath}");
            EditorUtility.DisplayDialog("Build Test Scene",
                $"场景构建完成！\n底图: {bgPixelW}x{bgPixelH}\n物件: {placed} 个\n保存: {SceneSavePath}", "OK");
        }

        private static void SetupSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (importer.spritePixelsPerUnit != PixelsPerUnit)
            {
                importer.spritePixelsPerUnit = PixelsPerUnit;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            // 确保大图不被压缩得太小
            var settings = importer.GetDefaultPlatformTextureSettings();
            if (settings.maxTextureSize < 4096)
            {
                settings.maxTextureSize = 4096;
                importer.SetPlatformTextureSettings(settings);
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
