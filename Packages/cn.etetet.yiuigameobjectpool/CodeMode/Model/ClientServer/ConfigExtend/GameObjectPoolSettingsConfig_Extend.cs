using System.Collections.Generic;
using YIUIFramework;

namespace ET
{
    public partial class GameObjectPoolSettingsConfig: IYIUIGameObjectPoolSettingsConfig
    {
        float IYIUIGameObjectPoolSettingsConfig.Timeout => this.Timeout;

        int IYIUIGameObjectPoolSettingsConfig.MaxCacheCount => this.MaxCacheCount;

        float IYIUIGameObjectPoolSettingsConfig.CacheTime => this.CacheTime;

        int IYIUIGameObjectPoolSettingsConfig.MinCacheCount => this.MinCacheCount;

        string IYIUIGameObjectPoolSettingsConfig.MaxCacheCountNewResName => this.MaxCacheCountNewResName;
    }
}