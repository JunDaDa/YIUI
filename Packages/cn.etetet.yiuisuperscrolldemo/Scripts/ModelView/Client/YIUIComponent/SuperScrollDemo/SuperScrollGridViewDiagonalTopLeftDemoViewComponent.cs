using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    public partial class SuperScrollGridViewDiagonalTopLeftDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        public EntityRef<YIUISuperScrollGridComponent> m_GridScrollRef;
        public YIUISuperScrollGridComponent GridScroll => m_GridScrollRef;

        public DataSourceMgr<SuperScrollView.ItemData> mDataSourceMgr;
    }
}