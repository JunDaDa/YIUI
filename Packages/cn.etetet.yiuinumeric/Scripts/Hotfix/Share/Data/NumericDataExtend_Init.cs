#if UNITY_EDITOR
#define NUMERIC_CHECK_SYMBOLS //数值检查宏 离开Unity要使用就在unity设置中添加这个宏
#endif
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 初始化
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataExtend
    {
        #region Init

        //根据配置档直接传入初始化
        //所以他是需要重新计算结果的
        public static void InitSet(this NumericData self, Dictionary<ENumericType, long> configData, bool isPushEvent = false)
        {
            self.NumericDic.Clear();
            using var existResult = HashSetComponent<int>.Create();
            using var limitGrow   = ListComponent<NumericValueLimitConfig>.Create(); //成长限制
            using var limitResult = ListComponent<NumericValueLimitConfig>.Create(); //结果限制

            foreach (var data in configData)
            {
                var numericEnum = data.Key;
                var numericType = (int)numericEnum;

                #if NUMERIC_CHECK_SYMBOLS
                if (!numericEnum.CheckChangeNumeric()) return;
                #endif

                var limitConfig = NumericValueLimitConfigCategory.Instance.GetOrDefault(numericEnum);
                if (limitConfig != null)
                {
                    limitGrow.Add(limitConfig);
                    continue;
                }

                var value = data.Value;

                if (!numericEnum.IsNotGrowNumeric())
                {
                    existResult.Add(numericType / 10);
                }

                self.ChangeValue(numericType, value, isPushEvent);
            }

            foreach (var numericType in existResult)
            {
                var limitConfig = NumericValueLimitConfigCategory.Instance.GetOrDefault((ENumericType)numericType);
                if (limitConfig != null)
                {
                    limitResult.Add(limitConfig);
                    continue;
                }

                self.UpdateResult(numericType * 10 + 1, isPushEvent);
            }

            existResult.Clear();

            limitGrow.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var limitConfig in limitGrow)
            {
                var numericEnum = limitConfig.Id;
                var numericType = (int)numericEnum;
                var value       = configData[numericEnum];

                if (!numericEnum.IsNotGrowNumeric())
                {
                    existResult.Add(numericType / 10);
                }

                self.ChangeValue(numericType, value, isPushEvent);
            }

            foreach (var numericType in existResult)
            {
                self.UpdateResult(numericType * 10 + 1, isPushEvent);
            }

            limitResult.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var limitConfig in limitResult)
            {
                var numericEnum = limitConfig.Id;
                self.UpdateResult((int)numericEnum * 10 + 1, isPushEvent);
            }
        }

        //根据配置档直接传入初始化
        //所以他是需要重新计算结果的
        public static void InitSet(this NumericData self, Dictionary<int, long> configData, bool isPushEvent = false)
        {
            self.NumericDic.Clear();
            using var existResult = HashSetComponent<int>.Create();
            using var limitGrow   = ListComponent<NumericValueLimitConfig>.Create(); //成长限制
            using var limitResult = ListComponent<NumericValueLimitConfig>.Create(); //结果限制

            foreach (var data in configData)
            {
                var numericType = data.Key;

                var numericEnum = (ENumericType)numericType;

                #if NUMERIC_CHECK_SYMBOLS
                if (!numericEnum.CheckChangeNumeric()) return;
                #endif

                var limitConfig = NumericValueLimitConfigCategory.Instance.GetOrDefault(numericEnum);
                if (limitConfig != null)
                {
                    limitGrow.Add(limitConfig);
                    continue;
                }

                var value = data.Value;

                if (!numericEnum.IsNotGrowNumeric())
                {
                    existResult.Add(numericType / 10);
                }

                self.ChangeValue(numericType, value, isPushEvent);
            }

            foreach (var numericType in existResult)
            {
                var limitConfig = NumericValueLimitConfigCategory.Instance.GetOrDefault((ENumericType)numericType);
                if (limitConfig != null)
                {
                    limitResult.Add(limitConfig);
                    continue;
                }

                self.UpdateResult(numericType * 10 + 1, isPushEvent);
            }

            existResult.Clear();

            limitGrow.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var limitConfig in limitGrow)
            {
                var numericEnum = limitConfig.Id;
                var numericType = (int)numericEnum;
                var value       = configData[numericType];

                if (!numericEnum.IsNotGrowNumeric())
                {
                    existResult.Add(numericType / 10);
                }

                self.ChangeValue(numericType, value, isPushEvent);
            }

            foreach (var numericType in existResult)
            {
                self.UpdateResult(numericType * 10 + 1, isPushEvent);
            }

            limitResult.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var limitConfig in limitResult)
            {
                var numericEnum = limitConfig.Id;
                self.UpdateResult((int)numericEnum * 10 + 1, isPushEvent);
            }
        }

        //同步服务器的数值
        //是计算了结果的所以不需要计算限制等等情况
        public static void InitToServer(this NumericData self, Dictionary<int, long> serverData, bool isPushEvent = false)
        {
            self.NumericDic.Clear();
            foreach (var (key, numericValue) in serverData)
            {
                self.NumericDic[key] = numericValue;
            }

            if (isPushEvent)
            {
                self.PushEventAll();
            }
        }

        #endregion
    }
}