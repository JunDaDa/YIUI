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
    public partial class SuperScrollListViewLeftToRightDemoViewComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "SuperScrollDemo";
        public const string ResName = "SuperScrollListViewLeftToRightDemoView";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIViewComponent> u_UIView;
        public YIUIViewComponent UIView => u_UIView;
        public SuperScrollView.LoopListView2 u_ComListView;
        public EntityRef<ET.Client.SuperScrollDemoBackCommonComponent> u_UISuperScrollDemoBackCommon;
        public ET.Client.SuperScrollDemoBackCommonComponent UISuperScrollDemoBackCommon => u_UISuperScrollDemoBackCommon;

    }
}