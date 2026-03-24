using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    public partial class SuperScrollListViewLeftToRightDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        public EntityRef<YIUISuperScrollListComponent> m_ListScrollRef;
        public YIUISuperScrollListComponent ListScroll => m_ListScrollRef;

        public DataSourceMgr<SuperScrollView.ItemData> mDataSourceMgr;
    }
}