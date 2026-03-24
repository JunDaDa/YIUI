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
    [FriendOf(typeof(SuperScrollDemoSpinViewItem1Component))]
    public static partial class SuperScrollDemoSpinViewItem1ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoSpinViewItem1Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoSpinViewItem1Component self)
        {
        }

        public static void Refresh(this SuperScrollDemoSpinViewItem1Component self, string content)
        {
            self.u_DataContent.SetValue(content);
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}