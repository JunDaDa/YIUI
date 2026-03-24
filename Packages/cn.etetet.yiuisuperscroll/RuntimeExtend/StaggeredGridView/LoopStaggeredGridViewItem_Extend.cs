using ET;
using YIUIFramework;

namespace SuperScrollView
{
    public partial class LoopStaggeredGridViewItem
    {
        private EntityRef<Entity> m_OwnerEntityRef;

        public Entity OwnerEntity => m_OwnerEntityRef;

        public bool SetOwnerEntity(Entity ownerEntity)
        {
            if (ownerEntity != null)
            {
                m_OwnerEntityRef = ownerEntity;
                return true;
            }
            else
            {
                m_OwnerEntityRef = default;
                return false;
            }
        }

        private UIBindCDETable m_CDETable;

        public UIBindCDETable YIUICDETable
        {
            get
            {
                if (m_CDETable == null)
                {
                    m_CDETable = GetComponent<UIBindCDETable>();
                }

                return m_CDETable;
            }
        }

        public string ResName
        {
            get
            {
                return YIUICDETable?.ResName;
            }
        }

        /// <summary>
        /// 获取项目在组中的索引数据
        /// </summary>
        public (int groupIndex, int indexInGroup) GetGroupIndexData()
        {
            if (ParentListView != null)
            {
                var indexData = ParentListView.GetItemIndexData(ItemIndex);
                return indexData != null ? (indexData.mGroupIndex, indexData.mIndexInGroup) : (-1, -1);
            }
            return (-1, -1);
        }

        /// <summary>
        /// 获取项目大小（考虑垂直/水平布局）
        /// </summary>
        public float GetItemSize()
        {
            return ItemSize;
        }

        /// <summary>
        /// 获取项目大小加间距
        /// </summary>
        public float GetItemSizeWithPadding()
        {
            return ItemSizeWithPadding;
        }

        /// <summary>
        /// 获取项目的各种位置信息
        /// </summary>
        public (float topY, float bottomY, float leftX, float rightX) GetPositionInfo()
        {
            return (TopY, BottomY, LeftX, RightX);
        }

        /// <summary>
        /// 判断项目是否在可见区域内
        /// </summary>
        public bool IsInViewPort()
        {
            if (ParentListView == null) return false;

            var viewPortSize = ParentListView.ViewPortSize;
            if (ParentListView.IsVertList)
            {
                var containerY = ParentListView.ContainerTrans.anchoredPosition.y;
                var itemTopY = TopY - containerY;
                var itemBottomY = BottomY - containerY;
                return itemBottomY <= viewPortSize && itemTopY >= 0;
            }
            else
            {
                var containerX = ParentListView.ContainerTrans.anchoredPosition.x;
                var itemLeftX = LeftX - containerX;
                var itemRightX = RightX - containerX;
                return itemLeftX <= viewPortSize && itemRightX >= 0;
            }
        }

        /// <summary>
        /// 获取与视口中心的距离
        /// </summary>
        public float GetDistanceWithViewPortCenter()
        {
            return DistanceWithViewPortSnapCenter;
        }
    }
}