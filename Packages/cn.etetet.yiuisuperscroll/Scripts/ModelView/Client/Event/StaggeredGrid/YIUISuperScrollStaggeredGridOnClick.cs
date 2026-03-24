//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollStaggeredGridOnClick
    {
        /// <summary>
        /// 点击事件
        /// 调用SetOnClick方法设置点击事件信息后生效
        /// </summary>
        void OnClick(Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select);
    }

    public interface IYIUISuperScrollStaggeredGridOnClick<in T1, in T2> : ISystemType, IYIUISuperScrollStaggeredGridOnClick
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollStaggeredGridOnClickSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollStaggeredGridOnClick<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollStaggeredGridOnClick<T1, T2>);
        }

        void IYIUISuperScrollStaggeredGridOnClick.OnClick(Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select)
        {
            YIUISuperScrollStaggeredGridOnClick((T1)self, (T2)item, superScrollStaggeredGrid, index, select);
        }

        protected abstract void YIUISuperScrollStaggeredGridOnClick(T1 self, T2 item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select);
    }
}