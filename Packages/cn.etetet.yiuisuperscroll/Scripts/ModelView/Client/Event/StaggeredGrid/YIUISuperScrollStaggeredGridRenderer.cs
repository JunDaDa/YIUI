//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollStaggeredGridRenderer
    {
        /// <summary>
        /// 渲染数据项
        /// </summary>
        /// <param name="self">渲染器实体</param>
        /// <param name="item">显示对象</param>
        /// <param name="superScrollStaggeredGrid">是哪个错列网格如果有多个时用这个来区分</param>
        /// <param name="index">数据的索引</param>
        /// <param name="select">是否被选中</param>
        void Renderer(Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select);
    }

    public interface IYIUISuperScrollStaggeredGridRenderer<in T1, in T2> : ISystemType, IYIUISuperScrollStaggeredGridRenderer
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollStaggeredGridRendererSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollStaggeredGridRenderer<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollStaggeredGridRenderer<T1, T2>);
        }

        void IYIUISuperScrollStaggeredGridRenderer.Renderer(Entity self, Entity item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select)
        {
            YIUISuperScrollStaggeredGridRenderer((T1)self, (T2)item, superScrollStaggeredGrid, index, select);
        }

        protected abstract void YIUISuperScrollStaggeredGridRenderer(T1 self, T2 item, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index, bool select);
    }
}