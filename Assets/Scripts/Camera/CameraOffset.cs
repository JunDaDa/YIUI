using UnityEngine;

/// <summary>
/// 调试用相机控制器。挂在 MainCamera 上，手动拖入角色 Target。
/// Inspector 实时调参，调好后通过 Editor 按钮保存 JSON。
/// </summary>
[DefaultExecutionOrder(100)]
public class CameraOffset : MonoBehaviour
{
    [Header("目标（手动拖入角色）")]
    public Transform Target;

    [Header("跟随")]
    public Vector3 Offset = new Vector3(0f, 35f, -20f);
    [Range(0.01f, 1f)]
    public float LerpSpeed = 0.1f;

    [Header("缩放（滚轮调整 Offset.y）")]
    public float ZoomSpeed = 5f;
    public float MinZoom = 15f;
    public float MaxZoom = 60f;

    private bool m_FirstFrame = true;

    private void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        // 滚轮缩放：直接调整 Offset.y
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
        {
            float newY = Mathf.Clamp(Offset.y - scroll * ZoomSpeed, MinZoom, MaxZoom);
            Offset = new Vector3(Offset.x, newY, Offset.z);
        }

        Vector3 targetPos = Target.position + Offset;

        if (m_FirstFrame)
        {
            // 首帧 snap 居中
            transform.position = targetPos;
            m_FirstFrame = false;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, LerpSpeed);
        }
    }

    /// <summary>
    /// Inspector 修改 Target 时重新 snap
    /// </summary>
    private void OnValidate()
    {
        m_FirstFrame = true;
    }
}
