#pragma warning disable CS0414
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

using xpTURN.AssetLink;

namespace xpTURN.AssetLink.Tests
{
    public class AssetRefSceneTests
    {
        private string _scene_name_1 = "Empty1";
        private string _scene_asset_guid_1 = "8c312b7852ed344d281f4e5d059f69b7";
        private string _scene_asset_guid_2 = "6a69d13e01a764f3aa4035c9b9a6934d";

        /// <summary>AssetRef for scene loaded via LoadSceneAsync. Call UnLoadScene in TearDown if not released.</summary>
        private List<AssetRef> _loadedSceneRefs = new List<AssetRef>();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("AssetRefSceneTests OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("AssetRefSceneTests SetUp");

#if ASSETLINK_UNITASK_INTEGRATION
            // Configure UniTask to propagate exceptions as-is
            UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Exception;
            UniTaskScheduler.PropagateOperationCanceledException = true;
#endif
            _loadedSceneRefs.Clear();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log("AssetRefSceneTests TearDown");

            foreach (var link in _loadedSceneRefs)
            {
                if (link != null && link.IsValid())
                {
                    var unloadHandle = link.UnLoadScene();
                    yield return unloadHandle.ToUniTask().ToCoroutine();
                }
            }
            _loadedSceneRefs.Clear();
        }

#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator LoadSceneAsync_ShouldLoadScene() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var sceneRef1 = new AssetRef(_scene_asset_guid_1);
            var sceneRef2 = new AssetRef(_scene_asset_guid_2);
            _loadedSceneRefs.Add(sceneRef1);
            _loadedSceneRefs.Add(sceneRef2);

            // Act
            var handle1 = sceneRef1.LoadSceneAsync(LoadSceneMode.Single, true, 100);
            var sceneInstance1 = await handle1.ToUniTask();

            // Assert
            Assert.That(sceneInstance1.Scene.isLoaded, Is.True);
            Assert.That(sceneRef1.IsValid(), Is.True);
            string name1 = sceneInstance1.Scene.name;

            var activeScene = SceneManager.GetActiveScene();
            Assert.That(name1, Is.EqualTo(activeScene.name));

            // AssetRef.LoadSceneAsync -> previous sceneLink1 is unloaded implicitly.
            var handle2 = sceneRef2.LoadSceneAsync(LoadSceneMode.Single, true, 100);
            var sceneInstance2 = await handle2.ToUniTask();

            // Assert
            Assert.That(sceneInstance2.Scene.isLoaded, Is.True);
            Assert.That(sceneRef2.IsValid(), Is.True);
            string name2 = sceneInstance2.Scene.name;

            //
            activeScene = SceneManager.GetActiveScene();
            Assert.That(name2, Is.EqualTo(activeScene.name));
            Assert.That(sceneRef1.IsValid(), Is.False);

            // Manual unload required when scene use has ended regardless of Addressables.
            // await sceneRef1.UnLoadScene().ToUniTask();

            Assert.That(AddressablesTracker.GetHandles(_scene_asset_guid_1).Count, Is.EqualTo(0));

            // In Single mode loading next scene unloads the previous. Set active scene to temp then unload sceneRef2.
            var tempScene = SceneManager.CreateScene($"tempScene-{Guid.NewGuid()}");
            bool actived = SceneManager.SetActiveScene(tempScene);
            Assert.That(actived, Is.True);

            // Manual unload required when scene use has ended regardless of Addressables.
            await sceneRef2.UnLoadScene().ToUniTask();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator LoadSceneAsync_WithAdditive_ShouldLoadScene() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var sceneRef = new AssetRef(_scene_asset_guid_1);
            _loadedSceneRefs.Add(sceneRef);

            // Act
            var handle = sceneRef.LoadSceneAsync(LoadSceneMode.Additive, true, 100);
            var sceneInstance = await handle.ToUniTask();

            // Assert
            Assert.That(sceneInstance.Scene.isLoaded, Is.True);

            string scene_name_1 = Application.isEditor ? _scene_name_1 : _scene_asset_guid_1; // In Player build scene name is returned as GUID.
            Assert.That(sceneInstance.Scene.name, Is.EqualTo(scene_name_1));

            bool found = Enumerable.Range(0, SceneManager.loadedSceneCount)
                .Any(i => SceneManager.GetSceneAt(i).name == scene_name_1);
            Assert.That(found, Is.True);

            // Cleanup
            var unloadHandle = sceneRef.UnLoadScene();
            await unloadHandle.ToUniTask();

            await UniTask.DelayFrame(1);

            Assert.That(sceneRef.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator LoadSceneAsync_WhenAlreadyLoaded_ShouldLogError() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Attempting to load AssetRef Scene that has already been loaded. Handle is exposed through getter OperationHandle");

            var sceneRef = new AssetRef(_scene_asset_guid_1);
            _loadedSceneRefs.Add(sceneRef);
            await sceneRef.LoadSceneAsync(LoadSceneMode.Additive, true).ToUniTask();

            // Act - Attempt to load again when already loaded
            _ = sceneRef.LoadSceneAsync(LoadSceneMode.Additive, true);

            // Cleanup
            await sceneRef.UnLoadScene().ToUniTask();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator UnLoadScene_ShouldUnloadScene() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var sceneRef = new AssetRef(_scene_asset_guid_1);
            _loadedSceneRefs.Add(sceneRef);
            var sceneInstance = await sceneRef.LoadSceneAsync(LoadSceneMode.Additive, true).ToUniTask();

            Assert.That(sceneInstance.Scene.isLoaded, Is.True);

            // Act
            await sceneRef.UnLoadScene().ToUniTask();

            await UniTask.DelayFrame(1);

            // Assert
            Assert.That(sceneRef.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });
#endif
    }
}
