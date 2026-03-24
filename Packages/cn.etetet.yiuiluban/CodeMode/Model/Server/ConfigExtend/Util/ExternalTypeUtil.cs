using Unity.Mathematics;

namespace ET
{
    public static partial class ExternalTypeUtil
    {
        public static float2 UnityMathematicsFloat2(f2 f2)
        {
            return new float2(f2.X, f2.Y);
        }

        public static float3 UnityMathematicsFloat3(f3 f3)
        {
            return new float3(f3.X, f3.Y, f3.Z);
        }

        public static float4 UnityMathematicsFloat4(f4 f4)
        {
            return new float4(f4.X, f4.Y, f4.Z, f4.W);
        }

        public static quaternion UnityMathematicsQuaternion4(q4 q4)
        {
            return new quaternion(q4.X, q4.Y, q4.Z, q4.W);
        }
    }
}