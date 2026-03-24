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
    [FriendOf(typeof(SuperScrollGridViewDiagonalTopLeftDemoViewComponent))]
    public static partial class SuperScrollGridViewDiagonalTopLeftDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollGridViewDiagonalTopLeftDemoViewComponent self)
        {
            self.m_GridScrollRef = self.AddChild<YIUISuperScrollGridComponent, LoopGridView>(self.u_ComGridView);
            self.GridScroll.SetOnClickAll();
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollGridViewDiagonalTopLeftDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollGridViewDiagonalTopLeftDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollGridViewDiagonalTopLeftDemoViewComponent self, ParamVo vo)
        {
            self.mDataSourceMgr = new DataSourceMgr<SuperScrollView.ItemData>(5000);
            self.GridScroll.SetListItemCount(5000);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void YIUISuperScrollGridRenderer(this SuperScrollGridViewDiagonalTopLeftDemoViewComponent self, SuperScrollDemoGridViewItem3Component item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            item.Refresh(index, select);
            var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseRowColItem>();
            itemScript.SetItemData(itemData, index, row, column);
        }

        [EntitySystem]
        private static void YIUISuperScrollGridOnClick(this SuperScrollGridViewDiagonalTopLeftDemoViewComponent self, SuperScrollDemoGridViewItem3Component item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            item.Select(select);
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}