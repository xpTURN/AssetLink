using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = UnityEngine.Debug;
using UnityEngine.ResourceManagement.ResourceProviders;


#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace xpTURN.AssetLink
{
    public static class AddressablesTracker
    {
        public enum HANDLE_TYPE
        {
            NONE,
            ASSET_OWNER, // for AssetLink/AssetRef --> LoadAssetAsync
            ASSET_OWNER_SCENE, // for AssetLink/AssetRef --> LoadSceneAsync
            ASSET_OWNER_SPAWNER, // for AssetLinkSpawner/AssetRefSpawner
            ASSET_RELEASER, // for DoAutoRelease
        };

        public class TrackedHandle
        {
            public WeakReference<IAssetOwner> AssetOwner { get; private set; } = new(null);
            public HANDLE_TYPE Type { get; internal set; } = HANDLE_TYPE.NONE;
            public bool Unreferenced { get; internal set; } = false;
            public string RuntimeKey { get; internal set; } = string.Empty;
            public long AssetOwnerId { get; internal set; } = 0;
            public int SpawnCount { get; internal set; } = 0;
            public AsyncOperationHandle OperationHandle { get; private set; } = default;
            public DateTime RequestTime { get; private set; } = DateTime.MinValue;
            public StackTrace RequestTrace { get; private set; } = null;

            internal void Setup(IAssetOwner assetOwner, HANDLE_TYPE type, AsyncOperationHandle operationHandle, DateTime reqTime, StackTrace trace)
            {
                AssetOwner.SetTarget(assetOwner);
                Type = type;
                Unreferenced = false;
                RuntimeKey = assetOwner.RuntimeKeyString;
                AssetOwnerId = assetOwner.OwnerId;
                SpawnCount = type == HANDLE_TYPE.ASSET_RELEASER ? 1 : 0;
                OperationHandle = operationHandle;
                RequestTime = reqTime;
                RequestTrace = trace;
            }

            internal void Reset()
            {
                AssetOwner.SetTarget(null);
                Type = HANDLE_TYPE.NONE;
                Unreferenced = false;
                RuntimeKey = string.Empty;
                AssetOwnerId = 0;
                SpawnCount = 0;
                OperationHandle = default;
                RequestTime = DateTime.MinValue;
                RequestTrace = null;
            }

            internal bool ValidReference()
            {
                if (Type == HANDLE_TYPE.ASSET_OWNER_SPAWNER)
                    return true;

                return AssetOwner.TryGetTarget(out var _);
            }

            internal bool EnableForceRelease()
            {
                if (Type == HANDLE_TYPE.ASSET_OWNER_SPAWNER
                    || Type == HANDLE_TYPE.ASSET_RELEASER
                    || Type == HANDLE_TYPE.ASSET_OWNER_SCENE)
                    return false;

                if (!OperationHandle.IsValid() || AssetOwner.TryGetTarget(out _))
                    return false;

                return true;
            }

            internal void OnDestroyedScene(AsyncOperationHandle operationHandle)
            {
                operationHandle.Destroyed -= OnDestroyedScene;
                AddressablesTracker.Remove(RuntimeKey, AssetOwnerId);
            }

            internal TrackedHandleDTO ToDTO()
            {
                var dto = dtoPool.Get();
                dto.Type = Type;
                dto.Unreferenced = !ValidReference();
                dto.RuntimeKey = RuntimeKey;
                dto.AssetOwnerId = AssetOwnerId;
                dto.SpawnCount = SpawnCount;
                dto.RequestTime = RequestTime;
                dto.RequestTrace = RequestTrace;
                dto.IsHandleValid = OperationHandle.IsValid();
                dto.Status = OperationHandle.IsValid() ? OperationHandle.Status.ToString() : "Invalid";
                return dto;
            }
        }

        /// <summary>
        /// Data transfer object for trackedHandle. Used by editor diagnostics; no references to runtime handles.
        /// </summary>
        public class TrackedHandleDTO
        {
            public HANDLE_TYPE Type { get; internal set; }
            public bool Unreferenced { get; internal set; }
            public string RuntimeKey { get; internal set; }
            public long AssetOwnerId { get; internal set; }
            public int SpawnCount { get; internal set; }
            public DateTime RequestTime { get; internal set; }
            public StackTrace RequestTrace { get; internal set; }
            public bool IsHandleValid { get; internal set; }
            public string Status { get; internal set; }

            internal void Reset()
            {
                Type = HANDLE_TYPE.NONE;
                Unreferenced = false;
                RuntimeKey = string.Empty;
                AssetOwnerId = 0;
                SpawnCount = 0;
                RequestTime = default;
                RequestTrace = null;
                IsHandleValid = false;
                Status = null;
            }
        }

        private static Dictionary<string, SortedDictionary<long, TrackedHandle>> trackedHandles = new();

        private const int k_poolCapacity = 10000;
        private static ObjectPool<TrackedHandle> handlePool = new(() => new TrackedHandle(), item => item.Reset(), k_poolCapacity);
        private static ObjectPool<TrackedHandleDTO> dtoPool = new(() => new TrackedHandleDTO(), dto => dto.Reset(), k_poolCapacity);

        internal static bool EnableStackTrace { get; private set; } = true;
        internal static int PoolCapacity { get; private set; } = k_poolCapacity;

        public static void SetStackTrace(bool enable)
        {
#if UNITY_INCLUDE_TESTS
            Debug.Log($"AddressablesTracker.SetStackTrace: {enable}");
#endif
            EnableStackTrace = enable;
        }

        public static void SetHandlePool(int poolCapacity)
        {
#if UNITY_INCLUDE_TESTS
            Debug.Log($"AddressablesTracker.SetHandlePool: {poolCapacity}");
#endif
            PoolCapacity = poolCapacity;
            handlePool.SetPoolCapacity(PoolCapacity);
            dtoPool.SetPoolCapacity(PoolCapacity);
        }

#if UNITY_INCLUDE_TESTS
        public static int PoolMaxCount => handlePool.MaxCount;
        public static int PoolUsedCount => handlePool.UsedCount;
        public static int PoolRemaining => handlePool.Remaining;

        public static void ResetPool()
        {
            handlePool = new(() => new TrackedHandle(), handler => handler.Reset(), k_poolCapacity);
        }

        internal static SortedDictionary<long, TrackedHandle> GetHandles(string assetName)
        {
            if (!trackedHandles.TryGetValue(assetName, out var handles))
            {
                return new();
            }

            return handles;
        }
#endif

        internal static TrackedHandle Add(IAssetOwner assetOwner, HANDLE_TYPE handleType, AsyncOperationHandle operationHandle)
        {
            StackTrace trace = null;
            if (EnableStackTrace)
            {
                trace = new StackTrace(0, true);
            }

            long assetOwnerId = assetOwner.OwnerId;
            var trackedHandle = handlePool.Get();
            trackedHandle.Setup(assetOwner, handleType, operationHandle, DateTime.Now, trace);

            if (!trackedHandles.TryGetValue(assetOwner.RuntimeKeyString, out var handles))
            {
                handles = new SortedDictionary<long, TrackedHandle>();
                trackedHandles[assetOwner.RuntimeKeyString] = handles;
            }

            handles.Add(assetOwnerId, trackedHandle);
            return trackedHandle;
        }

        internal static void Remove(string runtimeKey, long ownerId)
        {
            if (!trackedHandles.TryGetValue(runtimeKey, out var handles))
            {
                Debug.LogError($"AddressablesTracker: Remove: AssetOwner {runtimeKey} not found in trackedHandles");
                return;
            }

            handles.TryGetValue(ownerId, out var trackedHandle);
            if (trackedHandle != null)
            {
                handlePool.Release(trackedHandle);
                handles.Remove(ownerId);
                if (handles.Count == 0)
                {
                    trackedHandles.Remove(runtimeKey);
                }
            }
            else
            {
                Debug.LogError($"AssetOwner {runtimeKey} not found in trackedHandles");
            }
        }

        internal static int GetSpawnCount(string key, long assetOwnerId)
        {
            if (!trackedHandles.TryGetValue(key, out var handles))
            {
                return 0;
            }

            handles.TryGetValue(assetOwnerId, out var trackedHandle);
            if (trackedHandle == null)
            {
                return 0;
            }

            return trackedHandle.SpawnCount;
        }

        internal static void IncreaseSpawnCount(string key, long assetOwnerId)
        {
            if (!trackedHandles.TryGetValue(key, out var handles))
            {
                return;
            }

            handles.TryGetValue(assetOwnerId, out var trackedHandle);
            if (trackedHandle == null)
            {
                Debug.LogError($"AssetOwner {key} not found in trackedHandles");
                return;
            }

            trackedHandle.SpawnCount++;
        }

        internal static void DecreaseSpawnCount(string key, long assetOwnerId)
        {
            if (!trackedHandles.TryGetValue(key, out var handles))
            {
                Debug.LogError($"AddressablesTracker: DecreaseSpawnCount: AssetOwner {key} not found in trackedHandles");
                return;
            }

            handles.TryGetValue(assetOwnerId, out var trackedHandle);
            if (trackedHandle == null)
            {
                Debug.LogError($"AddressablesTracker: DecreaseSpawnCount: AssetOwner {assetOwnerId} not found in trackedHandles");
                return;
            }

            trackedHandle.SpawnCount--;
            if (trackedHandle.SpawnCount <= 0)
            {
                trackedHandle.AssetOwner.TryGetTarget(out var assetOwner);

                // When assetOwner is alive, it holds ownership of OperationHandle. Request release.
                if (assetOwner != null)
                {
#if UNITY_INCLUDE_TESTS
                    Debug.Log($"AddressablesTracker: DecreaseSpawnCount: AssetOwner {key} is released. By AssetOwner");
#endif
                    assetOwner.ReleaseAsset();
                }
                // When assetOwner is gone, AddressablesTracker handles the release.
                else
                {
#if UNITY_INCLUDE_TESTS
                    Debug.Log($"AddressablesTracker: DecreaseSpawnCount: AssetOwner {key} is released. By AddressablesTracker");
#endif

                    if (trackedHandle.OperationHandle.IsValid())
                        trackedHandle.OperationHandle.Release();

                    handlePool.Release(trackedHandle);
                    handles.Remove(assetOwnerId);

                    if (handles.Count == 0)
                        trackedHandles.Remove(key);
                }
            }
        }

        /// <summary>
        /// Reports unreferenced handles (AssetOwner no longer referenced) without releasing them.
        /// </summary>
        /// <returns>List of unreferenced trackedHandle. Does not release; caller may use this list for inspection or later release.</returns>
        public static int ReportUnreferencedHandles()
        {
            int unreferencedCount = 0;
            foreach (var (name, handles) in trackedHandles)
            {
                foreach (var trackedHandle in handles.Values)
                {
                    if (trackedHandle.ValidReference())
                        continue;

                    trackedHandle.Unreferenced = true;
                    ++unreferencedCount;
                    if (EnableStackTrace)
                        Debug.LogError($"AddressablesTracker: Unreferenced AddressableAsset\nAddressableAsset: '[{trackedHandle.RequestTime:HH:mm:ss}] {name}'\nCallStack: {trackedHandle.RequestTrace?.ToStringForUnityConsole()}");
                    else
                        Debug.LogError($"AddressablesTracker: Unreferenced AddressableAsset\nAddressableAsset: '[{trackedHandle.RequestTime:HH:mm:ss}] {name}'");
                }
            }
            if (unreferencedCount > 0)
                Debug.LogError($"AddressablesTracker: Found {unreferencedCount} unreferenced AddressableAsset(s)");
            return unreferencedCount;
        }

        /// <summary>
        /// Returns true if the handle was unreferenced and released (caller should remove from list).
        /// </summary>
        private static bool TryReleaseUnreferencedHandle(TrackedHandle trackedHandle, string assetName, bool report)
        {
            if (!trackedHandle.EnableForceRelease())
                return false;

            if (trackedHandle.ValidReference())
                return false;

            if (report)
            {
                if (EnableStackTrace)
                    Debug.LogError($"AddressablesTracker: Unreferenced AddressableAsset released\nAddressableAsset: '[{trackedHandle.RequestTime:HH:mm:ss}] {assetName}'\nCallStack: {trackedHandle.RequestTrace.ToStringForUnityConsole()}");
                else
                    Debug.LogError($"AddressablesTracker: Unreferenced AddressableAsset released\nAddressableAsset: '[{trackedHandle.RequestTime:HH:mm:ss}] {assetName}'");
            }

            if (trackedHandle.OperationHandle.IsValid())
                trackedHandle.OperationHandle.Release();

            handlePool.Release(trackedHandle);
            return true;
        }

        /// <summary>
        /// Releases unreferenced handles.
        /// But not release the handles that are assets of scene links.
        /// </summary>
        /// <param name="report">If true, reports the released handles.</param>
        public static void ReleaseUnreferencedHandles(bool report)
        {
            int releasedCount = 0;
            foreach (var (name, handles) in trackedHandles)
            {
                var keysToRemove = new List<long>();
                foreach (var kv in handles)
                {
                    if (TryReleaseUnreferencedHandle(kv.Value, name, report))
                    {
                        keysToRemove.Add(kv.Key);
                    }
                }

                releasedCount += keysToRemove.Count;
                foreach (var key in keysToRemove)
                {
                    handles.Remove(key);
                }
            }

            // Remove empty entries
            var emptyKeys = trackedHandles.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToArray();
            foreach (var key in emptyKeys)
            {
                trackedHandles.Remove(key);
            }

            //
            if (report && releasedCount > 0)
            {
                Debug.LogError($"AddressablesTracker: Released {releasedCount} unreferenced AddressableAsset(s)");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Returns a snapshot of tracked handles for editor diagnostics as DTOs. Editor only.
        /// DTOs are from the pool; call ReleaseTrackedHandleDTOsToPool when discarding the snapshot.
        /// </summary>
        internal static IReadOnlyDictionary<string, List<TrackedHandleDTO>> GetTrackedHandlesSnapshotForEditor()
        {
            var result = new SortedDictionary<string, List<TrackedHandleDTO>>();
            foreach (var kv in trackedHandles)
            {
                var dtos = new List<TrackedHandleDTO>(kv.Value.Count);
                foreach (var hi in kv.Value.Values)
                {
                    dtos.Add(hi.ToDTO());
                }
                result[kv.Key] = dtos;
            }
            return result;
        }

        /// <summary>
        /// Returns DTOs from a snapshot to the pool. Call this when discarding a snapshot (e.g. before capturing a new one).
        /// </summary>
        internal static void ReleaseTrackedHandleDTOsToPool(IReadOnlyDictionary<string, List<TrackedHandleDTO>> snapshot)
        {
            if (snapshot == null) return;
            foreach (var list in snapshot.Values)
            {
                if (list == null) continue;
                foreach (var dto in list)
                {
                    dtoPool.Release(dto);
                }
            }
        }

        /// <summary>
        /// Releases all handles when the playmode state changes.
        /// </summary>
        internal static void ReleaseHandleWhenPlaymodeStateChanged()
        {
            var names = trackedHandles.Keys.ToArray();
            foreach (var name in names)
            {
                if (!trackedHandles.TryGetValue(name, out var handles))
                    continue;

                foreach (var trackedHandle in handles.Values)
                {
                    if (trackedHandle.OperationHandle.IsValid())
                    {
#if UNITY_INCLUDE_TESTS
                        Debug.Log($"AddressablesTracker: ReleaseHandleWhenPlaymodeStateChanged: Release OperationHandle for {name}");
#endif
                        trackedHandle.OperationHandle.Release();
                    }

                    handlePool.Release(trackedHandle);
                }
                handles.Clear();
                trackedHandles.Remove(name);
            }
        }

        [InitializeOnLoadMethod]
        static void RegisterForPlaymodeChange()
        {
            EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
        }

        static void EditorApplicationOnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (EditorSettings.enterPlayModeOptionsEnabled && AddressablesEx.ReinitializeAddressables)
            {
                AddressablesTracker.ReleaseHandleWhenPlaymodeStateChanged();
            }
        }
#endif
    }
}
