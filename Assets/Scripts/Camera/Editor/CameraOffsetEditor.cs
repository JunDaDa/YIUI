using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraOffset))]
public class CameraOffsetEditor : Editor
{
    private static readonly string s_SavePath =
        Path.Combine("Assets", "Scripts", "Camera", "CameraOffset.json");

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        CameraOffset cam = (CameraOffset)target;

        if (GUILayout.Button("Save to JSON", GUILayout.Height(30)))
        {
            SaveToJson(cam);
        }

        if (GUILayout.Button("Load from JSON", GUILayout.Height(25)))
        {
            LoadFromJson(cam);
        }

        EditorGUILayout.HelpBox($"保存路径: {s_SavePath}", MessageType.Info);
    }

    private static void SaveToJson(CameraOffset cam)
    {
        var data = new CameraOffsetData
        {
            Offset = cam.Offset,
            LerpSpeed = cam.LerpSpeed,
            ZoomSpeed = cam.ZoomSpeed,
            MinZoom = cam.MinZoom,
            MaxZoom = cam.MaxZoom
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(s_SavePath, json);
        AssetDatabase.Refresh();
        Debug.Log($"[CameraOffset] Saved to {s_SavePath}:\n{json}");
    }

    private static void LoadFromJson(CameraOffset cam)
    {
        if (!File.Exists(s_SavePath))
        {
            Debug.LogWarning($"[CameraOffset] File not found: {s_SavePath}");
            return;
        }

        string json = File.ReadAllText(s_SavePath);
        var data = JsonUtility.FromJson<CameraOffsetData>(json);

        Undo.RecordObject(cam, "Load CameraOffset from JSON");
        cam.Offset = data.Offset;
        cam.LerpSpeed = data.LerpSpeed;
        cam.ZoomSpeed = data.ZoomSpeed;
        cam.MinZoom = data.MinZoom;
        cam.MaxZoom = data.MaxZoom;

        Debug.Log($"[CameraOffset] Loaded from {s_SavePath}");
    }

    [System.Serializable]
    private struct CameraOffsetData
    {
        public Vector3 Offset;
        public float LerpSpeed;
        public float ZoomSpeed;
        public float MinZoom;
        public float MaxZoom;
    }
}
