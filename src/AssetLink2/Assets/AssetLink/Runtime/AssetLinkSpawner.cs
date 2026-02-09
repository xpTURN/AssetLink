#if ASSETLINK_UNITASK_INTEGRATION
#pragma warning disable CS0809
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

using Cysharp.Threading.Tasks;

using xpTURN.AssetLink.Utility;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace xpTURN.AssetLink
{
    /// <summary>
    /// AssetLinkSpawner is a class that spawns a GameObject asset.
    /// It tracks the spawn count of the asset and releases the asset when the spawn count is 0.
    /// </summary>
    [Serializable]
    public sealed class AssetLinkSpawner : AssetLinkT<GameObject>
    {
        public AssetLinkSpawner(string name) : base(name)
        {
        }

        /// <summary>
        /// Whether the AssetLink is a spawner. Spawner is a special AssetLink that can spawn assets.
        /// </summary>
        public override bool IsSpawner => true;


        /// <summary>Not supported. Use <see cref="SpawnAsync"/> instead.</summary>
        [Obsolete("Use SpawnAsync instead for AssetLinkSpawner.", true)]
        public override AsyncOperationHandle<GameObject> LoadAssetAsync<GameObject>() =>
            throw new NotSupportedException("Use SpawnAsync instead for AssetLinkSpawner.");

        /// <summary>Not supported. Use <see cref="SpawnAsync"/> instead.</summary>
        [Obsolete("Use SpawnAsync instead for AssetLinkSpawner.", true)]
        public override AsyncOperationHandle<GameObject> LoadAssetAsync() =>
            throw new NotSupportedException("Use SpawnAsync instead for AssetLinkSpawner.");

        /// <summary>Not supported. Use <see cref="SpawnAsync"/> instead.</summary>
        [Obsolete("Use SpawnAsync instead for AssetLinkSpawner.", true)]
        public override AsyncOperationHandle<GameObject> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null) =>
            throw new NotSupportedException("Use SpawnAsync instead for AssetLinkSpawner.");

        /// <summary>Not supported. Use <see cref="SpawnAsync"/> instead.</summary>
        [Obsolete("Use SpawnAsync instead for AssetLinkSpawner.", true)]
        public override AsyncOperationHandle<GameObject> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false) =>
            throw new NotSupportedException("Use SpawnAsync instead for AssetLinkSpawner.");

        private AsyncOperationHandle<GameObject> InnerLoadAssetAsync()
        {
            AsyncOperationHandle<GameObject> result = default(AsyncOperationHandle<GameObject>);
            if (OperationHandle.IsValid())
                Debug.LogError("Attempting to load AssetLink that has already been loaded. Handle is exposed through getter OperationHandle");
            else
            {
                result = Addressables.LoadAssetAsync<GameObject>(RuntimeKey);
                OperationHandle = result;
                AddressablesTracker.Add(this, AddressablesTracker.HANDLE_TYPE.ASSET_OWNER_SPAWNER, result);
            }

            return result;
        }

        public override void ReleaseAsset()
        {
            var spawnCount = AddressablesTracker.GetSpawnCount(RuntimeKeyString, OwnerId);
            if (spawnCount > 0)
            {
                Debug.LogError($"AssetLinkSpawner {RuntimeKeyString} is not released. Spawn count: {spawnCount}");
                return;
            }

            if (!OperationHandle.IsValid())
            {
                Debug.LogWarning("Cannot release a null or unloaded asset.");
                return;
            }

            AddressablesTracker.Remove(RuntimeKeyString, OwnerId);
            OperationHandle.Release();
            OperationHandle = default(AsyncOperationHandle);
        }

        /// <summary>
        /// Loads asset if needed, then instantiates via GameObject.InstantiateAsync. Returns the spawned GameObject.
        /// </summary>
        public async UniTask<GameObject> SpawnAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!OperationHandle.IsValid())
            {
                var loadHandle = InnerLoadAssetAsync();

                await loadHandle.ToUniTask();

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load asset for AssetLinkSpawner {RuntimeKeyString}");
                    ReleaseAsset();
                    return null;
                }
            }

            var prefab = Asset as GameObject;
            if (prefab == null)
            {
                Debug.LogError($"Failed to load asset for AssetLinkSpawner {RuntimeKeyString}");
                ReleaseAsset();
                return null;
            }

#if UNITY_6000_0_OR_NEWER
            var asyncOp = GameObject.InstantiateAsync(prefab, parent, position, rotation);

            await asyncOp.ToUniTask();

            var result = asyncOp.Result;
            var goObj = result != null && result.Length > 0 ? result[0] : null;
#else
            var goObj = GameObject.Instantiate(prefab, position, rotation, parent);
#endif
            if (goObj == null)
            {
                Debug.LogError($"Failed to Instantiate for AssetLinkSpawner {RuntimeKeyString}");
                ReleaseAsset();
                return null;
            }

            DoAutoRelease.SetupForSpawner(goObj, RuntimeKeyString, OwnerId);
            return goObj;
        }

        /// <summary>
        /// Loads asset if needed, then instantiates via GameObject.InstantiateAsync. Returns the spawned GameObject.
        /// </summary>
        public async UniTask<GameObject> SpawnAsync(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            if (!OperationHandle.IsValid())
            {
                var loadHandle = InnerLoadAssetAsync();

                await loadHandle.ToUniTask();

                if (loadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load asset for AssetLinkSpawner {RuntimeKeyString}");
                    ReleaseAsset();
                    return null;
                }
            }

            var prefab = Asset as GameObject;
            if (prefab == null)
            {
                Debug.LogError($"Failed to load asset for AssetLinkSpawner {RuntimeKeyString}");
                ReleaseAsset();
                return null;
            }

#if UNITY_6000_0_OR_NEWER
            var asyncOp = GameObject.InstantiateAsync(prefab, new InstantiateParameters { parent = parent, worldSpace = instantiateInWorldSpace });

            await asyncOp.ToUniTask();

            var result = asyncOp.Result;
            var goObj = result != null && result.Length > 0 ? result[0] : null;
#else
            var goObj = GameObject.Instantiate(prefab, parent, instantiateInWorldSpace);
#endif
            if (goObj == null)
            {
                Debug.LogError($"Failed to Instantiate for AssetLinkSpawner {RuntimeKeyString}");
                ReleaseAsset();
                return null;
            }

            DoAutoRelease.SetupForSpawner(goObj, RuntimeKeyString, OwnerId);
            return goObj;
        }
    }
}
#endif