using System;
using System.Collections.Generic;
using SuperScrollView;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollListComponent))]
    public static partial class YIUISuperScrollListComponentSystem
    {
        static partial void AwakeOnClick(this YIUISuperScrollListComponent self)
        {
            self.m_MaxClickCount = Mathf.Max(1, self.Owner.u_MaxClickCount);
            self.m_RepetitionCancel = self.Owner.u_RepetitionCancel;
            self.m_AutoCancelLast = self.Owner.u_AutoCancelLast;
            self.m_OnClickItemQueue.Clear();
            self.m_OnClickItemHashSet.Clear();
        }

        public static void SetOnClickAll(this YIUISuperScrollListComponent self, string itemClickEventName = YIUISuperScrollListComponent.ClickItemEvent)
        {
            for (int index = 0; index < self.Owner.ItemPrefabDataList.Count; index++)
            {
                self.SetOnClick(index, itemClickEventName);
            }
        }

        public static void SetOnClick(this YIUISuperScrollListComponent self, int prefabIndex = 0, string itemClickEventName = YIUISuperScrollListComponent.ClickItemEvent)
        {
            if (self.GetItemOnClickInit(prefabIndex))
            {
                Debug.LogError($"OnClick 相关只能初始化一次 且不能修改");
                return;
            }

            if (string.IsNullOrEmpty(itemClickEventName))
            {
                Debug.LogError($"必须有事件名称");
                return;
            }

            var resName = self.Owner.GetItemPoolResName(prefabIndex);
            self.m_ItemClickEventName[resName] = itemClickEventName;
            self.m_OnClickInit[resName] = true;
        }

        public static void SetOnClickCheck(this YIUISuperScrollListComponent self, int prefabIndex = 0, bool value = true)
        {
            if (!self.GetItemOnClickInit(prefabIndex))
            {
                Debug.LogError($"有Click 才可以,有检查");
                return;
            }

            var resName = self.Owner.GetItemPoolResName(prefabIndex);
            self.m_ItemClickCheck[resName] = value;
        }

        public static void SetBanSelectAll(this YIUISuperScrollListComponent self, bool value = true)
        {
            for (int index = 0; index < self.Owner.ItemPrefabDataList.Count; index++)
            {
                self.SetBanSelect(index, value);
            }
        }

        public static void SetBanSelect(this YIUISuperScrollListComponent self, int prefabIndex = 0, bool value = true)
        {
            if (!self.GetItemOnClickInit(prefabIndex))
            {
                Debug.LogError($"有Click 才可以,禁止选中");
                return;
            }

            var resName = self.Owner.GetItemPoolResName(prefabIndex);
            self.m_ItemBanSelect[resName] = value;
        }

        //动态改变 自动取消上一个选择的
        public static void ChangeAutoCancelLast(this YIUISuperScrollListComponent self, bool autoCancelLast)
        {
            self.m_AutoCancelLast = autoCancelLast;
        }

        //动态改变 重复选择 则取消选择
        public static void ChangeRepetitionCancel(this YIUISuperScrollListComponent self, bool repetitionCancel)
        {
            self.m_RepetitionCancel = repetitionCancel;
        }

        //动态改变 最大可选数量
        public static void ChangeMaxClickCount(this YIUISuperScrollListComponent self, int count, bool reset = true)
        {
            self.ClearSelect(reset);
            self.m_MaxClickCount = Mathf.Max(1, count);
        }

        //点击前判断
        private static bool OnClickCheck(this YIUISuperScrollListComponent self, int index, LoopListViewItem2 item)
        {
            if (!self.GetItemOnClickInit(item)) return false;

            if (!self.GetItemClickCheck(item)) return true;

            var select = !self.m_OnClickItemHashSet.Contains(index);

            return YIUISuperScrollListHelper.OnClickCheck(self.GetItemClickCheckType(item), self.OwnerEntity, item.OwnerEntity, self, index, select);
        }

        //传入对象 选中目标
        public static void OnClickItem(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            if (!self.GetItemOnClickInit(item)) return;

            var index = self.GetItemIndex(item);
            if (index < 0)
            {
                Debug.LogError($"无法选中一个不在显示中的对象");
                return;
            }

            if (!self.OnClickCheck(index, item))
            {
                return;
            }

            var banSelect = self.GetItemBanSelect(item);

            var select = !banSelect && self.OnClickItemQueueEnqueue(index);

            self.OnClickItem(index, item, select);
        }

        private static bool OnClickItemQueueEnqueue(this YIUISuperScrollListComponent self, int index)
        {
            if (self.m_OnClickItemHashSet.Contains(index))
            {
                if (self.m_RepetitionCancel)
                {
                    self.RemoveSelectIndex(index);
                    return false;
                }
                else
                {
                    return true;
                }
            }

            if (self.m_OnClickItemQueue.Count >= self.m_MaxClickCount)
            {
                if (self.m_AutoCancelLast)
                {
                    self.OnClickItemQueuePeek();
                }
                else
                {
                    return false;
                }
            }

            self.OnClickItemHashSetAdd(index);
            self.m_OnClickItemQueue.Enqueue(index);
            return true;
        }

        private static void SetDefaultSelect(this YIUISuperScrollListComponent self, int index)
        {
            self.OnClickItemQueueEnqueue(index);
        }

        private static void SetDefaultSelect(this YIUISuperScrollListComponent self, List<int> indexs)
        {
            foreach (var index in indexs)
            {
                self.SetDefaultSelect(index);
            }
        }

        private static bool GetItemOnClickInit(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            return self.m_OnClickInit.GetValueOrDefault(item.ResName, false);
        }

        private static bool GetItemOnClickInit(this YIUISuperScrollListComponent self, int prefabIndex)
        {
            return self.m_OnClickInit.GetValueOrDefault(self.Owner.GetItemPoolResName(prefabIndex), false);
        }

        private static string GetItemClickEventName(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            return self.m_ItemClickEventName.GetValueOrDefault(item.ResName, null);
        }

        private static string GetItemClickEventName(this YIUISuperScrollListComponent self, int prefabIndex)
        {
            return self.m_ItemClickEventName.GetValueOrDefault(self.Owner.GetItemPoolResName(prefabIndex), null);
        }

        private static bool GetItemClickCheck(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            return self.m_ItemClickCheck.GetValueOrDefault(item.ResName, false);
        }

        private static bool GetItemClickCheck(this YIUISuperScrollListComponent self, int prefabIndex)
        {
            return self.m_ItemClickCheck.GetValueOrDefault(self.Owner.GetItemPoolResName(prefabIndex), false);
        }

        private static bool GetItemBanSelect(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            return self.m_ItemBanSelect.GetValueOrDefault(item.ResName, false);
        }

        private static void OnClickItem(this YIUISuperScrollListComponent self, int index, LoopListViewItem2 item, bool select)
        {
            if (!self.GetItemOnClickInit(item)) return;
            YIUISuperScrollListHelper.OnClick(self.GetItemClickType(item), self.OwnerEntity, item.OwnerEntity, self, index, select);
        }

        private static void AddOnClickEvent(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            if (!self.GetItemOnClickInit(item)) return;

            var eventTable = item.YIUICDETable?.EventTable;
            if (eventTable == null)
            {
                Debug.LogError($"目标item 没有 event表 请检查");
                return;
            }

            var clickEventName = self.GetItemClickEventName(item);
            var uEventClickItem = eventTable.FindEvent<UIEventP0>(clickEventName);
            if (uEventClickItem == null)
            {
                Debug.LogError($"当前监听的事件未找到 请检查 {eventTable.gameObject.name} 中是否有这个事件 {clickEventName}");
                self.m_OnClickInit[item.ResName] = false;
            }
            else
            {
                EntityRef<YIUISuperScrollListComponent> selfRef = self;
                uEventClickItem.Add(() =>
                {
                    if (selfRef.Entity == null)
                    {
                        Log.Error($"OnClickItem 事件回调时，YIUISuperScrollListComponent 为空");
                        return;
                    }

                    selfRef.Entity.OnClickItem(item);
                });
            }
        }

        private static void OnClickItemQueuePeek(this YIUISuperScrollListComponent self)
        {
            var index = self.m_OnClickItemQueue.Dequeue();
            self.OnClickItemHashSetRemove(index);
            if (index < self.ItemStart || index >= self.ItemEnd) return;
            var item = self.GetItemByIndex(index);
            if (item != null)
            {
                self.OnClickItem(index, item, false);
            }
        }

        private static void OnClickItemHashSetAdd(this YIUISuperScrollListComponent self, int index)
        {
            self.m_OnClickItemHashSet.Add(index);
        }

        private static void OnClickItemHashSetRemove(this YIUISuperScrollListComponent self, int index)
        {
            self.m_OnClickItemHashSet.Remove(index);
        }

        private static void RemoveSelectIndex(this YIUISuperScrollListComponent self, int index)
        {
            self.OnClickItemHashSetRemove(index);

            var count = self.m_OnClickItemQueue.Count;
            if (count == 0) return;

            if (count == 1)
            {
                var val = self.m_OnClickItemQueue.Peek();
                if (val == index)
                {
                    self.m_OnClickItemQueue.Dequeue();
                }
            }
            else
            {
                using var listTemp = ListComponent<int>.Create();

                for (var i = 0; i < count; i++)
                {
                    var val = self.m_OnClickItemQueue.Dequeue();
                    if (val != index)
                    {
                        listTemp.Add(val);
                    }
                }

                foreach (var val in listTemp)
                {
                    self.m_OnClickItemQueue.Enqueue(val);
                }
            }
        }

        private static Type GetItemClickType(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            var resName = item.ResName;
            if (!self.m_ClickSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollListOnClick<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_ClickSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static Type GetItemClickCheckType(this YIUISuperScrollListComponent self, LoopListViewItem2 item)
        {
            var resName = item.ResName;
            if (!self.m_ClickCheckSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollListOnClickCheck<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_ClickCheckSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }
    }
}