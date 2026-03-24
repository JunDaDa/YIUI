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
    public partial class SuperScrollSpinDatePickerDemoViewComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "SuperScrollDemo";
        public const string ResName = "SuperScrollSpinDatePickerDemoView";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIViewComponent> u_UIView;
        public YIUIViewComponent UIView => u_UIView;
        public SuperScrollView.LoopListView2 u_ComScrollViewYear;
        public SuperScrollView.LoopListView2 u_ComScrollViewMonth;
        public SuperScrollView.LoopListView2 u_ComScrollViewDay;
        public YIUIFramework.UIDataValueString u_DataYear;
        public YIUIFramework.UIDataValueString u_DataMonth;
        public YIUIFramework.UIDataValueString u_DataDay;
        public EntityRef<ET.Client.SuperScrollDemoBackCommonComponent> u_UISuperScrollDemoBackCommon;
        public ET.Client.SuperScrollDemoBackCommonComponent UISuperScrollDemoBackCommon => u_UISuperScrollDemoBackCommon;

    }
}