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
    [FriendOf(typeof(SuperScrollTreeViewDemoItem1Component))]
    public static partial class SuperScrollTreeViewDemoItem1ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollTreeViewDemoItem1Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollTreeViewDemoItem1Component self)
        {
        }

        #region YIUIEvent开始
        
        [YIUIInvoke(SuperScrollTreeViewDemoItem1Component.OnEventClickInvoke)]
        private static void OnEventClickInvoke(this SuperScrollTreeViewDemoItem1Component self)
        {

        }
        #endregion YIUIEvent结束
    }
}
