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
    [FriendOf(typeof(SuperScrollListViewLeftToRightDemoViewComponent))]
    public static partial class SuperScrollListViewLeftToRightDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollListViewLeftToRightDemoViewComponent self)
        {
            self.m_ListScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComListView);
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollListViewLeftToRightDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollListViewLeftToRightDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollListViewLeftToRightDemoViewComponent self, ParamVo vo)
        {
            self.mDataSourceMgr = new DataSourceMgr<SuperScrollView.ItemData>(100);
            self.ListScroll.SetListItemCount(100);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollListViewLeftToRightDemoViewComponent self, SuperScrollDemoItem3Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseHorizontalItem>();
            itemScript.SetItemData(itemData, index);
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}