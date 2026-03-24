using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.View)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class SuperScrollChatViewDemoViewComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "SuperScrollDemo";
        public const string ResName = "SuperScrollChatViewDemoView";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIViewComponent> u_UIView;
        public YIUIViewComponent UIView => u_UIView;
        public SuperScrollView.LoopListView2 u_ComChatScroll;
        public UnityEngine.UI.InputField u_ComScrollToInputField;
        public EntityRef<ET.Client.SuperScrollDemoBackCommonComponent> u_UISuperScrollDemoBackCommon;
        public ET.Client.SuperScrollDemoBackCommonComponent UISuperScrollDemoBackCommon => u_UISuperScrollDemoBackCommon;
        public UITaskEventP0 u_EventAppendChatLeft;
        public UITaskEventHandleP0 u_EventAppendChatLeftHandle;
        public const string OnEventAppendChatLeftInvoke = "SuperScrollChatViewDemoViewComponent.OnEventAppendChatLeftInvoke";
        public UITaskEventP0 u_EventAppendChatRight;
        public UITaskEventHandleP0 u_EventAppendChatRightHandle;
        public const string OnEventAppendChatRightInvoke = "SuperScrollChatViewDemoViewComponent.OnEventAppendChatRightInvoke";
        public UITaskEventP0 u_EventScrollTo;
        public UITaskEventHandleP0 u_EventScrollToHandle;
        public const string OnEventScrollToInvoke = "SuperScrollChatViewDemoViewComponent.OnEventScrollToInvoke";

    }
}