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
    [FriendOf(typeof(SuperScrollDemoBackCommonComponent))]
    public static partial class SuperScrollDemoBackCommonComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoBackCommonComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoBackCommonComponent self)
        {
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}