#if ASSETLINK_UNITASK_INTEGRATION
#pragma warning disable CS0414
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

using xpTURN.AssetLink;
using xpTURN.AssetLink.Utility;

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

namespace xpTURN.AssetLink.Tests
{
    public class AssetLinkSpawnerTests
    {
        private string _prefab_asset_name = "Prefabs/Empty.prefab";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("AssetLinkSpawnerTests OneTimeSetUp");
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("AssetLinkSpawnerTests SetUp");

#if ASSETLINK_UNITASK_INTEGRATION
            UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Exception;
            UniTaskScheduler.PropagateOperationCanceledException = true;
#endif

            AddressablesTracker.ResetPool();
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("AssetLinkSpawnerTests TearDown");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AddressablesTracker.ReleaseUnreferencedHandles(true);
        }

        [Test]
        public void Constructor_WithName_ShouldSetRuntimeKey()
        {
            var spawner = new AssetLinkSpawner(_prefab_asset_name);

            Assert.That(spawner.RuntimeKeyString, Is.EqualTo(_prefab_asset_name));
            Assert.That(spawner.RuntimeKeyIsValid(), Is.True);
        }

        [Test]
        public void Constructor_WithNull_ShouldAllowEmptyKey()
        {
            var spawner = new AssetLinkSpawner(null);

            Assert.That(spawner.RuntimeKeyString, Is.Null.Or.Empty);
        }

        [Test]
        public void SetAssetName_WhenSpawnerHasName_ShouldSetRuntimeKey()
        {
            var spawner = new AssetLinkSpawner(_prefab_asset_name);

            Assert.That(spawner.RuntimeKeyString, Is.EqualTo(_prefab_asset_name));

            spawner.SetAssetName(string.Empty);
            Assert.That(spawner.RuntimeKeyString, Is.Empty);
        }


#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator SpawnAsync_WithPositionRotationParent_ShouldSpawn() => UniTask.ToCoroutine(async () =>
        {
            var spawner = new AssetLinkSpawner(_prefab_asset_name);

            GameObject go = await spawner.SpawnAsync(Vector3.zero, Quaternion.identity, null);

            Assert.That(go, Is.Not.Null);
            Assert.That(go.name, Is.EqualTo("Empty (Clone)"));

            int spawnCount = AddressablesTracker.GetSpawnCount(_prefab_asset_name, spawner.OwnerId);
            Assert.That(spawnCount, Is.EqualTo(1));

            go.name = "Empty (SpawnAsync_WithPositionRotationParent_ShouldSpawn)";
            Object.Destroy(go);

            await UniTask.DelayFrame(2);

            Assert.That(AddressablesTracker.GetSpawnCount(_prefab_asset_name, spawner.OwnerId), Is.EqualTo(0));
            spawner.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator SpawnAsync_WithParentOverload_ShouldSpawn() => UniTask.ToCoroutine(async () =>
        {
            var spawner = new AssetLinkSpawner(_prefab_asset_name);

            GameObject go = await spawner.SpawnAsync(null, false);

            Assert.That(go, Is.Not.Null);
            Assert.That(go.name, Is.EqualTo("Empty (Clone)"));

            int spawnCount = AddressablesTracker.GetSpawnCount(_prefab_asset_name, spawner.OwnerId);
            Assert.That(spawnCount, Is.EqualTo(1));

            go.name = "Empty(SpawnAsync_WithParentOverload_ShouldSpawn)";
            Object.Destroy(go);

            await UniTask.DelayFrame(2);

            spawner.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator ReleaseAsset_WhenSpawnCountZero_ShouldRelease() => UniTask.ToCoroutine(async () =>
        {
            var spawner = new AssetLinkSpawner(_prefab_asset_name);
            GameObject go = await spawner.SpawnAsync(Vector3.zero, Quaternion.identity, null);

            Assert.That(go, Is.Not.Null);
            go.name = "Empty (ReleaseAsset_WhenSpawnCountZero_ShouldRelease)";
            Object.Destroy(go);

            await UniTask.DelayFrame(2);

            Assert.That(AddressablesTracker.GetSpawnCount(_prefab_asset_name, spawner.OwnerId), Is.EqualTo(0));
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));

            spawner.ReleaseAsset();

            await UniTask.DelayFrame(1);

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));

            LogAssert.Expect(LogType.Log, new Regex(@"Called DoAutoRelease.OnDestroy for.*"));
        });

        [UnityTest]
        public IEnumerator ReleaseAsset_WhenSpawnCountGreaterThanZero_ShouldLogErrorAndNotRelease() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Error, new Regex(@"AssetLinkSpawner .* is not released\. Spawn count: 1"));

            var spawner = new AssetLinkSpawner(_prefab_asset_name);
            GameObject go = await spawner.SpawnAsync(Vector3.zero, Quaternion.identity, null);

            Assert.That(go, Is.Not.Null);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(1));

            spawner.ReleaseAsset();

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(1));

            go.name = "Empty(ReleaseAsset_WhenSpawnCountGreaterThanZero)";
            Object.Destroy(go);

            await UniTask.DelayFrame(2);

            spawner.ReleaseAsset();

            await UniTask.DelayFrame(1);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator SpawnAsync_MultipleInstances_ShouldTrackSpawnCountAndReleaseByAssetOwner() => UniTask.ToCoroutine(async () =>
        {
            var spawner = new AssetLinkSpawner(_prefab_asset_name);
            var instances = new List<GameObject>();

            for (int i = 0; i < 5; i++)
            {
                GameObject go = await spawner.SpawnAsync(Vector3.zero, Quaternion.identity, null);
                Assert.That(go, Is.Not.Null);
                go.name = $"Empty (SpawnAsync_MultipleInstances-{i:D3})";
                instances.Add(go);
            }

            await UniTask.DelayFrame(1);

            Assert.That(AddressablesTracker.GetSpawnCount(_prefab_asset_name, spawner.OwnerId), Is.EqualTo(5));

            foreach (var go in instances)
            {
                Object.Destroy(go);
            }

            await UniTask.DelayFrame(2);

            Assert.That(AddressablesTracker.GetSpawnCount(_prefab_asset_name, spawner.OwnerId), Is.EqualTo(0));
            spawner.ReleaseAsset();

            await UniTask.DelayFrame(1);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator SpawnAsync_MultipleInstances_ShouldTrackSpawnCountAndReleaseByAddressablesTracker() => UniTask.ToCoroutine(async () =>
        {
            long spawnerId = 0;
            var instances = new List<GameObject>();

            {
                var spawner = new AssetLinkSpawner(_prefab_asset_name);
                spawnerId = spawner.OwnerId;
                for (int i = 0; i < 5; i++)
                {
                    GameObject go = await spawner.SpawnAsync(Vector3.zero, Quaternion.identity, null);
                    Assert.That(go, Is.Not.Null);
                    go.name = $"Empty (SpawnAsync_MultipleInstances-{i:D3})";
                    instances.Add(go);
                }

                spawner = null;    
            }

            await UniTask.DelayFrame(1);
            Assert.That(AddressablesTracker.GetSpawnCount(_prefab_asset_name, spawnerId), Is.EqualTo(5));

            GC.Collect();

            await UniTask.WaitForSeconds(1);

            foreach (var go in instances)
            {
                Object.Destroy(go);
            }

            await UniTask.DelayFrame(2);

            Assert.That(AddressablesTracker.GetSpawnCount(_prefab_asset_name, spawnerId), Is.EqualTo(0));

            await UniTask.DelayFrame(1);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));
        });
#endif
    }
}
#endif