using System.Collections.Generic;
using UnityEngine;
using UnityGameObject = UnityEngine.GameObject;
using ET;

namespace YIUIFramework
{
    /// <summary>
    /// 可以自动回收的对象池
    /// </summary>
    internal class YIUIAutoRecycleAsyncObjectPool : IRefPool
    {
        private EntityRef<Entity> m_EntityRef;
        private ITimeProvider m_Timer;
        private string m_ResName;
        public string ResName => m_ResName;
        private bool m_InitPoolInfo;

        private bool m_HaveUpdate;
        private float m_Timeout; // 最大显示时间 <=0 表示永不过期
        private int m_MaxCacheCount = int.MaxValue; // 同时显示的最大数量 <=0 表示不限制
        private float m_CacheTime; // 缓存池中保留的时间 <=0 表示永久保留
        private int m_MinCacheCount = int.MaxValue; // 缓存池中的最大数量 <0 表示不限制
        private string m_MaxCacheCountNewResName; //超过最大显示时 返回一个新的资源

        private float m_LastIdleTime; //上一次空闲时间
        private readonly float m_IdleTimeout = 60; //空闲时长 超过这个时间 则回收对象池

        private class PoolVo : IRefPool
        {
            public float EndTime;
            public GameObject Value;
            public bool IsMax;

            public void Recycle()
            {
                EndTime = 0;
                Value = null;
                IsMax = false;
            }
        }

        public void Recycle()
        {
            Clear();
            m_EntityRef = default;
            m_Timer = null;
            m_ResName = null;
            m_HaveUpdate = false;
            m_InitPoolInfo = false;
            m_MaxCacheCount = int.MaxValue;
            m_MinCacheCount = int.MaxValue;
            m_MaxCacheCountNewResName = null;
            m_CacheTime = 0;
            m_Timeout = 0;
            m_LastIdleTime = 0;
        }

        private readonly Dictionary<GameObject, PoolVo> m_Uses = new();
        private readonly List<PoolVo> m_Pools = new();
        private readonly List<PoolVo> m_MaxPools = new();

        internal void ResetInitInfo(Entity entity, ITimeProvider timer, string resName)
        {
            m_EntityRef = entity;
            m_Timer = timer;
            m_ResName = resName;
        }

        private async ETTask<GameObject> InstantiateGameObjectAsync(string resName)
        {
            var obj = await EventSystem.Instance.YIUIInvokeEntityAsync<YIUIInvokeEntity_InstantiateGameObject, ETTask<UnityGameObject>>(m_EntityRef, new YIUIInvokeEntity_InstantiateGameObject { ResName = resName });
            if (obj == null)
            {
                Debug.LogError($"没有加载到这个资源 {resName}");
                return null;
            }

            obj.AddComponent<YIUIGameObjectPoolAutoRelease>().m_EntityRef = m_EntityRef;

            if (!m_InitPoolInfo)
            {
                //优先级：配置 > 组件
                IYIUIGameObjectPoolSettingsConfig configSettings = YIUIGameObjectPool.Inst.GetSettings(m_ResName) ?? obj.GetComponent<YIUIGameObjectPoolInfo>();
                if (configSettings != null)
                {
                    m_CacheTime = configSettings.CacheTime > 0 ? configSettings.CacheTime : 0;
                    m_Timeout = configSettings.Timeout > 0 ? configSettings.Timeout : 0;
                    m_MaxCacheCount = configSettings.MaxCacheCount > 0 ? configSettings.MaxCacheCount : int.MaxValue;
                    m_MinCacheCount = configSettings.MinCacheCount >= 0 ? configSettings.MinCacheCount : int.MaxValue;
                    m_HaveUpdate = m_Timeout > 0 || m_CacheTime > 0;
                    m_MaxCacheCountNewResName = configSettings.MaxCacheCountNewResName;

                    if (configSettings.MaxCacheCount > 0 && string.IsNullOrEmpty(m_MaxCacheCountNewResName))
                    {
                        Debug.LogError($"{m_ResName} 最大缓存数量为 {configSettings.MaxCacheCount}，但是没有设置新的资源 强制修改为无上限制");
                        m_MaxCacheCount = int.MaxValue;
                    }
                }

                m_InitPoolInfo = true;
            }

            return obj;
        }

        /// <summary>
        /// 获取一个对象
        /// </summary>
        internal async ETTask<GameObject> Get()
        {
            var result = m_Uses.Count >= m_MaxCacheCount ? await GetByMaxPool() : await GetByPool();
            if (result.Value == null)
            {
                RefPool.Put(result);
                return null;
            }

            result.EndTime = m_Timeout > 0 ? m_Timer.Time + m_Timeout : 0; //使用倒计时
            m_Uses.Add(result.Value, result);
            return result.Value;
        }

        private async ETTask<PoolVo> GetByPool()
        {
            if (m_Pools.Count > 0)
            {
                return m_Pools.Pop();
            }

            var result = RefPool.Get<PoolVo>();
            result.IsMax = false;
            result.Value = await InstantiateGameObjectAsync(m_ResName);
            return result;
        }

        private async ETTask<PoolVo> GetByMaxPool()
        {
            if (m_MaxPools.Count > 0)
            {
                return m_MaxPools.Pop();
            }

            var result = RefPool.Get<PoolVo>();
            result.IsMax = true;
            result.Value = await InstantiateGameObjectAsync(m_MaxCacheCountNewResName);
            return result;
        }

        /// <summary>
        /// 回收一个对象
        /// </summary>
        internal bool Put(GameObject value)
        {
            if (value == null)
            {
                Debug.LogError($"回收对象为空，无法回收！{m_ResName}");
                return false;
            }

            if (!m_Uses.ContainsKey(value))
            {
                Debug.LogError($"对象 {value.name} 不在 {m_ResName}缓存池中，无法回收！直接删除");
                UnityEngine.Object.Destroy(value);
                return false;
            }

            var poolVo = m_Uses[value];
            m_Uses.Remove(value);

            //没有缓存时间 但是有缓存数量限制
            if (m_CacheTime <= 0 && m_MinCacheCount >= 0)
            {
                if (m_Pools.Count >= m_MinCacheCount)
                {
                    //超过的就直接摧毁 不回收了
                    DestroyRemove(poolVo);
                    return false;
                }
            }

            poolVo.EndTime = m_CacheTime > 0 ? m_Timer.Time + m_CacheTime : 0; //缓存倒计时

            if (poolVo.IsMax)
            {
                m_MaxPools.Add(poolVo);
            }
            else
            {
                m_Pools.Add(poolVo);
            }

            return true;
        }

        private void DestroyRemove(PoolVo poolVo)
        {
            var value = poolVo.Value;
            if (value != null)
            {
                UnityEngine.Object.Destroy(value);
            }
            else
            {
                Debug.LogError($"缓存池中存在空对象，请检查！你不应该删除被缓存管理的对象 {m_ResName}");
            }

            RefPool.Put(poolVo);
        }

        //外部准备摧毁对象 这里只是移除引用
        internal void DestroyRemove(GameObject value)
        {
            if (!m_Uses.ContainsKey(value)) return;
            var poolVo = m_Uses[value];
            m_Uses.Remove(value);
            RefPool.Put(poolVo);
        }

        //清楚所有缓存 包括正在使用的 所以慎用
        internal void Clear()
        {
            //回收使用中的对象
            foreach (var poolVo in m_Uses.Values)
            {
                DestroyRemove(poolVo);
            }

            m_Uses.Clear();

            //回收池中所有对象
            for (int i = 0; i < m_Pools.Count; i++)
            {
                DestroyRemove(m_Pools[i]);
            }

            m_Pools.Clear();

            //回收最大缓存池中所有对象  
            for (int i = 0; i < m_MaxPools.Count; i++)
            {
                DestroyRemove(m_MaxPools[i]);
            }

            m_MaxPools.Clear();
        }

        internal void Update()
        {
            if (!m_HaveUpdate) return;
            CheckTimeout();
            CheckCacheTime();
            CheckMaxPoolCacheTime();
        }

        private void CheckTimeout()
        {
            if (m_Timeout <= 0) return;

            var count = m_Uses.Count;
            if (count < 1)
            {
                return;
            }

            //优化 没必要一次性吧所有超时的都遍历出来 允许一帧回收1个 这样就不需要每次遍历且还需要临时列表存储了
            foreach (var poolVo in m_Uses.Values)
            {
                if (m_Timer.Time >= poolVo.EndTime)
                {
                    if (poolVo.Value == null)
                    {
                        foreach (var item in m_Uses)
                        {
                            if (item.Value == poolVo)
                            {
                                m_Uses.Remove(item.Key);
                                DestroyRemove(poolVo);
                                break;
                            }
                        }
                    }
                    else
                    {
                        YIUIGameObjectPool.Inst.Put(poolVo.Value);
                    }

                    break;
                }
            }
        }

        private void CheckCacheTime()
        {
            if (m_CacheTime <= 0) return;

            var count = m_Pools.Count;
            if (count <= m_MinCacheCount)
            {
                return;
            }

            foreach (var poolVo in m_Pools)
            {
                if (m_Timer.Time >= poolVo.EndTime)
                {
                    m_Pools.Remove(poolVo);
                    DestroyRemove(poolVo);
                    break;
                }
            }
        }

        private void CheckMaxPoolCacheTime()
        {
            if (m_CacheTime <= 0) return;

            var count = m_MaxPools.Count;
            if (count <= 0)
            {
                return;
            }

            foreach (var poolVo in m_MaxPools)
            {
                if (m_Timer.Time >= poolVo.EndTime)
                {
                    m_MaxPools.Remove(poolVo);
                    DestroyRemove(poolVo);
                    break;
                }
            }
        }

        //是否空闲 如果空闲则回收对象池
        internal bool IsIdle()
        {
            if (m_Uses.Count < 1 && m_Pools.Count < 1)
            {
                if (m_LastIdleTime <= 0)
                {
                    m_LastIdleTime = m_Timer.Time;
                }

                if (m_Timer.Time - m_LastIdleTime > m_IdleTimeout)
                {
                    return true;
                }
            }
            else
            {
                m_LastIdleTime = 0;
            }

            return false;
        }
    }
}