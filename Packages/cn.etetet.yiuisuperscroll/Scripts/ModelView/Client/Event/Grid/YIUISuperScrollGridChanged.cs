//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollGridChanged
    {
        void Changed(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, bool select);
    }

    public interface IYIUISuperScrollGridChanged<in T1, in T2> : ISystemType, IYIUISuperScrollGridChanged
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollGridChangedSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollGridChanged<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollGridChanged<T1, T2>);
        }

        void IYIUISuperScrollGridChanged.Changed(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, bool select)
        {
            YIUISuperScrollGridChanged((T1)self, (T2)item, superScrollGrid, index, select);
        }

        protected abstract void YIUISuperScrollGridChanged(T1 self, T2 item, YIUISuperScrollGridComponent superScrollGrid, int index, bool select);
    }
}