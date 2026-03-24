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
    [FriendOf(typeof(SuperScrollDemoGridViewItem2Component))]
    public static partial class SuperScrollDemoGridViewItem2ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoGridViewItem2Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoGridViewItem2Component self)
        {
        }

        public static void Refresh(this SuperScrollDemoGridViewItem2Component self, int index, bool select)
        {
            self.Select(select);
        }

        public static void Select(this SuperScrollDemoGridViewItem2Component self, bool value)
        {
            self.u_DataSelect.SetValue(value);
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollDemoGridViewItem2Component.OnEventClickInvoke)]
        private static void OnEventClickInvoke(this SuperScrollDemoGridViewItem2Component self)
        {
        }

        #endregion YIUIEvent结束
    }
}