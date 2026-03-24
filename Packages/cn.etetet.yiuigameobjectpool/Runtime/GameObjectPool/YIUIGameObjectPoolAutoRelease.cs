using ET;
using UnityEngine;

namespace YIUIFramework
{
    /// <summary>
    /// 被对象池管理的对象
    /// </summary>
    [AddComponentMenu("")] // 不要显示在添加组件菜单中 由代码自动添加
    public class YIUIGameObjectPoolAutoRelease : MonoBehaviour
    {
        public EntityRef<Entity> m_EntityRef;

        private void OnDestroy()
        {
            if (YIUISingletonHelper.IsQuitting) return;
            YIUIGameObjectPool.Inst?.DestroyRemove(this.gameObject);
            if (m_EntityRef.Entity == null || m_EntityRef.Entity.IsDisposed) return;
            EventSystem.Instance?.YIUIInvokeEntitySync(m_EntityRef, new YIUIInvokeEntity_ReleaseInstantiate { obj = this.gameObject });
        }
    }
}