using System;
using UnityEngine;
using YIUIFramework;
using SuperScrollView;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2025.8.5
    /// Desc
    /// </summary>
    [FriendOf(typeof(SuperScrollStaggeredViewTopToBottomDemoViewComponent))]
    public static partial class SuperScrollStaggeredViewTopToBottomDemoViewComponentSystem
    {
        [EntitySystem]
        private static void YIUIInitialize(this SuperScrollStaggeredViewTopToBottomDemoViewComponent self)
        {
            self.m_StaggeredGridScrollRef = self.AddChild<YIUISuperScrollStaggeredGridComponent, LoopStaggeredGridView, GridViewLayoutParam>(self.u_ComStaggeredView, new()
            {
                mPadding1         = 16,
                mPadding2         = 16,
                mColumnOrRowCount = 3,
            });

            self.m_ItemHeightArrayForDemo = new int[self.m_Count];
            for (int i = 0; i < self.m_ItemHeightArrayForDemo.Length; ++i)
            {
                self.m_ItemHeightArrayForDemo[i] = UnityEngine.Random.Range(0, 20);
            }
        }

        [EntitySystem]
        private static void Destroy(this SuperScrollStaggeredViewTopToBottomDemoViewComponent self)
        {
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollStaggeredViewTopToBottomDemoViewComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this SuperScrollStaggeredViewTopToBottomDemoViewComponent self, ParamVo vo)
        {
            self.m_DataSourceMgr = new DataSourceMgr<SuperScrollView.ItemData>(100);
            self.StaggeredGridScroll.SetListItemCount(100);
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static void YIUISuperScrollStaggeredGridRenderer(this SuperScrollStaggeredViewTopToBottomDemoViewComponent self, SuperScrollStaggeredViewDemoItem1Component item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select)
        {
            var itemData   = self.m_DataSourceMgr.GetItemDataByIndex(index);
            var itemScript = item.UIBase.OwnerGameObject.GetComponent<BaseHorizontalItem>();
            itemScript.SetItemData(itemData, index);
            var itemHeight = self.m_MinHeight + self.m_ItemHeightArrayForDemo[index % self.m_ItemHeightArrayForDemo.Length] * 10f;
            item.UIBase.OwnerRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
        }

        #region YIUIEvent开始

        #endregion YIUIEvent结束
    }
}