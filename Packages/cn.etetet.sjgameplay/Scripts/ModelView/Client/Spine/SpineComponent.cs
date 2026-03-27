using Spine.Unity;

namespace ET.Client
{
    /// <summary>
    /// Spine 动画控制器 ET 组件。
    /// 包装 Unity 侧的 SkeletonAnimation，通过 SpineComponentSystem 操作。
    /// 注意：ET 的 GetComponent 从 Entity 组件集合查找，
    /// Unity 的 GetComponent 从 GameObject 上查找 MonoBehaviour，两者含义不同。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class SpineComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// Unity 侧的 SkeletonAnimation 组件引用。
        /// 在 Awake 中通过 GameObjectComponent.GameObject.GetComponent 获取（Unity GetComponent）。
        /// </summary>
        public SkeletonAnimation SkeletonAnimation;

        /// <summary>
        /// 当前正在播放的动画名。使用 SpineAnimNames 中的常量。
        /// </summary>
        public string CurrentAnimation;

        /// <summary>
        /// 当前动画是否循环播放。
        /// </summary>
        public bool IsLoop;
    }
}