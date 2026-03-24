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
    public partial class SuperScrollDemoButtonCommonComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "SuperScrollDemo";
        public const string ResName = "SuperScrollDemoButtonCommon";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public UnityEngine.UI.InputField u_ComSetCountInputField;
        public UnityEngine.UI.InputField u_ComScrollToInputField;
        public UnityEngine.UI.InputField u_ComAddInputField;
        public EntityRef<ET.Client.YIUICloseCommonComponent> u_UIYIUICloseCommon;
        public ET.Client.YIUICloseCommonComponent UIYIUICloseCommon => u_UIYIUICloseCommon;
        public UITaskEventP0 u_EventSetCount;
        public UITaskEventHandleP0 u_EventSetCountHandle;
        public const string OnEventSetCountInvoke = "SuperScrollDemoButtonCommonComponent.OnEventSetCountInvoke";
        public UITaskEventP0 u_EventScrollTo;
        public UITaskEventHandleP0 u_EventScrollToHandle;
        public const string OnEventScrollToInvoke = "SuperScrollDemoButtonCommonComponent.OnEventScrollToInvoke";
        public UITaskEventP0 u_EventAddAt;
        public UITaskEventHandleP0 u_EventAddAtHandle;
        public const string OnEventAddAtInvoke = "SuperScrollDemoButtonCommonComponent.OnEventAddAtInvoke";

    }
}