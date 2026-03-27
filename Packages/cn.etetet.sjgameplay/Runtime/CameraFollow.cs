using UnityEngine;

/// <summary>
/// 相机跟随控制器 MonoBehaviour。挂在 MainCamera 上，Inspector 实时调参。
/// 由 CameraComponentSystem 通过 GetComponent 访问并设置 Target。
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标（运行时由 ET 自动设置）")]
    public Transform Target;

    [Header("位置偏移")]
    public float OffsetX = 0f;
    public float OffsetY = 4f;
    public float OffsetZ = -4f;

    [Header("跟随平滑")]
    [Tooltip("SmoothDamp 平滑时间，越小跟随越紧")]
    public float SmoothTime = 0.15f;

    [Header("缩放（滚轮调整 OffsetY）")]
    public float ZoomSpeed = 5f;
    public float MinZoom = 15f;
    public float MaxZoom = 60f;

    private bool m_FirstFrame = true;
    private Vector3 m_Velocity;

    public void SetFollowTarget(Transform target)
    {
        Target = target;
        m_FirstFrame = true;
        m_Velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        // TODO: 缩放功能暂时屏蔽
        // float scroll = Input.mouseScrollDelta.y;
        // if (scroll != 0f)
        // {
        //     OffsetY = Mathf.Clamp(OffsetY - scroll * ZoomSpeed, MinZoom, MaxZoom);
        // }

        Vector3 targetPos = Target.position + new Vector3(OffsetX, OffsetY, OffsetZ);

        if (m_FirstFrame)
        {
            transform.position = targetPos;
            m_FirstFrame = false;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, targetPos, ref m_Velocity, SmoothTime);
        }
    }
}
