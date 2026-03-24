using Sirenix.OdinInspector;
using UnityEngine;

namespace SuperScrollView
{
    public partial class LoopListView2
    {
        [SerializeField]
        [LabelText("最大可点击数")]
        [MinValue(1)]
        public int u_MaxClickCount = 1;

        [SerializeField]
        [LabelText("自动取消上一个选择")]
        [MinValue(1)]
        public bool u_AutoCancelLast = true;

        [SerializeField]
        [LabelText("重复点击则取消")]
        public bool u_RepetitionCancel;

        public LoopListViewItem2 NewListViewItem(int prefabIndex)
        {
            if (prefabIndex < 0 || prefabIndex >= mItemPoolList.Count)
            {
                Debug.LogError($"索引越界:{prefabIndex}, 当前长度:{mItemPoolList.Count}");
                return null;
            }

            var pool = mItemPoolList[prefabIndex];
            LoopListViewItem2 item = pool.GetItem(mCurCreatingItemIndex);
            RectTransform rf = item.CachedRectTransform;
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

        public int GetFirstItemIndex()
        {
            if (mItemList.Count == 0)
            {
                return -1;
            }

            return mItemList[0].ItemIndex;
        }

        public int GetLastItemIndex()
        {
            if (mItemList.Count == 0)
            {
                return -1;
            }

            return mItemList[^1].ItemIndex;
        }
    }
}