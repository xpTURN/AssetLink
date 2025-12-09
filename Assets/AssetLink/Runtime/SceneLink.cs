using System;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace xpTURN.Link
{
    /// <summary>
    /// The AssetLink class asynchronously loads scenes using Addressables.
    /// </summary>
    [System.Serializable]
    public partial class SceneLink : AssetLink, ISceneLink
    {
        #region Public Methods
        public SceneLink()
        {
#if UNITY_EDITOR
            LinkableTypeForInspector = typeof(SceneAsset);
#endif
        }

        public void LoadSceneAsync(Action<SceneInstance> onSceneLoaded, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }

            if (IsLoad)
            {
                DebugLogger.LogError($"[SceneLink] Scene already loaded: {Key}");
                return;
            }

            IsLoad = true;
            AddressableManager.LoadSceneAsync(this, onSceneLoaded, loadMode, activateOnLoad);
        }

        public void UnloadSceneAsync(Action<string> onSceneUnloaded)
        {
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }

            if (IsLoad == false)
            {
                DebugLogger.LogError($"[SceneLink] Scene not loaded: {Key}");
                return;
            }

            AddressableManager.UnloadSceneAsync(this, onSceneUnloaded);
            IsLoad = false;
        }

        public SceneInstance GetSceneResult()
        {
            if (string.IsNullOrEmpty(Key))
            {
                DebugLogger.LogError("[AssetLink] GetSceneResult: Key is not assigned.");
                return default(SceneInstance);
            }
            if (IsLoad == false)
            {
                DebugLogger.LogError($"[AssetLink] GetSceneResult: Asset not loaded for key {Key}.");
                return default(SceneInstance);
            }

            var asyncHandle = AddressableManager.GetHandler(Key);
            if (asyncHandle == null)
            {
                DebugLogger.LogError($"[AssetLink] GetSceneResult: AsyncHandler not found for key {Key}");
                return default(SceneInstance);
            }

            return asyncHandle.SceneHandle.Result;
        }
        #endregion
    }
}
