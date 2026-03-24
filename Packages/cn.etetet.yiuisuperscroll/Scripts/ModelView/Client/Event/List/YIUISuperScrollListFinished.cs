//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollListFinished
    {
        void Finished(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }

    public interface IYIUISuperScrollListFinished<in T1, in T2> : ISystemType, IYIUISuperScrollListFinished
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollListFinishedSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollListFinished<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollListFinished<T1, T2>);
        }

        void IYIUISuperScrollListFinished.Finished(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            YIUISuperScrollListFinished((T1)self, (T2)item, superScrollList, index, select);
        }

        protected abstract void YIUISuperScrollListFinished(T1 self, T2 item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }
}