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
    [EntitySystemOf(typeof(SuperScrollDemoBackCommonComponent))]
    public static partial class SuperScrollDemoBackCommonComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollDemoBackCommonComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollDemoBackCommonComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollDemoBackCommonComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_UIYIUICloseCommon = self.UIBase.CDETable.FindUIOwner<ET.Client.YIUICloseCommonComponent>("YIUICloseCommon");

        }
    }
}
