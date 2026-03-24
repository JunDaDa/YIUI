using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.8.1
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollDemoItem4Component))]
    public static partial class SuperScrollDemoItem4ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoItem4Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoItem4Component self)
        {
        }

        public static void Refresh(this SuperScrollDemoItem4Component self, int index, bool select)
        {
            self.Select(select);
        }

        public static void Select(this SuperScrollDemoItem4Component self, bool value)
        {
            self.u_DataSelect.SetValue(value);
        }

        #region YIUIEvent开始

        
        [YIUIInvoke(SuperScrollDemoItem4Component.OnEventClickInvoke)]
        private static void OnEventClickInvoke(this SuperScrollDemoItem4Component self)
        {

        }
        #endregion YIUIEvent结束
    }
}