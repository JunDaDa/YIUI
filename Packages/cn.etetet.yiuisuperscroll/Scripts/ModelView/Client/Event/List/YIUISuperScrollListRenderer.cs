//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollListRenderer
    {
        /// <summary>
        /// 渲染数据项
        /// </summary>
        /// <param name="self">渲染器实体</param>
        /// <param name="item">显示对象</param>
        /// <param name="superScrollList">是哪个列表如果有多个时用这个来区分</param>
        /// <param name="index">数据的索引</param>
        /// <param name="select">是否被选中</param>
        void Renderer(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }

    public interface IYIUISuperScrollListRenderer<in T1, in T2> : ISystemType, IYIUISuperScrollListRenderer
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollListRendererSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollListRenderer<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollListRenderer<T1, T2>);
        }

        void IYIUISuperScrollListRenderer.Renderer(Entity self, Entity item, YIUISuperScrollListComponent superScrollList, int index, bool select)
        {
            YIUISuperScrollListRenderer((T1)self, (T2)item, superScrollList, index, select);
        }

        protected abstract void YIUISuperScrollListRenderer(T1 self, T2 item, YIUISuperScrollListComponent superScrollList, int index, bool select);
    }
}