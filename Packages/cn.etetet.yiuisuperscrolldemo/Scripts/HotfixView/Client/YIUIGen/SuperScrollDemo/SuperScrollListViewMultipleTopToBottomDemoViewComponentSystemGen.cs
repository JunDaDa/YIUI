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
    [EntitySystemOf(typeof(SuperScrollListViewMultipleTopToBottomDemoViewComponent))]
    public static partial class SuperScrollListViewMultipleTopToBottomDemoViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIView = self.UIBase.GetComponent<YIUIViewComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIView.ViewWindowType = EViewWindowType.View;
            self.UIView.StackOption = EViewStackOption.VisibleTween;

            self.u_ComListView = self.UIBase.ComponentTable.FindComponent<SuperScrollView.LoopListView2>("u_ComListView");
            self.u_UISuperScrollDemoBackCommon = self.UIBase.CDETable.FindUIOwner<ET.Client.SuperScrollDemoBackCommonComponent>("SuperScrollDemoBackCommon");

        }
    }
}
