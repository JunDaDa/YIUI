using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 数值的多语言本地化
    /// 因为服务器在Unity独立运行时 也需要配置做可视化
    /// 所以这个多语言功能放在这里 而不是客户端独用
    /// </summary>
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericLocalization
    {
        #region 数值的名称

        /// <summary>
        /// 获取数值的名称
        /// </summary>
        public static string GetLocalizationName(this ENumericType numeric)
        {
            var config = numeric.GetLocalizationConfig();
            if (config == null)
            {
                return GetLocalizationNameByCheck(numeric);
            }

            //TODO 这里最终会接到 I2 本地化
            return config.Localization;
        }

        //如果策划配置档没有配置 则到检查配置中获取
        //这个配置没有多语言 只有本地描述
        //TODO 只能做到临时作用 这个方法将会在真实流程时返回null 并且报错
        //TODO 或者继续扩展工具 这里的名字和描述 额外对接到I2本地化  这样 本地化表就作为增量修改表来使用 可以大大降低一些基础内容
        public static string GetLocalizationNameByCheck(ENumericType numeric)
        {
            //开启此宏 则强制报错必须配置 否则有配置用配置 没有配置用检查配置
            #if NUMERIC_FORCE_LOCALIZATION
            Log.Error($"本地化配置 NumericLocalization 没有找到 {numeric}");
            return "";
            #else
            var finalEnum = numeric.GetNumericFinalEnum();
            var finalCfg = finalEnum.GetCheckConfig();
            return finalCfg == null ? "" : $"{finalCfg.Alias}{GetNumericGrowAlias(numeric)}";
            #endif
        }

        //1-N的默认叫法
        private static string GetNumericGrowAlias(ENumericType numericType)
        {
            var numericId = (int)numericType;

            if (numericId is >= NumericConst.Min and <= NumericConst.Max)
            {
                return "";
            }

            if (numericId is >= NumericConst.ChangeMin and <= NumericConst.ChangeMax)
            {
                var mod = numericId % 10;
                if (mod is >= 1 and <= NumericConst.RangeMax)
                {
                    //TODO 这里是写的中文 如果你有多语言这里可以改成对应的语言
                    switch (mod)
                    {
                        case 1:
                            return "基础";
                        case 2:
                            return "增加";
                        case 3:
                            return "增加百分比";
                        case 4:
                            return "最终增加";
                        case 5:
                            return "最终增加百分比";
                        case 6:
                            return "结果增加";
                        default:
                        {
                            Log.Error($"是否新增了一个类型 这里还未定义 {mod}");
                            return mod.ToString();
                        }
                    }
                }
            }

            Log.Error($"非法的类型 {numericType}");
            return "";
        }

        #endregion

        #region 数值的描述

        /// <summary>
        /// 获取数值的描述
        /// </summary>
        public static string GetLocalizationDescribe(this ENumericType numeric)
        {
            var config = numeric.GetLocalizationConfig();
            if (config == null)
            {
                return GetLocalizationDescribeByCheck(numeric);
            }

            //TODO 这里最终会接到 I2 本地化
            return config.Describe;
        }

        //如果策划配置档没有配置 则到检查配置中获取
        //这个配置没有多语言 只有本地描述
        //TODO 只能做到临时作用 这个方法将会在真实流程时返回null 并且报错
        //TODO 或者继续扩展工具 这里的名字和描述 额外对接到I2本地化  这样 本地化表就作为增量修改表来使用 可以大大降低一些基础内容
        private static string GetLocalizationDescribeByCheck(ENumericType numeric)
        {
            //开启此宏 则强制报错必须配置 否则有配置用配置 没有配置用检查配置
            #if NUMERIC_FORCE_LOCALIZATION
            Log.Error($"本地化配置 NumericLocalization 没有找到 {numeric}");
            return "";
            #else
            var finalEnum = numeric.GetNumericFinalEnum();
            var finalCfg = finalEnum.GetCheckConfig();
            return finalCfg == null ? "" : finalCfg.Desc;
            #endif
        }

        #endregion

        #region 数值的值

        /// <summary>
        /// 获取数值转字符串表现
        /// </summary>
        /// <param name="formatSpecifier">支持自定义传入说明符 否则默认使用配置中的</param>
        public static string GetLocalizationValue(this NumericData self,
                                                  ENumericType numeric,
                                                  string formatSpecifier = "")
        {
            object value = self.GetObjectValue(numeric);
            if (value is string s) return s;
            return GetValueFormat(value, numeric, formatSpecifier);
        }

        /// <summary>
        /// 获取数值转字符串表现
        /// 需要自行保证这个值一定是数值系统转换后的值
        /// 比如float类型 根据需求判断是否需要/NumericConst.FloatRate
        /// </summary>
        /// <param name="formatSpecifier">支持自定义传入说明符 否则默认使用配置中的</param>
        public static string GetLocalizationValue(this ENumericType numeric,
                                                  object value,
                                                  string formatSpecifier = "")
        {
            if (value is string s) return s;
            return GetValueFormat(value, numeric, formatSpecifier);
        }

        /// <summary>
        /// 获取数值转字符串表现
        /// 如果是浮点数，直接去掉浮点部分再计算
        /// </summary>
        /// <param name="self"></param>
        /// <param name="numeric">数字类型</param>
        /// <param name="keepLength">保留的浮点数长度(最大2位)</param>
        /// <returns></returns>
        public static string GetLocalizationDisplayValue(this NumericData self, ENumericType numeric, int keepLength = 0)
        {
            long value = 0;
            switch (numeric.GetNumericValueType())
            {
                case ENumericValueType.Long:
                    value = self.GetAsLong(numeric);
                    break;
                case ENumericValueType.Float:
                    value = (long)self.GetAsFloat(numeric);
                    break;
                case ENumericValueType.Int:
                    value = self.GetAsInt(numeric);
                    break;
            }

            #if YIUI
            return NumberDisplayHelper.FormatBy3n2Rule(value, keepLength);
            #else
            return ConvertLocalizationDisplayValue(value, keepLength);
            #endif
        }

        //临时方案 建议每个项目组自行实现更加完善的本地化方案
        public static string ConvertLocalizationDisplayValue(long value, int keepLength = 0)
        {
            keepLength = math.clamp(keepLength, 0, 2);

            // 转换为千分位数字
            var array = value.ToString($"N0").Split(',');

            string suffix, valueStr;

            // 获取后缀和最后的值
            switch (array.Length)
            {
                case 2:
                    suffix = "K";
                    valueStr = $"{array[0]}.{array[1]}";
                    break;
                case 3:
                    suffix = "M";
                    valueStr = $"{array[0]}.{array[1]}";
                    break;
                case 4:
                    suffix = "B";
                    valueStr = $"{array[0]}.{array[1]}";
                    break;
                case 5:
                    suffix = "T";
                    valueStr = $"{array[0]}.{array[1]}";
                    break;
                case 6:
                    suffix = "E";
                    valueStr = $"{array[0]}.{array[1]}";
                    break;
                case 1:
                default:
                    return $"{array[0]}";
            }

            return $"{float.Parse(valueStr).ToString($"F{keepLength}")}{suffix}";
        }

        #region ValueFormat

        /*
         理论上你想的所有都可以展示出来 不懂的别乱传 请看说明
         在C#中格式化字符串
         https://lib9kmxvq7k.feishu.cn/wiki/DREsw891FiLDMHk05eacJ7PsnGb
         案例:
         基础值是: {0:X} X 为替换内容
            C  货币  2.5.ToString("C")  ￥2.50
            D  十进制数  25.ToString("D5")  00025
            E  科学型  25000.ToString("E")  2.500000E+005
            F  固定点  25.ToString("F2")  25.00
            G  常规  2.5.ToString("G")  2.5
            N  数字  2500000.ToString("N")  2,500,000.00
            X  十六进制  255.ToString("X") FF
         */

        public static string GetValueFormat(object value, ENumericType numeric, string formatSpecifier = "")
        {
            if (!string.IsNullOrEmpty(formatSpecifier))
            {
                return ValueFormat(value, formatSpecifier);
            }

            var config = numeric.GetLocalizationConfig();
            if (config == null || string.IsNullOrEmpty(config.Specifier))
            {
                return ValueFormat(value, "G"); //默认使用G
            }

            return ValueFormat(value, config.Specifier);
        }

        public static string ValueFormat(object value, string formatSpecifier)
        {
            return string.Format($"{{0:{formatSpecifier}}}", value);
        }

        /// <summary>
        /// 获取数值转字符串表现
        /// </summary>
        /// <param name="formatSpecifier">支持自定义传入说明符 否则默认使用配置中的</param>
        public static string GetLocalizationValue(this NumericDataComponent self,
                                                  ENumericType numeric,
                                                  string formatSpecifier = "")
        {
            object value = self.GetObjectValue(numeric);
            if (value is string s) return s;
            return GetValueFormat(value, numeric, formatSpecifier);
        }

        /// <summary>
        /// 获取数值转字符串表现
        /// 如果是浮点数，直接去掉浮点部分再计算
        /// </summary>
        /// <param name="self"></param>
        /// <param name="numeric">数字类型</param>
        /// <param name="keepLength">保留的浮点数长度(最大2位)</param>
        /// <returns></returns>
        public static string GetLocalizationDisplayValue(this NumericDataComponent self, ENumericType numeric, int keepLength = 0)
        {
            long value = 0;
            switch (numeric.GetNumericValueType())
            {
                case ENumericValueType.Long:
                    value = self.GetAsLong(numeric);
                    break;
                case ENumericValueType.Float:
                    value = (long)self.GetAsFloat(numeric);
                    break;
                case ENumericValueType.Int:
                    value = self.GetAsInt(numeric);
                    break;
            }

            return ConvertLocalizationDisplayValue(value, keepLength);
        }

        #endregion

        #endregion

        #region 其他

        //获取本地化配置档
        public static NumericLocalizationConfig GetLocalizationConfig(this ENumericType numeric, bool log = false)
        {
            if (NumericLocalizationConfigCategory.Instance == null) return null;

            if (NumericLocalizationConfigCategory.Instance.DataMap.TryGetValue(numeric, out var config))
            {
                return config;
            }

            if (log)
            {
                Log.Error($"NumericLocalization 表中没有找到这个配置挡 {numeric}");
            }

            return null;
        }

        #endregion
    }
}