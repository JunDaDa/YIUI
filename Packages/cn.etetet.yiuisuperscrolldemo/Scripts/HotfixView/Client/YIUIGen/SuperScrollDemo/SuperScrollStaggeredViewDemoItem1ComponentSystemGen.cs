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
    [EntitySystemOf(typeof(SuperScrollStaggeredViewDemoItem1Component))]
    public static partial class SuperScrollStaggeredViewDemoItem1ComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollStaggeredViewDemoItem1Component self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollStaggeredViewDemoItem1Component self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollStaggeredViewDemoItem1Component self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();


        }
    }
}
