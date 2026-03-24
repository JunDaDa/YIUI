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
    [EntitySystemOf(typeof(SuperScrollChatViewDemoViewComponent))]
    public static partial class SuperScrollChatViewDemoViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollChatViewDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollChatViewDemoViewComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollChatViewDemoViewComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIView = self.UIBase.GetComponent<YIUIViewComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIView.ViewWindowType = EViewWindowType.View;
            self.UIView.StackOption = EViewStackOption.VisibleTween;

            self.u_ComChatScroll = self.UIBase.ComponentTable.FindComponent<SuperScrollView.LoopListView2>("u_ComChatScroll");
            self.u_ComScrollToInputField = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.InputField>("u_ComScrollToInputField");
            self.u_EventAppendChatLeft = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventAppendChatLeft");
            self.u_EventAppendChatLeftHandle = self.u_EventAppendChatLeft.Add(self,SuperScrollChatViewDemoViewComponent.OnEventAppendChatLeftInvoke);
            self.u_EventAppendChatRight = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventAppendChatRight");
            self.u_EventAppendChatRightHandle = self.u_EventAppendChatRight.Add(self,SuperScrollChatViewDemoViewComponent.OnEventAppendChatRightInvoke);
            self.u_EventScrollTo = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventScrollTo");
            self.u_EventScrollToHandle = self.u_EventScrollTo.Add(self,SuperScrollChatViewDemoViewComponent.OnEventScrollToInvoke);
            self.u_UISuperScrollDemoBackCommon = self.UIBase.CDETable.FindUIOwner<ET.Client.SuperScrollDemoBackCommonComponent>("SuperScrollDemoBackCommon");

        }
    }
}
