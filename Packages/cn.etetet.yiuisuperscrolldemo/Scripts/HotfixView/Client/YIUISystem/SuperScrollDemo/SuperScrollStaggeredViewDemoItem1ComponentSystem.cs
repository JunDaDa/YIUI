using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2025.8.5
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollStaggeredViewDemoItem1Component))]
    public static partial class SuperScrollStaggeredViewDemoItem1ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollStaggeredViewDemoItem1Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollStaggeredViewDemoItem1Component self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
