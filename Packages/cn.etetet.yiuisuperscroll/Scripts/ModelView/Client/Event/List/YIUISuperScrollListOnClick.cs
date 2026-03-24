//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollListOnClick
    {
        /// <summary>
        /// 点击事件
        /// 调用SetOnClick方法设置点击事件信息后生效
        /// </summary>
        void OnClick(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }

    public interface IYIUISuperScrollListOnClick<in T1, in T2> : ISystemType, IYIUISuperScrollListOnClick
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollListOnClickSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollListOnClick<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollListOnClick<T1, T2>);
        }

        void IYIUISuperScrollListOnClick.OnClick(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            YIUISuperScrollListOnClick((T1)self, (T2)item, superScrollList, index, select);
        }

        protected abstract void YIUISuperScrollListOnClick(T1 self, T2 item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }
}