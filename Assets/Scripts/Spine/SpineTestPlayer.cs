using System;
using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// Spine 动画测试播放器。挂在带 SkeletonAnimation 的 GO 上，
/// 运行时在 Inspector 中选择动画播放。与 ET 无关联。
/// </summary>
public class SpineTestPlayer : MonoBehaviour
{
    [Header("动画控制")]
    [SpineAnimation]
    public string CurrentAnimation;

    public bool Loop = true;

    [Range(0.1f, 3f)]
    public float TimeScale = 1f;

    [Header("状态（只读）")]
    [SerializeField]
    private string[] m_AnimationNames;

    private SkeletonAnimation m_SkeletonAnimation;
    private string m_LastAnimation;

    private void Awake()
    {
        m_SkeletonAnimation = GetComponent<SkeletonAnimation>();
        if (m_SkeletonAnimation == null)
        {
            Debug.LogError("[SpineTestPlayer] No SkeletonAnimation found on this GameObject!");
            return;
        }

        // 缓存所有动画名称（Inspector 只读展示）
        var animations = m_SkeletonAnimation.Skeleton.Data.Animations;
        m_AnimationNames = new string[animations.Count];
        for (int i = 0; i < animations.Count; i++)
        {
            m_AnimationNames[i] = animations.Items[i].Name;
        }

        // 默认播放第一个动画
        if (string.IsNullOrEmpty(CurrentAnimation) && m_AnimationNames.Length > 0)
        {
            CurrentAnimation = m_AnimationNames[0];
        }

        PlayAnimation(CurrentAnimation, Loop);
    }

    private void Update()
    {
        if (m_SkeletonAnimation == null)
        {
            return;
        }

        m_SkeletonAnimation.timeScale = TimeScale;

        // Inspector 中切换了动画
        if (CurrentAnimation != m_LastAnimation)
        {
            PlayAnimation(CurrentAnimation, Loop);
        }
    }

    private void PlayAnimation(string animationName, bool loop)
    {
        if (m_SkeletonAnimation == null || string.IsNullOrEmpty(animationName))
        {
            return;
        }

        m_SkeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
        m_LastAnimation = animationName;
        Debug.Log($"[SpineTestPlayer] Playing: {animationName} (loop={loop})");
    }
}
