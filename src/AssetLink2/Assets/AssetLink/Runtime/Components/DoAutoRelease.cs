using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using xpTURN.AssetLink.Utility;

namespace xpTURN.AssetLink
{
    internal class DoAutoRelease : MonoBehaviour, IAssetOwner
    {
        public enum TYPE { NONE, ASSET_RELEASER, ASSET_SPAWNER }

        [NonSerialized]
        TYPE type = TYPE.NONE;
        long ownerId = 0;
        long spawnerId = 0;
        string runtimeKey = string.Empty;

        public TYPE Type => type;
        public long SpawnerId => spawnerId;
        public string RuntimeKey => runtimeKey;

        public virtual long OwnerId =>  ownerId;
        public virtual bool IsSpawner => false;
        public virtual string RuntimeKeyString => runtimeKey;

        public virtual void ReleaseAsset()
        {
            switch (type)
            {
                case TYPE.ASSET_RELEASER: // by AssetLink, AssetRef
                    {
                        var result = Addressables.ReleaseInstance(gameObject);
                        if (!result)
                        {
                            Debug.LogError($"DoAutoRelease.ReleaseAsset: failed to release instance {gameObject.name}");
                        }
                        AddressablesTracker.Remove(RuntimeKeyString, OwnerId);
                    }
                    break;

                case TYPE.ASSET_SPAWNER: // by AssetLinkSpawner, AssetRefSpawner
                    {
                        AddressablesTracker.DecreaseSpawnCount(RuntimeKeyString, SpawnerId);
                    }
                    break;
            }
        }

        void OnDestroy()
        {
#if UNITY_INCLUDE_TESTS
            Debug.Log($"Called DoAutoRelease.OnDestroy for {gameObject.name}");
#endif
            ReleaseAsset();
        }

        public static void Setup(AsyncOperationHandle operationHandle, string runtimeKey)
        {
            var goObj = operationHandle.Result as GameObject;
            if (goObj == null)
            {
                Debug.LogError($"DoAutoRelease.Setup: operationHandle.Result is not a GameObject");
                return;
            }

            var doAuto = goObj.GetOrAddComponent<DoAutoRelease>();
            doAuto.enabled = true;
            doAuto.type = TYPE.ASSET_RELEASER;
            doAuto.ownerId = GenerateInstanceId.Next();
            doAuto.runtimeKey = runtimeKey;

            //
            AddressablesTracker.Add(doAuto, AddressablesTracker.HANDLE_TYPE.ASSET_RELEASER, operationHandle);
        }

        public static void SetupForSpawner(GameObject goObj, string runtimeKey, long spawnerId)
        {
            if (goObj == null)
            {
                Debug.LogError($"DoAutoRelease.Setup: operationHandle.Result is not a GameObject");
                return;
            }

            var doAuto = goObj.GetOrAddComponent<DoAutoRelease>();
            doAuto.enabled = true;
            doAuto.type = TYPE.ASSET_SPAWNER;
            doAuto.ownerId = GenerateInstanceId.Next();
            doAuto.spawnerId = spawnerId;
            doAuto.runtimeKey = runtimeKey;

            //
            AddressablesTracker.IncreaseSpawnCount(runtimeKey, spawnerId);
        }
    }
}
