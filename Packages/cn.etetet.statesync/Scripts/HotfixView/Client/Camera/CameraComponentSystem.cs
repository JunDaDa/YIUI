using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(CameraComponent))]
    [FriendOf(typeof(CameraComponent))]
    public static partial class CameraComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CameraComponent self)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Log.Error("CameraComponent: Camera.main is null");
                return;
            }

            self.MainCamera = cam;
            self.CameraTransform = cam.transform;
        }

        [EntitySystem]
        private static void Update(this CameraComponent self)
        {
            // 调试阶段由 CameraOffset 控制，此处暂空
        }

        [EntitySystem]
        private static void LateUpdate(this CameraComponent self)
        {
            // 调试阶段由 CameraOffset 控制，此处暂空
            // TODO: 调试完成后将 CameraOffset 的参数迁移到这里
        }

        [EntitySystem]
        private static void Destroy(this CameraComponent self)
        {
            self.MainCamera = null;
            self.CameraTransform = null;
        }
    }
}
