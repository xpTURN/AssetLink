using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace xpTURN.Link
{
    internal static partial class AddressableManager
    {
        /// <summary>
        /// AsyncHandler is a class that manages objects loaded asynchronously via Addressables.
        /// Handles exceptions when the callback object is destroyed before the load is complete.
        /// </summary>
        public partial class AsyncHandler
        {
            #region Public Members
            public bool IsDone => _handle.IsDone;
            public Object Result => _handle.Result;
            public string Key => _key;

#if UNITY_INCLUDE_TESTS
            public AsyncOperationHandle<Object> Handle => _handle;
            public AsyncOperationHandle<SceneInstance> SceneHandle => _sceneHandle;
            public int RefLinkCount => _itemList.Count;
            public static int PoolMaxCount => _itemPool.MaxCount;
            public static int PoolUsedCount => _itemPool.UsedCount;
            public static int PoolRemaining => _itemPool.Remaining;
#endif
            #endregion

            #region Public Methods : Scene
            public void LoadSceneAsync(string key, Action<SceneInstance> onSceneLoaded, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true)
            {
                //  Only start loading if this is the first time.
                if (_itemList.Count == 0)
                {
                    _key = key;
                    _sceneHandle = Addressables.LoadSceneAsync(key, loadMode, activateOnLoad);
                    _sceneHandle.Completed += OnLoadCompleted;
                }

                //  Create management item
                var item = _itemPool.Get();
                item.Setup(key, onSceneLoaded);
                _itemList.Add(item);

                //  If already loaded, invoke the callback immediately.
                if (_sceneHandle.IsDone)
                {
                    if (_sceneHandle.OperationException != null)
                    {
                        DebugLogger.LogError($"[AddressableManager] LoadSceneAsync Exception: {Key}\r\n{_sceneHandle.OperationException}");
                        return;
                    }

                    item.OnCallback(_sceneHandle.Result);
                }
            }

            public void UnloadSceneAsync(string key, Action<string> onSceneUnloaded)
            {
                RemoveRefLink(key);

                //  Release when there are no reference objects.
                if (_itemList.Count == 0)
                {
                    //  However, if it is released before loading is complete, the Addressables reference count will not decrease, so release is postponed.
                    if (_sceneHandle.IsValid() && !_sceneHandle.IsDone)
                    {
                        AddressableManager.PostponeHandler(this);
                        return;
                    }

                    //  
                    var unloadHandle = Addressables.UnloadSceneAsync(_sceneHandle);
                    var capturedKey = Key;
                    unloadHandle.Completed += (asyncOperation) => OnUnloadSceneCompleted(asyncOperation, capturedKey, onSceneUnloaded);
                    _sceneHandle = default;
                }
            }

            private static void OnUnloadSceneCompleted(AsyncOperationHandle<SceneInstance> asyncOperation, string key, Action<string> onSceneUnloaded)
            {
                if (asyncOperation.OperationException != null)
                {
                    DebugLogger.LogError($"[AddressableManager] UnloadSceneAsync Exception: {key}\r\n{asyncOperation.OperationException}");
                    return;
                }

                onSceneUnloaded?.Invoke(key);
            }

            public void LoadSceneAsync(IAssetLink assetLink, Action<SceneInstance> onSceneLoaded, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true)
            {
                //  Only start loading if this is the first time.
                if (_itemList.Count == 0)
                {
                    _key = assetLink.Key;
                    _sceneHandle = Addressables.LoadSceneAsync(assetLink.Key, loadMode, activateOnLoad);
                    _sceneHandle.Completed += OnLoadCompleted;
                }

                //  Create management item
                var item = _itemPool.Get();
                item.Setup(assetLink, onSceneLoaded);
                _itemList.Add(item);

                //  If already loaded, invoke the callback immediately.
                if (_sceneHandle.IsDone)
                {
                    if (_sceneHandle.OperationException != null)
                    {
                        DebugLogger.LogError($"[AddressableManager] LoadSceneAsync Exception: {Key}\r\n{_sceneHandle.OperationException}");
                        return;
                    }

                    item.OnCallback(_sceneHandle.Result);
                }
            }

            public void UnloadSceneAsync(IAssetLink assetLink, Action<string> onSceneUnloaded)
            {
                RemoveRefLink(assetLink);

                //  Release when there are no reference objects.
                if (_itemList.Count == 0)
                {
                    //  However, if it is released before loading is complete, the Addressables reference count will not decrease, so release is postponed.
                    if (_sceneHandle.IsValid() && !_sceneHandle.IsDone)
                    {
                        AddressableManager.PostponeHandler(this);
                        return;
                    }

                    //  
                    var unloadHandle = Addressables.UnloadSceneAsync(_sceneHandle);
                    var capturedKey = Key;
                    unloadHandle.Completed += (asyncOperation) => OnUnloadSceneCompleted(asyncOperation, capturedKey, onSceneUnloaded);
                    _sceneHandle = default;
                }
            }
            #endregion

            #region Public Methods : Object
            public void LoadAsync(IAssetLink assetLink, Action<Object> onAssetLoaded = null)
            {
                if (_itemList.Exists(node => IsSameLink(node, assetLink))
                    || _itemList.Exists(node => IsSameCallback(node, onAssetLoaded)))
                {
                    throw new InvalidOperationException("Already loaded.");
                }

                //  Only start loading if this is the first time.
                if (_itemList.Count == 0)
                {
                    _key = assetLink.Key;
                    _handle = Addressables.LoadAssetAsync<Object>(assetLink.Key);
                    _handle.Completed += OnLoadCompleted;
                }

                //  Create management item
                var item = _itemPool.Get();
                item.Setup(assetLink, onAssetLoaded);
                _itemList.Add(item);

                //  If already loaded, invoke the callback immediately.
                if (_handle.IsDone)
                {
                    if (_handle.OperationException != null)
                    {
                        DebugLogger.LogError($"[AddressableManager] LoadAsync Exception: {Key}\r\n{_handle.OperationException}");
                        return;

                    }
                    //  If already loaded, invoke the callback immediately.
                    item.OnCallback(_handle.Result);
                }
            }

            public void Release(IAssetLink assetLink)
            {
                RemoveRefLink(assetLink);

                //  Release when there are no reference objects.
                if (_itemList.Count == 0)
                {
                    //  However, if it is released before loading is complete, the Addressables reference count will not decrease, so release is postponed.
                    if (_handle.IsValid() && !_handle.IsDone)
                    {
                        AddressableManager.PostponeHandler(this);
                        return;
                    }

                    Addressables.Release(_handle);
                    _handle = default;
                }
            }
            #endregion

            #region Public Methods : Prefab
            public void InstantiateAsync(IAssetLink assetLink, Action<Object> onObjectInstantiated, Vector3 position, Quaternion rotation, Transform parent = null, bool worldSpace = true)
            {
                //  Only start loading if this is the first time.
                if (_itemList.Count == 0)
                {
                    _key = assetLink.Key;
                    _handle = Addressables.LoadAssetAsync<Object>(assetLink.Key);
                    _handle.Completed += OnLoadCompleted;
                }

                //  Create management item
                var item = _itemPool.Get();
                item.Setup(assetLink, onObjectInstantiated, position, rotation, parent, worldSpace);
                _itemList.Add(item);

                //  If loading is complete, instantiate the object.
                if (_handle.IsDone)
                {
                    if (_handle.OperationException != null)
                    {
                        DebugLogger.LogError($"[AddressableManager] InstantiateAsync Exception: {Key}\r\n{_handle.OperationException}");
                        return;
                    }

                    ProcessInstantiate(_handle.Result, position, rotation, worldSpace, parent, item);
                }
            }

            private void ProcessInstantiate(Object result, Vector3 position, Quaternion rotation, bool worldSpace, Transform parent, Item item)
            {
                var asyncInstantiate = GameObject.InstantiateAsync<Object>(result, position, rotation, new InstantiateParameters { worldSpace = worldSpace, parent = parent });
                var awaiter = asyncInstantiate.GetAwaiter();
                var handler = this;
                awaiter.OnCompleted(() =>
                {
                    var goObject = awaiter.GetResult()[0] as GameObject;
                    if (!goObject.TryGetComponent<ReleaseOnDestory>(out var forDestroy))
                    {
                        forDestroy = goObject.AddComponent<ReleaseOnDestory>();
                    }

                    forDestroy.SetHandler(handler, item);

                    //  Invoke the callback if it exists.
                    item.OnCallback(goObject);
                });
            }

            public void ReleaseInstance(Item item)
            {
                _itemList.Remove(item);

                //  Release when there are no reference objects.
                if (_itemList.Count == 0)
                {
                    Addressables.Release(_handle);
                    _handle = default;
                }
            }
            #endregion

            #region Public Methods
            public bool IsUnused(bool report)
            {
                RemoveUnusedRefLink(report);
                return IsDone && _itemList.Count == 0;
            }

            public void Reset()
            {
                _key = string.Empty;
                _handle = default;

                _itemList.ForEach(item => _itemPool.Release(item));
                _itemList.Clear();
            }
            #endregion

            #region Private Methods
            bool IsSameLink(Item item, IAssetLink assetLink)
            {
                return item.Link == assetLink;
            }

            bool IsSameCallback(Item item, Action<Object> callback)
            {
                return item.Callback == callback;
            }

            private void RemoveUnusedRefLink(bool report)
            {
                var key = Key;
                _itemList.RemoveAll(item =>
                {
                    if (item.IsDanglingLink())
                    {
#if DEBUG
                        if (report)
                        {
                            DebugLogger.LogError($"[AddressableManager] NullLink: {key}\r\n{StackTraceParser.ToString(item.Trace)}");
                        }
#endif
                        _itemPool.Release(item);
                        return true;
                    }
                    return false;
                });
            }

            private void RemoveRefLink(string key)
            {
                _itemList.RemoveAll(item =>
                {
                    if (item.Key == key)
                    {
                        _itemPool.Release(item);
                        return true;
                    }
                    return false;
                });
            }

            private void RemoveRefLink(IAssetLink link)
            {
                _itemList.RemoveAll(item =>
                {
                    if (IsSameLink(item, link))
                    {
                        _itemPool.Release(item);
                        return true;
                    }
                    return false;
                });
            }

            private void OnLoadCompleted(AsyncOperationHandle<SceneInstance> asyncOperation)
            {
                if (asyncOperation.OperationException != null)
                {
                    DebugLogger.LogError($"[AddressableManager] LoadSceneAsync Exception: {Key}\r\n{asyncOperation.OperationException}");
                    return;
                }

                //  Release if there are no callbacks.
                if (_itemList.Count == 0)
                {
                    AddressableManager.ReleasePostponeHandler(this);
                    Addressables.UnloadSceneAsync(asyncOperation);

                    _sceneHandle = default;
                    return;
                }

                //
                foreach (var item in _itemList)
                {
                    item.OnCallback(asyncOperation.Result);
                }
            }

            private void OnLoadCompleted(AsyncOperationHandle<Object> asyncOperation)
            {
                if (asyncOperation.OperationException != null)
                {
                    DebugLogger.LogError($"[AddressableManager] LoadAsync Exception: {Key}\r\n{asyncOperation.OperationException}");
                    return;
                }

                //  Release if there are no callbacks.
                if (_itemList.Count == 0)
                {
                    AddressableManager.ReleasePostponeHandler(this);
                    Addressables.Release(asyncOperation);
                    _handle = default;
                    return;
                }

                //
                foreach (var item in _itemList)
                {
                    if (item.IsInstantiate)
                    {
                        // Do not instantiate if the parent has disappeared.
                        if (item.HasParent && item.IsDanglingParent())
                        {
                            DebugLogger.LogWarning($"[AddressableManager] The Parent is gone during InstantiateAsync : {Key}");

                            // Instancing count must be returned, so release it.
                            DanglingRefManager.Instance.AddDanglingRef(this, item);
                            continue;
                        }

                        ProcessInstantiateInLoop(_handle.Result, item);
                    }
                    else
                    {
                        item.OnCallback(asyncOperation.Result);
                    }
                }
            }

            private void ProcessInstantiateInLoop(Object result, Item item)
            {
                var asyncInstantiate = GameObject.InstantiateAsync<Object>(result, item.Position, item.Rotation, new InstantiateParameters { worldSpace = item.WorldSpace, parent = item.Parent });
                var awaiter = asyncInstantiate.GetAwaiter();
                var handler = this;
                var key = Key;
                awaiter.OnCompleted(() =>
                {
                    Object[] arrayObject = awaiter.GetResult();

                    // If instance creation fails, the reference must be released. For example, if the specified parent is invalid, etc.
                    if (arrayObject == null || arrayObject.Length == 0 || arrayObject[0] == null)
                    {
                        // The instancing count must be returned, so release it.
                        DebugLogger.LogError($"[AddressableManager] InstantiateAsync failed Instancing : {key}");
                        DanglingRefManager.Instance.AddDanglingRef(handler, item);
                        return;
                    }

                    var goObject = arrayObject[0] as GameObject;
                    if (!goObject.TryGetComponent<ReleaseOnDestory>(out var existingForDestroy))
                    {
                        existingForDestroy = goObject.AddComponent<ReleaseOnDestory>();
                    }

                    existingForDestroy.SetHandler(handler, item);

                    if (item.HasParent && item.IsDanglingParent())
                    {
                        DebugLogger.LogWarning($"[AddressableManager] The Parent is gone during InstantiateAsync : {key}");

                        // Destroy the already instantiated object.
                        GameObject.Destroy(goObject);
                        return;
                    }

                    //  Invoke the callback if it exists.
                    item.OnCallback(goObject);
                });
            }

#if UNITY_INCLUDE_TESTS
            public static void ResetPool()
            {
                _itemPool = new ObjectPool<Item>(
                    () => new Item(),
                    item => item.Reset(),
                    k_poolCapacity
                );
            }
#endif
            #endregion

            #region private members
            private const int k_poolCapacity = 9000;

            private string _key;
            private AsyncOperationHandle<Object> _handle;
            private AsyncOperationHandle<SceneInstance> _sceneHandle;
            private List<Item> _itemList = new ();
            private static ObjectPool<Item> _itemPool = new (() => new Item(), item => item.Reset(), k_poolCapacity);
            #endregion
        }
    }
}