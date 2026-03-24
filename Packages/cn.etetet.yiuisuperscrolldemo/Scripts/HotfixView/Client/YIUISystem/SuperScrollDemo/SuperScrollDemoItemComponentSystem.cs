using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.7.30
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollDemoItemComponent))]
    public static partial class SuperScrollDemoItemComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollDemoItemComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollDemoItemComponent self)
        {
        }

        public static void Refresh(this SuperScrollDemoItemComponent self, int index, bool select)
        {
            self.u_DataName.SetValue(index.ToString());
            self.Select(select);
        }

        public static void Select(this SuperScrollDemoItemComponent self, bool value)
        {
            self.u_DataSelect.SetValue(value);
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollDemoItemComponent.OnEventClickInvoke)]
        private static void OnEventClickInvoke(this SuperScrollDemoItemComponent self)
        {
        }

        #endregion YIUIEvent结束
    }
}