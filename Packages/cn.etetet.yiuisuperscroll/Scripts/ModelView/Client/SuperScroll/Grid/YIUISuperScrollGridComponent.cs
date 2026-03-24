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
    public partial class YIUISuperScrollGridComponent : Entity, IAwake<LoopGridView>, IDestroy
    {
        public EntityRef<Entity> m_OwnerEntity;
        public Entity OwnerEntity => m_OwnerEntity;

        public EntityRef<YIUIBindComponent> m_YIUIBindRef;
        public YIUIBindComponent YIUIBind => m_YIUIBindRef;

        public LoopGridView m_Owner;
        public LoopGridView Owner => m_Owner;

        public Type m_GetPrefabIndexType;

        public Type GetPrefabIndexType
        {
            get
            {
                if (m_GetPrefabIndexType == null)
                {
                    m_GetPrefabIndexType = typeof(IYIUISuperScrollGridGetPrefab<>).MakeGenericType(OwnerEntity?.GetType());
                }

                return m_GetPrefabIndexType;
            }
        }

        public readonly Dictionary<string, Type> m_RendererSystemTypeDict = new();
    }
}