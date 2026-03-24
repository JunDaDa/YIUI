using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 数值限制系统
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataExtend
    {
        public static long GetLimitValueMin(this NumericValueLimitData limitData, NumericData data)
        {
            switch (limitData)
            {
                case NumericValueLimitNone _:
                    return long.MinValue;
                case NumericValueLimitNumber limitNumber:
                    return limitNumber.Value;
                case NumericValueLimitNumeric limitNumeric:
                    return data.NumericDic.GetValueOrDefault((int)limitNumeric.Value, 0);
                default:
                    Log.Error($"未实现的NumericValueLimitData类型: {limitData.GetType()}");
                    return long.MinValue;
            }
        }

        public static long GetLimitValueMax(this NumericValueLimitData limitData, NumericData data)
        {
            switch (limitData)
            {
                case NumericValueLimitNone _:
                    return long.MaxValue;
                case NumericValueLimitNumber limitNumber:
                    return limitNumber.Value;
                case NumericValueLimitNumeric limitNumeric:
                    return data.NumericDic.GetValueOrDefault((int)limitNumeric.Value, 0);
                default:
                    Log.Error($"未实现的NumericValueLimitData类型: {limitData.GetType()}");
                    return long.MaxValue;
            }
        }

        public static long GetLimitValueReset(this NumericValueLimitData limitData, NumericData data, int numericType)
        {
            var result = 0L;
            switch (limitData)
            {
                case NumericValueLimitNone _:
                    break;
                case NumericValueLimitFormula limitFormula:
                    result = EventSystem.Instance.Invoke<Invoke_NumericFormula, long>(limitFormula.Value, new Invoke_NumericFormula(data, numericType));
                    break;
                case NumericValueLimitNumericAdd limitNumericAdd:
                    foreach (var addType in limitNumericAdd.Value)
                    {
                        result += data.GetRealValue(addType);
                    }

                    break;
                default:
                    Log.Error($"未实现的NumericValueLimitData类型: {limitData.GetType()}");
                    break;
            }

            return result;
        }

        public static bool CheckForceNumeric(this int numericType)
        {
            return ((ENumericType)numericType).CheckForceNumeric();
        }

        public static bool CheckForceNumeric(this ENumericType numericType)
        {
            var limitConfig = numericType.GetNumericLimitConfig();
            if (limitConfig == null)
            {
                return false;
            }

            return !(limitConfig.Reset is NumericValueLimitNone);
        }

        public static NumericValueLimitConfig GetNumericLimitConfig(this int numericType)
        {
            return ((ENumericType)numericType).GetNumericLimitConfig();
        }

        public static NumericValueLimitConfig GetNumericLimitConfig(this ENumericType numericType)
        {
            return NumericValueLimitConfigCategory.Instance.GetOrDefault(numericType);
        }

        /// <summary>
        /// 获取改动前的“重置累加值”总和
        /// 用于在影响触发时先移除旧依赖累加，再让限制系统重新累加新值
        /// </summary>
        public static long GetLastOriginalValue(this NumericAffect affect)
        {
            var data = affect.Data;
            var targetNumericType = affect.AT;
            var changedNumericType = affect.NT;
            var currentValue = data.GetRealValue(targetNumericType);
            var limitConfig = targetNumericType.GetNumericLimitConfig();
            if (limitConfig == null)
            {
                Log.Error($"调用错误 limitConfig 类型 ");
                return currentValue;
            }

            long result = 0;

            switch (limitConfig.Reset)
            {
                case NumericValueLimitNone _:
                    break;
                case NumericValueLimitNumericAdd limitNumericAdd:
                    foreach (var addType in limitNumericAdd.Value)
                    {
                        var addId = (int)addType;

                        if (addId == changedNumericType)
                        {
                            result += affect.O;
                        }
                        else
                        {
                            result += data.GetRealValue(addId);
                        }
                    }

                    break;
                default:
                    Log.Error($"未处理的类型 {limitConfig.Reset.GetType()}");
                    break;
            }

            return currentValue - result;
        }

        /// <summary>
        /// 获取原始值
        /// 注意不能是有相关影响的数值改变时调用,否则会获取到错误的原始值
        /// </summary>
        public static long GetOriginalValue(this NumericData data, int numericType)
        {
            var currentValue = data.GetRealValue(numericType);

            var limitConfig = numericType.GetNumericLimitConfig();
            if (limitConfig == null)
            {
                Log.Error($"未配置 {numericType} 类型的重置值 所以这个值没有被其他影响,则不存在需要获取原始值的需求");
                return currentValue;
            }

            var allLimitValue = limitConfig.Reset.GetLimitValueReset(data, numericType);
            return currentValue - allLimitValue;
        }
    }
}