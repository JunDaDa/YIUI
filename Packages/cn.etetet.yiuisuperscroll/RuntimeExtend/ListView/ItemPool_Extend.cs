using YIUIFramework;

namespace SuperScrollView
{
    public partial class ItemPool
    {
        private UIBindCDETable m_CDETable;

        public UIBindCDETable YIUICDETable
        {
            get
            {
                if (m_CDETable == null)
                {
                    m_CDETable = this.mPrefabObj.GetComponent<UIBindCDETable>();
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