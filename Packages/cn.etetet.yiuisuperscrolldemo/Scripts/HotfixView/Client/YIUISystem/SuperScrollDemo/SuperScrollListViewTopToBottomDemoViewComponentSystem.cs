using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.8.1
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollListViewTopToBottomDemoViewComponent))]
    public static partial class SuperScrollListViewTopToBottomDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollListViewTopToBottomDemoViewComponent self)
        {
            self.m_ListScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComListView);
            self.ListScroll.SetOnClick();
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollListViewTopToBottomDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollListViewTopToBottomDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollListViewTopToBottomDemoViewComponent self, ParamVo vo)
        {
            self.mDataSourceMgr = new DataSourceMgr<SuperScrollView.ItemData>(100);
            self.ListScroll.SetListItemCount(100);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollListViewTopToBottomDemoViewComponent self, SuperScrollDemoItemComponent item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            item.Refresh(index, select);

            var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseVerticalItem>();
            itemScript.SetItemData(itemData, index);
        }

        [EntitySystem]
        private static void YIUISuperScrollListOnClick(this SuperScrollListViewTopToBottomDemoViewComponent self, SuperScrollDemoItemComponent item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            item.Select(select);
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}