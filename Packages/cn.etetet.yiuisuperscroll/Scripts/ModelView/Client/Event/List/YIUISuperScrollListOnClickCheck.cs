//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollListOnClickCheck
    {
        /// <summary>
        /// 点击之前调用 点击事件检查 
        /// 调用SetOnClickCheck方法设置点击事件信息后生效
        /// </summary>
        bool OnClickCheck(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }

    public interface IYIUISuperScrollListOnClickCheck<in T1, in T2> : ISystemType, IYIUISuperScrollListOnClickCheck
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollListOnClickCheckSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollListOnClickCheck<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollListOnClickCheck<T1, T2>);
        }

        bool IYIUISuperScrollListOnClickCheck.OnClickCheck(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            return YIUISuperScrollListOnClickCheck((T1)self, (T2)item, superScrollList, index, select);
        }

        protected abstract bool YIUISuperScrollListOnClickCheck(T1 self, T2 item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }
}