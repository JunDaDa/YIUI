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
    [FriendOf(typeof(SuperScrollListViewMultipleTopToBottomDemoViewComponent))]
    public static partial class SuperScrollListViewMultipleTopToBottomDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self)
        {
            self.m_ListScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComListView);
            self.ListScroll.SetOnClickAll(); //所有都可以点击
            self.ListScroll.SetBanSelect(1); //第二个预制可以点但是不能选中
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, ParamVo vo)
        {
            self.mDataSourceMgr = new DataSourceMgr<SuperScrollView.ItemData>(100);
            self.ListScroll.SetListItemCount(100);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static int YIUISuperScrollListGetPrefab(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, YIUISuperScrollListComponent superScrollList, int index)
        {
            //根据数据或算法返回预制的index
            return index % 3;
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, SuperScrollDemoItemComponent item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            //根据索引获取数据后 自行刷新item 这里只是演示用
            item.Refresh(index, select);
            var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
            //用官方的组件刷新的只是为了演示用 正常应该使用YIUI实现自己的刷新逻辑
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseVerticalItem>();
            itemScript.SetItemData(itemData, index);
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, SuperScrollDemoItem2Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            item.Refresh(index, select);
            var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<SliderItem>();
            itemScript.SetItemData(itemData, index);
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, SuperScrollDemoItem4Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            item.Refresh(index, select);
            var itemData = self.mDataSourceMgr.GetItemDataByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<InputFieldItem>();
            itemScript.SetItemData(itemData, index);
        }

        [EntitySystem]
        private static void YIUISuperScrollListOnClick(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, SuperScrollDemoItemComponent item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            item.Select(select);
        }

        [EntitySystem]
        private static void YIUISuperScrollListOnClick(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, SuperScrollDemoItem2Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            item.Select(select);
        }

        [EntitySystem]
        private static void YIUISuperScrollListOnClick(this SuperScrollListViewMultipleTopToBottomDemoViewComponent self, SuperScrollDemoItem4Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            item.Select(select);
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}