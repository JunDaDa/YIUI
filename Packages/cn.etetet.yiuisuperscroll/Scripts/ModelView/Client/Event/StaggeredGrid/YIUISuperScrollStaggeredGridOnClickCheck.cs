//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollStaggeredGridOnClickCheck
    {
        /// <summary>
        /// 点击之前调用 点击事件检查 
        /// 调用SetOnClickCheck方法设置点击事件信息后生效
        /// </summary>
        bool OnClickCheck(Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select);
    }

    public interface IYIUISuperScrollStaggeredGridOnClickCheck<in T1, in T2> : ISystemType, IYIUISuperScrollStaggeredGridOnClickCheck
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollStaggeredGridOnClickCheckSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollStaggeredGridOnClickCheck<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollStaggeredGridOnClickCheck<T1, T2>);
        }

        bool IYIUISuperScrollStaggeredGridOnClickCheck.OnClickCheck(Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select)
        {
            return YIUISuperScrollStaggeredGridOnClickCheck((T1)self, (T2)item, superScrollStaggeredGrid, index, select);
        }

        protected abstract bool YIUISuperScrollStaggeredGridOnClickCheck(T1 self, T2 item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select);
    }
}