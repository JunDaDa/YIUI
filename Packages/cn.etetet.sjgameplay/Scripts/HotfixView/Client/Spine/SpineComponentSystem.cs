using Spine;
using Spine.Unity;

namespace ET.Client
{
    [EntitySystemOf(typeof(SpineComponent))]
    public static partial class SpineComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpineComponent self)
        {
            // ET GetComponent: 从 Entity 组件集合中按类型查找 GameObjectComponent
            GameObjectComponent goComponent = self.GetParent<Unit>().GetComponent<GameObjectComponent>();
            if (goComponent == null)
            {
                return;
            }

            // Unity GetComponent: 从 GameObject 上查找 SkeletonAnimation MonoBehaviour
            SkeletonAnimation skeletonAnimation = goComponent.GameObject.GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                return;
            }

            self.SkeletonAnimation = skeletonAnimation;
        }

        [EntitySystem]
        private static void Destroy(this SpineComponent self)
        {
            self.SkeletonAnimation = null;
            self.CurrentAnimation = null;
            self.IsLoop = false;
        }

        /// <summary>
        /// 播放指定名称的动画。animName 应使用 SpineAnimNames 中的常量。
        /// </summary>
        public static TrackEntry Play(this SpineComponent self, string animName, bool loop, int trackIndex = 0)
        {
            if (self.SkeletonAnimation == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(animName))
            {
                return null;
            }

            self.CurrentAnimation = animName;
            self.IsLoop = loop;
            return self.SkeletonAnimation.AnimationState.SetAnimation(trackIndex, animName, loop);
        }

        /// <summary>
        /// 按 MotionType 播放对应的 Spine 动画。
        /// 使用 switch 映射到 SpineAnimNames 常量，避免 ToString() 产生 GC。
        /// </summary>
        public static TrackEntry PlayByMotionType(this SpineComponent self, MotionType motionType, bool loop = true)
        {
            string animName = MotionTypeToAnimName(motionType);
            if (animName == null)
            {
                return null;
            }

            return self.Play(animName, loop);
        }

        /// <summary>
        /// 在当前轨道追加一个动画，等当前动画播完后播放。
        /// </summary>
        public static TrackEntry AddAnimation(this SpineComponent self, string animName, bool loop, float delay, int trackIndex = 0)
        {
            if (self.SkeletonAnimation == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(animName))
            {
                return null;
            }

            return self.SkeletonAnimation.AnimationState.AddAnimation(trackIndex, animName, loop, delay);
        }

        /// <summary>
        /// 清空指定轨道的动画。
        /// </summary>
        public static void SetEmptyAnimation(this SpineComponent self, int trackIndex, float mixDuration)
        {
            if (self.SkeletonAnimation == null)
            {
                return;
            }

            self.SkeletonAnimation.AnimationState.SetEmptyAnimation(trackIndex, mixDuration);
        }

        /// <summary>
        /// 查询 SkeletonData 中是否存在指定名称的动画。
        /// </summary>
        public static bool HasAnimation(this SpineComponent self, string animName)
        {
            if (self.SkeletonAnimation == null)
            {
                return false;
            }

            return self.SkeletonAnimation.Skeleton.Data.FindAnimation(animName) != null;
        }

        /// <summary>
        /// 设置 Spine 动画播放速度。
        /// </summary>
        public static void SetTimeScale(this SpineComponent self, float timeScale)
        {
            if (self.SkeletonAnimation == null)
            {
                return;
            }

            self.SkeletonAnimation.timeScale = timeScale;
        }

        /// <summary>
        /// 设置 Spine 角色水平翻转（左右朝向）。
        /// 通过 Skeleton.ScaleX 实现，不产生 GC。
        /// </summary>
        public static void SetFlipX(this SpineComponent self, bool flip)
        {
            if (self.SkeletonAnimation == null)
            {
                return;
            }

            self.SkeletonAnimation.Skeleton.ScaleX = flip ? -1f : 1f;
        }

        /// <summary>
        /// MotionType 到 Spine 动画名的映射。
        /// 使用 switch + 字符串常量，零 GC 开销。
        /// </summary>
        private static string MotionTypeToAnimName(MotionType motionType)
        {
            return motionType switch
            {
                MotionType.Idle => SpineAnimNames.Idle,
                MotionType.Run  => SpineAnimNames.Run,
                _               => null,
            };
        }
    }
}