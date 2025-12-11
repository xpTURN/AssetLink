using System;
using System.Collections;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
using UnityEditor;
#endif

using xpTURN.Link;

namespace xpTURN.Link.Tests
{
    public class ObjectLinkTests
    {
        private string _player_armature = "thirdpersoncontroller/prefabs/playerarmature.prefab";
        private string _player_capsule = "thirdpersoncontroller/prefabs/playercapsule.prefab";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync();
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("SetUp");

            // Reset the pool for testing
            AddressableManager.ResetPool();
            AddressableManager.AsyncHandler.ResetPool();
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("TearDown");
        }

        [UnityTest]
        public IEnumerator AssignKey_ShouldSetKey()
        {
            // Assign the key and verify
            var ObjectLink_001 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_001.AssignKey(_player_armature);
            Assert.That(ObjectLink_001.Key, Is.EqualTo(_player_armature));

            yield return null;

            ObjectLink_001.Reset();
            Assert.That(ObjectLink_001.Key, Is.Empty);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator AssignAsset_ShouldSetAsset()
        {
            // Find the asset path from the addressable key
            string path = AddressableKeyFinder.FindAssetPathFromAddressableKey(_player_armature);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            var ObjectLink_001 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_001.AssignAsset(asset);
            Assert.That(asset, Is.EqualTo(ObjectLink_001.AssetForInspector));
            Assert.That(ObjectLink_001.Key, Is.EqualTo(_player_armature));

            yield return null;

            ObjectLink_001.Reset();
            Assert.That(ObjectLink_001.Key, Is.Empty);
        }
#endif

        [UnityTest]
        public IEnumerator LoadAssetAsync_ShouldLoadAsset()
        {
            // 
            bool isLoaded_001 = false;
            bool isLoaded_002 = false;
            bool isLoaded_003 = false;
            bool isLoaded_004 = false;
            bool isLoaded_005 = false;

            var ObjectLink_001 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_001.AssignKey(_player_armature);
            ObjectLink_001.LoadAssetAsync(asset => isLoaded_001 = true);

            var ObjectLink_002 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_002.AssignKey(_player_armature);
            ObjectLink_002.LoadAssetAsync(asset => isLoaded_002 = true);

            var ObjectLink_003 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_003.AssignKey(_player_armature);
            ObjectLink_003.LoadAssetAsync(asset => isLoaded_003 = true);

            var ObjectLink_004 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_004.AssignKey(_player_armature);
            ObjectLink_004.LoadAssetAsync(asset => isLoaded_004 = true);

            var ObjectLink_005 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_005.AssignKey(_player_armature);
            ObjectLink_005.LoadAssetAsync(asset => isLoaded_005 = true);

            yield return new WaitForSeconds(1f); // Wait for the async load to complete

            // Assert that the handler is not null
            var handler = AddressableManager.GetHandler(_player_armature);
            Assert.That(handler, Is.Not.Null);

            // Assert that all assets are loaded
            Assert.That(isLoaded_001 && isLoaded_002 && isLoaded_003 && isLoaded_004 && isLoaded_005, Is.True);

            // release references
            ObjectLink_001.Release();
            Assert.That(handler.RefLinkCount, Is.EqualTo(4));

            ObjectLink_002.Release();
            ObjectLink_003.Release();
            Assert.That(handler.RefLinkCount, Is.EqualTo(2));
            Assert.That(ObjectLink_002.Key, Is.EqualTo(_player_armature));
            Assert.That(ObjectLink_003.Key, Is.EqualTo(_player_armature));

            ObjectLink_004.Reset();
            ObjectLink_005.Reset();
            Assert.That(handler.RefLinkCount, Is.EqualTo(0));

            Assert.That(AddressableManager.PoolMaxCount, Is.EqualTo(1));
            Assert.That(AddressableManager.PoolRemaining, Is.EqualTo(1));
            Assert.That(AddressableManager.PoolUsedCount, Is.EqualTo(0));

            Assert.That(AddressableManager.AsyncHandler.PoolMaxCount, Is.EqualTo(5));
            Assert.That(AddressableManager.AsyncHandler.PoolRemaining, Is.EqualTo(5));
            Assert.That(AddressableManager.AsyncHandler.PoolUsedCount, Is.EqualTo(0));

            Assert.That(AddressableManager.IsPostponeHandler(handler), Is.False);
            Assert.That(AddressableManager.GetHandler(_player_armature), Is.Null);
        }

        [UnityTest]
        public IEnumerator Release_ShouldReleaseAsset()
        {
            //
            var ObjectLink_001 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_001.AssignKey(_player_armature);
            ObjectLink_001.LoadAssetAsync(asset => { });

            var handler_001 = AddressableManager.GetHandler(_player_armature);
            Assert.That(handler_001, Is.Not.Null);

            yield return null;

            ObjectLink_001.Release();
            var handler = AddressableManager.GetHandler(_player_armature);
            Assert.That(handler, Is.Null);
            Assert.That(AddressableManager.IsPostponeHandler(handler_001), Is.True);

            yield return new WaitForSeconds(1f); // Wait for the callback to be invoked

            Assert.That(AddressableManager.IsPostponeHandler(handler_001), Is.False);
        }

        [UnityTest]
        public IEnumerator Reset_ShouldClearKeyAndAsset()
        {
            //
            var ObjectLink_001 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_001.AssignKey(_player_armature);
            ObjectLink_001.LoadAssetAsync(asset => { });

            yield return new WaitForSeconds(1f); // Wait for the async load to complete

            ObjectLink_001.Reset();

            yield return new WaitForSeconds(1f); // Wait for the callback to be invoked

            var ObjectLink_002 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_002.AssignKey(_player_capsule);

            ObjectLink_002.Reset();

            Assert.That(ObjectLink_001.Key, Is.Empty);
            Assert.That(ObjectLink_002.Key, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ReleaseAllDeletedObjects_ShouldReleaseAllDeletedObjects()
        {
            // 
            {
                var ObjectLink_001 = new ObjectLink<UnityEngine.Object>();
                ObjectLink_001.AssignKey(_player_armature);
                ObjectLink_001.LoadAssetAsync(asset => { });

                var ObjectLink_002 = new ObjectLink<UnityEngine.Object>();
                ObjectLink_002.AssignKey(_player_armature);
                ObjectLink_002.LoadAssetAsync(asset => { });

                var ObjectLink_003 = new ObjectLink<UnityEngine.Object>();
                ObjectLink_003.AssignKey(_player_capsule);
                ObjectLink_003.LoadAssetAsync(asset => { });

                var ObjectLink_004 = new ObjectLink<UnityEngine.Object>();
                ObjectLink_004.AssignKey(_player_capsule);
                ObjectLink_004.LoadAssetAsync(asset => { });

                var ObjectLink_005 = new ObjectLink<UnityEngine.Object>();
                ObjectLink_005.AssignKey(_player_capsule);
                ObjectLink_005.LoadAssetAsync(asset => { });

                yield return new WaitForSeconds(1f); // Wait for the async load to complete

                // release references
                ObjectLink_001 = null;
                ObjectLink_002 = null;
                ObjectLink_003 = null;
                ObjectLink_004 = null;
                ObjectLink_005 = null;
            }

            yield return new WaitForSeconds(1f);

            GC.Collect();

            yield return new WaitForSeconds(1f); // Wait for GC.Collect() to complete

            AddressableManager.ReleaseAllDeletedObjects();

            Assert.That(AddressableManager.PoolMaxCount, Is.EqualTo(2));
            Assert.That(AddressableManager.PoolUsedCount, Is.EqualTo(0));
            Assert.That(AddressableManager.PoolRemaining, Is.EqualTo(2));

            Assert.That(AddressableManager.AsyncHandler.PoolUsedCount, Is.EqualTo(0));
            Assert.That(AddressableManager.AsyncHandler.PoolRemaining, Is.EqualTo(5));

            Assert.That(AddressableManager.GetHandler(_player_armature), Is.Null);
        }

        [UnityTest]
        public IEnumerator LoadAssetAsync_SameTimeLoad()
        {
            // 비동기 로드를 시작합니다.
            bool isLoaded_001 = false;
            bool isLoaded_002 = false;

            var ObjectLink_001 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_001.AssignKey(_player_armature);
            ObjectLink_001.LoadAssetAsync(asset => { isLoaded_001 = true; });
            var handler_001 = AddressableManager.GetHandler(_player_armature);

            yield return null;

            ObjectLink_001.Release(); // 1st link release
            Assert.That(AddressableManager.IsPostponeHandler(handler_001), Is.True);

            var ObjectLink_002 = new ObjectLink<UnityEngine.Object>();
            ObjectLink_002.AssignKey(_player_armature);
            ObjectLink_002.LoadAssetAsync(asset => { isLoaded_002 = true; });
            var handler_002 = AddressableManager.GetHandler(_player_armature);

            Assert.That(isLoaded_001, Is.False);
            Assert.That(isLoaded_002, Is.False);

            Assert.That(handler_001, Is.Not.EqualTo(handler_002));
            Assert.That(handler_001.Handle, Is.EqualTo(handler_002.Handle));

            yield return new WaitForSeconds(1f); // Wait for the async load to complete
            Assert.That(isLoaded_001, Is.False);
            Assert.That(isLoaded_002, Is.True);

            //
            LogAssert.Expect(LogType.Error, new Regex(@"^\[ObjectLink\] Not loaded:.*$"));
            ObjectLink_001.Release();
            ObjectLink_002.Release();

            Assert.That(AddressableManager.IsPostponeHandler(handler_001), Is.False);
            Assert.That(AddressableManager.IsPostponeHandler(handler_002), Is.False);

            var handler = AddressableManager.GetHandler(_player_armature);
            Assert.That(handler, Is.Null);

            yield return new WaitForSeconds(1f); // Wait for the callback to be invoked

            Assert.That(AddressableManager.IsPostponeHandler(handler), Is.False);
        }

        [UnityTest]
        public IEnumerator InstantiateAsync_ShouldInstantiatePrefab()
        {
            // Arrange
            var prefabLink = new PrefabLink();
            prefabLink.AssignKey(_player_armature);

            // Act
            UnityEngine.GameObject goPlayer = null;
            prefabLink.InstantiateAsync(obj => { goPlayer = obj; }, Vector3.zero, Quaternion.identity, null, true);
            while (goPlayer == null) yield return null;

            // Assert
            Assert.That(goPlayer, Is.Not.Null);
            Assert.That(goPlayer.name, Is.EqualTo("PlayerArmature (Clone)"));

            goPlayer.name = "PlayerArmature (InstantiateAsync_ShouldInstantiatePrefab)";

            // Act
            GameObject.Destroy(goPlayer);
            yield return new WaitForSeconds(1f); // Wait for the destruction to complete

            // Assert
            Assert.That(AddressableManager.GetHandler(_player_armature), Is.Null);
        }

        [UnityTest]
        public IEnumerator LoadAndInstantiateAsync()
        {
            // Arrange
            var objectLink = new ObjectLink<UnityEngine.Object>();
            objectLink.AssignKey(_player_armature);

            var prefabLink = new PrefabLink();
            prefabLink.AssignKey(_player_armature);

            // Act
            UnityEngine.Object objAsset = null;
            objectLink.LoadAssetAsync(asset => { objAsset = asset; });

            //while (objAsset == null) yield return null;

            UnityEngine.GameObject goPlayer = null;
            prefabLink.InstantiateAsync(obj => { goPlayer = obj; }, Vector3.zero, Quaternion.identity, null, true);
            while (goPlayer == null) yield return null;

            // Assert
            Assert.That(goPlayer, Is.Not.Null);
            Assert.That(goPlayer.name, Is.EqualTo("PlayerArmature (Clone)"));

            goPlayer.name = "PlayerArmature (LoadAndInstantiateAsync - InstantiateAsync)";

            // Act
            objectLink.Release();
            GameObject.Destroy(goPlayer);
            yield return new WaitForSeconds(1f); // Wait for the destruction to complete

            // Assert
            Assert.That(AddressableManager.GetHandler(_player_armature), Is.Null);
        }
    }
}