using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ET.Editor
{
    /// <summary>
    /// 2D 场景编辑器：大底图 + 装饰物件摆放。
    /// 菜单：ET/Map Editor
    /// </summary>
    public class MapEditorWindow : EditorWindow
    {
        private enum EditMode
        {
            Select,
            PlaceDecoration,
            DrawCollider,
        }

        // --- 编辑状态 ---
        private EditMode m_EditMode = EditMode.Select;
        private Sprite m_SelectedDecoration;
        private Vector2 m_ScrollPos;
        private List<Sprite> m_DecorationSprites = new List<Sprite>();
        private string m_DecorationFolder = "Assets/Arts/Map/Decorations";
        private string m_BackgroundFolder = "Assets/Arts/Map/Backgrounds";

        // --- 碰撞绘制 ---
        private List<Vector3> m_ColliderPoints = new List<Vector3>();
        private bool m_IsDrawingCollider;

        // --- 分割导入 ---
        private string m_SegmentOutputDir = "";
        private string m_SegmentAssetDir = "Assets/Arts/Map/Segments";
        private string m_SegmentSortingLayer = "Foreground";

        // --- 场景引用 ---
        private Transform m_MapRoot;
        private Transform m_BackgroundNode;
        private Transform m_DecorationsNode;
        private Transform m_ForegroundNode;
        private Transform m_CollidersNode;
        private Transform m_SpawnPointsNode;

        [MenuItem("ET/Map Editor")]
        private static void ShowWindow()
        {
            var window = GetWindow<MapEditorWindow>("Map Editor");
            window.minSize = new Vector2(300f, 400f);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshDecorationList();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            DrawCreateMapSection();
            EditorGUILayout.Space(8);
            DrawBackgroundSection();
            EditorGUILayout.Space(8);
            DrawEditModeSection();
            EditorGUILayout.Space(8);

            if (m_EditMode == EditMode.PlaceDecoration)
            {
                DrawDecorationPalette();
            }
            else if (m_EditMode == EditMode.DrawCollider)
            {
                DrawColliderSection();
            }

            EditorGUILayout.Space(8);
            DrawSegmentImportSection();
        }

        // ============================================================
        // 创建新地图场景
        // ============================================================
        private void DrawCreateMapSection()
        {
            EditorGUILayout.LabelField("场景管理", EditorStyles.boldLabel);

            if (GUILayout.Button("创建新地图场景", GUILayout.Height(28)))
            {
                CreateNewMapScene();
            }

            if (GUILayout.Button("初始化当前场景层级", GUILayout.Height(24)))
            {
                EnsureMapHierarchy();
            }
        }

        private void CreateNewMapScene()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "新建地图场景", "NewMap", "unity", "选择保存位置");
            if (string.IsNullOrEmpty(path)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 创建 Directional Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 创建 Main Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0f, 7f, -6f);
            cam.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            camGo.AddComponent<CameraFollow>();

            // 创建地图层级结构
            EnsureMapHierarchy();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MapEditor] 创建地图场景: {path}");
        }

        private void EnsureMapHierarchy()
        {
            m_MapRoot = FindOrCreate("MapRoot", null);
            m_BackgroundNode = FindOrCreate("Background", m_MapRoot);
            m_DecorationsNode = FindOrCreate("Decorations", m_MapRoot);
            m_ForegroundNode = FindOrCreate("Foreground", m_MapRoot);
            m_CollidersNode = FindOrCreate("Colliders", m_MapRoot);
            m_SpawnPointsNode = FindOrCreate("SpawnPoints", m_MapRoot);
        }

        private static Transform FindOrCreate(string name, Transform parent)
        {
            Transform found = null;
            if (parent != null)
            {
                found = parent.Find(name);
            }
            else
            {
                var go = GameObject.Find(name);
                if (go != null) found = go.transform;
            }

            if (found != null) return found;

            var newGo = new GameObject(name);
            if (parent != null)
            {
                newGo.transform.SetParent(parent, false);
            }

            Undo.RegisterCreatedObjectUndo(newGo, $"Create {name}");
            return newGo.transform;
        }

        // ============================================================
        // 底图设置
        // ============================================================
        private void DrawBackgroundSection()
        {
            EditorGUILayout.LabelField("底图", EditorStyles.boldLabel);

            m_BackgroundFolder = EditorGUILayout.TextField("底图文件夹", m_BackgroundFolder);

            var newBg = (Sprite)EditorGUILayout.ObjectField(
                "拖入底图 Sprite", GetCurrentBackground(), typeof(Sprite), false);

            if (newBg != null && newBg != GetCurrentBackground())
            {
                SetBackground(newBg);
            }
        }

        private Sprite GetCurrentBackground()
        {
            if (m_BackgroundNode == null) EnsureMapHierarchy();
            if (m_BackgroundNode == null) return null;

            var sr = m_BackgroundNode.GetComponent<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }

        private void SetBackground(Sprite sprite)
        {
            EnsureMapHierarchy();

            var sr = m_BackgroundNode.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = m_BackgroundNode.gameObject.AddComponent<SpriteRenderer>();
            }

            Undo.RecordObject(sr, "Set Background");
            sr.sprite = sprite;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = 0;

            // 底图放在 XZ 平面上，旋转使其平铺在地面
            Undo.RecordObject(m_BackgroundNode, "Set Background Transform");
            m_BackgroundNode.rotation = Quaternion.Euler(90f, 0f, 0f);
            m_BackgroundNode.position = Vector3.zero;

            Debug.Log($"[MapEditor] 设置底图: {sprite.name}");
        }

        // ============================================================
        // 编辑模式切换
        // ============================================================
        private void DrawEditModeSection()
        {
            EditorGUILayout.LabelField("编辑模式", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            DrawModeButton("选择", EditMode.Select);
            DrawModeButton("放置装饰", EditMode.PlaceDecoration);
            DrawModeButton("绘制碰撞", EditMode.DrawCollider);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawModeButton(string label, EditMode mode)
        {
            bool active = m_EditMode == mode;
            GUI.backgroundColor = active ? Color.cyan : Color.white;
            if (GUILayout.Button(label, GUILayout.Height(24)))
            {
                m_EditMode = mode;
                if (mode == EditMode.PlaceDecoration)
                {
                    RefreshDecorationList();
                }
            }
            GUI.backgroundColor = Color.white;
        }

        // ============================================================
        // 装饰物件面板
        // ============================================================
        private void DrawDecorationPalette()
        {
            EditorGUILayout.LabelField("装饰物件", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            m_DecorationFolder = EditorGUILayout.TextField("素材文件夹", m_DecorationFolder);
            if (GUILayout.Button("刷新", GUILayout.Width(50)))
            {
                RefreshDecorationList();
            }
            EditorGUILayout.EndHorizontal();

            if (m_DecorationSprites.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    $"在 {m_DecorationFolder} 放入 Sprite 素材后点击刷新", MessageType.Info);
                return;
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            int columns = Mathf.Max(1, (int)(position.width / 80f));
            int col = 0;
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < m_DecorationSprites.Count; i++)
            {
                var sprite = m_DecorationSprites[i];
                if (sprite == null) continue;

                bool selected = m_SelectedDecoration == sprite;
                GUI.backgroundColor = selected ? Color.yellow : Color.white;

                if (GUILayout.Button(
                    AssetPreview.GetAssetPreview(sprite) ?? Texture2D.whiteTexture,
                    GUILayout.Width(64), GUILayout.Height(64)))
                {
                    m_SelectedDecoration = sprite;
                }

                GUI.backgroundColor = Color.white;
                col++;
                if (col >= columns)
                {
                    col = 0;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            if (m_SelectedDecoration != null)
            {
                EditorGUILayout.HelpBox(
                    $"已选: {m_SelectedDecoration.name}\n在 Scene View 中点击放置", MessageType.None);
            }
        }

        private void RefreshDecorationList()
        {
            m_DecorationSprites.Clear();

            if (!AssetDatabase.IsValidFolder(m_DecorationFolder)) return;

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { m_DecorationFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    m_DecorationSprites.Add(sprite);
                }
            }
        }

        // ============================================================
        // 碰撞绘制面板
        // ============================================================
        private void DrawColliderSection()
        {
            EditorGUILayout.LabelField("碰撞区域", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "在 Scene View 中左键点击添加顶点，按 Enter 完成多边形", MessageType.Info);

            if (m_ColliderPoints.Count > 0)
            {
                EditorGUILayout.LabelField($"当前顶点数: {m_ColliderPoints.Count}");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("完成多边形"))
            {
                FinishColliderPolygon();
            }
            if (GUILayout.Button("清除当前"))
            {
                m_ColliderPoints.Clear();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void FinishColliderPolygon()
        {
            if (m_ColliderPoints.Count < 3)
            {
                Debug.LogWarning("[MapEditor] 至少需要 3 个顶点");
                return;
            }

            EnsureMapHierarchy();

            int idx = m_CollidersNode.childCount;
            var go = new GameObject($"Collider_{idx:D2}");
            go.transform.SetParent(m_CollidersNode, false);
            go.layer = LayerMask.NameToLayer("Map");

            // 使用 3D BoxCollider/MeshCollider 保持与现有移动系统兼容
            // 每两个相邻点生成一段墙壁 BoxCollider
            for (int i = 0; i < m_ColliderPoints.Count; i++)
            {
                int next = (i + 1) % m_ColliderPoints.Count;
                Vector3 a = m_ColliderPoints[i];
                Vector3 b = m_ColliderPoints[next];

                var wallGo = new GameObject($"Wall_{i:D2}");
                wallGo.transform.SetParent(go.transform, false);
                wallGo.layer = LayerMask.NameToLayer("Map");

                var col = wallGo.AddComponent<BoxCollider>();
                Vector3 center = (a + b) * 0.5f;
                float length = Vector3.Distance(a, b);

                wallGo.transform.position = center;
                wallGo.transform.LookAt(b);
                col.size = new Vector3(0.2f, 2f, length);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create Collider Polygon");
            m_ColliderPoints.Clear();
            SceneView.RepaintAll();
            Debug.Log($"[MapEditor] 创建碰撞多边形: {go.name}");
        }

        // ============================================================
        // SAM 分割导入
        // ============================================================
        private void DrawSegmentImportSection()
        {
            EditorGUILayout.LabelField("SAM 分割导入", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            m_SegmentOutputDir = EditorGUILayout.TextField("分割输出目录", m_SegmentOutputDir);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string dir = EditorUtility.OpenFolderPanel("选择分割输出目录", m_SegmentOutputDir, "");
                if (!string.IsNullOrEmpty(dir))
                {
                    m_SegmentOutputDir = dir;
                }
            }
            EditorGUILayout.EndHorizontal();

            m_SegmentAssetDir = EditorGUILayout.TextField("资源保存目录", m_SegmentAssetDir);

            string[] layerOptions = { "Foreground", "Decoration" };
            int layerIdx = System.Array.IndexOf(layerOptions, m_SegmentSortingLayer);
            if (layerIdx < 0) layerIdx = 0;
            layerIdx = EditorGUILayout.Popup("排序层", layerIdx, layerOptions);
            m_SegmentSortingLayer = layerOptions[layerIdx];

            Sprite bgSprite = GetCurrentBackground();
            bool canImport = !string.IsNullOrEmpty(m_SegmentOutputDir)
                && System.IO.Directory.Exists(m_SegmentOutputDir)
                && bgSprite != null;

            if (bgSprite == null)
            {
                EditorGUILayout.HelpBox("请先设置底图 Sprite，用于计算世界坐标映射", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!canImport);
            if (GUILayout.Button("导入分割物件", GUILayout.Height(28)))
            {
                EnsureMapHierarchy();
                Transform targetNode = m_SegmentSortingLayer == "Foreground"
                    ? m_ForegroundNode
                    : m_DecorationsNode;

                int count = SegmentImporter.Import(
                    m_SegmentOutputDir,
                    m_SegmentAssetDir,
                    bgSprite,
                    targetNode,
                    m_SegmentSortingLayer);

                EditorUtility.DisplayDialog("导入完成", $"成功导入 {count} 个分割物件", "OK");
            }
            EditorGUI.EndDisabledGroup();
        }

        // ============================================================
        // Scene View 交互
        // ============================================================
        private void OnSceneGUI(SceneView sceneView)
        {
            if (m_EditMode == EditMode.Select) return;

            Event e = Event.current;

            if (m_EditMode == EditMode.PlaceDecoration)
            {
                HandleDecorationPlacement(e);
            }
            else if (m_EditMode == EditMode.DrawCollider)
            {
                HandleColliderDrawing(e, sceneView);
            }
        }

        private void HandleDecorationPlacement(Event e)
        {
            if (m_SelectedDecoration == null) return;
            if (e.type != EventType.MouseDown || e.button != 0) return;
            if (e.alt) return; // Alt+Click = orbit camera

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            // XZ 平面射线检测 (y=0)
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float distance)) return;

            Vector3 worldPos = ray.GetPoint(distance);
            worldPos.y = 0f;

            EnsureMapHierarchy();

            var go = new GameObject(m_SelectedDecoration.name);
            go.transform.SetParent(m_DecorationsNode, false);
            go.transform.position = worldPos;
            // 旋转使 Sprite 面向相机（XZ 地面上立起来倾斜）
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = m_SelectedDecoration;
            sr.sortingLayerName = "Decoration";
            // Z 轴排序：Z 越小（靠近相机）sortingOrder 越大
            sr.sortingOrder = Mathf.RoundToInt(-worldPos.z * 100f);

            Undo.RegisterCreatedObjectUndo(go, "Place Decoration");
            e.Use();
        }

        private void HandleColliderDrawing(Event e, SceneView sceneView)
        {
            // 绘制已有顶点
            Handles.color = Color.red;
            for (int i = 0; i < m_ColliderPoints.Count; i++)
            {
                Handles.SphereHandleCap(0, m_ColliderPoints[i], Quaternion.identity, 0.3f, EventType.Repaint);
                if (i > 0)
                {
                    Handles.DrawLine(m_ColliderPoints[i - 1], m_ColliderPoints[i]);
                }
            }

            // 闭合预览线
            if (m_ColliderPoints.Count > 2)
            {
                Handles.color = new Color(1f, 0f, 0f, 0.3f);
                Handles.DrawLine(m_ColliderPoints[m_ColliderPoints.Count - 1], m_ColliderPoints[0]);
            }

            // Enter 完成
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
            {
                FinishColliderPolygon();
                e.Use();
                return;
            }

            // 左键添加顶点
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane ground = new Plane(Vector3.up, Vector3.zero);
                if (ground.Raycast(ray, out float distance))
                {
                    Vector3 worldPos = ray.GetPoint(distance);
                    worldPos.y = 0f;
                    m_ColliderPoints.Add(worldPos);
                    sceneView.Repaint();
                }
                e.Use();
            }

            // 阻止默认点击行为
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
        }
    }
}
