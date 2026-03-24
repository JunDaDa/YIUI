using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.8.4
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollGridViewTopToBottomDemoViewComponent))]
    public static partial class SuperScrollGridViewTopToBottomDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollGridViewTopToBottomDemoViewComponent self)
        {
            self.m_GridScrollRef = self.AddChild<YIUISuperScrollGridComponent, LoopGridView>(self.u_ComGridView);
            self.GridScroll.SetOnClickAll();
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollGridViewTopToBottomDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollGridViewTopToBottomDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollGridViewTopToBottomDemoViewComponent self, ParamVo vo)
        {
            self.mDataSourceMgr = new DataSourceMgr<SuperScrollView.ItemData>(100);
            self.GridScroll.SetListItemCount(100);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void YIUISuperScrollGridRenderer(this SuperScrollGridViewTopToBottomDemoViewComponent self, SuperScrollDemoGridViewItem1Component item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            item.Refresh(index, select);
            var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseHorizontalItem>();
            itemScript.SetItemData(itemData, index);
        }

        [EntitySystem]
        private static void YIUISuperScrollGridOnClick(this SuperScrollGridViewTopToBottomDemoViewComponent self, SuperScrollDemoGridViewItem1Component item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            item.Select(select);
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}