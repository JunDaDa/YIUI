#if YIUI
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor
{
    [InitializeOnLoad]
    public static class YIUILubanExcelToolBar
    {
        private static Texture _cachedOpenIcon;
        private static Texture _cachedExportIcon;

        static YIUILubanExcelToolBar()
        {
            YIUIToolbarExtender.AddLeftToolbarGUI(OnLubanExcelToolbarGUI, 1100);
        }

        private static void OnLubanExcelToolbarGUI()
        {
            _cachedOpenIcon ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/cn.etetet.yiuiluban/Editor/YIUIToolbar/Icon/luban_excel_open.png");

            GUILayout.Space(20);
            GUIContent iconContent = new(string.Empty, _cachedOpenIcon);
            iconContent.tooltip = "查看配置表";
            if (GUILayout.Button(iconContent))
            {
                EditorApplication.ExecuteMenuItem("ET/YIUI Luban 配置工具");
            }

            GUILayout.Space(5);

            _cachedExportIcon ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/cn.etetet.yiuiluban/Editor/YIUIToolbar/Icon/luban_excel_export.png");

            GUIContent iconContent2 = new(string.Empty, _cachedExportIcon);
            iconContent2.tooltip = "配置生成";
            if (GUILayout.Button(iconContent2))
            {
                EditorApplication.ExecuteMenuItem("ET/Excel/ExcelExporter");
            }

            GUILayout.Space(20);
        }
    }
}
#endif