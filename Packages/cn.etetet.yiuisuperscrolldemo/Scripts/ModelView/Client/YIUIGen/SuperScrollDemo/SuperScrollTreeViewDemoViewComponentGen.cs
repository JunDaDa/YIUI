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
    public partial class SuperScrollTreeViewDemoViewComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "SuperScrollDemo";
        public const string ResName = "SuperScrollTreeViewDemoView";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIViewComponent> u_UIView;
        public YIUIViewComponent UIView => u_UIView;
        public SuperScrollView.LoopListView2 u_ComTreeScroll;
        public UnityEngine.UI.InputField u_ComAddInputFieldItem;
        public UnityEngine.UI.InputField u_ComAddInputFieldChild;
        public UnityEngine.UI.InputField u_ComScrollToInputFieldItem;
        public UnityEngine.UI.InputField u_ComScrollToInputFieldChild;
        public EntityRef<ET.Client.SuperScrollDemoBackCommonComponent> u_UISuperScrollDemoBackCommon;
        public ET.Client.SuperScrollDemoBackCommonComponent UISuperScrollDemoBackCommon => u_UISuperScrollDemoBackCommon;
        public UITaskEventP0 u_EventScrollTo;
        public UITaskEventHandleP0 u_EventScrollToHandle;
        public const string OnEventScrollToInvoke = "SuperScrollTreeViewDemoViewComponent.OnEventScrollToInvoke";
        public UITaskEventP0 u_EventExpandAll;
        public UITaskEventHandleP0 u_EventExpandAllHandle;
        public const string OnEventExpandAllInvoke = "SuperScrollTreeViewDemoViewComponent.OnEventExpandAllInvoke";
        public UITaskEventP0 u_EventCollapseAll;
        public UITaskEventHandleP0 u_EventCollapseAllHandle;
        public const string OnEventCollapseAllInvoke = "SuperScrollTreeViewDemoViewComponent.OnEventCollapseAllInvoke";
        public UITaskEventP0 u_EventAddAt;
        public UITaskEventHandleP0 u_EventAddAtHandle;
        public const string OnEventAddAtInvoke = "SuperScrollTreeViewDemoViewComponent.OnEventAddAtInvoke";

    }
}