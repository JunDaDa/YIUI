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
    [FriendOf(typeof(SuperScrollChatViewDemoViewComponent))]
    public static partial class SuperScrollChatViewDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollChatViewDemoViewComponent self)
        {
            self.m_ChatScrollRef = self.AddChild<YIUISuperScrollListComponent, LoopListView2>(self.u_ComChatScroll);
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollChatViewDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollChatViewDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollChatViewDemoViewComponent self, ParamVo vo)
        {
            self.ChatScroll.SetListItemCount(ChatMsgDataSourceMgr.Get.TotalItemCount);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static int YIUISuperScrollListGetPrefab(this SuperScrollChatViewDemoViewComponent self, YIUISuperScrollListComponent superScrollList, int index)
        {
            var itemData = ChatMsgDataSourceMgr.Get.GetChatMsgByIndex(index);
            return itemData.mPersonId == 0 ? 0 : 1;
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollChatViewDemoViewComponent self, SuperScrollChatViewDemoItem1Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var itemData = ChatMsgDataSourceMgr.Get.GetChatMsgByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<ChatViewItem>();
            itemScript.SetItemData(itemData, index);
        }

        [EntitySystem]
        private static void YIUISuperScrollListRenderer(this SuperScrollChatViewDemoViewComponent self, SuperScrollChatViewDemoItem2Component item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            var itemData = ChatMsgDataSourceMgr.Get.GetChatMsgByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<ChatViewItem>();
            itemScript.SetItemData(itemData, index);
        }

        #region YIUIEvent开始

        [YIUIInvoke(SuperScrollChatViewDemoViewComponent.OnEventScrollToInvoke)]
        private static async ETTask OnEventScrollToInvoke(this SuperScrollChatViewDemoViewComponent self)
        {
            int itemIndex = 0;
            if (int.TryParse(self.u_ComScrollToInputField.text, out itemIndex) == false)
            {
                return;
            }

            if ((itemIndex < 0) || (itemIndex >= self.ChatScroll.Owner.ItemTotalCount))
            {
                return;
            }

            self.ChatScroll.Owner.SetSnapTargetItemIndex(itemIndex, 10000);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(SuperScrollChatViewDemoViewComponent.OnEventAppendChatRightInvoke)]
        private static async ETTask OnEventAppendChatRightInvoke(this SuperScrollChatViewDemoViewComponent self)
        {
            ChatMsgDataSourceMgr.Get.AppendOneMsg(1);
            self.ChatScroll.SetListItemCount(ChatMsgDataSourceMgr.Get.TotalItemCount, false);
            self.ChatScroll.MovePanelToItemIndex(ChatMsgDataSourceMgr.Get.TotalItemCount - 1);
            await ETTask.CompletedTask;
        }

        [YIUIInvoke(SuperScrollChatViewDemoViewComponent.OnEventAppendChatLeftInvoke)]
        private static async ETTask OnEventAppendChatLeftInvoke(this SuperScrollChatViewDemoViewComponent self)
        {
            ChatMsgDataSourceMgr.Get.AppendOneMsg(0);
            self.ChatScroll.SetListItemCount(ChatMsgDataSourceMgr.Get.TotalItemCount, false);
            self.ChatScroll.MovePanelToItemIndex(ChatMsgDataSourceMgr.Get.TotalItemCount - 1);
            await ETTask.CompletedTask;
        }

        #endregion YIUIEvent结束
    }
}