#pragma warning disable CS0414
using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

using xpTURN.AssetLink;

namespace xpTURN.AssetLink.Tests
{
    public class AssetRefTests
    {
        // sample.jpg GUID
        private string _tex_asset_guid = "901221460c3a046f8b3dd3c0aa1631a4";
        // Empty.prefab GUID
        private string _prefab_asset_guid = "23f2109d9f7d945669a1e705e9e3f1ac";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("AssetRefTests OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("AssetRefTests SetUp");

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
            Debug.Log("AssetRefTests TearDown");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AddressablesTracker.ReleaseUnreferencedHandles(true);
        }


#if ASSETLINK_UNITASK_INTEGRATION
        #region SetAssetGUID Tests
        [UnityTest]
        public IEnumerator SetAssetGUID_ShouldSetGUID()
        {
            // Arrange
            var assetRef = new AssetRefT<Object>(null);

            // Act
            assetRef.SetAssetGUID(_prefab_asset_guid);

            // Assert
            Assert.That(assetRef.RuntimeKeyString, Is.EqualTo(_prefab_asset_guid));

            yield return null;

            // Act - Set to empty string
            assetRef.SetAssetGUID(string.Empty);

            // Assert
            Assert.That(assetRef.RuntimeKeyString, Is.Empty);

            yield return null;
        }

        #endregion

        #region LoadAssetAsync Tests

        [UnityTest]
        public IEnumerator LoadAssetAsync_ShouldLoadAsset() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_tex_asset_guid);

            // Act
            await assetRef.LoadAssetAsync<Texture>().ToUniTask();

            await UniTask.DelayFrame(10);

            // Assert
            Assert.That(assetRef.GetReferenceCount(), Is.EqualTo(1));
            Assert.That(assetRef.IsValid(), Is.True);
            Assert.That(assetRef.Asset, Is.Not.Null);

            // Cleanup
            assetRef.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator LoadAssetAsync_MultipleRefs_ShouldShareReferenceCount() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef1 = new AssetRef(_tex_asset_guid);
            var assetRef2 = new AssetRef(_tex_asset_guid);

            // Act
            await assetRef1.LoadAssetAsync<Texture>().ToUniTask();
            await assetRef2.LoadAssetAsync<Texture>().ToUniTask();

            await UniTask.DelayFrame(10);

            // Assert
            Assert.That(assetRef1.GetReferenceCount(), Is.EqualTo(2));
            Assert.That(assetRef2.GetReferenceCount(), Is.EqualTo(2));

            // Cleanup
            assetRef1.ReleaseAsset();

            await UniTask.DelayFrame(1);

            Assert.That(assetRef2.GetReferenceCount(), Is.EqualTo(1));

            assetRef2.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        #endregion

        #region ReleaseAsset Tests

        [UnityTest]
        public IEnumerator ReleaseAsset_ShouldRelease() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);
            await assetRef.LoadAssetAsync<GameObject>().ToUniTask();

            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_guid).Count, Is.EqualTo(1));

            await UniTask.DelayFrame(1);

            // Act
            assetRef.ReleaseAsset();

            // Assert
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_guid).Count, Is.EqualTo(0));
            Assert.That(assetRef.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        #endregion

        #region Reset Tests

        [UnityTest]
        public IEnumerator Reset_ShouldClearGUIDAndAsset() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);
            await assetRef.LoadAssetAsync<GameObject>().ToUniTask();

            // Act
            assetRef.Reset();

            // Assert
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_guid).Count, Is.EqualTo(0));
            Assert.That(string.IsNullOrEmpty(assetRef.RuntimeKeyString), Is.True);
            Assert.That(assetRef.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        #endregion

        #region Error Cases

        [UnityTest]
        public IEnumerator LoadAssetAsync_WhenAlreadyLoaded_ShouldLogError() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Attempting to load AssetRef that has already been loaded. Handle is exposed through getter OperationHandle");

            var assetRef = new AssetRef(_prefab_asset_guid);

            // Act - First load
            await assetRef.LoadAssetAsync<GameObject>().ToUniTask();

            // Act - Attempt to load again when already loaded (intentionally without await)
            _ = assetRef.LoadAssetAsync<GameObject>();

            // Cleanup
            assetRef.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator ReleaseAsset_WhenNotLoaded_ShouldLogWarning() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "Cannot release a null or unloaded asset.");

            var assetRef = new AssetRef(_prefab_asset_guid);

            // Act - Attempt Release without loading
            assetRef.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        #endregion

        #region State Validation

        [Test]
        public void RuntimeKeyIsValid_WhenAssetGUIDSet_ShouldReturnTrue()
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);

            // Act & Assert
            Assert.That(assetRef.RuntimeKeyIsValid(), Is.True);
        }

        [Test]
        public void RuntimeKeyIsValid_WhenEmpty_ShouldReturnFalse()
        {
            // Arrange
            var assetRef = new AssetRef();

            // Act & Assert
            Assert.That(assetRef.RuntimeKeyIsValid(), Is.False);

            // Set to empty string
            assetRef.SetAssetGUID(string.Empty);
            Assert.That(assetRef.RuntimeKeyIsValid(), Is.False);
        }

        [UnityTest]
        public IEnumerator IsValid_BeforeLoad_ShouldReturnFalse() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);

            // Assert - Before load
            Assert.That(assetRef.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator IsValid_AfterLoad_ShouldReturnTrue() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);

            // Act
            await assetRef.LoadAssetAsync<GameObject>().ToUniTask();

            // Assert - After load
            Assert.That(assetRef.IsValid(), Is.True);

            // Cleanup
            assetRef.ReleaseAsset();

            // Assert - After Release
            Assert.That(assetRef.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator IsDone_AfterLoadComplete_ShouldReturnTrue() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);

            // Act
            var handle = assetRef.LoadAssetAsync<GameObject>();

            // Assert - After load complete
            await handle.ToUniTask();

            Assert.That(assetRef.IsDone, Is.True);

            // Cleanup
            assetRef.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        #endregion

        #region Behavior Cases

        [UnityTest]
        public IEnumerator SetAssetGUID_WhenAlreadyLoaded_ShouldReleaseAndSetNew() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);
            await assetRef.LoadAssetAsync<GameObject>().ToUniTask();

            Assert.That(assetRef.IsValid(), Is.True);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_guid).Count, Is.EqualTo(1));

            // Act - Change to different asset GUID
            assetRef.SetAssetGUID(_tex_asset_guid);

            // Assert - Existing handle must be released
            Assert.That(assetRef.IsValid(), Is.False);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_guid).Count, Is.EqualTo(0));
            Assert.That(assetRef.RuntimeKeyString, Is.EqualTo(_tex_asset_guid));

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator SetAssetGUID_WhenSameAsset_ShouldNotRelease() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);
            await assetRef.LoadAssetAsync<GameObject>().ToUniTask();

            Assert.That(assetRef.IsValid(), Is.True);

            // Act - Set to same GUID
            assetRef.SetAssetGUID(_prefab_asset_guid);

            // Assert - Handle must be retained
            Assert.That(assetRef.IsValid(), Is.True);
            Assert.That(AddressablesTracker.GetHandles(_prefab_asset_guid).Count, Is.EqualTo(1));

            // Cleanup
            assetRef.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator Asset_AfterLoad_ShouldReturnLoadedAsset() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var assetRef = new AssetRef(_prefab_asset_guid);

            // Assert - Before load
            Assert.That(assetRef.Asset, Is.Null);

            // Act
            await assetRef.LoadAssetAsync<GameObject>().ToUniTask();

            // Assert - After load
            Assert.That(assetRef.Asset, Is.Not.Null);
            Assert.That(assetRef.Asset.name, Is.EqualTo("Empty"));

            // Cleanup
            assetRef.ReleaseAsset();

            // Assert - After Release
            Assert.That(assetRef.Asset, Is.Null);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator Constructor_WithAssetGUID_ShouldSetRuntimeKey() => UniTask.ToCoroutine(async () =>
        {
            // Act
            var assetRef = new AssetRef(_prefab_asset_guid);

            // Assert
            Assert.That(assetRef.RuntimeKeyString, Is.EqualTo(_prefab_asset_guid));
            Assert.That(assetRef.AssetGUID, Is.EqualTo(_prefab_asset_guid));
            Assert.That(assetRef.RuntimeKeyIsValid(), Is.True);

            await UniTask.DelayFrame(1);
        });
        #endregion
#endif
    }
}
