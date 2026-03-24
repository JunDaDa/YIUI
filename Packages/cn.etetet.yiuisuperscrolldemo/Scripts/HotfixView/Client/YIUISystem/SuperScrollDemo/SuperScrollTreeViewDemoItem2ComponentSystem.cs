using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.8.6
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollTreeViewDemoItem2Component))]
    public static partial class SuperScrollTreeViewDemoItem2ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollTreeViewDemoItem2Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollTreeViewDemoItem2Component self)
        {
        }

        public static void Select(this SuperScrollTreeViewDemoItem2Component self, bool value)
        {
            self.u_DataSelect.SetValue(value);
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollTreeViewDemoItem2Component.OnEventClickInvoke)]
        private static void OnEventClickInvoke(this SuperScrollTreeViewDemoItem2Component self)
        {
        }

        #endregion YIUIEvent结束
    }
}