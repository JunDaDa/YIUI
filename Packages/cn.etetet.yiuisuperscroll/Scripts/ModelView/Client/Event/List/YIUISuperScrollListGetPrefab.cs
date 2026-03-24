//------------------------------------------------------------
// Author: 亦亦
// Mail: 379338943@qq.com
// Data: 2023年2月12日
//------------------------------------------------------------

using System;

namespace ET.Client
{
    public interface IYIUISuperScrollListGetPrefab
    {
        /// <summary>
        /// 获取预制体索引
        /// </summary>
        /// <param name="self">实体</param>
        /// <param name="superScrollList">是哪个列表如果有多个时用这个来区分</param>
        /// <param name="index">当前被渲染的预制体索引</param>
        /// <returns>返回这个索引对应的预制体</returns>
        int GetPrefabIndex(Entity self, YIUISuperScrollListComponent superScrollList, int index);
    }

    public interface IYIUISuperScrollListGetPrefab<in T1> : ISystemType, IYIUISuperScrollListGetPrefab
    {
    }

    [EntitySystem]
    public abstract class YIUISuperScrollListGetPrefabSystem<T1, T2, T3> : SystemObject, IYIUISuperScrollListGetPrefab<T1>
            where T1 : Entity, IYIUIBind, IYIUIInitialize
            where T2 : Entity
    {
        Type ISystemType.Type()
        {
            return typeof(T1);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IYIUISuperScrollListGetPrefab<T1>);
        }

        int IYIUISuperScrollListGetPrefab.GetPrefabIndex(Entity self, YIUISuperScrollListComponent superScrollList, int index)
        {
            return YIUISuperScrollListGetPrefab((T1)self, superScrollList, index);
        }

        protected abstract int YIUISuperScrollListGetPrefab(T1 self, YIUISuperScrollListComponent superScrollList, int index);
    }
}