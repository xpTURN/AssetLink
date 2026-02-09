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
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

using xpTURN.AssetLink;
using xpTURN.AssetLink.Utility;

namespace xpTURN.AssetLink.Tests
{
    public class AssetLinkTests
    {
        private string _tex_asset_name = "Textures/sample.jpg";
        private string _prefab_asset_name = "Prefabs/Empty.prefab";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("SetUp");

#if ASSETLINK_UNITASK_INTEGRATION
            // Configure UniTask to propagate exceptions as-is
            UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Exception;
            UniTaskScheduler.PropagateOperationCanceledException = true;
#endif

            // Reset the pool for testing
            AddressablesTracker.ResetPool();
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("TearDown");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AddressablesTracker.ReleaseUnreferencedHandles(true);
        }

        [UnityTest]
        public IEnumerator SetAssetName_ShouldSetName()
        {
            // Assign the Name and verify
            var ObjectLink_001 = new AssetLinkT<UnityEngine.Object>(null);
            ObjectLink_001.SetAssetName(_prefab_asset_name);

            Assert.That(ObjectLink_001.RuntimeKeyString, Is.EqualTo(_prefab_asset_name));

            yield return null;

            ObjectLink_001.SetAssetName(string.Empty);
            Assert.That(ObjectLink_001.RuntimeKeyString, Is.Empty);

            yield return null;
        }

#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator LoadAssetAsync_ShouldSetReferenceCount() => UniTask.ToCoroutine(async () =>
        {
            var Link_001 = new AssetLink(_tex_asset_name);

            await Link_001.LoadAssetAsync<Texture>();

            await UniTask.DelayFrame(2); // Wait a few frames for internal ref count to update

            Assert.That(Link_001.GetReferenceCount(), Is.EqualTo(1));

            Link_001.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator LoadAssetAsync_ShouldLoadAsset() => UniTask.ToCoroutine(async () =>
        {
            // 
            var Link_001 = new AssetLink(_tex_asset_name);

            var ObjectLink_001 = new AssetLink(_prefab_asset_name);
            var ObjectLink_002 = new AssetLink(_prefab_asset_name);
            var ObjectLink_003 = new AssetLink(_prefab_asset_name);
            var ObjectLink_004 = new AssetLink(_prefab_asset_name);
            var ObjectLink_005 = new AssetLink(_prefab_asset_name);

            await Link_001.LoadAssetAsync<Texture>().ToUniTask();
            var task1 = ObjectLink_001.LoadAssetAsync<GameObject>().ToUniTask();
            var task2 = ObjectLink_002.LoadAssetAsync<GameObject>().ToUniTask();
            var task3 = ObjectLink_003.LoadAssetAsync<GameObject>().ToUniTask();
            var task4 = ObjectLink_004.LoadAssetAsync<GameObject>().ToUniTask();
            var task5 = ObjectLink_005.LoadAssetAsync<GameObject>().ToUniTask();

            await UniTask.WhenAll(task1, task2, task3, task4, task5);

            await UniTask.DelayFrame(2); // Wait a few frames for internal ref count to update

            Assert.That(ObjectLink_001.GetReferenceCount(), Is.EqualTo(5));
            Assert.That(ObjectLink_002.GetReferenceCount(), Is.EqualTo(5));
            Assert.That(ObjectLink_003.GetReferenceCount(), Is.EqualTo(5));
            Assert.That(ObjectLink_004.GetReferenceCount(), Is.EqualTo(5));
            Assert.That(ObjectLink_005.GetReferenceCount(), Is.EqualTo(5));

            // Assert that the handler is not null
            SortedDictionary<long, AddressablesTracker.TrackedHandle> handles = null;
            handles = AddressablesTracker.GetHandles(_prefab_asset_name);
            foreach (var item in handles.Values)
            {
                Assert.That(item.OperationHandle.IsValid(), Is.True);
            }

            Assert.That(handles, Is.Not.Null);
            Assert.That(handles.Count, Is.EqualTo(5));
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(5));

            // release references
            ObjectLink_001.ReleaseAsset();

            await UniTask.DelayFrame(1);

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(4));
            Assert.That(ObjectLink_005.GetReferenceCount(), Is.EqualTo(4));

            ObjectLink_002.ReleaseAsset();
            ObjectLink_003.ReleaseAsset();

            await UniTask.DelayFrame(1);

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(2));
            Assert.That(ObjectLink_005.GetReferenceCount(), Is.EqualTo(2));

            Assert.That(ObjectLink_002.RuntimeKeyString, Is.EqualTo(_prefab_asset_name));
            Assert.That(ObjectLink_003.RuntimeKeyString, Is.EqualTo(_prefab_asset_name));

            ObjectLink_004.Reset();

            await UniTask.DelayFrame(1);

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(1));
            Assert.That(ObjectLink_005.GetReferenceCount(), Is.EqualTo(1));

            ObjectLink_005.Reset();

            await UniTask.DelayFrame(1);

            Link_001.ReleaseAsset();

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));

            Assert.That(AddressablesTracker.PoolMaxCount, Is.EqualTo(6));
            Assert.That(AddressablesTracker.PoolRemaining, Is.EqualTo(6));
            Assert.That(AddressablesTracker.PoolUsedCount, Is.EqualTo(0));

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator ReleaseAsset_ShouldRelease() => UniTask.ToCoroutine(async () =>
        {
            //
            var ObjectLink_001 = new AssetLink();
            ObjectLink_001.SetAssetName(_prefab_asset_name);
            await ObjectLink_001.LoadAssetAsync<GameObject>().ToUniTask();

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(1));

            await UniTask.DelayFrame(1);

            ObjectLink_001.ReleaseAsset();
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator Reset_ShouldClearNameAndAsset() => UniTask.ToCoroutine(async () =>
        {
            //
            var ObjectLink_001 = new AssetLink();
            ObjectLink_001.SetAssetName(_prefab_asset_name);
            await ObjectLink_001.LoadAssetAsync<GameObject>().ToUniTask();

            ObjectLink_001.Reset();

            var ObjectLink_002 = new AssetLink();
            ObjectLink_002.SetAssetName(_prefab_asset_name);

            ObjectLink_002.Reset();

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));
            Assert.That(ObjectLink_001.RuntimeKeyString, Is.Empty);
            Assert.That(ObjectLink_002.RuntimeKeyString, Is.Empty);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator ReleaseUnreferencedHandles_ShouldReleaseAllUnreferencedAssets() => UniTask.ToCoroutine(async () =>
        {
            // Register log expectations because 5 AssetLinks are released as unreferenced
            // No CallStack (EnableStackTrace=false): message ends with "AddressableAsset: '...'"
            // With CallStack (EnableStackTrace=true): "AddressableAsset: '...'\nCallStack: ..." → (?:...) group matches
            const string unreferencedLogPattern = @"AddressablesTracker: Unreferenced AddressableAsset released\nAddressableAsset: '\[.*?\] .*'(?:\nCallStack:[\s\S]*)?";
            var regexOpts = RegexOptions.Singleline;
            LogAssert.Expect(LogType.Error, new Regex(unreferencedLogPattern, regexOpts));
            LogAssert.Expect(LogType.Error, new Regex(unreferencedLogPattern, regexOpts));
            LogAssert.Expect(LogType.Error, new Regex(unreferencedLogPattern, regexOpts));
            LogAssert.Expect(LogType.Error, new Regex(unreferencedLogPattern, regexOpts));
            LogAssert.Expect(LogType.Error, new Regex(unreferencedLogPattern, regexOpts));
            LogAssert.Expect(LogType.Error, new Regex(@"AddressablesTracker: Released \d+ unreferenced AddressableAsset\(s\)"));

            // 
            {
                var ObjectLink_001 = new AssetLink();
                ObjectLink_001.SetAssetName(_prefab_asset_name);
                await ObjectLink_001.LoadAssetAsync<GameObject>().ToUniTask();

                var ObjectLink_002 = new AssetLink();
                ObjectLink_002.SetAssetName(_prefab_asset_name);
                await ObjectLink_002.LoadAssetAsync<GameObject>().ToUniTask();

                var ObjectLink_003 = new AssetLink();
                ObjectLink_003.SetAssetName(_prefab_asset_name);
                await ObjectLink_003.LoadAssetAsync<GameObject>().ToUniTask();

                var ObjectLink_004 = new AssetLink();
                ObjectLink_004.SetAssetName(_prefab_asset_name);
                await ObjectLink_004.LoadAssetAsync<GameObject>().ToUniTask();

                var ObjectLink_005 = new AssetLink();
                ObjectLink_005.SetAssetName(_prefab_asset_name);
                await ObjectLink_005.LoadAssetAsync<GameObject>().ToUniTask();

                // Remove references
                ObjectLink_001 = null;
                ObjectLink_002 = null;
                ObjectLink_003 = null;
                ObjectLink_004 = null;
                ObjectLink_005 = null;
            }

            await UniTask.DelayFrame(2);
            GC.Collect();

            await UniTask.WaitForSeconds(1);

            AddressablesTracker.ReleaseUnreferencedHandles(true);

            Assert.That(AddressablesTracker.PoolUsedCount, Is.EqualTo(0));
            Assert.That(AddressablesTracker.PoolRemaining, Is.EqualTo(5));

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator ReleaseUnreferencedHandles_WhenProperlyReleased_ShouldNotReportUnreferenced() => UniTask.ToCoroutine(async () =>
        {
            // 
            {
                var ObjectLink_001 = new AssetLink();
                ObjectLink_001.SetAssetName(_prefab_asset_name);
                await ObjectLink_001.LoadAssetAsync<GameObject>().ToUniTask();

                var ObjectLink_002 = new AssetLink();
                ObjectLink_002.SetAssetName(_prefab_asset_name);
                await ObjectLink_002.LoadAssetAsync<GameObject>().ToUniTask();

                var ObjectLink_003 = new AssetLink();
                ObjectLink_003.SetAssetName(_prefab_asset_name);
                await ObjectLink_003.LoadAssetAsync<GameObject>().ToUniTask();

                var ObjectLink_004 = new AssetLink();
                ObjectLink_004.SetAssetName(_prefab_asset_name);
                await ObjectLink_004.LoadAssetAsync<GameObject>().ToUniTask();

                var ObjectLink_005 = new AssetLink();
                ObjectLink_005.SetAssetName(_prefab_asset_name);
                await ObjectLink_005.LoadAssetAsync<GameObject>().ToUniTask();

                // release references
                ObjectLink_001.ReleaseAsset();
                ObjectLink_002.ReleaseAsset();
                ObjectLink_003.ReleaseAsset();
                ObjectLink_004.ReleaseAsset();
                ObjectLink_005.ReleaseAsset();
            }

            await UniTask.DelayFrame(2);
            GC.Collect();

            await UniTask.WaitForSeconds(1);

            AddressablesTracker.ReleaseUnreferencedHandles(true);

            Assert.That(AddressablesTracker.PoolUsedCount, Is.EqualTo(0));
            Assert.That(AddressablesTracker.PoolRemaining, Is.EqualTo(5));

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator InstantiateAsync_ShouldInstantiate() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var prefabLink = new AssetLinkGameObject(_prefab_asset_name);

            // Act
            GameObject goEmpty = await prefabLink.InstantiateAsync(Vector3.zero, Quaternion.identity, null).ToUniTask();

            // Assert
            Assert.That(goEmpty, Is.Not.Null);
            Assert.That(goEmpty.name, Is.EqualTo("Empty(Clone)"));

            goEmpty.name = "Empty(InstantiateAsync_ShouldInstantiate)";

            await UniTask.DelayFrame(2);

            // Act
            GameObject.Destroy(goEmpty);
            goEmpty = null;

            await UniTask.DelayFrame(1);

            // Assert
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));

            await UniTask.DelayFrame(1);
            LogAssert.Expect(LogType.Log, new Regex(@"Called DoAutoRelease.OnDestroy for.*"));
        });

        [UnityTest]
        public IEnumerator LoadAndInstantiateAsync() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var objectLink = new AssetLink(_prefab_asset_name);
            var prefabLink = new AssetLinkGameObject(_prefab_asset_name);

            // Act
            var objAsset = await objectLink.LoadAssetAsync<Object>().ToUniTask();

            // Assert
            Assert.That(objAsset, Is.Not.Null);

            // Act
            var objPrefab = await prefabLink.LoadAssetAsync().ToUniTask();

            // Assert
            Assert.That(objPrefab, Is.Not.Null);
            Assert.That(objPrefab.name, Is.EqualTo("Empty"));

            Assert.That(prefabLink.GetReferenceCount(), Is.EqualTo(2));

            // Act
            GameObject goInstance = await prefabLink.InstantiateAsync(Vector3.zero, Quaternion.identity, null).ToUniTask();

            // Assert
            Assert.That(goInstance, Is.Not.Null);
            Assert.That(goInstance.name, Is.EqualTo("Empty(Clone)"));

            goInstance.name = "Empty(LoadAndInstantiateAsync)";

            await UniTask.DelayFrame(2);

            Assert.That(prefabLink.GetReferenceCount(), Is.EqualTo(2));

            // Act
            GameObject.Destroy(goInstance);

            // Wait for Addressables internal async completion
            await UniTask.DelayFrame(2);

            // Assert
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(2));
            Assert.That(prefabLink.GetReferenceCount(), Is.EqualTo(1));

            // Act
            objectLink.ReleaseAsset();

            // Assert
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(1));

            // Act
            prefabLink.ReleaseAsset();

            // Assert
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));

            // Wait for Addressables internal async completion
            await UniTask.DelayFrame(2);
            LogAssert.Expect(LogType.Log, new Regex(@"Called DoAutoRelease.OnDestroy for.*"));
        });

        [UnityTest]
        public IEnumerator InstantiateAsync_ManyInstances_ShouldCleanupOnDestroy() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var prefabLink = new AssetLinkGameObject(_prefab_asset_name);

            // Act
            List<GameObject> goList = new ();
            for (int i = 0; i < 10; ++i)
            {
                GameObject goInstance = await prefabLink.InstantiateAsync(Vector3.zero, Quaternion.identity, null).ToUniTask();

                // Assert
                Assert.That(goInstance, Is.Not.Null);
                Assert.That(goInstance.name, Is.EqualTo("Empty(Clone)"));

                goInstance.name = $"Empty(LoadAndInstantiateAsync-{i:D3})";
                goList.Add(goInstance);
            }

            await UniTask.DelayFrame(2);

            Assert.That(prefabLink.GetReferenceCount(), Is.EqualTo(0)); // InstantiateAsync counts refs per instance so load RefCount does not increase.

            // Act
            foreach (var ins in goList)
            {
                GameObject.Destroy(ins);
            }

            // Wait for Addressables internal async completion
            await UniTask.DelayFrame(2);

            // Assert
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));
            Assert.That(prefabLink.GetReferenceCount(), Is.EqualTo(0));

            // Wait for Addressables internal async completion
            await UniTask.DelayFrame(2);
            LogAssert.Expect(LogType.Log, new Regex(@"Called DoAutoRelease.OnDestroy for.*"));
        });

        #region Error Cases

        [UnityTest]
        public IEnumerator LoadAssetAsync_WhenAlreadyLoaded_ShouldLogError() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Attempting to load AssetLink that has already been loaded. Handle is exposed through getter OperationHandle");

            var link = new AssetLink(_prefab_asset_name);

            // Act - First load
            await link.LoadAssetAsync<GameObject>().ToUniTask();

            // Act - Attempt to load again when already loaded (intentionally without await)
            _ = link.LoadAssetAsync<GameObject>();

            // Cleanup
            link.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator ReleaseAsset_WhenNotLoaded_ShouldLogWarning() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "Cannot release a null or unloaded asset.");

            var link = new AssetLink(_prefab_asset_name);

            // Act - Attempt Release without loading
            link.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        #endregion

        #region State Validation

        [Test]
        public void RuntimeKeyIsValid_WhenAssetNameSet_ShouldReturnTrue()
        {
            // Arrange
            var link = new AssetLink(_prefab_asset_name);

            // Act & Assert
            Assert.That(link.RuntimeKeyIsValid(), Is.True);
        }

        [Test]
        public void RuntimeKeyIsValid_WhenEmpty_ShouldReturnFalse()
        {
            // Arrange
            var link = new AssetLink();

            // Act & Assert
            Assert.That(link.RuntimeKeyIsValid(), Is.False);

            // Set to empty string
            link.SetAssetName(string.Empty);
            Assert.That(link.RuntimeKeyIsValid(), Is.False);
        }

        [UnityTest]
        public IEnumerator IsValid_BeforeLoad_ShouldReturnFalse() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var link = new AssetLink(_prefab_asset_name);

            // Assert - Before load
            Assert.That(link.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator IsValid_AfterLoad_ShouldReturnTrue() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var link = new AssetLink(_prefab_asset_name);

            // Act
            await link.LoadAssetAsync<GameObject>().ToUniTask();

            // Assert - After load
            Assert.That(link.IsValid(), Is.True);

            // Cleanup
            link.ReleaseAsset();

            // Assert - After Release
            Assert.That(link.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator IsDone_AfterLoadComplete_ShouldReturnTrue() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var link = new AssetLink(_prefab_asset_name);

            // Act
            var handle = link.LoadAssetAsync<GameObject>();

            // Assert - May still be loading
            // IsDone is true after completion
            await handle.ToUniTask();

            Assert.That(link.IsDone, Is.True);

            // Cleanup
            link.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        #endregion

        #region Behavior Cases

        [UnityTest]
        public IEnumerator SetAssetName_WhenAlreadyLoaded_ShouldReleaseAndSetNew() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var link = new AssetLink(_prefab_asset_name);
            await link.LoadAssetAsync<GameObject>().ToUniTask();

            Assert.That(link.IsValid(), Is.True);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(1));

            // Act - Change to different asset name
            link.SetAssetName(_tex_asset_name);

            // Assert - Existing handle must be released
            Assert.That(link.IsValid(), Is.False);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(0));
            Assert.That(link.RuntimeKeyString, Is.EqualTo(_tex_asset_name));

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator SetAssetName_WhenSameAsset_ShouldNotRelease() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var link = new AssetLink(_prefab_asset_name);
            await link.LoadAssetAsync<GameObject>().ToUniTask();

            Assert.That(link.IsValid(), Is.True);

            // Act - Set to same name
            link.SetAssetName(_prefab_asset_name);

            // Assert - Handle must be retained
            Assert.That(link.IsValid(), Is.True);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_name).Count, Is.EqualTo(1));

            // Cleanup
            link.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator Asset_AfterLoad_ShouldReturnLoadedAsset() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var link = new AssetLink(_prefab_asset_name);

            // Assert - Before load
            Assert.That(link.Asset, Is.Null);

            // Act
            await link.LoadAssetAsync<GameObject>().ToUniTask();

            // Assert - After load
            Assert.That(link.Asset, Is.Not.Null);
            Assert.That(link.Asset.name, Is.EqualTo("Empty"));

            // Cleanup
            link.ReleaseAsset();

            // Assert - After Release
            Assert.That(link.Asset, Is.Null);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator Constructor_WithAssetName_ShouldSetRuntimeKey() => UniTask.ToCoroutine(async () =>
        {
            // Act
            var link = new AssetLink(_prefab_asset_name);

            // Assert
            Assert.That(link.RuntimeKeyString, Is.EqualTo(_prefab_asset_name));
            Assert.That(link.AssetName, Is.EqualTo(_prefab_asset_name));
            Assert.That(link.RuntimeKeyIsValid(), Is.True);

            await UniTask.DelayFrame(1);
        });

        #endregion
#endif
    }
}