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
    [FriendOf(typeof(SuperScrollDemoGridViewItem3Component))]
    public static partial class SuperScrollDemoGridViewItem3ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoGridViewItem3Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoGridViewItem3Component self)
        {
        }

        public static void Refresh(this SuperScrollDemoGridViewItem3Component self, int index, bool select)
        {
            self.Select(select);
        }

        public static void Select(this SuperScrollDemoGridViewItem3Component self, bool value)
        {
            self.u_DataSelect.SetValue(value);
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollDemoGridViewItem3Component.OnEventClickInvoke)]
        private static void OnEventClickInvoke(this SuperScrollDemoGridViewItem3Component self)
        {
        }

        #endregion YIUIEvent结束
    }
}