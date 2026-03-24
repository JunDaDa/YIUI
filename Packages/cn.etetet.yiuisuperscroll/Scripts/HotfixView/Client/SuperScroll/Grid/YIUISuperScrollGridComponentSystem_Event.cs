using System;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollGridComponent))]
    public static partial class YIUISuperScrollGridComponentSystem
    {
        //吸附动画真正结束
        public static void OnSnapItemFinished(this YIUISuperScrollGridComponent self, bool add = true)
        {
            self.Owner.mOnSnapItemFinished -= self.OnSnapItemFinished;

            if (add)
            {
                self.Owner.mOnSnapItemFinished += self.OnSnapItemFinished;
            }
        }

        //吸附动画最近 Item 发生变化
        public static void OnSnapNearestChanged(this YIUISuperScrollGridComponent self, bool add = true)
        {
            self.Owner.mOnSnapNearestChanged -= self.OnSnapNearestChanged;

            if (add)
            {
                self.Owner.mOnSnapNearestChanged += self.OnSnapNearestChanged;
            }
        }

        private static void OnSnapItemFinished(this YIUISuperScrollGridComponent self, LoopGridView gridView, LoopGridViewItem item)
        {
            var getType = self.GetItemFinishedType(item);
            var index = item.ItemIndex;
            var select = self.m_OnClickItemHashSet.Contains(index);
            YIUISuperScrollGridHelper.Finished(getType, self.OwnerEntity, item.OwnerEntity, self, index, select);
        }

        private static void OnSnapNearestChanged(this YIUISuperScrollGridComponent self, LoopGridView gridView)
        {
            var snapItem = self.GetSnapNearestItem();
            if (snapItem == null) return;

            var getType = self.GetItemChangedType(snapItem);
            var index = snapItem.ItemIndex;
            var select = self.m_OnClickItemHashSet.Contains(index);
            YIUISuperScrollGridHelper.Changed(getType, self.OwnerEntity, snapItem.OwnerEntity, self, index, select);
        }

        private static Type GetItemFinishedType(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            var resName = item.ResName;
            if (!self.m_FinishedSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollGridFinished<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_FinishedSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static Type GetItemChangedType(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            var resName = item.ResName;
            if (!self.m_ChangedSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollGridChanged<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_ChangedSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static LoopGridViewItem GetSnapNearestItem(this YIUISuperScrollGridComponent self)
        {
            var snapRowColumn = self.Owner.CurSnapNearestItemRowColumn;
            return self.Owner.GetShownItemByRowColumn(snapRowColumn.mRow, snapRowColumn.mColumn);
        }
    }
}