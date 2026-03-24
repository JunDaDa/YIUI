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
    [FriendOf(typeof(SuperScrollDemoItem3Component))]
    public static partial class SuperScrollDemoItem3ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoItem3Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoItem3Component self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
