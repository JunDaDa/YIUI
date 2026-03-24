using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 额外数值数据扩展
    /// 差异数据
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataExtend
    {
        /// <summary>
        /// 计算两个NumericData的差异
        /// </summary>
        public static Dictionary<int, long> GetDifference(this NumericData self, NumericData target, bool contain0)
        {
            return GetDifferenceInternal(self.NumericDic, target.NumericDic, contain0);
        }

        /// <summary>
        /// 计算NumericData与字典的差异
        /// </summary>
        public static Dictionary<int, long> GetDifference(this NumericData self, Dictionary<int, long> target, bool contain0 = true)
        {
            return GetDifferenceInternal(self.NumericDic, target, contain0);
        }

        /// <summary>
        /// 计算NumericData与ENumericType字典的差异
        /// </summary>
        public static Dictionary<ENumericType, long> GetDifference(this NumericData self, Dictionary<ENumericType, long> target, bool contain0 = true)
        {
            var selfData = self.NumericDic;

            var difference = GetDifferenceInternal(selfData, target, contain0);

            return difference;
        }

        /// <summary>
        /// 内部差异计算方法
        /// 比较两个字典，我与目标的差异，返回差异字典
        /// 以目标为准
        /// </summary>
        private static Dictionary<int, long> GetDifferenceInternal(Dictionary<int, long> selfData, Dictionary<int, long> targetData, bool contain0)
        {
            var difference = new Dictionary<int, long>();

            foreach (var kvp in selfData)
            {
                var key = kvp.Key;
                if (!contain0 && key.CheckContain0())
                {
                    continue;
                }

                var selfValue = kvp.Value;

                if (targetData.TryGetValue(key, out var targetValue))
                {
                    if (selfValue != targetValue)
                    {
                        difference[key] = targetValue;
                    }
                }
                else
                {
                    difference[key] = selfValue;
                }
            }

            foreach (var kvp in targetData)
            {
                var key = kvp.Key;
                if (!contain0 && key.CheckContain0())
                {
                    continue;
                }

                var targetValue = kvp.Value;

                if (selfData.TryGetValue(key, out var selfValue))
                {
                    if (selfValue != targetValue)
                    {
                        difference[key] = targetValue;
                    }
                }
                else
                {
                    difference[key] = targetValue;
                }
            }

            return difference;
        }

        private static Dictionary<ENumericType, long> GetDifferenceInternal(Dictionary<int, long> selfData, Dictionary<ENumericType, long> targetData, bool contain0)
        {
            var difference = new Dictionary<ENumericType, long>();

            foreach (var kvp in selfData)
            {
                var key = kvp.Key;
                if (!contain0 && key.CheckContain0())
                {
                    continue;
                }

                var keyEnum = (ENumericType)key;
                var selfValue = kvp.Value;

                if (targetData.TryGetValue(keyEnum, out var targetValue))
                {
                    if (selfValue != targetValue)
                    {
                        difference[keyEnum] = targetValue;
                    }
                }
                else
                {
                    difference[keyEnum] = selfValue;
                }
            }

            foreach (var kvp in targetData)
            {
                var key = kvp.Key;
                if (!contain0 && key.CheckContain0())
                {
                    continue;
                }

                var targetValue = kvp.Value;

                if (selfData.TryGetValue((int)key, out var selfValue))
                {
                    if (selfValue != targetValue)
                    {
                        difference[key] = targetValue;
                    }
                }
                else
                {
                    difference[key] = targetValue;
                }
            }

            return difference;
        }
    }
}