using UnityEngine;

/// <summary>
/// Z 轴深度排序。挂在需要按深度排序的物件上（装饰物、角色等）。
/// XZ 地面平面下，Z 越小（越靠近相机）= sortingOrder 越大（越在前面渲染）。
/// </summary>
public class ZSortRenderer : MonoBehaviour
{
    [Tooltip("排序精度乘数，值越大同一区域内排序越精细")]
    public int SortPrecision = 100;

    [Tooltip("基础偏移量，用于同一位置的多个物件的前后微调")]
    public int SortOffset = 0;

    private Renderer m_Renderer;

    private void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
    }

    private void LateUpdate()
    {
        if (m_Renderer == null) return;
        m_Renderer.sortingOrder = Mathf.RoundToInt(-transform.position.z * SortPrecision) + SortOffset;
    }
}
