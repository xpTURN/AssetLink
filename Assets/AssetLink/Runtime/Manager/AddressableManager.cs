using System;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace xpTURN.Link
{
    /// <summary>
    /// AddressableManager is a class that manages objects asynchronously loaded through Addressables.
    /// </summary>
    internal static partial class AddressableManager
    {
        #region Public Methods : Scene
        public static void LoadSceneAsync(string key, Action<SceneInstance> onSceneLoaded, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true)
        {
            if (_handlers.TryGetValue(key, out var asyncHandle))
            {
                DebugLogger.LogError($"[AddressableManager] LoadSceneAsync: {asyncHandle.Key}, Duplicated called!");
                return;
            }

            asyncHandle = _handlerPool.Get();
            asyncHandle.LoadSceneAsync(key, onSceneLoaded, loadMode, activateOnLoad);
            DebugLogger.Log($"[AddressableManager] LoadSceneAsync: {asyncHandle.Key}, RefCount:{asyncHandle.RefLinkCount}");

            _handlers.Add(key, asyncHandle);
        }

        public static void UnloadSceneAsync(string key, Action<string> onSceneUnloaded = null)
        {
            DebugLogger.Log($"[AddressableManager] UnloadSceneAsync: {key}");
            if (!_handlers.TryGetValue(key, out var asyncHandle))
            {
                DebugLogger.LogError($"[AddressableManager] UnloadSceneAsync: AsyncHandle not found {key}");
                return;
            }

            asyncHandle.UnloadSceneAsync(key, onSceneUnloaded);
            DebugLogger.Log($"[AddressableManager] UnloadSceneAsync: {asyncHandle.Key}, RefCount: {asyncHandle.RefLinkCount}");

            if (asyncHandle.IsUnused(false))
            {
                RemoveHandler(asyncHandle.Key);
            }
        }

        public static void LoadSceneAsync(IAssetLink assetLink, Action<SceneInstance> onSceneLoaded, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true)
        {
            if (_handlers.TryGetValue(assetLink.Key, out var asyncHandle))
            {
                DebugLogger.LogError($"[AddressableManager] LoadSceneAsync: {asyncHandle.Key}, Duplicated called!");
                return;
            }

            asyncHandle = _handlerPool.Get();
            asyncHandle.LoadSceneAsync(assetLink, onSceneLoaded, loadMode, activateOnLoad);
            DebugLogger.Log($"[AddressableManager] LoadSceneAsync: {asyncHandle.Key}, RefCount:{asyncHandle.RefLinkCount}");

            _handlers.Add(assetLink.Key, asyncHandle);
        }

        public static void UnloadSceneAsync(IAssetLink assetLink, Action<string> onSceneUnloaded = null)
        {
            DebugLogger.Log($"[AddressableManager] UnloadSceneAsync: {assetLink.Key}");
            if (!_handlers.TryGetValue(assetLink.Key, out var asyncHandle))
            {
                DebugLogger.LogError($"[AddressableManager] UnloadSceneAsync: AsyncHandle not found {assetLink.Key}");
                return;
            }

            asyncHandle.UnloadSceneAsync(assetLink, onSceneUnloaded);
            DebugLogger.Log($"[AddressableManager] UnloadSceneAsync: {asyncHandle.Key}, RefCount: {asyncHandle.RefLinkCount}");

            if (asyncHandle.IsUnused(false))
            {
                RemoveHandler(asyncHandle.Key);
            }
        }
        #endregion

        #region Public Methods : Object
        public static void LoadAssetAsync(IAssetLink assetLink, Action<Object> onAssetLoaded)
        {
            if (_handlers.TryGetValue(assetLink.Key, out var asyncHandle))
            {
                asyncHandle.LoadAsync(assetLink, onAssetLoaded);
                DebugLogger.Log($"[AddressableManager] LoadAssetAsync: {asyncHandle.Key}, RefCount:{asyncHandle.RefLinkCount}");
                return;
            }

            asyncHandle = _handlerPool.Get();
            asyncHandle.LoadAsync(assetLink, onAssetLoaded);
            DebugLogger.Log($"[AddressableManager] LoadAssetAsync: {asyncHandle.Key}, RefCount:{asyncHandle.RefLinkCount}");

            _handlers.Add(assetLink.Key, asyncHandle);
        }

        public static void ReleaseAsset(IAssetLink assetLink)
        {
            if (!_handlers.TryGetValue(assetLink.Key, out var asyncHandle))
            {
                DebugLogger.LogError($"[AddressableManager] ReleaseAsset: Asset not found {assetLink.Key}");
                return;
            }

            asyncHandle.Release(assetLink);
            DebugLogger.Log($"[AddressableManager] ReleaseAsset: {assetLink.Key}, RefCount: {asyncHandle.RefLinkCount}");

            if (asyncHandle.IsUnused(false))
            {
                RemoveHandler(asyncHandle.Key);
            }
        }
        #endregion

        #region Public Methods : Prefab
        public static void InstantiateAsync(IAssetLink assetLink, Action<Object> onObjectInstantiated, Transform parent = null, bool worldSpace = true)
        {
            InstantiateAsync(assetLink, onObjectInstantiated, Vector3.zero, Quaternion.identity, parent, worldSpace);
        }

        public static void InstantiateAsync(IAssetLink assetLink, Action<Object> onObjectInstantiated, Vector3 position, Quaternion rotation, Transform parent = null, bool worldSpace = true)
        {
            if (_handlers.TryGetValue(assetLink.Key, out var asyncHandle))
            {
                asyncHandle.InstantiateAsync(assetLink, onObjectInstantiated, position, rotation, parent, worldSpace);
                DebugLogger.Log($"[AddressableManager] InstantiateAsync: {asyncHandle.Key}, RefCount:{asyncHandle.RefLinkCount}");
                return;
            }

            asyncHandle = _handlerPool.Get();
            asyncHandle.InstantiateAsync(assetLink, onObjectInstantiated, position, rotation, parent, worldSpace);
            DebugLogger.Log($"[AddressableManager] InstantiateAsync: {asyncHandle.Key}, RefCount:{asyncHandle.RefLinkCount}");

            _handlers.Add(assetLink.Key, asyncHandle);
        }

        public static void ReleaseInstance(AddressableManager.AsyncHandler asyncHandle, AddressableManager.AsyncHandler.Item item)
        {
            if (asyncHandle == null)
            {
                DebugLogger.LogError("[AddressableManager] ReleaseInstance: asyncHandle is null");
                return;
            }

            if (item == null)
            {
                DebugLogger.LogError("[AddressableManager] ReleaseInstance: item is null");
                return;
            }

            if (item.IsInstantiate == false)
            {
                DebugLogger.LogError($"[AddressableManager] ReleaseInstance: Asset is not instantiated {asyncHandle.Key}");
                return;
            }

            asyncHandle.ReleaseInstance(item);
            DebugLogger.Log($"[AddressableManager] ReleaseInstance: {asyncHandle.Key}, RefCount: {asyncHandle.RefLinkCount}");

            if (asyncHandle.IsUnused(false))
            {
                RemoveHandler(asyncHandle.Key);
            }
        }
        #endregion

        #region Public Methods
        public static void ReleaseAllDeletedObjects(bool report = false)
        {
            DebugLogger.Log("[AddressableManager] ReleaseAllDeletedObjects");

            foreach (var kvp in _handlers)
            {
                if (kvp.Value.IsUnused(report))
                {
                    _keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in _keysToRemove)
            {
                RemoveHandler(key);
            }

            _keysToRemove.Clear();
        }
        #endregion

        #region Internal Functions
        internal static AsyncHandler GetHandler(string key)
        {
            if (_handlers.TryGetValue(key, out var asyncHandle))
            {
                return asyncHandle;
            }

            return null;
        }

        internal static void RemoveHandler(string key)
        {
            DebugLogger.Log($"[AddressableManager] RemoveHandler: {key}");

            _handlers.Remove(key, out var handler);
            _handlerPool.Release(handler);
        }

        internal static bool IsPostponeHandler(AsyncHandler handler)
        {
            return _postponeHandlers.IndexOf(handler) != -1;
        }

        internal static void PostponeHandler(AsyncHandler handler)
        {
            DebugLogger.Log($"[AddressableManager] PostponeHandler: {handler.Key}");

            _handlers.Remove(handler.Key);
            _postponeHandlers.Add(handler);
        }

        internal static void ReleasePostponeHandler(AsyncHandler handler)
        {
            DebugLogger.Log($"[AddressableManager] ReleasePostponeHandler: {handler.Key}");

            _postponeHandlers.Remove(handler);
            _handlerPool.Release(handler);
        }
        #endregion

        #region Private Members
        public const int k_poolCapacity = 3000;
        private static Dictionary<string, AsyncHandler> _handlers = new ();
        private static List<AsyncHandler> _postponeHandlers = new ();
        private static List<string> _keysToRemove = new ();

        private static ObjectPool<AsyncHandler> _handlerPool = new (() => new AsyncHandler(), handler => handler.Reset(), k_poolCapacity);
        #endregion
    }
}