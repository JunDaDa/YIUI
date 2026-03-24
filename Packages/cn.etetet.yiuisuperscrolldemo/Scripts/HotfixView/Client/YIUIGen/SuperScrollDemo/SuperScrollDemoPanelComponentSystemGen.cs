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
    [FriendOf(typeof(YIUIWindowComponent))]
    [FriendOf(typeof(YIUIPanelComponent))]
    [EntitySystemOf(typeof(SuperScrollDemoPanelComponent))]
    public static partial class SuperScrollDemoPanelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SuperScrollDemoPanelComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this SuperScrollDemoPanelComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this SuperScrollDemoPanelComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIPanel = self.UIBase.GetComponent<YIUIPanelComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIPanel.Layer = EPanelLayer.Panel;
            self.UIPanel.PanelOption = EPanelOption.TimeCache;
            self.UIPanel.StackOption = EPanelStackOption.VisibleTween;
            self.UIPanel.Priority = 0;
            self.UIPanel.CachePanelTime = 10;

            self.u_EventOpenDemo = self.UIBase.EventTable.FindEvent<UITaskEventP1<string>>("u_EventOpenDemo");
            self.u_EventOpenDemoHandle = self.u_EventOpenDemo.Add(self,SuperScrollDemoPanelComponent.OnEventOpenDemoInvoke);
            self.u_UIYIUIClose_Black = self.UIBase.CDETable.FindUIOwner<ET.Client.YIUICloseCommonComponent>("YIUIClose_Black");

        }
    }
}
