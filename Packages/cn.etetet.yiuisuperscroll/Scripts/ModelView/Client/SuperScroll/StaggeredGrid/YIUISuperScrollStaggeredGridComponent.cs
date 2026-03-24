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
    public partial class YIUISuperScrollStaggeredGridComponent : Entity, IAwake<LoopStaggeredGridView, GridViewLayoutParam>, IDestroy
    {
        public EntityRef<Entity> m_OwnerEntity;
        public Entity OwnerEntity => m_OwnerEntity;

        public EntityRef<YIUIBindComponent> m_YIUIBindRef;
        public YIUIBindComponent YIUIBind => m_YIUIBindRef;

        public LoopStaggeredGridView m_Owner;
        public LoopStaggeredGridView Owner => m_Owner;

        public Type m_GetPrefabIndexType;

        public Type GetPrefabIndexType
        {
            get
            {
                if (m_GetPrefabIndexType == null)
                {
                    m_GetPrefabIndexType = typeof(IYIUISuperScrollStaggeredGridGetPrefab<>).MakeGenericType(OwnerEntity?.GetType());
                }

                return m_GetPrefabIndexType;
            }
        }

        // 通用的渲染器和点击事件系统类型
        public readonly Dictionary<string, Type> m_RendererSystemTypeDict = new();
        public readonly Dictionary<string, Type> m_ClickSystemTypeDict = new();
        public readonly Dictionary<string, Type> m_ClickCheckSystemTypeDict = new();
        public readonly Dictionary<string, Type> m_GetItemSizeSystemTypeDict = new();
    }
}