//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollGridRenderer
    {
        /// <summary>
        /// 渲染数据项
        /// </summary>
        /// <param name="self">渲染器实体</param>
        /// <param name="item">显示对象</param>
        /// <param name="superScrollGrid">是哪个网格如果有多个时用这个来区分</param>
        /// <param name="index">数据的索引</param>
        /// <param name="row">行索引</param>
        /// <param name="column">列索引</param>
        /// <param name="select">是否被选中</param>
        void Renderer(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select);
    }

    public interface IYIUISuperScrollGridRenderer<in T1, in T2> : ISystemType, IYIUISuperScrollGridRenderer
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollGridRendererSystem<T1, T2, T3, T4, T5, T6, T7> : SystemObject, IYIUISuperScrollGridRenderer<T1, T2>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity, IYIUIBind, IYIUIInitialize
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollGridRenderer<T1, T2>);
        }

        void IYIUISuperScrollGridRenderer.Renderer(Entity self, Entity item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select)
        {
            YIUISuperScrollGridRenderer((T1)self, (T2)item, superScrollGrid, index, row, column, select);
        }

        protected abstract void YIUISuperScrollGridRenderer(T1 self, T2 item, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column, bool select);
    }
}