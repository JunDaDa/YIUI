using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Common)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class SuperScrollTreeViewDemoItem2Component : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "SuperScrollDemo";
        public const string ResName = "SuperScrollTreeViewDemoItem2";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public YIUIFramework.UIDataValueBool u_DataSelect;
        public UIEventP0 u_EventClick;
        public UIEventHandleP0 u_EventClickHandle;
        public const string OnEventClickInvoke = "SuperScrollTreeViewDemoItem2Component.OnEventClickInvoke";

    }
}