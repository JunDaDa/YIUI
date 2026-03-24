namespace YIUIFramework
{
    /// <summary>
    /// 游戏物体缓存信息
    /// </summary>
    public interface IYIUIGameObjectPoolSettingsConfig
    {
        //[LabelText("最大显示时间 <=0 表示永不过期")]
        public float Timeout { get; }

        //[LabelText("同时显示的最大数量 <=0 表示不限制")]
        public int MaxCacheCount { get; }

        //[LabelText("缓存池中保留的时间 <=0 表示永久保留")]
        public float CacheTime { get; }

        //[LabelText("缓存池中的最大数量 <0 表示不限制")]
        public int MinCacheCount { get; }

        //[LabelText("超过最大显示时 返回一个新的资源")]
        public string MaxCacheCountNewResName { get; }
    }
}