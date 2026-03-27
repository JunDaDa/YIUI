using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class CameraComponent : Entity, IAwake, IDestroy
    {
        public Camera MainCamera;
        public Transform CameraTransform;
        public Transform FollowTarget;
        public EntityRef<Unit> TargetUnit;

        [StaticField]
        public static CameraComponent Instance;
    }
}
