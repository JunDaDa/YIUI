using Sirenix.OdinInspector;
using UnityEngine;

namespace SuperScrollView
{
    public partial class LoopGridView
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

        public LoopGridViewItem NewListViewItem(int prefabIndex)
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
            item.ParentGridView = this;
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

        public int GetFirstShownItemIndex()
        {
            if (!mListViewInited || mItemTotalCount == 0 || mItemGroupList.Count == 0)
            {
                return -1;
            }

            var row = mCurFrameItemRangeData.mMinRow;
            var column = mCurFrameItemRangeData.mMinColumn;

            return GetItemIndexByRowColumn(row, column);
        }

        public int GetLastShownItemIndex()
        {
            if (!mListViewInited || mItemTotalCount == 0 || mItemGroupList.Count == 0)
            {
                return -1;
            }

            var row = mCurFrameItemRangeData.mMaxRow;
            var column = mCurFrameItemRangeData.mMaxColumn;

            return GetItemIndexByRowColumn(row, column);
        }

        public RowColumnPair GetFirstShownRowColumn()
        {
            var firstIndex = GetFirstShownItemIndex();
            return firstIndex >= 0 ? GetRowColumnByItemIndex(firstIndex) : new RowColumnPair(-1, -1);
        }

        public RowColumnPair GetLastShownRowColumn()
        {
            var lastIndex = GetLastShownItemIndex();
            return lastIndex >= 0 ? GetRowColumnByItemIndex(lastIndex) : new RowColumnPair(-1, -1);
        }
    }
}