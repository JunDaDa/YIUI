using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using System.Globalization;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(SuperScrollDemoPanelComponent))]
    public static partial class SuperScrollDemoPanelComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoPanelComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoPanelComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollDemoPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollDemoPanelComponent.OnEventOpenDemoInvoke)]
        private static async ETTask OnEventOpenDemoInvoke(this SuperScrollDemoPanelComponent self, string p1)
        {
            if (string.IsNullOrEmpty(p1))
            {
                TipsHelper.OpenSync<TipsTextViewComponent>(self.Scene(), "未实现的Demo");
                return;
            }

            TipsHelper.OpenSync(self.Scene(), p1);
            await ETTask.CompletedTask;
        }

        #endregion YIUIEvent结束
    }
}