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

using xpTURN.AssetLink;

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

namespace xpTURN.AssetLink.Tests
{
    public class AssetLinkSceneTests
    {
        private string _scene_name_1 = "Empty1";
        private string _scene_asset_name_1 = "Scenes/Empty1.unity";
        private string _scene_asset_guid_1 = "8c312b7852ed344d281f4e5d059f69b7";
        private string _scene_asset_name_2 = "Scenes/Empty2.unity";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("AssetLinkSceneTests OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("AssetLinkSceneTests SetUp");

#if ASSETLINK_UNITASK_INTEGRATION
            // Configure UniTask to propagate exceptions as-is
            UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Exception;
            UniTaskScheduler.PropagateOperationCanceledException = true;
#endif
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log("AssetLinkSceneTests TearDown");
            yield return null;
        }

#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator LoadSceneAsync_ShouldLoadScene() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var sceneLink1 = new AssetLink(_scene_asset_name_1);
            var sceneLink2 = new AssetLink(_scene_asset_name_2);

            // Act
            var handle1 = sceneLink1.LoadSceneAsync(LoadSceneMode.Single, true, 100);
            var sceneInstance1 = await handle1.ToUniTask();

            // Assert
            Assert.That(sceneInstance1.Scene.isLoaded, Is.True);
            Assert.That(sceneLink1.IsValid(), Is.True);
            string name1 = sceneInstance1.Scene.name;

            var activeScene = SceneManager.GetActiveScene();
            Assert.That(name1, Is.EqualTo(activeScene.name));

            // AssetLink.LoadSceneAsync -> previous sceneLink1 is unloaded implicitly.
            var handle2 = sceneLink2.LoadSceneAsync(LoadSceneMode.Single, true, 100);
            var sceneInstance2 = await handle2.ToUniTask();

            // Assert
            Assert.That(sceneInstance2.Scene.isLoaded, Is.True);
            Assert.That(sceneLink2.IsValid(), Is.True);
            string name2 = sceneInstance2.Scene.name;

            activeScene = SceneManager.GetActiveScene();
            Assert.That(name2, Is.EqualTo(activeScene.name));
            Assert.That(sceneLink1.IsValid(), Is.False);

            // Manual unload required when scene use has ended regardless of Addressables.
            // await sceneLink1.UnLoadScene().ToUniTask();

            Assert.That(AddressablesTracker.GetHandles(_scene_asset_name_1).Count, Is.EqualTo(0));

            // In Single mode loading next scene unloads the previous. Set active scene to temp then unload sceneLink2.
            var tempScene = SceneManager.CreateScene($"tempScene-{Guid.NewGuid()}");
            bool actived = SceneManager.SetActiveScene(tempScene);
            Assert.That(actived, Is.True);

            //
            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator LoadSceneAsync_WithAdditive_ShouldLoadScene() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var sceneLink = new AssetLink(_scene_asset_name_1);

            // Act
            var handle = sceneLink.LoadSceneAsync(LoadSceneMode.Additive, true, 100);
            var sceneInstance = await handle.ToUniTask();

            // Assert
            Assert.That(sceneInstance.Scene.isLoaded, Is.True);

            string scene_name_1 = Application.isEditor ? _scene_name_1 : _scene_asset_guid_1; // In Player build scene name is returned as GUID.
            Assert.That(sceneInstance.Scene.name, Is.EqualTo(scene_name_1));

            bool found = Enumerable.Range(0, SceneManager.loadedSceneCount)
                .Any(i => SceneManager.GetSceneAt(i).name == scene_name_1);
            Assert.That(found, Is.True);

            // Cleanup
            await sceneLink.UnLoadScene().ToUniTask();

            await UniTask.DelayFrame(1);

            Assert.That(sceneLink.IsValid(), Is.False);

            found = Enumerable.Range(0, SceneManager.loadedSceneCount)
                .Any(i => SceneManager.GetSceneAt(i).name == scene_name_1);
            Assert.That(found, Is.False);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator LoadSceneAsync_WhenAlreadyLoaded_ShouldLogError() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Attempting to load AssetLink Scene that has already been loaded. Handle is exposed through getter OperationHandle");

            var sceneLink = new AssetLink(_scene_asset_name_1);
            await sceneLink.LoadSceneAsync(LoadSceneMode.Additive, true).ToUniTask();

            // Act - Attempt to load again when already loaded
            _ = sceneLink.LoadSceneAsync(LoadSceneMode.Additive, true);

            // Cleanup
            await sceneLink.UnLoadScene().ToUniTask();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator UnLoadScene_ShouldUnloadScene() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var sceneLink = new AssetLink(_scene_asset_name_1);
            var sceneInstance = await sceneLink.LoadSceneAsync(LoadSceneMode.Additive, true).ToUniTask();

            Assert.That(sceneInstance.Scene.isLoaded, Is.True);

            // Act
            var unloadHandle = sceneLink.UnLoadScene();
            await unloadHandle.ToUniTask();

            await UniTask.DelayFrame(1);

            // Assert
            Assert.That(sceneLink.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });
#endif
    }
}
