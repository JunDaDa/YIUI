using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    public partial class SuperScrollStaggeredViewTopToBottomDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        public EntityRef<YIUISuperScrollStaggeredGridComponent> m_StaggeredGridScrollRef;
        public YIUISuperScrollStaggeredGridComponent StaggeredGridScroll => m_StaggeredGridScrollRef;

        public DataSourceMgr<SuperScrollView.ItemData> m_DataSourceMgr;
        public int[] m_ItemHeightArrayForDemo = null;
        public float m_MinHeight = 260.0f;
        public int m_Count = 100;
    }
}