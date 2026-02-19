#if ASSETLINK_UNITASK_INTEGRATION
#pragma warning disable CS0809 // Obsolete override intentionally
using System;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using Cysharp.Threading.Tasks;

namespace xpTURN.AssetLink
{
    /// <summary>
    /// AssetRefSpawner is a class that spawns a GameObject asset by GUID.
    /// It tracks the spawn count of the asset and releases the asset when the spawn count is 0.
    /// </summary>
    [Serializable]
    public sealed class AssetRefSpawner : AssetRefT<GameObject>, ISerializationCallbackReceiver
    {
        /// <summary>Serializes the first load so concurrent SpawnAsync calls share one OperationHandle. Not serialized.</summary>
        /// <remarks>Unity runs game logic on the main thread, so there is no true multithreading here. The lock keeps the "load once, then share the handle" logic clear and makes it safe if multiple SpawnAsync calls overlap (e.g. interleaved at await points).</remarks>
        [NonSerialized] private object _loadLock;

        public AssetRefSpawner(string guid) : base(guid)
        {
            _loadLock = new object();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            OnAfterDeserializeImpl();
        }

        /// <inheritdoc/>
        protected override void OnAfterDeserializeImpl()
        {
            base.OnAfterDeserializeImpl();
            _loadLock ??= new object();
        }

        /// <summary>
        /// Whether the AssetLink is a spawner. Spawner is a special AssetLink that can spawn assets.
        /// </summary>
        public override bool IsSpawner => true;

        /// <summary>Not supported. Use <see cref="SpawnAsync"/> instead.</summary>
        [Obsolete("Use SpawnAsync instead for AssetRefSpawner.", true)]
        public override AsyncOperationHandle<GameObject> LoadAssetAsync<GameObject>() =>
            throw new NotSupportedException("Use SpawnAsync instead for AssetRefSpawner.");

        /// <summary>Not supported. Use <see cref="SpawnAsync"/> instead.</summary>
        [Obsolete("Use SpawnAsync instead for AssetRefSpawner.", true)]
        public override AsyncOperationHandle<GameObject> LoadAssetAsync() =>
            throw new NotSupportedException("Use SpawnAsync instead for AssetRefSpawner.");

        /// <summary>Not supported. Use <see cref="SpawnAsync"/> instead.</summary>
        [Obsolete("Use SpawnAsync instead for AssetRefSpawner.", true)]
        public override AsyncOperationHandle<GameObject> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null) =>
            throw new NotSupportedException("Use SpawnAsync instead for AssetRefSpawner.");

        /// <summary>Not supported. Use <see cref="SpawnAsync"/> instead.</summary>
        [Obsolete("Use SpawnAsync instead for AssetRefSpawner.", true)]
        public override AsyncOperationHandle<GameObject> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false) =>
            throw new NotSupportedException("Use SpawnAsync instead for AssetRefSpawner.");

        async UniTask<GameObject> InnerLoadAssetAsync()
        {
            bool weStartedLoad = false;
            // Serialize first load so only one LoadAssetAsync runs; other callers reuse the same OperationHandle and await it.
            lock (_loadLock)
            {
                if (!OperationHandle.IsValid())
                {
                    var result = Addressables.LoadAssetAsync<GameObject>(RuntimeKey);
                    OperationHandle = result;
                    AddressablesTracker.Add(this, AddressablesTracker.HANDLE_TYPE.ASSET_OWNER_SPAWNER, result);
                    weStartedLoad = true;
                }
            }

            await OperationHandle.ToUniTask();
            if (OperationHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to load asset for AssetRefSpawner {RuntimeKeyString}");
                if (weStartedLoad)
                    ReleaseAsset();
                return null;
            }

            return OperationHandle.Result as GameObject;
        }

        public override void ReleaseAsset()
        {
            var spawnCount = AddressablesTracker.GetSpawnCount(RuntimeKeyString, OwnerId);
            if (spawnCount > 0)
            {
                Debug.LogError($"AssetRefSpawner {RuntimeKeyString} is not released. Spawn count: {spawnCount}");
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
            var prefab = await InnerLoadAssetAsync();
            if (prefab == null) return null;

#if UNITY_6000_0_OR_NEWER
            var asyncOp = GameObject.InstantiateAsync(prefab, parent, position, rotation);
            var result = (await asyncOp.ToUniTask())[0];
#else
            var result = GameObject.Instantiate(prefab, position, rotation, parent);
#endif

            DoAutoRelease.SetupForSpawner(result, RuntimeKeyString, OwnerId);
            return result;
        }

#if UNITY_6000_0_OR_NEWER
        /// <summary>
        /// Loads asset if needed, then instantiates <paramref name="count"/> instances via GameObject.InstantiateAsync.
        /// </summary>
        /// <returns>Spawned GameObjects, or an empty array if the prefab failed to load.</returns>
        /// <remarks>
        /// If Awake is costly, use AsyncInstantiateOperation.SetIntegrationTimeMS() to spread the load.
        /// </remarks>
        public async UniTask<GameObject[]> SpawnAsync(int count, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var prefab = await InnerLoadAssetAsync();
            if (prefab == null) return Array.Empty<GameObject>();

            var asyncOp = GameObject.InstantiateAsync(prefab, count, parent, position, rotation);
            var result = await asyncOp.ToUniTask();

            for (int i = 0; i < result.Length; ++i)
            {
                var go = result[i];
                DoAutoRelease.SetupForSpawner(go, RuntimeKeyString, OwnerId);
            }

            return result;
        }
#endif

        /// <summary>
        /// Loads asset if needed, then instantiates via GameObject.InstantiateAsync. Returns the spawned GameObject.
        /// </summary>
        public async UniTask<GameObject> SpawnAsync(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            var prefab = await InnerLoadAssetAsync();
            if (prefab == null) return null;

#if UNITY_6000_0_OR_NEWER
            var asyncOp = GameObject.InstantiateAsync(prefab, new InstantiateParameters { parent = parent, worldSpace = instantiateInWorldSpace });
            var result = (await asyncOp.ToUniTask())[0];
#else
            var result = GameObject.Instantiate(prefab, parent, instantiateInWorldSpace);
#endif

            DoAutoRelease.SetupForSpawner(result, RuntimeKeyString, OwnerId);
            return result;
        }

#if UNITY_6000_0_OR_NEWER
        /// <summary>
        /// Loads asset if needed, then instantiates <paramref name="count"/> instances via GameObject.InstantiateAsync.
        /// </summary>
        /// <returns>Spawned GameObjects, or an empty array if the prefab failed to load.</returns>
        /// <remarks>
        /// If Awake is costly, use AsyncInstantiateOperation.SetIntegrationTimeMS() to spread the load.
        /// </remarks>
        public async UniTask<GameObject[]> SpawnAsync(int count, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            var prefab = await InnerLoadAssetAsync();
            if (prefab == null) return Array.Empty<GameObject>();

            var asyncOp = GameObject.InstantiateAsync(prefab, count, new InstantiateParameters { parent = parent, worldSpace = instantiateInWorldSpace });
            var result = await asyncOp.ToUniTask();

            for (int i = 0; i < result.Length; ++i)
            {
                var go = result[i];
                DoAutoRelease.SetupForSpawner(go, RuntimeKeyString, OwnerId);
            }

            return result;
        }
#endif
    }
}
#endif