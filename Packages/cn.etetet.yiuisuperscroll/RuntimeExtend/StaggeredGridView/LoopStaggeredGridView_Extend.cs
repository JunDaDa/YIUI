using Sirenix.OdinInspector;
using UnityEngine;

namespace SuperScrollView
{
    public partial class LoopStaggeredGridView
    {
        [SerializeField]
        [LabelText("最大可点击数")]
        [MinValue(1)]
        public int u_MaxClickCount = 1;

        [SerializeField]
        [LabelText("自动取消上一个选择")]
        public bool u_AutoCancelLast = true;

        [SerializeField]
        [LabelText("重复点击则取消")]
        public bool u_RepetitionCancel;

        public LoopStaggeredGridViewItem NewListViewItem(int prefabIndex)
        {
            if (prefabIndex < 0 || prefabIndex >= ItemPrefabDataList.Count)
            {
                Debug.LogError($"索引越界:{prefabIndex}, 当前长度:{ItemPrefabDataList.Count}");
                return null;
            }

            var pool = mItemPoolList[prefabIndex];
            var item = pool.GetItem(mCurCreatingItemIndex);
            var rf = item.CachedRectTransform;
            rf.SetParent(mContainerTrans);
            rf.localScale = Vector3.one;
            rf.anchoredPosition3D = Vector3.zero;
            rf.localEulerAngles = Vector3.zero;
            item.ParentListView = this;
            return item;
        }

        public string GetItemPoolResName(int prefabIndex)
        {
            if (prefabIndex < 0 || prefabIndex >= mItemPoolList.Count)
            {
                Debug.LogError($"索引越界:{prefabIndex}, 当前长度:{mItemPoolList.Count}");
                return null;
            }

            return mItemPoolList[prefabIndex].ResName;
        }

        /// <summary>
        /// 获取当前可见的第一个项目索引
        /// </summary>
        /// <returns>第一个可见项目的索引，如果没有可见项目则返回-1</returns>
        public int GetFirstShownItemIndex()
        {
            if (!IsInited || mItemGroupList?.Count == 0)
            {
                return -1;
            }

            var firstIndex = int.MaxValue;

            // 遍历所有组，找出最小的项目索引
            foreach (var group in mItemGroupList)
            {
                var groupItems = group.ItemList;
                if (groupItems.Count > 0)
                {
                    int groupFirstIndex = groupItems[0].ItemIndex;
                    if (groupFirstIndex < firstIndex)
                    {
                        firstIndex = groupFirstIndex;
                    }
                }
            }

            return firstIndex == int.MaxValue ? -1 : firstIndex;
        }

        /// <summary>
        /// 获取当前可见的最后一个项目索引
        /// </summary>
        /// <returns>最后一个可见项目的索引，如果没有可见项目则返回-1</returns>
        public int GetLastShownItemIndex()
        {
            if (!IsInited || mItemGroupList?.Count == 0)
            {
                return -1;
            }

            var lastIndex = -1;

            // 遍历所有组，找出最大的项目索引
            foreach (var group in mItemGroupList)
            {
                var groupItems = group.ItemList;
                if (groupItems.Count > 0)
                {
                    int groupLastIndex = groupItems[^1].ItemIndex;
                    if (groupLastIndex > lastIndex)
                    {
                        lastIndex = groupLastIndex;
                    }
                }
            }

            return lastIndex;
        }
    }
}