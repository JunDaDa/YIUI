using ET;
using YIUIFramework;

namespace SuperScrollView
{
    public partial class LoopGridViewItem
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
        /// 获取在网格中的位置信息
        /// </summary>
        public RowColumnPair GetRowColumnPair()
        {
            return new RowColumnPair(Row, Column);
        }

        /// <summary>
        /// 判断是否为同一个网格位置
        /// </summary>
        public bool IsSamePosition(int row, int column)
        {
            return Row == row && Column == column;
        }

        /// <summary>
        /// 判断是否为同一个网格位置
        /// </summary>
        public bool IsSamePosition(RowColumnPair pair)
        {
            return Row == pair.mRow && Column == pair.mColumn;
        }
    }
}