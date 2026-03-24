using Sirenix.OdinInspector;
using UnityEngine;

namespace YIUIFramework
{
    /// <summary>
    /// 游戏物体缓存信息
    /// 挂载了就有信息 没挂载就全部默认值
    /// </summary>
    [AddComponentMenu("YIUIFramework/GameObjectPool/YIUI对象池缓存信息 【YIUIGameObjectPoolInfo】")]
    public class YIUIGameObjectPoolInfo: SerializedMonoBehaviour, IYIUIGameObjectPoolSettingsConfig
    {
        [LabelText("最大显示时间 <=0 表示永不过期")]
        [SerializeField]
        private float Timeout;

        [LabelText("同时显示的最大数量 <=0 表示不限制")]
        [SerializeField]
        private int MaxCacheCount;

        [LabelText("缓存池中保留的时间 <=0 表示永久保留")]
        [SerializeField]
        private float CacheTime;

        [LabelText("缓存池中的最大数量 <0 表示不限制")]
        [SerializeField]
        private int MinCacheCount = -1;

        [ShowIf("ShowNewResName")]
        [LabelText("超过最大显示时 返回一个新的资源")]
        [SerializeField]
        private string MaxCacheCountNewResName;

        //通常这个资源是一个空节点不带渲染那种,
        //最快的做法就是在之前的预制体上拷贝一个新的 吧跟渲染相关的都删掉 就保留其他
        //为什么要保留其他的呢 比如技能发的一个飞行道具有碰撞的 你不能给我一个非常空的预制体吧 这样连基本的碰撞都没有了
        private bool ShowNewResName => MaxCacheCount > 0;

        float IYIUIGameObjectPoolSettingsConfig.Timeout => this.Timeout;

        int IYIUIGameObjectPoolSettingsConfig.MaxCacheCount => this.MaxCacheCount;

        float IYIUIGameObjectPoolSettingsConfig.CacheTime => this.CacheTime;

        int IYIUIGameObjectPoolSettingsConfig.MinCacheCount => this.MinCacheCount;

        string IYIUIGameObjectPoolSettingsConfig.MaxCacheCountNewResName => this.MaxCacheCountNewResName;
    }
}