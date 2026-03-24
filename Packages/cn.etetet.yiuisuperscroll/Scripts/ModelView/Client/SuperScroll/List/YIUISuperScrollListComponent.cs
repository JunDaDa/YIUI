//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using SuperScrollView;

namespace ET.Client
{
    [ChildOf]
    public partial class YIUISuperScrollListComponent : Entity, IAwake<LoopListView2>, IDestroy
    {
        public EntityRef<Entity> m_OwnerEntity;
        public Entity OwnerEntity => m_OwnerEntity;

        public EntityRef<YIUIBindComponent> m_YIUIBindRef;
        public YIUIBindComponent YIUIBind => m_YIUIBindRef;

        public LoopListView2 m_Owner;
        public LoopListView2 Owner => m_Owner;

        public Type m_GetPrefabIndexType;

        public Type GetPrefabIndexType
        {
            get
            {
                if (m_GetPrefabIndexType == null)
                {
                    m_GetPrefabIndexType = typeof(IYIUISuperScrollListGetPrefab<>).MakeGenericType(OwnerEntity?.GetType());
                }

                return m_GetPrefabIndexType;
            }
        }

        public readonly Dictionary<string, Type> m_RendererSystemTypeDict = new();
    }
}