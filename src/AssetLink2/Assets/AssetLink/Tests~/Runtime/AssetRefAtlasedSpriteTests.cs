#pragma warning disable CS0414
using System.Collections;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AddressableAssets;

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

using xpTURN.AssetLink;

namespace xpTURN.AssetLink.Tests
{
    public class AssetRefAtlasedSpriteTests
    {
        // SampleAtlas.spriteatlasv2 GUID
        private string _sprite_atlas_asset_guid = "cf1b2283d28ff47658f9318abda41f71";
        private string _sprite_atlas_sub_name_1 = "piece_2_3";
        private string _sprite_atlas_sub_name_2 = "piece_3_3";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("AssetRefAtlasedSpriteTests OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("AssetRefAtlasedSpriteTests SetUp");

#if ASSETLINK_UNITASK_INTEGRATION
            // Configure UniTask to propagate exceptions as-is
            UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Exception;
            UniTaskScheduler.PropagateOperationCanceledException = true;
#endif
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("AssetRefAtlasedSpriteTests TearDown");
        }

        [Test]
        public void SubObjectName_ShouldAffectRuntimeKey()
        {
            // Arrange
            var atlasRef = new AssetRefAtlasedSprite(_sprite_atlas_asset_guid);

            // Assert - When SubObjectName is absent
            Assert.That(atlasRef.RuntimeKeyString, Is.EqualTo(_sprite_atlas_asset_guid));

            // Act - Set SubObjectName
            atlasRef.SetAssetGUID(atlasRef.AssetGUID, _sprite_atlas_sub_name_1);

            // Assert - RuntimeKey including SubObjectName
            var expectedKey = $"{_sprite_atlas_asset_guid}[{_sprite_atlas_sub_name_1}]";
            Assert.That(atlasRef.RuntimeKeyString, Is.EqualTo(expectedKey));

            // Act - Remove SubObjectName
            atlasRef.SetAssetGUID(atlasRef.AssetGUID, null);

            // Assert - Restore to original RuntimeKey
            Assert.That(atlasRef.RuntimeKeyString, Is.EqualTo(_sprite_atlas_asset_guid));
        }

#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator Reset_ShouldClearSubObjectName() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var atlasRef = new AssetRefAtlasedSprite(_sprite_atlas_asset_guid);
            atlasRef.SetAssetGUID(atlasRef.AssetGUID, _sprite_atlas_sub_name_1);
            await atlasRef.LoadAssetAsync().ToUniTask();

            // Assert - State after load
            Assert.That(atlasRef.SubObjectName, Is.EqualTo(_sprite_atlas_sub_name_1));
            Assert.That(atlasRef.IsValid(), Is.True);

            // Act
            atlasRef.Reset();

            // Assert - State after Reset
            Assert.That(atlasRef.SubObjectName, Is.Null);
            Assert.That(atlasRef.AssetGUID, Is.EqualTo(string.Empty));
            Assert.That(atlasRef.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator FromAtlas_ShouldLoadSprite() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Load individual sprite from sprite atlas
            var atlasRef = new AssetRefAtlasedSprite(_sprite_atlas_asset_guid);
            atlasRef.SetAssetGUID(atlasRef.AssetGUID, _sprite_atlas_sub_name_1);

            // Act
            var sprite = await atlasRef.LoadAssetAsync().ToUniTask();

            // Assert
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite, Is.TypeOf<Sprite>());
            Assert.That(sprite.name.Replace("(Clone)", ""), Is.EqualTo(_sprite_atlas_sub_name_1));
            Assert.That(atlasRef.SubObjectName, Is.EqualTo(_sprite_atlas_sub_name_1));

            // RuntimeKey must include SubObjectName
            var expectedKey = $"{_sprite_atlas_asset_guid}[{_sprite_atlas_sub_name_1}]";
            Assert.That(atlasRef.RuntimeKeyString, Is.EqualTo(expectedKey));

            // Cleanup
            atlasRef.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator MultipleSprites_ShouldTrackSeparately() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Load different sprite from same atlas
            var atlasRef1 = new AssetRefAtlasedSprite(_sprite_atlas_asset_guid);
            atlasRef1.SetAssetGUID(atlasRef1.AssetGUID, _sprite_atlas_sub_name_1);

            var atlasRef2 = new AssetRefAtlasedSprite(_sprite_atlas_asset_guid);
            atlasRef2.SetAssetGUID(atlasRef2.AssetGUID, _sprite_atlas_sub_name_2);

            // Act
            var sprite1 = await atlasRef1.LoadAssetAsync().ToUniTask();
            var sprite2 = await atlasRef2.LoadAssetAsync().ToUniTask();

            // Wait for Addressables internal async completion
            await UniTask.DelayFrame(10);

            // Assert
            Assert.That(sprite1, Is.Not.Null);
            Assert.That(sprite1.name.Replace("(Clone)", ""), Is.EqualTo(_sprite_atlas_sub_name_1));

            Assert.That(sprite2, Is.Not.Null);
            Assert.That(sprite2.name.Replace("(Clone)", ""), Is.EqualTo(_sprite_atlas_sub_name_2));

            Assert.That(sprite1, Is.Not.EqualTo(sprite2));

            Assert.That(atlasRef1.GetReferenceCount(), Is.EqualTo(1));
            Assert.That(atlasRef2.GetReferenceCount(), Is.EqualTo(1));

            // Cleanup
            atlasRef1.ReleaseAsset();

            await UniTask.DelayFrame(1);

            Assert.That(atlasRef2.GetReferenceCount(), Is.EqualTo(1));

            atlasRef2.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });
#endif
    }
}
