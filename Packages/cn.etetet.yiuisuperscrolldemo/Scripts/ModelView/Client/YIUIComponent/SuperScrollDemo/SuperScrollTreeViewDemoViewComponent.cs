using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    public partial class SuperScrollTreeViewDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        public EntityRef<YIUISuperScrollListComponent> m_TreeScrollRef;
        public YIUISuperScrollListComponent TreeScroll => m_TreeScrollRef;
        public readonly TreeViewDataSourceMgr<SuperScrollView.ItemData> m_TreeViewDataSourceMgr = new();
        public readonly TreeViewItemCountMgr m_TreeItemCountMgr = new();
        public int m_CurrentSelectItemIndex = -1;
        public int m_CurrentSelectIndex = -1;
    }
}