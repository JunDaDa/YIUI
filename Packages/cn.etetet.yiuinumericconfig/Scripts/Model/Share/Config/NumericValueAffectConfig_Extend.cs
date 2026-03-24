using System;
using System.Collections.Generic;

namespace ET
{
    public sealed partial class NumericValueAffectConfig
    {
        private readonly HashSet<ENumericType> m_AffectHash = new();

        private readonly Dictionary<ENumericType, long> m_AffectUniqueId = new();

        public bool IsAffect(ENumericType numericType)
        {
            return m_AffectHash.Contains(numericType);
        }

        public long GetAffectUniqueId(ENumericType numericType)
        {
            if (!IsAffect(numericType))
            {
                Log.Error($"{Id} 不存在影响类型 {numericType} 请正确使用");
                return 0;
            }

            if (!m_AffectUniqueId.TryGetValue(numericType, out var uniqueId))
            {
                uniqueId = GenerateUniqueId((int)Id, (int)numericType);
                m_AffectUniqueId.Add(numericType, uniqueId);
            }

            return uniqueId;
        }

        private long GenerateUniqueId(int value1, int value2)
        {
            if (value1 <= 0 || value2 <= 0)
            {
                Log.Error($"生成唯一ID失败: 值必须大于0 请检查: {value1}, {value2}");
                return 0;
            }

            return ((long)value1 << 32) | (value2 & 0xFFFFFFFFL);
        }

        partial void EndRef()
        {
            foreach (var affect in Affects)
            {
                if (!m_AffectHash.Add(affect))
                {
                    Log.Error($"{Id} 重复添加影响类型 {affect} 请检查配置修改");
                }
            }
        }
    }
}