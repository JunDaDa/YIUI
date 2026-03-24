#if UNITY_EDITOR
#define NUMERIC_CHECK_SYMBOLS //数值检查宏 离开Unity要使用就在unity设置中添加这个宏
#endif

using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 额外数值数据扩展
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataExtend
    {
        #region Private 禁止开放

        private static long GetByKey(this NumericData self, int key)
        {
            if (self == null)
            {
                Log.Error($"不可能NULL的数据");
                return 0;
            }

            if (self.NumericDic == null)
            {
                Log.Error($"不可能NULL的数据");
                return 0;
            }

            self.NumericDic.TryGetValue(key, out var value);
            return value;
        }

        private static void ChangeByKey(this NumericData self,
                                        int numericType,
                                        long value,
                                        bool isPushEvent = true,
                                        bool isAdd = true,
                                        bool check = true)
        {
            #if NUMERIC_CHECK_SYMBOLS
            if (check && !numericType.CheckChangeNumeric()) return;
            #endif

            //这里一定是基础数据的改变
            //基础数据也会推送 根据需求监听
            //也可以根据需求取消推送
            if (self.ChangeValue(numericType, value, isPushEvent, isAdd))
            {
                //基础数据改变后刷新最终数据
                self.UpdateResult(numericType, isPushEvent);
            }
        }

        /// <summary>
        /// 修改目标值
        /// </summary>
        /// <param name="self"></param>
        /// <param name="numericType">修改类型</param>
        /// <param name="value">值</param>
        /// <param name="isPushEvent">推送</param>
        /// <param name="isAdd">默认true = (+=)  false = 覆盖操作 (不明白的禁止传false)</param>
        private static bool ChangeValue(this NumericData self,
                                        int numericType,
                                        long value,
                                        bool isPushEvent = true,
                                        bool isAdd = true)
        {
            var numericEnum = (ENumericType)numericType;

            var force = numericEnum.CheckForceNumeric();

            //如果是+ 但是+为0 则不做任何操作
            if (!force && isAdd && value == 0)
            {
                return false;
            }

            var oldValue = self.GetByKey(numericType);

            //如果是赋值 但是当前值与赋值相同则不做任何操作
            if (!force && !isAdd && oldValue == value)
            {
                return false;
            }

            var newValue = isAdd ? oldValue + value : value;

            var limitConfig = NumericValueLimitConfigCategory.Instance.GetOrDefault(numericEnum);

            if (limitConfig != null)
            {
                newValue += limitConfig.Reset.GetLimitValueReset(self, numericType);
                var minLimitValue = limitConfig.Min.GetLimitValueMin(self);
                var maxLimitValue = limitConfig.Max.GetLimitValueMax(self);
                self.NumericDic[numericType] = Math.Clamp(newValue, minLimitValue, maxLimitValue);
            }
            else
            {
                self.NumericDic[numericType] = newValue;
            }

            var affectConfig = NumericValueAffectConfigCategory.Instance.GetOrDefault(numericEnum);

            var limitValue = self.NumericDic[numericType];

            if (affectConfig != null)
            {
                foreach (var affect in affectConfig.Affects)
                {
                    if (!affect.CheckChangeNumeric()) continue;

                    var uniqueId = affectConfig.GetAffectUniqueId(affect);
                    if (uniqueId == 0) continue;
                    var affectType = (int)affect;
                    var affectCurrent = self.NumericDic.GetValueOrDefault(affectType, 0);
                    var affectNewValue = EventSystem.Instance.Invoke<NumericAffect, long>(uniqueId, new NumericAffect
                    {
                        Data = self,
                        NumericType = numericType,
                        Old = oldValue,
                        New = limitValue,
                        AffectNumericType = affectType,
                        AffectCurrent = affectCurrent,
                    });

                    self.ChangeByKey(affectType, affectNewValue, isPushEvent, false);
                }
            }

            if (isPushEvent)
            {
                self.PushEvent(numericType, limitValue, oldValue);
            }

            return true;
        }

        /// <summary>
        /// 更新最终值 核心内部公式算法
        /// 只有基础数据发生变化时会自动更新
        /// 禁止外部调用
        /// 传入的ID 一定是 大于 NumericConst.Max
        /// </summary>
        private static void UpdateResult(this NumericData self, int numericType, bool isPushEvent)
        {
            //非成长无需计算
            if (numericType.IsNotGrowNumeric())
            {
                return;
            }

            self.UpdateResultInvoke(numericType, isPushEvent);
        }

        private static void UpdateResultInvoke(this NumericData self, int numericType, bool isPushEvent)
        {
            var formulaId = numericType.GetNumericFormulaId();
            var result = EventSystem.Instance.Invoke<Invoke_NumericFormula, long>(formulaId, new Invoke_NumericFormula(self, numericType));
            var final = numericType / 10;
            self.ChangeValue(final, result, isPushEvent, false);
        }

        #endregion
    }
}