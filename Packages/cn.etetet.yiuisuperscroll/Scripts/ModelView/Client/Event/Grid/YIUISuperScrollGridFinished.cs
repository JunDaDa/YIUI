//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollGridFinished
    {
        void Finished(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, bool select);
    }

    public interface IYIUISuperScrollGridFinished<in T1, in T2> : ISystemType, IYIUISuperScrollGridFinished
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollGridFinishedSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollGridFinished<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollGridFinished<T1, T2>);
        }

        void IYIUISuperScrollGridFinished.Finished(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, bool select)
        {
            YIUISuperScrollGridFinished((T1)self, (T2)item, superScrollGrid, index, select);
        }

        protected abstract void YIUISuperScrollGridFinished(T1 self, T2 item, YIUISuperScrollGridComponent superScrollGrid, int index, bool select);
    }
}