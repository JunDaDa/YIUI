using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor
{
    [InitializeOnLoad]
    public static class YIUILocalizationExcelToolBar
    {
        private static Texture _cachedIcon;

        static YIUILocalizationExcelToolBar()
        {
            YIUIToolbarExtender.AddLeftToolbarGUI(OnLubanExcelToolbarGUI, 1000);
        }

        private static void OnLubanExcelToolbarGUI()
        {
            _cachedIcon ??= AssetDatabase.LoadAssetAtPath<Texture>("Packages/cn.etetet.yiuilocalizationpro/Editor/Toolbar/Icon/icon_localization_chn.png");
            GUILayout.Space(5);
            GUIContent iconContent = new(string.Empty, _cachedIcon);
            iconContent.tooltip = "多语言导出";
            if (GUILayout.Button(iconContent))
            {
                YIUILocalizationCreate.CreateI2LocalizationByXlsx();
            }

            GUILayout.Space(5);
        }
    }
}