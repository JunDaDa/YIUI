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
    [EntitySystemOf(typeof(SuperScrollDemoItem3Component))]
    public static partial class SuperScrollDemoItem3ComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollDemoItem3Component self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollDemoItem3Component self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollDemoItem3Component self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();


        }
    }
}
