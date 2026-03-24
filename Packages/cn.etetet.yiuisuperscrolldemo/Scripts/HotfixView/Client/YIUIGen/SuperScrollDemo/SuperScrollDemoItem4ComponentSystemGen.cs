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
    [EntitySystemOf(typeof(SuperScrollDemoItem4Component))]
    public static partial class SuperScrollDemoItem4ComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollDemoItem4Component self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollDemoItem4Component self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollDemoItem4Component self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_DataSelect = self.UIBase.DataTable.FindDataValue<YIUIFramework.UIDataValueBool>("u_DataSelect");
            self.u_EventClick = self.UIBase.EventTable.FindEvent<UIEventP0>("u_EventClick");
            self.u_EventClickHandle = self.u_EventClick.Add(self,SuperScrollDemoItem4Component.OnEventClickInvoke);

        }
    }
}
