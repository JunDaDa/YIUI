using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 相加其他数据
    /// 自己与目标相加
    /// 与Copy最大的区别就是不会修改自身
    /// 而是得到一个新的相加后的数据
    /// 注意因为是创建的新数据 owner 为null 需要手动设置
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataExtend
    {
        public static NumericData Add(this NumericData self, NumericData target)
        {
            using var list = ListComponent<NumericData>.Create();

            list.Add(self);
            list.Add(target);

            return Create(list);
        }

        public static NumericData Add(this NumericData self, params NumericData[] allData)
        {
            using var list = ListComponent<NumericData>.Create();

            list.Add(self);
            foreach (var data in allData)
            {
                list.Add(data);
            }

            return Create(list);
        }

        public static NumericData Add(this NumericData self, IEnumerable<NumericData> allData)
        {
            using var list = ListComponent<NumericData>.Create();

            list.Add(self);
            foreach (var data in allData)
            {
                list.Add(data);
            }

            return Create(list);
        }

        public static NumericData Add(this NumericData self, NumericDataComponent target)
        {
            using var list = ListComponent<NumericData>.Create();

            list.Add(self);
            list.Add(target.NumericData);

            return Create(list);
        }

        public static NumericData Add(this NumericData self, params NumericDataComponent[] allComponent)
        {
            using var list = ListComponent<NumericData>.Create();

            list.Add(self);
            foreach (var component in allComponent)
            {
                list.Add(component.NumericData);
            }

            return Create(list);
        }

        public static NumericData Add(this NumericData self, IEnumerable<NumericDataComponent> allComponent)
        {
            using var list = ListComponent<NumericData>.Create();

            list.Add(self);
            foreach (var component in allComponent)
            {
                list.Add(component.NumericData);
            }

            return Create(list);
        }
    }
}