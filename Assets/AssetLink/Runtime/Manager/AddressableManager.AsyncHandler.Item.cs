using System;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace xpTURN.Link
{
    internal static partial class AddressableManager
    {
        /// <summary>
        /// AsyncHandler is a class that manages objects loaded asynchronously via Addressables.
        /// If the callback object is no longer available at the time of load completion, it handles the exception.
        /// </summary>
        public partial class AsyncHandler
        {
            public class Item
            {
                #region Private Fields
                private string _key = string.Empty;
                private WeakReference<IAssetLink> _wrLink = new (null);
                private WeakReference<Action<Object>> _wrCallback = new (null);
                private WeakReference<Action<SceneInstance>> _wrSceneCallback = new(null);
                private bool _isInstantiate = false;
                private Vector3 _position = Vector3.zero;
                private Quaternion _rotation = Quaternion.identity;
                private bool _hasParent = false;
                private bool _worldSpace = true;
                private WeakReference<Transform> _wrParent = new (null);
                private StackTrace _trace = null;
                #endregion

                #region Public Properties
                public string Key => _key;
                public IAssetLink Link => _wrLink.TryGetTarget(out var link) ? link : null;
                public Action<Object> Callback => _wrCallback.TryGetTarget(out var callback) ? callback : null;
                public Action<SceneInstance> SceneCallback => _wrSceneCallback.TryGetTarget(out var sceneCallback) ? sceneCallback : null;
                public bool IsInstantiate => _isInstantiate;
                public Vector3 Position => _position;
                public Quaternion Rotation => _rotation;
                public bool HasParent => _hasParent;
                public bool WorldSpace => _worldSpace;
                public Transform Parent => _wrParent.TryGetTarget(out var parent) ? parent : null;
                public StackTrace Trace => _trace;
                #endregion

                #region Public Methods
                public void Setup(string key, Action<SceneInstance> callback)
                {
                    _key = key;
                    _wrSceneCallback.SetTarget(callback);
                    _isInstantiate = false;

#if DEBUG
                    _trace = new StackTrace(true);
#endif
                }

                public void Setup(IAssetLink link, Action<SceneInstance> callback)
                {
                    _wrLink.SetTarget(link);
                    _wrSceneCallback.SetTarget(callback);
                    _isInstantiate = false;

#if DEBUG
                    _trace = new StackTrace(true);
#endif
                }

                public void Setup(IAssetLink link, Action<Object> callback)
                {
                    _wrLink.SetTarget(link);
                    _wrCallback.SetTarget(callback);
                    _isInstantiate = false;

#if DEBUG
                    _trace = new StackTrace(true);
#endif
                }

                public void Setup(IAssetLink link, Action<Object> callback, Vector3 position, Quaternion rotation, Transform parent, bool worldSpace)
                {
                    _wrLink.SetTarget(link);
                    _wrCallback.SetTarget(callback);
                    _isInstantiate = true;
                    _position = position;
                    _rotation = rotation;
                    _hasParent = parent != null;
                    _wrParent.SetTarget(parent);
                    _worldSpace = worldSpace;

#if DEBUG
                    _trace = new StackTrace(true);
#endif
                }

                public bool IsDanglingLink()
                {
                    return _wrLink.TryGetTarget(out var target) == false;
                }

                public void OnCallback(Object result)
                {
                    if (_wrCallback.TryGetTarget(out var action))
                    {
                        action.Invoke(result);
                    }
                }

                public void OnCallback(SceneInstance result)
                {
                    if (_wrSceneCallback.TryGetTarget(out var action))
                    {
                        action.Invoke(result);
                    }
                }

                public bool IsDanglingParent()
                {
                    return _wrParent.TryGetTarget(out var parent) == false;
                }

                public Transform GetParent()
                {
                    _wrParent.TryGetTarget(out var parent);
                    return parent;
                }

                public void Reset()
                {
                    _key = string.Empty;
                    _wrLink.SetTarget(null);
                    _wrCallback.SetTarget(null);
                    _wrParent.SetTarget(null);
                    _trace = null;
                }
                #endregion
            }
        }
    }
}