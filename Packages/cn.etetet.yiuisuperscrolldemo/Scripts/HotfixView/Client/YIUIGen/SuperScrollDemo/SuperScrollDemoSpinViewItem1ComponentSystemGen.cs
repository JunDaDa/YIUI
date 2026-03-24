using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [FriendOf(typeof(YIUIChild))]
    [EntitySystemOf(typeof(SuperScrollDemoSpinViewItem1Component))]
    public static partial class SuperScrollDemoSpinViewItem1ComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollDemoSpinViewItem1Component self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollDemoSpinViewItem1Component self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollDemoSpinViewItem1Component self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_DataContent = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueString>("u_DataContent");

        }
    }
}
