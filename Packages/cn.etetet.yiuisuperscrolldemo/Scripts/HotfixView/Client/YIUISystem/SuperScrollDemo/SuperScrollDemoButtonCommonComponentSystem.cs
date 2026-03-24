using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    [FriendOf(typeof(SuperScrollDemoButtonCommonComponent))]
    public static partial class SuperScrollDemoButtonCommonComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoButtonCommonComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoButtonCommonComponent self)
        {
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollDemoButtonCommonComponent.OnEventAddAtInvoke)]
        private static async ETTask OnEventAddAtInvoke(this SuperScrollDemoButtonCommonComponent self)
        {
            int.TryParse(self.u_ComAddInputField.text, out int value);
            self.DynamicEvent(new YIUISuperScrollEvent_AddAt { Index = value }).NoContext();
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(SuperScrollDemoButtonCommonComponent.OnEventScrollToInvoke)]
        private static async ETTask OnEventScrollToInvoke(this SuperScrollDemoButtonCommonComponent self)
        {
            int.TryParse(self.u_ComScrollToInputField.text, out int value);
            self.DynamicEvent(new YIUISuperScrollEvent_ScrollTo { Index = value }).NoContext();
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(SuperScrollDemoButtonCommonComponent.OnEventSetCountInvoke)]
        private static async ETTask OnEventSetCountInvoke(this SuperScrollDemoButtonCommonComponent self)
        {
            int.TryParse(self.u_ComSetCountInputField.text, out int value);
            self.DynamicEvent(new YIUISuperScrollEvent_SetCount { Count = value }).NoContext();
            await ETTask.CompletedTask;
        }

        #endregion YIUIEvent结束
    }
}