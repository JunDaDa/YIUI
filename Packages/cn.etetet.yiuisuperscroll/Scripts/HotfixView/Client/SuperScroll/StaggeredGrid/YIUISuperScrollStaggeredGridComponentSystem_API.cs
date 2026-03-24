using System;
using System.Collections.Generic;
using System.Linq;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollStaggeredGridComponent))]
    public static partial class YIUISuperScrollStaggeredGridComponentSystem
    {
        public static void SetListItemCount(this YIUISuperScrollStaggeredGridComponent self, int count, bool resetPos = true)
        {
            self.Owner.SetListItemCount(count, resetPos);
        }

        public static void RefreshAllShownItem(this YIUISuperScrollStaggeredGridComponent self)
        {
            self.Owner.RefreshAllShownItem();
        }

        public static void RefreshItemByItemIndex(this YIUISuperScrollStaggeredGridComponent self, int itemIndex)
        {
            self.Owner.RefreshItemByItemIndex(itemIndex);
        }

        public static void OnItemSizeChanged(this YIUISuperScrollStaggeredGridComponent self, int itemIndex)
        {
            self.Owner.OnItemSizeChanged(itemIndex);
        }

        public static void MovePanelToItemIndex(this YIUISuperScrollStaggeredGridComponent self, int itemIndex, float offset = 0, float duration = 0)
        {
            self.Owner.MovePanelToItemIndex(itemIndex, offset, duration);
        }

        public static void MovePanelToItemIndexImmediately(this YIUISuperScrollStaggeredGridComponent self, int itemIndex, float offset = 0)
        {
            self.Owner.MovePanelToItemIndexImmediately(itemIndex, offset);
        }

        // StaggeredGrid 特有方法：更新内容大小到指定项目索引
        public static void UpdateContentSizeUpToItemIndex(this YIUISuperScrollStaggeredGridComponent self, int itemIndex)
        {
            self.Owner.UpdateContentSizeUpToItemIndex(itemIndex);
        }

        // StaggeredGrid 特有方法：重置布局参数
        public static void ResetGridViewLayoutParam(this YIUISuperScrollStaggeredGridComponent self, int itemTotalCount, GridViewLayoutParam layoutParam)
        {
            self.Owner.ResetGridViewLayoutParam(itemTotalCount, layoutParam);
        }

        //刷新时默认选中某个索引数据
        //注意这里相当于+=操作 如果你会频繁调用这个方法
        //又想每次刷新选中不同的索引
        //那么你应该先自行调用一次 ClearSelect
        public static void SetDataRefresh(this YIUISuperScrollStaggeredGridComponent self, int count, int itemIndex, bool resetPos = true)
        {
            self.SetDefaultSelect(itemIndex);
            self.SetListItemCount(count, resetPos);
        }

        //同上 请看注释 注意使用方式
        public static void SetDataRefresh(this YIUISuperScrollStaggeredGridComponent self, int count, List<int> itemIndexes, bool resetPos = true)
        {
            self.SetDefaultSelect(itemIndexes);
            self.SetListItemCount(count, resetPos);
        }

        //刷新时默认选中某个索引数据 并滚动到这个位置
        public static void SetDataRefresh(this YIUISuperScrollStaggeredGridComponent self, int count, int itemIndex, int scrollTo, float duration = 0)
        {
            self.SetDefaultSelect(itemIndex);
            self.SetListItemCount(count, false);
            self.MovePanelToItemIndex(scrollTo, 0, duration);
        }

        public static int GetItemIndex(this YIUISuperScrollStaggeredGridComponent self, Entity entity)
        {
            var gameObject = entity.GetParent<YIUIChild>()?.OwnerGameObject;
            if (gameObject == null) return -1;
            return gameObject.GetComponent<LoopStaggeredGridViewItem>()?.ItemIndex ?? -1;
        }

        public static int GetItemIndex(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridViewItem item)
        {
            return item?.ItemIndex ?? -1;
        }

        public static int GetItemIndexInGroup(this YIUISuperScrollStaggeredGridComponent self, Entity entity)
        {
            var gameObject = entity.GetParent<YIUIChild>()?.OwnerGameObject;
            if (gameObject == null) return -1;
            return gameObject.GetComponent<LoopStaggeredGridViewItem>()?.ItemIndexInGroup ?? -1;
        }

        public static int GetItemIndexInGroup(this YIUISuperScrollStaggeredGridComponent self, LoopStaggeredGridViewItem item)
        {
            return item?.ItemIndexInGroup ?? -1;
        }

        public static ItemIndexData GetItemIndexData(this YIUISuperScrollStaggeredGridComponent self, int itemIndex)
        {
            return self.Owner.GetItemIndexData(itemIndex);
        }

        //只能获取当前可见的对象
        public static LoopStaggeredGridViewItem GetItemByIndex(this YIUISuperScrollStaggeredGridComponent self, int itemIndex)
        {
            return self.Owner.GetShownItemByItemIndex(itemIndex);
        }

        //判断某个对象是否被选中
        public static bool IsSelect(this YIUISuperScrollStaggeredGridComponent self, Entity item)
        {
            return self.m_OnClickItemHashSet.Contains(self.GetItemIndex(item));
        }

        //判断某个索引是否被选中
        public static bool IsSelectByIndex(this YIUISuperScrollStaggeredGridComponent self, int itemIndex)
        {
            return self.m_OnClickItemHashSet.Contains(itemIndex);
        }

        //reset=把之前选择的都取消掉 讲道理应该都是true
        //false出问题自己查
        public static void ClearSelect(this YIUISuperScrollStaggeredGridComponent self, bool reset = true)
        {
            if (reset)
            {
                var selectCount = self.m_OnClickItemHashSet.Count;
                for (var i = 0; i < selectCount; i++)
                {
                    self.OnClickItemQueuePeek();
                }
            }

            self.m_OnClickItemQueue.Clear();
            self.m_OnClickItemHashSet.Clear();
        }

        //获取当前所有被选择的索引
        public static List<int> GetSelectIndex(this YIUISuperScrollStaggeredGridComponent self)
        {
            return self.m_OnClickItemQueue.ToList();
        }

        //只能得到当前可见的 不可见的拿不到
        public static List<LoopStaggeredGridViewItem> GetSelectItem(this YIUISuperScrollStaggeredGridComponent self)
        {
            var selectList = new List<LoopStaggeredGridViewItem>();
            foreach (var index in self.GetSelectIndex())
            {
                var item = self.GetItemByIndex(index);
                if (item != null)
                {
                    selectList.Add(item);
                }
            }

            return selectList;
        }

        /// <summary>
        /// 垂直滚动
        /// </summary>
        public static void Vertical(this YIUISuperScrollStaggeredGridComponent self, bool value)
        {
            self.Owner.ScrollRect.vertical = value;
        }

        /// <summary>
        /// 水平滚动
        /// </summary>
        public static void Horizontal(this YIUISuperScrollStaggeredGridComponent self, bool value)
        {
            self.Owner.ScrollRect.horizontal = value;
        }

        /// <summary>
        /// 获取视口大小
        /// </summary>
        public static float GetViewPortSize(this YIUISuperScrollStaggeredGridComponent self)
        {
            return self.Owner.ViewPortSize;
        }

        /// <summary>
        /// 获取视口宽度
        /// </summary>
        public static float GetViewPortWidth(this YIUISuperScrollStaggeredGridComponent self)
        {
            return self.Owner.ViewPortWidth;
        }

        /// <summary>
        /// 获取视口高度
        /// </summary>
        public static float GetViewPortHeight(this YIUISuperScrollStaggeredGridComponent self)
        {
            return self.Owner.ViewPortHeight;
        }

        /// <summary>
        /// 获取内容大小
        /// </summary>
        public static float GetContentSize(this YIUISuperScrollStaggeredGridComponent self)
        {
            return self.Owner.GetContentSize();
        }

        /// <summary>
        /// 是否是垂直列表
        /// </summary>
        public static bool IsVertList(this YIUISuperScrollStaggeredGridComponent self)
        {
            return self.Owner.IsVertList;
        }

        /// <summary>
        /// 获取布局参数
        /// </summary>
        public static GridViewLayoutParam GetLayoutParam(this YIUISuperScrollStaggeredGridComponent self)
        {
            return self.Owner.LayoutParam;
        }

        /// <summary>
        /// 清除自动移动数据
        /// </summary>
        public static void ClearAutoMoveToItemData(this YIUISuperScrollStaggeredGridComponent self)
        {
            self.Owner.ClearAutoMoveToItemData();
        }

        /// <summary>
        /// 重置列表视图
        /// </summary>
        public static void ResetListView(this YIUISuperScrollStaggeredGridComponent self, bool resetPos = true)
        {
            self.Owner.ResetListView(resetPos);
        }

        /// <summary>
        /// 获取项目组
        /// </summary>
        public static StaggeredGridItemGroup GetItemGroupByIndex(this YIUISuperScrollStaggeredGridComponent self, int index)
        {
            return self.Owner.GetItemGroupByIndex(index);
        }
    }
}