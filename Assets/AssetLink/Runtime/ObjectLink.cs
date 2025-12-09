using System;

using Object = UnityEngine.Object;

namespace xpTURN.Link
{
    /// <summary>
    /// The ObjectLink class manages objects asynchronously loaded via Addressables.
    /// </summary>
    [System.Serializable]
    public partial class ObjectLink<T> : AssetLink, IObjectLink<T> where T : Object
    {
        #region Private Fields
        private WeakReference<Action<T>> _wrCallback = new(null);
        #endregion

        #region Private Methods
        private void OnLoaded(Object asset)
        {
            if (_wrCallback.TryGetTarget(out var callback))
            {
                callback.Invoke(asset as T);
            }
        }
        #endregion

        #region Public Methods
        public ObjectLink()
        {
#if UNITY_EDITOR
            LinkableTypeForInspector = typeof(T);
#endif
        }

        public void LoadAssetAsync(System.Action<T> onAssetLoaded)
        {
            _wrCallback.SetTarget(onAssetLoaded);
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }

            if (IsLoad)
            {
                DebugLogger.LogError($"[ObjectLink] Already loaded: {Key}");
                OnLoaded(GetResult());
                return;
            }

            IsLoad = true;
            AddressableManager.LoadAssetAsync(this, OnLoaded);
        }

        public void Release()
        {
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }

            if (IsLoad == false)
            {
                DebugLogger.LogError($"[ObjectLink] Not loaded: {Key}");
                return;
            }

            AddressableManager.ReleaseAsset(this);
            _wrCallback.SetTarget(null);
            IsLoad = false;
        }

        public T GetResult()
        {
            if (string.IsNullOrEmpty(Key))
            {
                DebugLogger.LogError("[AssetLink] GetResult: Key is not assigned.");
                return null;
            }
            if (IsLoad == false)
            {
                DebugLogger.LogError($"[AssetLink] GetResult: Asset not loaded for key {Key}.");
                return null;
            }
            var asyncHandle = AddressableManager.GetHandler(Key);
            if (asyncHandle == null)
            {
                DebugLogger.LogError($"[AssetLink] GetResult: AsyncHandler not found for key {Key}");
                return null;
            }

            return asyncHandle.Handle.Result as T;
        }
        #endregion
    }
}
