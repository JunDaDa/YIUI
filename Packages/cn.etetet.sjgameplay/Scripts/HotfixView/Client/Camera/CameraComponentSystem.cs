using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(CameraComponent))]
    [FriendOf(typeof(CameraComponent))]
    public static partial class CameraComponentSystem
    {
        public static void SetFollowTarget(this CameraComponent self, Transform target)
        {
            self.FollowTarget = target;
            Log.Info($"[Camera] SetFollowTarget target={target?.name}");

            CameraFollow cameraFollow = self.MainCamera?.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetFollowTarget(target);
            }
            else
            {
                Log.Warning("[Camera] CameraFollow not found on MainCamera");
            }
        }

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
            CameraComponent.Instance = self;

            ApplyConfig(self);
        }

        /// <summary>
        /// 从 GlobalParam key-value 表读取相机参数，写入 CameraFollow MonoBehaviour。
        /// </summary>
        public static void ApplyConfig(this CameraComponent self)
        {
            var cfg = GlobalParamConfigCategory.Instance;
            if (cfg == null)
            {
                Log.Warning("[Camera] GlobalParamConfigCategory is null, skip ApplyConfig");
                return;
            }

            CameraFollow cameraFollow = self.MainCamera?.GetComponent<CameraFollow>();
            if (cameraFollow == null)
            {
                Log.Warning("[Camera] CameraFollow not found, skip ApplyConfig");
                return;
            }

            cameraFollow.OffsetX = GetFloat(cfg, "CameraOffsetX");
            cameraFollow.OffsetY = GetFloat(cfg, "CameraOffsetY", 7f);
            cameraFollow.OffsetZ = GetFloat(cfg, "CameraOffsetZ", -6f);
            cameraFollow.SmoothTime = GetFloat(cfg, "CameraSmoothTime", 0.15f);
            cameraFollow.ZoomSpeed = GetFloat(cfg, "CameraZoomSpeed", 5f);

            // 正交模式参数
            float orthoSize = GetFloat(cfg, "CameraOrthoSize", 5f);
            float minZoom = GetFloat(cfg, "CameraMinZoom", 2f);
            float maxZoom = GetFloat(cfg, "CameraMaxZoom", 15f);
            bool useOrtho = GetFloat(cfg, "CameraOrthographic", 1f) > 0.5f;

            if (useOrtho)
            {
                cameraFollow.SetOrthographic(orthoSize, minZoom, maxZoom);
                Log.Info($"[Camera] Orthographic mode: size={orthoSize}, zoom=[{minZoom},{maxZoom}]");
            }
            else
            {
                cameraFollow.MinZoom = minZoom;
                cameraFollow.MaxZoom = maxZoom;
                if (self.MainCamera != null)
                {
                    self.MainCamera.fieldOfView = GetFloat(cfg, "CameraFOV", 60f);
                }
            }

            Log.Info($"[Camera] ApplyConfig OK: Offset=({cameraFollow.OffsetX},{cameraFollow.OffsetY},{cameraFollow.OffsetZ})");
        }

        private static float GetFloat(GlobalParamConfigCategory cfg, string key, float defaultValue = 0f)
        {
            var param = cfg.GetOrDefault(key);
            if (param != null)
            {
                return param.Value;
            }

            return defaultValue;
        }

        [EntitySystem]
        private static void Destroy(this CameraComponent self)
        {
            self.MainCamera = null;
            self.CameraTransform = null;
            self.FollowTarget = null;
            CameraComponent.Instance = null;
        }
    }
}
