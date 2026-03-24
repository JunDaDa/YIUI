using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.7.31
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollDemoItem2Component))]
    public static partial class SuperScrollDemoItem2ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoItem2Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoItem2Component self)
        {
        }

        public static void Refresh(this SuperScrollDemoItem2Component self, int index, bool select)
        {
            self.u_DataIndex.SetValue(index);
            self.Select(select);
        }

        public static void Select(this SuperScrollDemoItem2Component self, bool value)
        {
            self.u_DataSelect.SetValue(value);
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollDemoItem2Component.OnEventClickInvoke)]
        private static void OnEventClickInvoke(this SuperScrollDemoItem2Component self)
        {
        }

        #endregion YIUIEvent结束
    }
}