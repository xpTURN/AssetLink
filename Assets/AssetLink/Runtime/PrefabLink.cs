using System;

using UnityEngine;
using Object = UnityEngine.Object;

namespace xpTURN.Link
{
    /// <summary>
    /// The PrefabLink class asynchronously instantiates prefabs via Addressables.
    /// </summary>
    [System.Serializable]
    public partial class PrefabLink : AssetLink, IPrefabLink
    {
        #region Private Fields
        private WeakReference<Action<GameObject>> _wrCallback = new(null);
        #endregion

        #region Private Methods
        private void OnInstantiate(Object obj)
        {
            if (_wrCallback.TryGetTarget(out var callback))
            {
                callback.Invoke(obj as GameObject);
            }
        }
        #endregion

        #region Public Methods
        public PrefabLink()
        {
#if UNITY_EDITOR
            LinkableTypeForInspector = typeof(GameObject);
#endif
        }

        public void InstantiateAsync(Action<GameObject> onObjectInstantiated, Transform parent = null, bool worldSpace = true)
        {
            _wrCallback.SetTarget(onObjectInstantiated);
            InstantiateAsync(OnInstantiate, Vector3.zero, Quaternion.identity, parent, worldSpace);
        }

        public void InstantiateAsync(Action<GameObject> onObjectInstantiated, Vector3 position, Quaternion rotation, Transform parent = null, bool worldSpace = true)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }

            IsLoad = true;
            _wrCallback.SetTarget(onObjectInstantiated);
            AddressableManager.InstantiateAsync(this, OnInstantiate, position, rotation, parent, worldSpace);
        }
        #endregion
    }
}
