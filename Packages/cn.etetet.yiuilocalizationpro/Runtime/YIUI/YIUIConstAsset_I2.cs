using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace YIUIFramework
{
    public partial class YIUIConstAsset
    {
        [BoxGroup("多语言设置", CenterLabel = true)]
        [LabelText("编辑器下模拟运行时")]
        public bool I2UseRuntimeModule = false;

        [BoxGroup("多语言设置", CenterLabel = true)]
        [LabelText("默认多语言")]
        public string I2DefaultLanguage = "Chinese";

        [BoxGroup("多语言设置", CenterLabel = true)]
        [LabelText("代码注释语言")]
        public string I2CodeCommentLanguage = "Chinese";

        [BoxGroup("多语言设置", CenterLabel = true)]
        [LabelText("关闭字体二级语言功能")]
        public bool I2CloseSecondaryTranslation = false; //这个是根据字体不同二次切换

        [BoxGroup("多语言设置", CenterLabel = true)]
        [LabelText("空翻译报错")] //就是必须填写翻译内容，否则报错 与自动补齐不冲突
        public bool I2NullTranslationError = false;

        [BoxGroup("多语言设置", CenterLabel = true)]
        [LabelText("没填写的翻译 自动补齐")] //具体补齐规则看源码或文档
        public bool I2AutoComplete = false;

        [BoxGroup("多语言设置", CenterLabel = true)]
        [LabelText("如果自动补齐 填充 方便辨识")]
        [ShowIf("I2AutoComplete")]
        public string I2AutoCompleteFormat = "{0}[Null]";
    }
}