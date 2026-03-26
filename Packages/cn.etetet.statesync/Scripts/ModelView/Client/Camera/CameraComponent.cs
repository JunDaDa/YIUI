using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class CameraComponent : Entity, IAwake, IUpdate, ILateUpdate, IDestroy
    {
        public Camera MainCamera;
        public Transform CameraTransform;

        // 跟随参数
        public Vector3 Offset;          // 相机相对角色的固定偏移
        public float LerpSpeed;         // 跟随插值速度 (0~1 区间, 越大越快)

        // 缩放参数
        public float ZoomSpeed;
        public float MinZoom;
        public float MaxZoom;
        public float CurrentZoom;

        public bool IsFirstFollow;      // 首次跟随标记，用于 snap
        public EntityRef<Unit> TargetUnit;
    }
}
