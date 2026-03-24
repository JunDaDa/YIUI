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
    [EntitySystemOf(typeof(SuperScrollTreeViewDemoViewComponent))]
    public static partial class SuperScrollTreeViewDemoViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollTreeViewDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollTreeViewDemoViewComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollTreeViewDemoViewComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIView = self.UIBase.GetComponent<YIUIViewComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIView.ViewWindowType = EViewWindowType.View;
            self.UIView.StackOption = EViewStackOption.VisibleTween;

            self.u_ComTreeScroll = self.UIBase.ComponentTable.FindComponent<SuperScrollView.LoopListView2>("u_ComTreeScroll");
            self.u_ComAddInputFieldItem = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.InputField>("u_ComAddInputFieldItem");
            self.u_ComAddInputFieldChild = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.InputField>("u_ComAddInputFieldChild");
            self.u_ComScrollToInputFieldItem = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.InputField>("u_ComScrollToInputFieldItem");
            self.u_ComScrollToInputFieldChild = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.InputField>("u_ComScrollToInputFieldChild");
            self.u_EventScrollTo = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventScrollTo");
            self.u_EventScrollToHandle = self.u_EventScrollTo.Add(self,SuperScrollTreeViewDemoViewComponent.OnEventScrollToInvoke);
            self.u_EventExpandAll = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventExpandAll");
            self.u_EventExpandAllHandle = self.u_EventExpandAll.Add(self,SuperScrollTreeViewDemoViewComponent.OnEventExpandAllInvoke);
            self.u_EventCollapseAll = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventCollapseAll");
            self.u_EventCollapseAllHandle = self.u_EventCollapseAll.Add(self,SuperScrollTreeViewDemoViewComponent.OnEventCollapseAllInvoke);
            self.u_EventAddAt = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventAddAt");
            self.u_EventAddAtHandle = self.u_EventAddAt.Add(self,SuperScrollTreeViewDemoViewComponent.OnEventAddAtInvoke);
            self.u_UISuperScrollDemoBackCommon = self.UIBase.CDETable.FindUIOwner<ET.Client.SuperScrollDemoBackCommonComponent>("SuperScrollDemoBackCommon");

        }
    }
}
