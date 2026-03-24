using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public partial class SuperScrollChatViewDemoViewComponent : Entity, IYIUIOpen<ParamVo>
    {
        public EntityRef<YIUISuperScrollListComponent> m_ChatScrollRef;
        public YIUISuperScrollListComponent ChatScroll => m_ChatScrollRef;
    }
}