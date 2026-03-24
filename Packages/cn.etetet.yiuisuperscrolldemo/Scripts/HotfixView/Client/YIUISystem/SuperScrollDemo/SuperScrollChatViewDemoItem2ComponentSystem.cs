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
    [FriendOf(typeof(SuperScrollChatViewDemoItem2Component))]
    public static partial class SuperScrollChatViewDemoItem2ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollChatViewDemoItem2Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollChatViewDemoItem2Component self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
