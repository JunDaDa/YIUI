//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollStaggeredGridGetPrefab
    {
        /// <summary>
        /// 获取预制体索引
        /// </summary>
        /// <param name="self">实体</param>
        /// <param name="superScrollStaggeredGrid">是哪个错列网格如果有多个时用这个来区分</param>
        /// <param name="index">当前被渲染的项目索引</param>
        /// <returns>返回这个索引对应的预制体</returns>
        int GetPrefabIndex(Entity self, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index);
    }

    public interface IYIUISuperScrollStaggeredGridGetPrefab<in T1> : ISystemType, IYIUISuperScrollStaggeredGridGetPrefab
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollStaggeredGridGetPrefabSystem<T1, T2, T3> : SystemObject, IYIUISuperScrollStaggeredGridGetPrefab<T1>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollStaggeredGridGetPrefab<T1>);
        }

        int IYIUISuperScrollStaggeredGridGetPrefab.GetPrefabIndex(Entity self, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index)
        {
            return YIUISuperScrollStaggeredGridGetPrefab((T1)self, superScrollStaggeredGrid, index);
        }

        protected abstract int YIUISuperScrollStaggeredGridGetPrefab(T1 self, YIUISuperScrollStaggeredGridComponent superScrollStaggeredGrid, int index);
    }
}