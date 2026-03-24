using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    /// <summary>
    /// Author  Lsy
    /// Date    2025.8.6
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollTreeViewDemoViewComponent))]
    public static partial class SuperScrollTreeViewDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollTreeViewDemoViewComponent self)
        {
            self.m_TreeScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComTreeScroll);
            var count = self.m_TreeViewDataSourceMgr.TreeViewItemCount;
            for (var i = 0; i < count; ++i)
            {
                var childCount = self.m_TreeViewDataSourceMgr.GetItemDataByIndex(i).ChildCount;
                self.m_TreeItemCountMgr.AddTreeItem(childCount, true);
            }

            self.TreeScroll.SetOnClickAll();
            self.TreeScroll.SetBanSelectAll();
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollTreeViewDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollTreeViewDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollTreeViewDemoViewComponent self, ParamVo vo)
        {
            self.TreeScroll.SetListItemCount(self.m_TreeItemCountMgr.GetTotalItemAndChildCount());
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static int YIUISuperScrollListGetPrefab(this SuperScrollTreeViewDemoViewComponent self, YIUISuperScrollListComponent superScrollList, int index)
        {
            var countData = self.m_TreeItemCountMgr.QueryTreeItemByTotalIndex(index);
            return countData.IsChild(index) ? 1 : 0;
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollTreeViewDemoViewComponent self, SuperScrollTreeViewDemoItem1Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var countData = self.m_TreeItemCountMgr.QueryTreeItemByTotalIndex(index);
            var treeItemIndex = countData.mTreeItemIndex;
            var treeViewItemData = self.m_TreeViewDataSourceMgr.GetItemDataByIndex(treeItemIndex);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<TreeViewItemHead>();
            itemScript.mText.text = treeViewItemData.mName;
            itemScript.SetItemData(treeItemIndex, countData.mIsExpand);
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollTreeViewDemoViewComponent self, SuperScrollTreeViewDemoItem2Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var countData = self.m_TreeItemCountMgr.QueryTreeItemByTotalIndex(index);
            var treeItemIndex = countData.mTreeItemIndex;
            var treeViewItemData = self.m_TreeViewDataSourceMgr.GetItemDataByIndex(treeItemIndex);
            int childIndex = countData.GetChildIndex(index);
            var itemData = treeViewItemData.GetItemChildDataByIndex(childIndex);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<TreeViewItem>();
            itemScript.SetItemData(itemData, treeItemIndex, childIndex);
            var isSelected = (self.m_CurrentSelectIndex == childIndex) && (self.m_CurrentSelectItemIndex == treeItemIndex);
            item.Select(isSelected);
        }

        [EntitySystem]
        private static void YIUISuperScrollListOnClick(this SuperScrollTreeViewDemoViewComponent self, SuperScrollTreeViewDemoItem1Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var countData = self.m_TreeItemCountMgr.QueryTreeItemByTotalIndex(index);
            var treeItemIndex = countData.mTreeItemIndex;
            self.m_TreeItemCountMgr.ToggleItemExpand(treeItemIndex);
            self.TreeScroll.SetListItemCount(self.m_TreeItemCountMgr.GetTotalItemAndChildCount(), false);
            self.TreeScroll.RefreshAllShownItem();
        }

        [EntitySystem]
        private static void YIUISuperScrollListOnClick(this SuperScrollTreeViewDemoViewComponent self, SuperScrollTreeViewDemoItem2Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var countData = self.m_TreeItemCountMgr.QueryTreeItemByTotalIndex(index);
            var treeItemIndex = countData.mTreeItemIndex;
            int childIndex = countData.GetChildIndex(index);
            self.m_CurrentSelectItemIndex = treeItemIndex;
            self.m_CurrentSelectIndex = childIndex;
            self.TreeScroll.RefreshAllShownItem();
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollTreeViewDemoViewComponent.OnEventScrollToInvoke)]
        private static async ETTask OnEventScrollToInvoke(this SuperScrollTreeViewDemoViewComponent self)
        {
            if (!int.TryParse(self.u_ComScrollToInputFieldItem.text, out var itemIndex))
            {
                return;
            }

            if (!int.TryParse(self.u_ComScrollToInputFieldChild.text, out var childIndex))
            {
                return;
            }

            if (itemIndex < 0 || childIndex < 0)
            {
                return;
            }

            self.m_TreeItemCountMgr.SetItemExpand(itemIndex, true);

            var finalIndex = 0;
            var itemCountData = self.m_TreeItemCountMgr.GetTreeItem(itemIndex);
            if (itemCountData == null)
            {
                return;
            }

            var childCount = itemCountData.mChildCount;
            if (itemCountData.mIsExpand == false || childCount == 0 || childIndex == 0)
            {
                finalIndex = itemCountData.mBeginIndex;
            }
            else
            {
                if (childIndex >= childCount)
                {
                    return;
                }

                finalIndex = itemCountData.mBeginIndex + childIndex + 1;
            }

            self.TreeScroll.MovePanelToItemIndex(finalIndex);

            await ETTask.CompletedTask;
        }

        [YIUIInvoke(SuperScrollTreeViewDemoViewComponent.OnEventAddAtInvoke)]
        private static async ETTask OnEventAddAtInvoke(this SuperScrollTreeViewDemoViewComponent self)
        {
            if (!int.TryParse(self.u_ComAddInputFieldItem.text, out var itemIndex))
            {
                return;
            }

            if (!int.TryParse(self.u_ComAddInputFieldChild.text, out var childIndex))
            {
                childIndex = 0;
            }

            if (itemIndex < 0 || childIndex < 0)
            {
                return;
            }

            var itemCountData = self.m_TreeItemCountMgr.GetTreeItem(itemIndex);
            if (itemCountData == null)
            {
                return;
            }

            int childCount = itemCountData.mChildCount;
            if (childIndex > childCount)
            {
                return;
            }

            var newData = self.m_TreeViewDataSourceMgr.AddNewItemChild(itemIndex, childIndex);
            newData.mDesc += " [New]";
            self.m_TreeItemCountMgr.SetItemChildCount(itemIndex, childCount + 1);
            self.TreeScroll.SetListItemCount(self.m_TreeItemCountMgr.GetTotalItemAndChildCount(), false);
            self.TreeScroll.RefreshAllShownItem();
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(SuperScrollTreeViewDemoViewComponent.OnEventCollapseAllInvoke)]
        private static async ETTask OnEventCollapseAllInvoke(this SuperScrollTreeViewDemoViewComponent self)
        {
            var count = self.m_TreeItemCountMgr.TreeViewItemCount;
            for (var i = 0; i < count; ++i)
            {
                self.m_TreeItemCountMgr.SetItemExpand(i, false);
            }

            self.TreeScroll.SetListItemCount(self.m_TreeItemCountMgr.GetTotalItemAndChildCount(), false);
            self.TreeScroll.RefreshAllShownItem();
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(SuperScrollTreeViewDemoViewComponent.OnEventExpandAllInvoke)]
        private static async ETTask OnEventExpandAllInvoke(this SuperScrollTreeViewDemoViewComponent self)
        {
            var count = self.m_TreeItemCountMgr.TreeViewItemCount;
            for (var i = 0; i < count; ++i)
            {
                self.m_TreeItemCountMgr.SetItemExpand(i, true);
            }

            self.TreeScroll.SetListItemCount(self.m_TreeItemCountMgr.GetTotalItemAndChildCount(), false);
            self.TreeScroll.RefreshAllShownItem();
            await ETTask.CompletedTask;
        }

        #endregion YIUIEvent结束
    }
}