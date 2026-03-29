using Pathfinding;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// NavMesh 验证工具：WASD 移动 + Spine idle/run 动画 + A* NavMesh 碰撞检测。
/// 挂到带 SkeletonAnimation 的角色上，Play 模式下测试 navmesh 是否合理。
/// </summary>
public class NavMeshTestMover : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float m_MoveSpeed = 5f;

    [Header("动画")]
    [SerializeField] private string m_IdleAnim = "idle";
    [SerializeField] private string m_RunAnim = "run";

    private SkeletonAnimation m_Skeleton;
    private bool m_IsMoving;

    private void Awake()
    {
        m_Skeleton = GetComponentInChildren<SkeletonAnimation>();
    }

    private void Start()
    {
        // 自动吸附到最近可行走点
        SnapToNavMesh();

        if (m_Skeleton != null)
        {
            m_Skeleton.AnimationState.SetAnimation(0, m_IdleAnim, true);
        }
    }

    /// <summary>
    /// 将角色位置吸附到最近的 navmesh 可行走点。
    /// </summary>
    private void SnapToNavMesh()
    {
        if (AstarPath.active == null)
            return;

        var info = AstarPath.active.GetNearest(transform.position, NearestNodeConstraint.Walkable);
        if (info.node != null && info.node.Walkable)
        {
            transform.position = info.position;
            Debug.Log($"[NavMeshTest] 吸附到 navmesh: {info.position}");
        }
        else
        {
            Debug.LogWarning("[NavMeshTest] 附近没有可行走的 navmesh 节点！");
        }
    }

    private void Update()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.W)) z += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;

        bool wantsMove = x != 0f || z != 0f;

        if (wantsMove)
        {
            Vector3 direction = new Vector3(x, 0f, z).normalized;
            Vector3 targetPos = transform.position + direction * m_MoveSpeed * Time.deltaTime;

            // A* NavMesh 碰撞检测：将目标位置钳制到可行走区域
            transform.position = ClampToNavMesh(targetPos);

            // 朝向翻转
            if (m_Skeleton != null)
            {
                if (x > 0f) m_Skeleton.Skeleton.ScaleX = -Mathf.Abs(m_Skeleton.Skeleton.ScaleX);
                else if (x < 0f) m_Skeleton.Skeleton.ScaleX = Mathf.Abs(m_Skeleton.Skeleton.ScaleX);
            }

            // 切换到 run
            if (!m_IsMoving)
            {
                m_IsMoving = true;
                if (m_Skeleton != null)
                {
                    m_Skeleton.AnimationState.SetAnimation(0, m_RunAnim, true);
                }
            }
        }
        else
        {
            // 切换到 idle
            if (m_IsMoving)
            {
                m_IsMoving = false;
                if (m_Skeleton != null)
                {
                    m_Skeleton.AnimationState.SetAnimation(0, m_IdleAnim, true);
                }
            }
        }
    }

    /// <summary>
    /// 将目标位置钳制到 navmesh 可行走区域内。
    /// 始终返回 navmesh 上最近的可行走点，确保角色不会离开 navmesh。
    /// </summary>
    private Vector3 ClampToNavMesh(Vector3 position)
    {
        if (AstarPath.active == null)
            return position;

        var info = AstarPath.active.GetNearest(position, NearestNodeConstraint.Walkable);
        if (info.node == null || !info.node.Walkable)
            return transform.position; // 完全找不到可行走点，保持原位

        // 始终使用 navmesh 投影点，保留原始 Y 高度
        return new Vector3(info.position.x, position.y, info.position.z);
    }

    private void OnDrawGizmosSelected()
    {
        // 画出最近可行走点
        if (Application.isPlaying && AstarPath.active != null)
        {
            var info = AstarPath.active.GetNearest(transform.position, NearestNodeConstraint.Walkable);
            if (info.node != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, info.position);
                Gizmos.DrawSphere(info.position, 0.1f);
            }
        }
    }
}
