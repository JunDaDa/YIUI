using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.8.4
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollDemoGridViewItem1Component))]
    public static partial class SuperScrollDemoGridViewItem1ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoGridViewItem1Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoGridViewItem1Component self)
        {
        }

        public static void Refresh(this SuperScrollDemoGridViewItem1Component self, int index, bool select)
        {
            self.Select(select);
        }

        public static void Select(this SuperScrollDemoGridViewItem1Component self, bool value)
        {
            self.u_DataSelect.SetValue(value);
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollDemoGridViewItem1Component.OnEventClickInvoke)]
        private static void OnEventClickInvoke(this SuperScrollDemoGridViewItem1Component self)
        {
        }

        #endregion YIUIEvent结束
    }
}