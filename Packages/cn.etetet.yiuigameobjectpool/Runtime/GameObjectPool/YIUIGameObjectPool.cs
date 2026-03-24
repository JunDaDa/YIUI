using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YIUIFramework;

namespace ET
{
    [YIUISingleton]
    public class YIUIGameObjectPool : YIUIMonoSingleton<YIUIGameObjectPool>, ITimeProvider
    {
        protected override void OnInitSingleton()
        {
            InitPool();
        }

        protected override void OnDestroy()
        {
            if (YIUISingletonHelper.IsQuitting) return;
            SceneManager.sceneLoaded -= OnSceneLoadedHandler;
            base.OnDestroy();
            this.ClearPool();
        }

        public void Update()
        {
            Time += UnityEngine.Time.deltaTime;
            foreach (var pool in m_Pools.Values)
            {
                pool.Update();
                if (pool.IsIdle())
                {
                    m_Pools.Remove(pool.ResName);
                    RefPool.Put(pool);
                    break;
                }
            }
        }

        //切换场景过后是否重置对象池
        private const bool m_SceneLoadedResetPool = true;

        private void OnSceneLoadedHandler(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            if (m_SceneLoadedResetPool)
            {
                ClearPool();
                InitPool();
            }
        }

        //初始化
        private void InitPool()
        {
            Time = 0;
            SceneManager.sceneLoaded -= OnSceneLoadedHandler;
            SceneManager.sceneLoaded += OnSceneLoadedHandler;
            m_PoolRoot = new GameObject("YIUIGameObjectPoolRoot");
            m_PoolRootTransform = m_PoolRoot.transform;
            m_PoolRoot.SetActive(false);
            m_PoolRoot.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(m_PoolRoot);
        }

        //清理
        private void ClearPool()
        {
            foreach (var pool in m_Pools.Values)
            {
                RefPool.Put(pool);
            }

            UnityEngine.Object.Destroy(m_PoolRoot);
            m_PoolRoot = null;
            m_PoolRootTransform = null;
            Time = 0;
            m_Pools.Clear();
            m_GameObjectToResName.Clear();
        }

        private GameObject m_PoolRoot;
        private Transform m_PoolRootTransform;

        public float Time { get; private set; }

        private readonly Dictionary<string, YIUIAutoRecycleAsyncObjectPool> m_Pools = new();

        private readonly Dictionary<GameObject, string> m_GameObjectToResName = new();

        public IYIUIGameObjectPoolSettingsConfig GetSettings(string resName)
        {
            return EventSystem.Instance.Invoke<YIUIInvokeGetGameObjectPoolSettings, IYIUIGameObjectPoolSettingsConfig>(new YIUIInvokeGetGameObjectPoolSettings
            {
                ResName = resName
            });
        }

        public async ETTask<GameObject> Get(string resName, Transform parent = null)
        {
            var pool = GetAutoRecycleAsyncObjectPool(resName);
            var go = await pool.Get();

            //TODO 还可处理超过最大容量 临时处理返回null 
            if (go == null) return null;

            if (parent != null)
            {
                //更多的Parent 操作 可以自行返回后添加
                go.transform.SetParent(parent);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }

            m_GameObjectToResName.Add(go, resName);
            return go;
        }

        public bool Put(GameObject obj)
        {
            if (YIUISingletonHelper.IsQuitting)
            {
                return true;
            }

            if (!m_GameObjectToResName.Remove(obj, out string resName))
            {
                return false;
            }

            var pool = GetAutoRecycleAsyncObjectPool(resName);
            var result = pool.Put(obj);
            if (result)
            {
                obj.transform.SetParent(m_PoolRootTransform);
            }

            return result;
        }

        internal void DestroyRemove(GameObject obj)
        {
            if (!m_GameObjectToResName.Remove(obj, out string resName))
            {
                return;
            }

            var pool = GetAutoRecycleAsyncObjectPool(resName);
            pool.DestroyRemove(obj);
        }

        private YIUIAutoRecycleAsyncObjectPool GetAutoRecycleAsyncObjectPool(string resName)
        {
            if (!m_Pools.ContainsKey(resName))
            {
                var pool = RefPool.Get<YIUIAutoRecycleAsyncObjectPool>();
                pool.ResetInitInfo(Entity, this, resName);
                m_Pools.Add(resName, pool);
            }

            return m_Pools[resName];
        }
    }

    public struct YIUIInvokeGetGameObjectPoolSettings
    {
        public string ResName;
    }
}