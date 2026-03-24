//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollStaggeredGridGetItemSize
    {
        /// <summary>
        /// 获取项目大小和间距
        /// StaggeredGridView 特有的接口，用于支持动态项目大小
        /// </summary>
        /// <param name="self">实体</param>
        /// <param name="superScrollStaggeredGrid">是哪个错列网格如果有多个时用这个来区分</param>
        /// <param name="index">项目索引</param>
        /// <returns>返回 (itemSize, itemPadding) 元组</returns>
        (float, float) GetItemSize(Entity self, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index);
    }

    public interface IYIUISuperScrollStaggeredGridGetItemSize<in T1> : ISystemType, IYIUISuperScrollStaggeredGridGetItemSize
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollStaggeredGridGetItemSizeSystem<T1, T2, T3> : SystemObject, IYIUISuperScrollStaggeredGridGetItemSize<T1>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollStaggeredGridGetItemSize<T1>);
        }

        (float, float) IYIUISuperScrollStaggeredGridGetItemSize.GetItemSize(Entity self, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index)
        {
            return YIUISuperScrollStaggeredGridGetItemSize((T1)self, superScrollStaggeredGrid, index);
        }

        protected abstract (float, float) YIUISuperScrollStaggeredGridGetItemSize(T1 self, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index);
    }
}