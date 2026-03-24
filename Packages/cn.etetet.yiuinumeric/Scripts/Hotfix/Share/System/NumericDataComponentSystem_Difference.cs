using System.Collections.Generic;

namespace ET
{
    [FriendOf(typeof(NumericDataComponent))]
    public static partial class NumericDataComponentSystem
    {
        public static Dictionary<int, long> GetDifference(this NumericDataComponent self, NumericDataComponent target, bool contain0 = true)
        {
            return self.NumericData.GetDifference(target.NumericData, contain0);
        }

        public static Dictionary<int, long> GetDifference(this NumericDataComponent self, NumericData target, bool contain0 = true)
        {
            return self.NumericData.GetDifference(target, contain0);
        }

        public static Dictionary<int, long> GetDifference(this NumericDataComponent self, Dictionary<int, long> target, bool contain0 = true)
        {
            return self.NumericData.GetDifference(target, contain0);
        }

        public static Dictionary<ENumericType, long> GetDifference(this NumericDataComponent self, Dictionary<ENumericType, long> target, bool contain0 = true)
        {
            return self.NumericData.GetDifference(target, contain0);
        }
    }
}