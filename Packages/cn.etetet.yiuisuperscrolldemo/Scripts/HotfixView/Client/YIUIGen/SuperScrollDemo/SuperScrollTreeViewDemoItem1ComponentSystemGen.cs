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
    [EntitySystemOf(typeof(SuperScrollTreeViewDemoItem1Component))]
    public static partial class SuperScrollTreeViewDemoItem1ComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollTreeViewDemoItem1Component self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollTreeViewDemoItem1Component self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollTreeViewDemoItem1Component self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_EventClick = self.UIBase.EventTable.FindEvent<UIEventP0>("u_EventClick");
            self.u_EventClickHandle = self.u_EventClick.Add(self,SuperScrollTreeViewDemoItem1Component.OnEventClickInvoke);

        }
    }
}
