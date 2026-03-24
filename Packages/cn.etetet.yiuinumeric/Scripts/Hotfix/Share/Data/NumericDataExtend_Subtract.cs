using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 数值相减得出一个新数值
    /// 目前只支持 A-(B,C,D...)
    /// 不支持 A-B-C-D... (自行扩展)
    /// 强行用 ((A-B)-C)-D... 也能实现不过效率不高
    /// 注意因为是创建的新数据 owner 为null 需要手动设置
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataExtend
    {
        public static NumericData Subtract(this NumericData self, NumericData target)
        {
            return SubtractToData(self, target);
        }

        public static NumericData Subtract(this NumericData self, params NumericData[] allData)
        {
            return SubtractToData(self, Create(allData));
        }

        public static NumericData Subtract(this NumericData self, IEnumerable<NumericData> allData)
        {
            return SubtractToData(self, Create(allData));
        }

        public static NumericData Subtract(this NumericData self, NumericDataComponent target)
        {
            return SubtractToData(self, target.NumericData);
        }

        public static NumericData Subtract(this NumericData self, params NumericDataComponent[] allData)
        {
            return SubtractToData(self, Create(allData));
        }

        public static NumericData Subtract(this NumericData self, IEnumerable<NumericDataComponent> allData)
        {
            return SubtractToData(self, Create(allData));
        }

        private static NumericData SubtractToData(NumericData minuendData, NumericData subtrahendData)
        {
            var numericData = NumericData.Create();

            using var tempDic = NumericDictionaryPool<int, long>.Create();
            using var keys    = HashSetComponent<int>.Create();

            foreach (var key in minuendData.NumericDic.Keys)
            {
                if (key <= NumericConst.Max && !key.IsNotGrowNumeric())
                {
                    continue;
                }

                keys.Add(key);
            }

            foreach (var key in subtrahendData.NumericDic.Keys)
            {
                if (key <= NumericConst.Max && !key.IsNotGrowNumeric())
                {
                    continue;
                }

                keys.Add(key);
            }

            foreach (var key in keys)
            {
                minuendData.NumericDic.TryGetValue(key, out var minuend);       //被减数
                subtrahendData.NumericDic.TryGetValue(key, out var subtrahend); //减数
                tempDic.Add(key, minuend - subtrahend);
            }

            numericData.InitSet(tempDic);
            return numericData;
        }
    }
}