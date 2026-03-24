//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollGridGetPrefab
    {
        /// <summary>
        /// 获取预制体索引
        /// </summary>
        /// <param name="self">实体</param>
        /// <param name="superScrollGrid">是哪个网格如果有多个时用这个来区分</param>
        /// <param name="index">当前被渲染的项目索引</param>
        /// <param name="row">行索引</param>
        /// <param name="column">列索引</param>
        /// <returns>返回这个索引对应的预制体</returns>
        int GetPrefabIndex(Entity self, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column);
    }

    public interface IYIUISuperScrollGridGetPrefab<in T1> : ISystemType, IYIUISuperScrollGridGetPrefab
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollGridGetPrefabSystem<T1, T2, T3, T4, T5> : SystemObject, IYIUISuperScrollGridGetPrefab<T1>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollGridGetPrefab<T1>);
        }

        int IYIUISuperScrollGridGetPrefab.GetPrefabIndex(Entity self, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column)
        {
            return YIUISuperScrollGridGetPrefab((T1)self, superScrollGrid, index, row, column);
        }

        protected abstract int YIUISuperScrollGridGetPrefab(T1 self, YIUISuperScrollGridComponent superScrollGrid, int index, int row, int column);
    }
}