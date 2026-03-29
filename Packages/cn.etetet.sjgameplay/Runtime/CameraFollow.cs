using UnityEngine;

/// <summary>
/// 相机跟随控制器 MonoBehaviour。挂在 MainCamera 上，Inspector 实时调参。
/// 由 CameraComponentSystem 通过 GetComponent 访问并设置 Target。
/// 支持正交/透视两种模式，运行时通过 Orthographic 字段切换。
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标（运行时由 ET 自动设置）")]
    public Transform Target;

    [Header("位置偏移")]
    public float OffsetX = 0f;
    public float OffsetY = 7f;
    public float OffsetZ = -6f;

    [Header("跟随平滑")]
    [Tooltip("SmoothDamp 平滑时间，越小跟随越紧")]
    public float SmoothTime = 0.15f;

    [Header("正交模式")]
    public bool Orthographic = true;
    public float OrthographicSize = 5f;

    [Header("缩放")]
    public float ZoomSpeed = 5f;
    public float MinZoom = 2f;
    public float MaxZoom = 15f;

    private bool m_FirstFrame = true;
    private Vector3 m_Velocity;
    private Camera m_Camera;

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }

    public void SetFollowTarget(Transform target)
    {
        Target = target;
        m_FirstFrame = true;
        m_Velocity = Vector3.zero;
    }

    /// <summary>
    /// 切换到正交模式并应用参数
    /// </summary>
    public void SetOrthographic(float size, float minZoom, float maxZoom)
    {
        if (m_Camera == null) m_Camera = GetComponent<Camera>();
        Orthographic = true;
        OrthographicSize = size;
        MinZoom = minZoom;
        MaxZoom = maxZoom;
        m_Camera.orthographic = true;
        m_Camera.orthographicSize = size;
    }

    private void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        // 滚轮缩放（正交模式调 orthographicSize）
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f && m_Camera != null)
        {
            if (Orthographic)
            {
                OrthographicSize = Mathf.Clamp(
                    OrthographicSize - scroll * ZoomSpeed * 0.5f, MinZoom, MaxZoom);
                m_Camera.orthographicSize = OrthographicSize;
            }
        }

        Vector3 targetPos = Target.position + new Vector3(OffsetX, OffsetY, OffsetZ);

        if (m_FirstFrame)
        {
            transform.position = targetPos;
            m_FirstFrame = false;
        }
        else if (Orthographic || SmoothTime <= 0.001f)
        {
            // 正交模式直接跟随，消除 SmoothDamp 导致的像素抖动
            transform.position = targetPos;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, targetPos, ref m_Velocity, SmoothTime);
        }
    }
}
