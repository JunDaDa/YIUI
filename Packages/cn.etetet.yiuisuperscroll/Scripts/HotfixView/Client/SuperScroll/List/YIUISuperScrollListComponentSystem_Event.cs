using System;
using SuperScrollView;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollListComponent))]
    public static partial class YIUISuperScrollListComponentSystem
    {
        //吸附动画真正结束
        public static void OnSnapItemFinished(this YIUISuperScrollListComponent self, bool add = true)
        {
            self.Owner.mOnSnapItemFinished -= self.OnSnapItemFinished;

            if (add)
            {
                self.Owner.mOnSnapItemFinished += self.OnSnapItemFinished;
            }
        }

        //吸附动画最近 Item 发生变化
        public static void OnSnapNearestChanged(this YIUISuperScrollListComponent self, bool add = true)
        {
            self.Owner.mOnSnapNearestChanged -= self.OnSnapNearestChanged;

            if (add)
            {
                self.Owner.mOnSnapNearestChanged += self.OnSnapNearestChanged;
            }
        }

        //平滑滚动动画完成 调用 必须滚动时间>0 才会触发
        public static void OnSmoothMovePanelToItemFinished(this YIUISuperScrollListComponent self, bool add = true)
        {
            self.Owner.mOnSmoothMovePanelToItemFinished -= self.OnSmoothMovePanelToItemFinished;

            if (add)
            {
                self.Owner.mOnSmoothMovePanelToItemFinished += self.OnSmoothMovePanelToItemFinished;
            }
        }

        private static void OnSnapItemFinished(this YIUISuperScrollListComponent self, LoopListView2 listView, LoopListViewItem2 item)
        {
            var getType = self.GetItemFinishedType(item);
            var index = item.ItemIndex;
            var select = self.m_OnClickItemHashSet.Contains(index);
            YIUISuperScrollListHelper.Finished(getType, self.OwnerEntity, item.OwnerEntity, self, index, select);
        }

        private static void OnSnapNearestChanged(this YIUISuperScrollListComponent self, LoopListView2 listView, LoopListViewItem2 item)
        {
            var getType = self.GetItemChangedType(item);
            var index = item.ItemIndex;
            var select = self.m_OnClickItemHashSet.Contains(index);
            YIUISuperScrollListHelper.Changed(getType, self.OwnerEntity, item.OwnerEntity, self, index, select);
        }

        private static void OnSmoothMovePanelToItemFinished(this YIUISuperScrollListComponent self, LoopListView2 listView, int index, float offset)
        {
            var item = self.Owner.GetShownItemByItemIndex(index);
            if (item == null)
            {
                Log.Error($"必须滚动到可显示的对象上，才能触发OnSmoothMovePanelToItemFinished事件,{index}");
                return;
            }

            var getType = self.GetItemMovedType(item);
            var select = self.m_OnClickItemHashSet.Contains(index);
            YIUISuperScrollListHelper.Moved(getType, self.OwnerEntity, item.OwnerEntity, self, index, select);
        }

        private static Type GetItemFinishedType(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            var resName = item.ResName;
            if (!self.m_FinishedSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollListFinished<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_FinishedSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static Type GetItemChangedType(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            var resName = item.ResName;
            if (!self.m_ChangedSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollListChanged<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_ChangedSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static Type GetItemMovedType(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            var resName = item.ResName;
            if (!self.m_MovedSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollListMoved<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_MovedSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }
    }
}