using ET;
using YIUIFramework;

namespace SuperScrollView
{
    public partial class LoopListViewItem2
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
    }
}