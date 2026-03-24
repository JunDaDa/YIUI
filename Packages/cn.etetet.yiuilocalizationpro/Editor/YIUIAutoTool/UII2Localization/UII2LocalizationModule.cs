#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace YIUIFramework.Editor
{
    [YIUIAutoMenu("多语言", 100100)]
    public partial class UII2LocalizationModule : BaseYIUIToolModule
    {
        [Button("文档", 30, Icon = SdfIconType.Link45deg, IconAlignment = IconAlignment.LeftOfText)]
        [PropertyOrder(-99999)]
        public void OpenDocument()
        {
            Application.OpenURL("https://lib9kmxvq7k.feishu.cn/wiki/ZOKxwi5XsijdX8kPU9McSxs1nxd");
        }

        [LabelText("全数据名称")]
        [ShowInInspector]
        [ReadOnly]
        public const string UII2SourceResName = "AllSource";

        [LabelText("全数据保存路径")]
        [FolderPath]
        [ShowInInspector]
        [ReadOnly]
        public const string UII2SourceResPath = "Packages/cn.etetet.yiuilocalizationpro/Assets/Editor/I2Localization"; //这是编辑器下的数据 平台运行时 是不需要的

        [LabelText("指定数据保存路径")]
        [FolderPath]
        [ShowInInspector]
        [ReadOnly]
        public const string UII2TargetLanguageResPath = "Packages/cn.etetet.yiuilocalizationpro/Assets/GameRes/I2Localization"; //运行时的资源是拆分的 根据需求加载

        [Button("打开多语言数据", 20)]
        [GUIColor(0.4f, 0.8f, 1)]
        private void OpenI2Languages()
        {
            EditorApplication.ExecuteMenuItem("Tools/I2 Localization/Open I2Languages.asset");
        }

        [Button("生成", 50, Icon = SdfIconType.ArrowClockwise, IconAlignment = IconAlignment.LeftOfText)]
        [GUIColor(0f, 1f, 1f)]
        private void ImportAllCsvTips()
        {
            YIUIAutoTool.CloseWindow();
            YIUILocalizationCreate.CreateI2LocalizationByXlsx();
        }
    }
}
#endif