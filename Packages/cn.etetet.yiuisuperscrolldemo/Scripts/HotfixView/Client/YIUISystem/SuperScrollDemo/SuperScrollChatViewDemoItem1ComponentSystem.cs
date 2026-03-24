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
    [FriendOf(typeof(SuperScrollChatViewDemoItem1Component))]
    public static partial class SuperScrollChatViewDemoItem1ComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollChatViewDemoItem1Component self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollChatViewDemoItem1Component self)
        {
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
