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
    [EntitySystemOf(typeof(SuperScrollDemoButtonCommonComponent))]
    public static partial class SuperScrollDemoButtonCommonComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollDemoButtonCommonComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollDemoButtonCommonComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollDemoButtonCommonComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_ComSetCountInputField = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.InputField>("u_ComSetCountInputField");
            self.u_ComScrollToInputField = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.InputField>("u_ComScrollToInputField");
            self.u_ComAddInputField = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.InputField>("u_ComAddInputField");
            self.u_EventSetCount = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventSetCount");
            self.u_EventSetCountHandle = self.u_EventSetCount.Add(self,SuperScrollDemoButtonCommonComponent.OnEventSetCountInvoke);
            self.u_EventScrollTo = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventScrollTo");
            self.u_EventScrollToHandle = self.u_EventScrollTo.Add(self,SuperScrollDemoButtonCommonComponent.OnEventScrollToInvoke);
            self.u_EventAddAt = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventAddAt");
            self.u_EventAddAtHandle = self.u_EventAddAt.Add(self,SuperScrollDemoButtonCommonComponent.OnEventAddAtInvoke);
            self.u_UIYIUICloseCommon = self.UIBase.CDETable.FindUIOwner<ET.Client.YIUICloseCommonComponent>("YIUICloseCommon");

        }
    }
}
