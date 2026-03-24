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
    [EntitySystemOf(typeof(SuperScrollChatViewDemoItem1Component))]
    public static partial class SuperScrollChatViewDemoItem1ComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollChatViewDemoItem1Component self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollChatViewDemoItem1Component self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollChatViewDemoItem1Component self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();


        }
    }
}
