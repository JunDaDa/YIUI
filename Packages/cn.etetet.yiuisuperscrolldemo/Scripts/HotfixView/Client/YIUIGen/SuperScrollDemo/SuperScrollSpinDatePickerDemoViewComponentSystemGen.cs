using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [FriendOf(typeof(YIUIChild))]
    [FriendOf(typeof(YIUIWindowComponent))]
    [FriendOf(typeof(YIUIViewComponent))]
    [EntitySystemOf(typeof(SuperScrollSpinDatePickerDemoViewComponent))]
    public static partial class SuperScrollSpinDatePickerDemoViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollSpinDatePickerDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollSpinDatePickerDemoViewComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollSpinDatePickerDemoViewComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIView = self.UIBase.GetComponent<YIUIViewComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIView.ViewWindowType = EViewWindowType.View;
            self.UIView.StackOption = EViewStackOption.VisibleTween;

            self.u_ComScrollViewYear = self.UIBase.ComponentTable.FindComponent<SuperScrollView.LoopListView2>("u_ComScrollViewYear");
            self.u_ComScrollViewMonth = self.UIBase.ComponentTable.FindComponent<SuperScrollView.LoopListView2>("u_ComScrollViewMonth");
            self.u_ComScrollViewDay = self.UIBase.ComponentTable.FindComponent<SuperScrollView.LoopListView2>("u_ComScrollViewDay");
            self.u_DataYear = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataYear");
            self.u_DataMonth = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataMonth");
            self.u_DataDay = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataDay");
            self.u_UISuperScrollDemoBackCommon = self.UIBase.CDETable.FindUIOwner<ET.Client.SuperScrollDemoBackCommonComponent>("SuperScrollDemoBackCommon");

        }
    }
}
