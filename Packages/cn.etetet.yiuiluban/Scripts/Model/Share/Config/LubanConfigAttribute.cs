using System;

namespace ET
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigAttribute : BaseAttribute
    {
        public int ConfigType { get; }

        public ConfigAttribute(int configType = 0)
        {
            this.ConfigType = configType;
        }
    }
}