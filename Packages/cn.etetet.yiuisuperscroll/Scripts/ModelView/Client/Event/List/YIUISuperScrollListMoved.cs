//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollListMoved
    {
        void Moved(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }

    public interface IYIUISuperScrollListMoved<in T1, in T2> : ISystemType, IYIUISuperScrollListMoved
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollListMovedSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollListMoved<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollListMoved<T1, T2>);
        }

        void IYIUISuperScrollListMoved.Moved(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            YIUISuperScrollListMoved((T1)self, (T2)item, superScrollList, index, select);
        }

        protected abstract void YIUISuperScrollListMoved(T1 self, T2 item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }
}