using System;
using System.Collections.Generic;
using System.Linq;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollGridComponent))]
    public static partial class YIUISuperScrollGridComponentSystem
    {
        public static void SetListItemCount(this YIUISuperScrollGridComponent self, int count, bool resetPos = true)
        {
            self.Owner.SetListItemCount(count, resetPos);
        }

        public static void RefreshAllShownItem(this YIUISuperScrollGridComponent self)
        {
            self.Owner.RefreshAllShownItem();
        }

        public static void RefreshItemByItemIndex(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            self.Owner.RefreshItemByItemIndex(itemIndex);
        }

        public static void RefreshItemByRowColumn(this YIUISuperScrollGridComponent self, int row, int column)
        {
            self.Owner.RefreshItemByRowColumn(row, column);
        }

        public static void MovePanelToItemIndex(this YIUISuperScrollGridComponent self, int itemIndex, float offsetX = 0, float offsetY = 0)
        {
            self.Owner.MovePanelToItemByIndex(itemIndex, offsetX, offsetY);
        }

        public static void MovePanelToItemByRowColumn(this YIUISuperScrollGridComponent self, int row, int column, float offsetX = 0, float offsetY = 0)
        {
            self.Owner.MovePanelToItemByRowColumn(row, column, offsetX, offsetY);
        }

        //刷新时默认选中某个索引数据
        //注意这里相当于+=操作 如果你会频繁调用这个方法
        //又想每次刷新选中不同的索引
        //那么你应该先自行调用一次 ClearSelect
        public static void SetDataRefresh(this YIUISuperScrollGridComponent self, int count, int itemIndex, bool resetPos = true)
        {
            self.SetDefaultSelect(itemIndex);
            self.SetListItemCount(count, resetPos);
        }

        //同上 请看注释 注意使用方式
        public static void SetDataRefresh(this YIUISuperScrollGridComponent self, int count, List<int> itemIndexes, bool resetPos = true)
        {
            self.SetDefaultSelect(itemIndexes);
            self.SetListItemCount(count, resetPos);
        }

        //刷新时默认选中某个索引数据 并滚动到这个位置(一瞬间 非动画滚动)
        public static void SetDataRefresh(this YIUISuperScrollGridComponent self, int count, int itemIndex, int scrollTo)
        {
            self.SetDefaultSelect(itemIndex);
            self.SetListItemCount(count, false);
            self.MovePanelToItemIndex(scrollTo);
        }

        // GridView 特有的方法：通过行列设置数据
        public static void SetDataRefreshByRowColumn(this YIUISuperScrollGridComponent self, int count, int row, int column, bool resetPos = true)
        {
            var itemIndex = self.Owner.GetItemIndexByRowColumn(row, column);
            self.SetDataRefresh(count, itemIndex, resetPos);
        }

        public static void SetDataRefreshByRowColumn(this YIUISuperScrollGridComponent self, int count, int row, int column, int scrollToRow, int scrollToColumn)
        {
            var itemIndex = self.Owner.GetItemIndexByRowColumn(row, column);
            var scrollToIndex = self.Owner.GetItemIndexByRowColumn(scrollToRow, scrollToColumn);
            self.SetDataRefresh(count, itemIndex, scrollToIndex);
        }

        public static int GetItemIndex(this YIUISuperScrollGridComponent self, Entity entity)
        {
            var gameObject = entity.GetParent<YIUIChild>()?.OwnerGameObject;
            if (gameObject == null) return -1;
            return gameObject.GetComponent<LoopGridViewItem>()?.ItemIndex ?? -1;
        }

        public static int GetItemIndex(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            return item?.ItemIndex ?? -1;
        }

        public static RowColumnPair GetRowColumn(this YIUISuperScrollGridComponent self, Entity entity)
        {
            var gameObject = entity.GetParent<YIUIChild>()?.OwnerGameObject;
            if (gameObject == null) return new RowColumnPair(-1, -1);
            var item = gameObject.GetComponent<LoopGridViewItem>();
            return item != null ? new RowColumnPair(item.Row, item.Column) : new RowColumnPair(-1, -1);
        }

        public static RowColumnPair GetRowColumn(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            return item != null ? new RowColumnPair(item.Row, item.Column) : new RowColumnPair(-1, -1);
        }

        //只能获取当前可见的对象
        public static LoopGridViewItem GetItemByIndex(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            return self.Owner.GetShownItemByItemIndex(itemIndex);
        }

        //只能获取当前可见的对象
        public static LoopGridViewItem GetItemByRowColumn(this YIUISuperScrollGridComponent self, int row, int column)
        {
            return self.Owner.GetShownItemByRowColumn(row, column);
        }

        //判断某个对象是否被选中
        public static bool IsSelect(this YIUISuperScrollGridComponent self, Entity item)
        {
            return self.m_OnClickItemHashSet.Contains(self.GetItemIndex(item));
        }

        //判断某个索引是否被选中
        public static bool IsSelectByIndex(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            return self.m_OnClickItemHashSet.Contains(itemIndex);
        }

        //判断某个行列是否被选中
        public static bool IsSelectByRowColumn(this YIUISuperScrollGridComponent self, int row, int column)
        {
            var itemIndex = self.Owner.GetItemIndexByRowColumn(row, column);
            return self.m_OnClickItemHashSet.Contains(itemIndex);
        }

        //reset=把之前选择的都取消掉 讲道理应该都是true
        //false出问题自己查
        public static void ClearSelect(this YIUISuperScrollGridComponent self, bool reset = true)
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
        public static List<int> GetSelectIndex(this YIUISuperScrollGridComponent self)
        {
            return self.m_OnClickItemQueue.ToList();
        }

        //获取当前所有被选择的行列对
        public static List<RowColumnPair> GetSelectRowColumn(this YIUISuperScrollGridComponent self)
        {
            var result = new List<RowColumnPair>();
            foreach (var index in self.GetSelectIndex())
            {
                result.Add(self.Owner.GetRowColumnByItemIndex(index));
            }
            return result;
        }

        //只能得到当前可见的 不可见的拿不到
        public static List<LoopGridViewItem> GetSelectItem(this YIUISuperScrollGridComponent self)
        {
            var selectList = new List<LoopGridViewItem>();
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
        public static void Vertical(this YIUISuperScrollGridComponent self, bool value)
        {
            self.Owner.ScrollRect.vertical = value;
        }

        /// <summary>
        /// 水平滚动
        /// </summary>
        public static void Horizontal(this YIUISuperScrollGridComponent self, bool value)
        {
            self.Owner.ScrollRect.horizontal = value;
        }

        /// <summary>
        /// 设置网格固定类型和数量
        /// </summary>
        public static void SetGridFixedGroupCount(this YIUISuperScrollGridComponent self, GridFixedType fixedType, int count)
        {
            self.Owner.SetGridFixedGroupCount(fixedType, count);
        }

        /// <summary>
        /// 设置项目大小
        /// </summary>
        public static void SetItemSize(this YIUISuperScrollGridComponent self, UnityEngine.Vector2 newSize)
        {
            self.Owner.SetItemSize(newSize);
        }

        /// <summary>
        /// 设置项目间距
        /// </summary>
        public static void SetItemPadding(this YIUISuperScrollGridComponent self, UnityEngine.Vector2 newPadding)
        {
            self.Owner.SetItemPadding(newPadding);
        }

        /// <summary>
        /// 设置容器边距
        /// </summary>
        public static void SetPadding(this YIUISuperScrollGridComponent self, UnityEngine.RectOffset newPadding)
        {
            self.Owner.SetPadding(newPadding);
        }
    }
}