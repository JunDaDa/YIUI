//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollGridOnClickCheck
    {
        /// <summary>
        /// 点击之前调用 点击事件检查 
        /// 调用SetOnClickCheck方法设置点击事件信息后生效
        /// </summary>
        bool OnClickCheck(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select);
    }

    public interface IYIUISuperScrollGridOnClickCheck<in T1, in T2> : ISystemType, IYIUISuperScrollGridOnClickCheck
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollGridOnClickCheckSystem<T1, T2, T3, T4, T5, T6, T7> : SystemObject, IYIUISuperScrollGridOnClickCheck<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollGridOnClickCheck<T1, T2>);
        }

        bool IYIUISuperScrollGridOnClickCheck.OnClickCheck(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            return YIUISuperScrollGridOnClickCheck((T1)self, (T2)item, superScrollGrid, index, row, column, select);
        }

        protected abstract bool YIUISuperScrollGridOnClickCheck(T1 self, T2 item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select);
    }
}