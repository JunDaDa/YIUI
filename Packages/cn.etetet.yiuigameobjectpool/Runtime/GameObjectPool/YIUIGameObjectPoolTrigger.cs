using ET;
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace YIUIFramework
{
    /// <summary>
    /// 对象池触发器
    /// 根据资源名称，从对象池中获取或创建GameObject，并在OnEnable和OnDisable时自动回收或缓存GameObject
    /// 一般挂载在空节点下
    /// 配合UIDataBindActive使用更好
    /// </summary>
    [LabelText("对象池触发器")]
    [AddComponentMenu("YIUIBind/Data/对象池触发器 【PoolTrigger】 YIUIDataGameObjectPoolTriggerYIUIBind")]
    public class YIUIGameObjectPoolTrigger : SerializedMonoBehaviour
    {
        [ShowInInspector]
        [SerializeField]
        [ReadOnly]
        [ShowIf("Show")]
        [LabelText("资源名称")]
        private string m_ResName;

        [BoxGroup("延迟", centerLabel: true)]
        [ShowInInspector]
        [SerializeField]
        [LabelText("延迟触发")]
        private bool m_DelayTrigger;

        [BoxGroup("延迟", centerLabel: true)]
        [ShowInInspector]
        [SerializeField]
        [LabelText("延迟时间(毫秒)")]
        [ShowIf(nameof(m_DelayTrigger))]
        private int m_DelayTime;

        [BoxGroup("偏移", centerLabel: true)]
        [ShowInInspector]
        [SerializeField]
        [LabelText("偏移")]
        private bool m_Offset;

        [BoxGroup("偏移", centerLabel: true)]
        [ShowInInspector]
        [SerializeField]
        [LabelText("位置偏移")]
        [ShowIf(nameof(m_Offset))]
        private Vector3 m_PositionOffset;

        [BoxGroup("偏移", centerLabel: true)]
        [ShowInInspector]
        [SerializeField]
        [LabelText("旋转偏移")]
        [ShowIf(nameof(m_Offset))]
        private Vector3 m_RotationOffset;

        [BoxGroup("偏移", centerLabel: true)]
        [ShowInInspector]
        [SerializeField]
        [LabelText("缩放偏移")]
        [ShowIf(nameof(m_Offset))]
        private Vector3 m_ScaleOffset = Vector3.one;

        #if UNITY_EDITOR

        [PropertyOrder(-900)]
        [HideLabel]
        [ShowInInspector, PreviewField(45, ObjectFieldAlignment.Left)]
        [OnValueChanged("OnValueChanged")]
        [NonSerialized]
        [Required("请选择一个预制体")]
        private GameObject m_SourceObject;

        private void OnValueChanged()
        {
            if (m_SourceObject == null)
            {
                m_ResName = "";
                return;
            }

            if (m_GameObject != null)
            {
                DestroyImmediate(m_GameObject);
            }

            m_ResName = m_SourceObject.name;
            m_SourceObject = GetSourceObject(false);
            if (m_SourceObject == null)
            {
                UnityTipsHelper.ShowErrorContext(gameObject, $"资源 [{m_ResName}] 不是预制体 请选择一个预制体!!!");
                m_ResName = "";
            }
        }

        private bool Show()
        {
            if (string.IsNullOrEmpty(m_ResName))
            {
                m_SourceObject = null;
                return true;
            }

            if (m_SourceObject != null)
            {
                return true;
            }

            m_SourceObject = GetSourceObject();

            return true;
        }

        //这个方法也可以作为外部调用统一检查 是否有无效配置
        //TODO 做一个可视化的检查工具
        public GameObject GetSourceObject(bool logError = true)
        {
            if (string.IsNullOrEmpty(m_ResName))
            {
                Debug.LogError($"{gameObject.name} 资源名称不能为空", gameObject);
                return null;
            }

            var assets = UnityEditor.AssetDatabase.FindAssets($"{m_ResName} t:Prefab", null);
            if (assets == null || assets.Length == 0)
            {
                if (logError)
                {
                    Debug.LogError($"{gameObject.name} 找不到资源: {m_ResName}", gameObject);
                }

                return null;
            }

            GameObject resObj = null;
            var paths = new List<string>();
            foreach (var guid in assets)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (obj == null)
                {
                    if (logError)
                    {
                        Debug.LogError($"{gameObject.name} 找不到资源: {m_ResName} path: {path}", gameObject);
                    }
                }
                else
                {
                    if (obj.name == m_ResName)
                    {
                        resObj = obj;
                        paths.Add(path);
                    }
                }
            }

            if (paths.Count > 1)
            {
                var errorTips = $"{gameObject.name} 找到多个资源: {m_ResName} 请确保资源名称唯一";
                foreach (var path in paths)
                {
                    errorTips += $"\n{path}";
                }

                Debug.LogError(errorTips, gameObject);
            }

            if (resObj != null)
            {
                return resObj;
            }

            if (logError)
            {
                Debug.LogError($"{gameObject.name} 找不到资源: {m_ResName}", gameObject);
            }

            return null;
        }

        [PropertyOrder(-1000)]
        [GUIColor(0, 1, 0)]
        [ButtonGroup]
        [Button("加载对象 (调试用)", 20)]
        private void LoadObject()
        {
            if (string.IsNullOrEmpty(m_ResName))
            {
                UnityTipsHelper.Show("资源名称不能为空 请先选择需要调试的资源");
                return;
            }

            if (m_SourceObject == null) return;
            m_GameObject = UnityEditor.PrefabUtility.InstantiatePrefab(m_SourceObject, transform) as GameObject;
            m_GameObject.hideFlags = HideFlags.DontSave;
            var selfTransform = m_GameObject.transform;
            selfTransform.localPosition = m_Offset ? m_PositionOffset : Vector3.zero;
            selfTransform.localRotation = m_Offset ? Quaternion.Euler(m_RotationOffset) : Quaternion.identity;
            selfTransform.localScale = m_Offset ? m_ScaleOffset : Vector3.one;
            UnityEditor.Selection.activeObject = m_GameObject;
        }

        [PropertyOrder(-1000)]
        [GUIColor(1, 0.6f, 0.4f)]
        [ButtonGroup]
        [Button("删除对象 (调试用)", 20)]
        private void DestroyObject()
        {
            if (m_GameObject == null) return;
            DestroyImmediate(m_GameObject);
        }

        [BoxGroup("偏移", centerLabel: true)]
        [GUIColor(0.4f, 0.8f, 1)]
        [Button("同步偏移 (调试用)", 30)]
        [ShowIf(nameof(ShowIfSyncOffset))]
        private void SyncOffset()
        {
            if (m_GameObject == null) return;
            if (!m_Offset) return;

            var sourceTransform = m_GameObject.transform;
            m_PositionOffset = sourceTransform.localPosition;
            m_RotationOffset = sourceTransform.localRotation.eulerAngles;
            m_ScaleOffset = sourceTransform.localScale;
        }

        private bool ShowIfSyncOffset()
        {
            if (!m_Offset)
            {
                return false;
            }

            if (m_GameObject == null)
            {
                return false;
            }

            if (UIOperationHelper.IsPlaying())
            {
                return false;
            }

            var sourceTransform = m_GameObject.transform;

            if (m_PositionOffset != sourceTransform.localPosition)
            {
                return true;
            }

            if (m_RotationOffset != sourceTransform.localRotation.eulerAngles)
            {
                return true;
            }

            if (m_ScaleOffset != sourceTransform.localScale)
            {
                return true;
            }

            return false;
        }

        #endif

        [NonSerialized]
        private bool m_Loading;

        [NonSerialized]
        private GameObject m_GameObject;

        [NonSerialized]
        private ETCancellationToken m_CancelToken;

        private void OnEnable()
        {
            if (m_Loading || m_GameObject != null) return;
            Load().NoContext();
        }

        private void OnDisable()
        {
            if (YIUISingletonHelper.IsQuitting) return;
            m_CancelToken?.Cancel();
            m_CancelToken = null;
            m_Loading = false;
            if (m_GameObject == null) return;
            DisablePut(m_GameObject).NoContext();
            m_GameObject = null;
        }

        private void OnDestroy()
        {
            if (YIUISingletonHelper.IsQuitting) return;
            m_CancelToken?.Cancel();
            m_CancelToken = null;
            if (m_GameObject == null) return;
            YIUIGameObjectPool.Inst?.Put(m_GameObject);
            m_GameObject = null;
        }

        private async ETTask Load()
        {
            using var coroutineLock = await EventSystem.Instance?.YIUIInvokeEntityAsync<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(YIUISingletonHelper.YIUIMgr, new YIUIInvokeEntity_CoroutineLock { Lock = this.GetHashCode() });

            if (m_Loading || m_GameObject != null) return;

            m_Loading = true;
            m_CancelToken = new ETCancellationToken();

            try
            {
                if (m_DelayTrigger)
                {
                    await EventSystem.Instance.YIUIInvokeEntityAsync<YIUIInvokeEntity_WaitAsync, ETTask>(YIUISingletonHelper.YIUIMgr, new YIUIInvokeEntity_WaitAsync
                    {
                        Time = m_DelayTime,
                        CancellationToken = m_CancelToken
                    });

                    if (m_CancelToken.IsCancel())
                    {
                        return;
                    }
                }

                var loadGameObject = await YIUIGameObjectPool.Inst.Get(m_ResName, gameObject.transform);

                if (m_CancelToken.IsCancel())
                {
                    YIUIGameObjectPool.Inst?.Put(loadGameObject);
                    return;
                }

                m_GameObject = loadGameObject;

                if (m_Offset && m_GameObject != null)
                {
                    var selfTransform = m_GameObject.transform;
                    selfTransform.localPosition = m_PositionOffset;
                    selfTransform.localRotation = Quaternion.Euler(m_RotationOffset);
                    selfTransform.localScale = m_ScaleOffset;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                m_Loading = false;
            }
        }

        private async ETTask DisablePut(GameObject obj)
        {
            if (EventSystem.Instance == null) return;

            using var coroutineLock = await EventSystem.Instance.YIUIInvokeEntityAsync<YIUIInvokeEntity_CoroutineLock, ETTask<Entity>>(YIUISingletonHelper.YIUIMgr, new YIUIInvokeEntity_CoroutineLock { Lock = this.GetHashCode() });

            await EventSystem.Instance.YIUIInvokeEntityAsync<YIUIInvokeEntity_WaitFrameAsync, ETTask>(YIUISingletonHelper.YIUIMgr, new YIUIInvokeEntity_WaitFrameAsync());

            if (YIUISingletonHelper.IsQuitting) return;

            if (obj != null)
            {
                YIUIGameObjectPool.Inst?.Put(obj);
            }
        }
    }
}