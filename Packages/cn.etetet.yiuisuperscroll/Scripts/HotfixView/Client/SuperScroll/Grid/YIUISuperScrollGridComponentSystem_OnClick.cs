using System;
using System.Collections.Generic;
using SuperScrollView;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    [FriendOf(typeof(YIUISuperScrollGridComponent))]
    public static partial class YIUISuperScrollGridComponentSystem
    {
        static partial void AwakeOnClick(this YIUISuperScrollGridComponent self)
        {
            self.m_MaxClickCount = Mathf.Max(1, self.Owner.u_MaxClickCount);
            self.m_RepetitionCancel = self.Owner.u_RepetitionCancel;
            self.m_AutoCancelLast = self.Owner.u_AutoCancelLast;
            self.m_OnClickItemQueue.Clear();
            self.m_OnClickItemHashSet.Clear();
        }

        public static void SetOnClickAll(this YIUISuperScrollGridComponent self, string itemClickEventName = YIUISuperScrollGridComponent.ClickItemEvent)
        {
            for (int index = 0; index < self.Owner.ItemPrefabDataList.Count; index++)
            {
                self.SetOnClick(index, itemClickEventName);
            }
        }

        public static void SetOnClick(this YIUISuperScrollGridComponent self, int prefabIndex = 0, string itemClickEventName = YIUISuperScrollGridComponent.ClickItemEvent)
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

        public static void SetOnClickCheck(this YIUISuperScrollGridComponent self, int prefabIndex = 0, bool value = true)
        {
            if (!self.GetItemOnClickInit(prefabIndex))
            {
                Debug.LogError($"有Click 才可以,有检查");
                return;
            }

            var resName = self.Owner.GetItemPoolResName(prefabIndex);
            self.m_ItemClickCheck[resName] = value;
        }

        public static void SetBanSelect(this YIUISuperScrollGridComponent self, int prefabIndex = 0, bool value = true)
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
        public static void ChangeAutoCancelLast(this YIUISuperScrollGridComponent self, bool autoCancelLast)
        {
            self.m_AutoCancelLast = autoCancelLast;
        }

        //动态改变 重复选择 则取消选择
        public static void ChangeRepetitionCancel(this YIUISuperScrollGridComponent self, bool repetitionCancel)
        {
            self.m_RepetitionCancel = repetitionCancel;
        }

        //动态改变 最大可选数量
        public static void ChangeMaxClickCount(this YIUISuperScrollGridComponent self, int count, bool reset = true)
        {
            self.ClearSelect(reset);
            self.m_MaxClickCount = Mathf.Max(1, count);
        }

        //点击前判断
        private static bool OnClickCheck(this YIUISuperScrollGridComponent self, int itemIndex, LoopGridViewItem item)
        {
            if (!self.GetItemOnClickInit(item)) return false;

            if (!self.GetItemClickCheck(item)) return true;

            var select = !self.m_OnClickItemHashSet.Contains(itemIndex);

            return YIUISuperScrollGridHelper.OnClickCheck(self.GetItemClickCheckType(item), self.OwnerEntity, item.OwnerEntity, self, itemIndex, item.Row, item.Column, select);
        }

        //传入对象 选中目标
        public static void OnClickItem(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            if (!self.GetItemOnClickInit(item)) return;

            var itemIndex = self.GetItemIndex(item);
            if (itemIndex < 0)
            {
                Debug.LogError($"无法选中一个不在显示中的对象");
                return;
            }

            if (!self.OnClickCheck(itemIndex, item))
            {
                return;
            }

            var banSelect = self.GetItemBanSelect(item);

            var select = !banSelect && self.OnClickItemQueueEnqueue(itemIndex);

            self.OnClickItem(itemIndex, item, select);
        }

        // 通过行列选中目标
        public static void OnClickItemByRowColumn(this YIUISuperScrollGridComponent self, int row, int column)
        {
            var item = self.GetItemByRowColumn(row, column);
            if (item != null)
            {
                self.OnClickItem(item);
            }
        }

        // 通过索引选中目标
        public static void OnClickItemByIndex(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            var item = self.GetItemByIndex(itemIndex);
            if (item != null)
            {
                self.OnClickItem(item);
            }
        }

        private static bool OnClickItemQueueEnqueue(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            if (self.m_OnClickItemHashSet.Contains(itemIndex))
            {
                if (self.m_RepetitionCancel)
                {
                    self.RemoveSelectIndex(itemIndex);
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

            self.OnClickItemHashSetAdd(itemIndex);
            self.m_OnClickItemQueue.Enqueue(itemIndex);
            return true;
        }

        private static void SetDefaultSelect(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            self.OnClickItemQueueEnqueue(itemIndex);
        }

        private static void SetDefaultSelect(this YIUISuperScrollGridComponent self, List<int> itemIndexes)
        {
            foreach (var itemIndex in itemIndexes)
            {
                self.SetDefaultSelect(itemIndex);
            }
        }

        private static bool GetItemOnClickInit(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            return self.m_OnClickInit.GetValueOrDefault(item.ResName, false);
        }

        private static bool GetItemOnClickInit(this YIUISuperScrollGridComponent self, int prefabIndex)
        {
            return self.m_OnClickInit.GetValueOrDefault(self.Owner.GetItemPoolResName(prefabIndex), false);
        }

        private static string GetItemClickEventName(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            return self.m_ItemClickEventName.GetValueOrDefault(item.ResName, null);
        }

        private static string GetItemClickEventName(this YIUISuperScrollGridComponent self, int prefabIndex)
        {
            return self.m_ItemClickEventName.GetValueOrDefault(self.Owner.GetItemPoolResName(prefabIndex), null);
        }

        private static bool GetItemClickCheck(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            return self.m_ItemClickCheck.GetValueOrDefault(item.ResName, false);
        }

        private static bool GetItemClickCheck(this YIUISuperScrollGridComponent self, int prefabIndex)
        {
            return self.m_ItemClickCheck.GetValueOrDefault(self.Owner.GetItemPoolResName(prefabIndex), false);
        }

        private static bool GetItemBanSelect(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            return self.m_ItemBanSelect.GetValueOrDefault(item.ResName, false);
        }

        private static void OnClickItem(this YIUISuperScrollGridComponent self, int itemIndex, LoopGridViewItem item, bool select)
        {
            if (!self.GetItemOnClickInit(item)) return;
            YIUISuperScrollGridHelper.OnClick(self.GetItemClickType(item), self.OwnerEntity, item.OwnerEntity, self, itemIndex, item.Row, item.Column, select);
        }

        private static void AddOnClickEvent(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
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
                EntityRef<YIUISuperScrollGridComponent> selfRef = self;
                uEventClickItem.Add(() =>
                {
                    if (selfRef.Entity == null)
                    {
                        Log.Error($"OnClickItem 事件回调时，YIUISuperScrollGridComponent 为空");
                        return;
                    }

                    selfRef.Entity.OnClickItem(item);
                });
            }
        }

        private static void OnClickItemQueuePeek(this YIUISuperScrollGridComponent self)
        {
            var itemIndex = self.m_OnClickItemQueue.Dequeue();
            self.OnClickItemHashSetRemove(itemIndex);

            if (itemIndex < self.ItemStart || itemIndex > self.ItemEnd) return;

            var item = self.GetItemByIndex(itemIndex);
            if (item != null)
            {
                self.OnClickItem(itemIndex, item, false);
            }
        }

        private static void OnClickItemHashSetAdd(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            self.m_OnClickItemHashSet.Add(itemIndex);
        }

        private static void OnClickItemHashSetRemove(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            self.m_OnClickItemHashSet.Remove(itemIndex);
        }

        private static void RemoveSelectIndex(this YIUISuperScrollGridComponent self, int itemIndex)
        {
            self.OnClickItemHashSetRemove(itemIndex);

            var count = self.m_OnClickItemQueue.Count;
            if (count == 0) return;

            if (count == 1)
            {
                var val = self.m_OnClickItemQueue.Peek();
                if (val == itemIndex)
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
                    if (val != itemIndex)
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

        private static Type GetItemClickType(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            var resName = item.ResName;
            if (!self.m_ClickSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollGridOnClick<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_ClickSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }

        private static Type GetItemClickCheckType(this YIUISuperScrollGridComponent self, LoopGridViewItem item)
        {
            var resName = item.ResName;
            if (!self.m_ClickCheckSystemTypeDict.TryGetValue(resName, out var getType))
            {
                getType = typeof(IYIUISuperScrollGridOnClickCheck<,>).MakeGenericType(self.OwnerEntity?.GetType(), item.OwnerEntity?.GetType());
                self.m_ClickCheckSystemTypeDict.Add(resName, getType);
            }

            return getType;
        }
    }
}