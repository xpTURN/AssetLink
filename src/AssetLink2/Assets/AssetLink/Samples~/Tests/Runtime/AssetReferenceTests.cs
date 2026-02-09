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

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

namespace xpTURN.AssetLink.Tests
{
    public class AssetReferenceTests
    {
        private string _tex_asset_guid = "901221460c3a046f8b3dd3c0aa1631a4";

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
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("TearDown");
        }

#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator LoadAssetAsync_SingleRef_ShouldSetReferenceCount() => UniTask.ToCoroutine(async () =>
        {
            var Ref_001 = new AssetReference(_tex_asset_guid);

            await Ref_001.LoadAssetAsync<Texture>();

            await UniTask.DelayFrame(10, delayTiming: PlayerLoopTiming.FixedUpdate); // Wait a few frames for internal ref count to update

            Assert.That(Ref_001.OperationHandle.GetReferenceCount(), Is.EqualTo(1));

            Ref_001.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator LoadAssetAsync_TwoRefs_ShouldShareReferenceCount() => UniTask.ToCoroutine(async () =>
        {
            var Ref_001 = new AssetReference(_tex_asset_guid);
            var Ref_002 = new AssetReference(_tex_asset_guid);

            await Ref_001.LoadAssetAsync<Texture>();
            await Ref_002.LoadAssetAsync<Texture>();

            await UniTask.DelayFrame(10, delayTiming: PlayerLoopTiming.FixedUpdate); // Wait a few frames for internal ref count to update

            Assert.That(Ref_001.OperationHandle.GetReferenceCount(), Is.EqualTo(2));
            Assert.That(Ref_002.OperationHandle.GetReferenceCount(), Is.EqualTo(2));

            Ref_001.ReleaseAsset();

            await UniTask.DelayFrame(1);

            Assert.That(Ref_002.OperationHandle.GetReferenceCount(), Is.EqualTo(1));

            Ref_002.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator LoadAssetAsync_ThreeRefs_ShouldShareReferenceCount() => UniTask.ToCoroutine(async () =>
        {
            var Ref_001 = new AssetReference(_tex_asset_guid);
            var Ref_002 = new AssetReference(_tex_asset_guid);
            var Ref_003 = new AssetReference(_tex_asset_guid);

            await Ref_001.LoadAssetAsync<Texture>();
            await Ref_002.LoadAssetAsync<Texture>();
            await Ref_003.LoadAssetAsync<Texture>();

            await UniTask.DelayFrame(10, delayTiming: PlayerLoopTiming.FixedUpdate); // Wait a few frames for internal ref count to update

            Assert.That(Ref_001.OperationHandle.GetReferenceCount(), Is.EqualTo(3));
            Assert.That(Ref_002.OperationHandle.GetReferenceCount(), Is.EqualTo(3));
            Assert.That(Ref_003.OperationHandle.GetReferenceCount(), Is.EqualTo(3));

            Ref_001.ReleaseAsset();

            await UniTask.DelayFrame(1);
            Assert.That(Ref_003.OperationHandle.GetReferenceCount(), Is.EqualTo(2));

            Ref_002.ReleaseAsset();

            await UniTask.DelayFrame(1);
            Assert.That(Ref_003.OperationHandle.GetReferenceCount(), Is.EqualTo(1));

            Ref_003.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });
#endif
    }
}