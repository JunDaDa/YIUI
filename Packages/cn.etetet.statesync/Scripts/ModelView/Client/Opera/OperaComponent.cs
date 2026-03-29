using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class OperaComponent: Entity, IAwake, IUpdate
    {
        public Vector3 ClickPoint;

        public int mapMask;

        public float3 LastDirection;    // cached last sent direction for change detection
        public bool IsDirectMoving;     // true when WASD movement is active

        // 缓存本地预测移动所需的引用，避免每帧 GetComponent
        public float MoveSpeed;
        public Transform MyUnitTransform;
    }
}
