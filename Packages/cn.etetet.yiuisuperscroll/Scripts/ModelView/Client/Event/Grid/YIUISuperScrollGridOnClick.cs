//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollGridOnClick
    {
        /// <summary>
        /// 点击事件
        /// 调用SetOnClick方法设置点击事件信息后生效
        /// </summary>
        void OnClick(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select);
    }

    public interface IYIUISuperScrollGridOnClick<in T1, in T2> : ISystemType, IYIUISuperScrollGridOnClick
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollGridOnClickSystem<T1, T2, T3, T4, T5, T6, T7> : SystemObject, IYIUISuperScrollGridOnClick<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollGridOnClick<T1, T2>);
        }

        void IYIUISuperScrollGridOnClick.OnClick(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            YIUISuperScrollGridOnClick((T1)self, (T2)item, superScrollGrid, index, row, column, select);
        }

        protected abstract void YIUISuperScrollGridOnClick(T1 self, T2 item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select);
    }
}