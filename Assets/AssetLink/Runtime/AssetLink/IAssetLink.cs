using System;

using UnityEngine;
using Object = UnityEngine.Object;

using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace xpTURN.Link
{
    /// <summary>
    /// The IAssetLink interface defines the functionality of the AssetLink class.
    /// </summary>
    public partial interface IAssetLink
    {
        string Key { get; }
        void AssignKey(string key);
        void Reset();
    }

    /// <summary>
    /// The IObjectLink interface defines the functionality for asynchronous loading via Addressables.
    /// </summary>
    public interface IObjectLink<T> where T : Object
    {
        void LoadAssetAsync(System.Action<T> onAssetLoaded);
        void Release();
    }

    /// <summary>
    /// The IPrefabLink interface defines the functionality for asynchronous instantiation via Addressables.
    /// </summary>
    public interface IPrefabLink
    {
        void InstantiateAsync(Action<GameObject> onObjectInstantiated, Transform parent = null, bool worldSpace = true);
        void InstantiateAsync(Action<GameObject> onObjectInstantiated, Vector3 position, Quaternion rotation, Transform parent = null, bool worldSpace = true);
    }

    /// <summary>
    /// The ISceneLink interface defines the functionality for asynchronously loading scenes via Addressables.
    /// </summary>
    public interface ISceneLink
    {
        void LoadSceneAsync(Action<SceneInstance> onSceneLoaded, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true);
    }
}
