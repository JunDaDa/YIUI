using System;
using System.Collections.Generic;
using System.Linq;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollListComponent))]
    public static partial class YIUISuperScrollListComponentSystem
    {
        public static void SetListItemCount(this YIUISuperScrollListComponent self, int count, bool resetPos = true)
        {
            self.Owner.SetListItemCount(count, resetPos);
        }

        public static void RefreshAllShownItem(this YIUISuperScrollListComponent self)
        {
            self.Owner.RefreshAllShownItem();
        }

        public static void OnItemSizeChanged(this YIUISuperScrollListComponent self, int itemIndex)
        {
            self.Owner.OnItemSizeChanged(itemIndex);
        }

        public static void MovePanelToItemIndex(this YIUISuperScrollListComponent self, int itemIndex, float offset = 0, float duration = 0)
        {
            self.Owner.MovePanelToItemIndex(itemIndex, offset, duration);
        }

        //刷新时默认选中某个索引数据
        //注意这里相当于+=操作 如果你会频繁调用这个方法
        //又想每次刷新选中不同的索引
        //那么你应该先自行调用一次 ClearSelect
        public static void SetDataRefresh(this YIUISuperScrollListComponent self, int count, int index, bool resetPos = true)
        {
            self.SetDefaultSelect(index);
            self.SetListItemCount(count, resetPos);
        }

        //同上 请看注释 注意使用方式
        public static void SetDataRefresh(this YIUISuperScrollListComponent self, int count, List<int> index, bool resetPos = true)
        {
            self.SetDefaultSelect(index);
            self.SetListItemCount(count, resetPos);
        }

        //刷新时默认选中某个索引数据 并滚动到这个位置(一瞬间 非动画滚动)
        public static void SetDataRefresh(this YIUISuperScrollListComponent self, int count, int index, int scrollTo)
        {
            self.SetDefaultSelect(index);
            self.SetListItemCount(count, false);
            self.MovePanelToItemIndex(scrollTo);
        }

        public static int GetItemIndex(this YIUISuperScrollListComponent self, Entity entity)
        {
            var gameObject = entity.GetParent<YIUIChild>()?.OwnerGameObject;
            if (gameObject == null) return -1;
            return gameObject.GetComponent<LoopListViewItem2>()?.ItemIndex ?? -1;
        }

        public static int GetItemIndex(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            return item?.ItemIndex ?? -1;
        }

        //只能获取当前可见的对象
        public static LoopListViewItem2 GetItemByIndex(this YIUISuperScrollListComponent self, int index)
        {
            return self.Owner.GetShownItemByItemIndex(index);
        }

        //判断某个对象是否被选中
        public static bool IsSelect(this YIUISuperScrollListComponent self, Entity item)
        {
            return self.m_OnClickItemHashSet.Contains(self.GetItemIndex(item));
        }

        //reset=吧之前选择的都取消掉 讲道理应该都是true
        //false出问题自己查
        public static void ClearSelect(this YIUISuperScrollListComponent self, bool reset = true)
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
        public static List<int> GetSelectIndex(this YIUISuperScrollListComponent self)
        {
            return self.m_OnClickItemQueue.ToList();
        }

        //只能得到当前可见的 不可见的拿不到
        public static List<LoopListViewItem2> GetSelectItem(this YIUISuperScrollListComponent self)
        {
            var selectList = new List<LoopListViewItem2>();
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
        public static void Vertical(this YIUISuperScrollListComponent self, bool value)
        {
            self.Owner.ScrollRect.vertical = value;
        }

        /// <summary>
        /// 水平滚动
        /// </summary>
        public static void Horizontal(this YIUISuperScrollListComponent self, bool value)
        {
            self.Owner.ScrollRect.horizontal = value;
        }
    }
}