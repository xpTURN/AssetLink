#pragma warning disable CS0414
using System.Collections;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AddressableAssets;

using xpTURN.AssetLink;

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

namespace xpTURN.AssetLink.Tests
{
    public class AssetLinkAtlasedSpriteTests
    {
        private string _sprite_atlas_asset_name = "Atlas/SampleAtlas.spriteatlasv2";
        private string _sprite_atlas_sub_name_1 = "piece_2_3";
        private string _sprite_atlas_sub_name_2 = "piece_3_3";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("AssetLinkAtlasedSpriteTests OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("AssetLinkAtlasedSpriteTests SetUp");

#if ASSETLINK_UNITASK_INTEGRATION
            // Configure UniTask to propagate exceptions as-is
            UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Exception;
            UniTaskScheduler.PropagateOperationCanceledException = true;
#endif
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("AssetLinkAtlasedSpriteTests TearDown");
        }

        [Test]
        public void SubObjectName_ShouldAffectRuntimeKey()
        {
            // Arrange
            var atlasLink = new AssetLinkAtlasedSprite(_sprite_atlas_asset_name);

            // Assert - When SubObjectName is absent
            Assert.That(atlasLink.RuntimeKeyString, Is.EqualTo(_sprite_atlas_asset_name));

            // Act - Set SubObjectName
            atlasLink.SetAssetName(atlasLink.AssetName, _sprite_atlas_sub_name_1);

            // Assert - RuntimeKey including SubObjectName
            var expectedKey = $"{_sprite_atlas_asset_name}[{_sprite_atlas_sub_name_1}]";
            Assert.That(atlasLink.RuntimeKeyString, Is.EqualTo(expectedKey));

            // Act - Remove SubObjectName
            atlasLink.SetAssetName(atlasLink.AssetName, null);

            // Assert - Restore to original RuntimeKey
            Assert.That(atlasLink.RuntimeKeyString, Is.EqualTo(_sprite_atlas_asset_name));
        }

#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator Reset_ShouldClearSubObjectName() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var atlasLink = new AssetLinkAtlasedSprite(_sprite_atlas_asset_name);
            atlasLink.SetAssetName(atlasLink.AssetName, _sprite_atlas_sub_name_1);
            await atlasLink.LoadAssetAsync().ToUniTask();

            // Assert - State after load
            Assert.That(atlasLink.SubObjectName, Is.EqualTo(_sprite_atlas_sub_name_1));
            Assert.That(atlasLink.IsValid(), Is.True);

            // Act
            atlasLink.Reset();

            // Assert - State after Reset
            Assert.That(atlasLink.SubObjectName, Is.Null);
            Assert.That(atlasLink.AssetName, Is.EqualTo(string.Empty));
            Assert.That(atlasLink.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator FromAtlas_ShouldLoadSprite() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Load individual sprite from sprite atlas
            var atlasLink = new AssetLinkAtlasedSprite(_sprite_atlas_asset_name);
            atlasLink.SetAssetName(atlasLink.AssetName, _sprite_atlas_sub_name_1);

            // Act
            var sprite = await atlasLink.LoadAssetAsync().ToUniTask();

            // Assert
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite, Is.TypeOf<Sprite>());
            Assert.That(sprite.name.Replace("(Clone)", ""), Is.EqualTo(_sprite_atlas_sub_name_1));
            Assert.That(atlasLink.SubObjectName, Is.EqualTo(_sprite_atlas_sub_name_1));

            // RuntimeKey must include SubObjectName
            var expectedKey = $"{_sprite_atlas_asset_name}[{_sprite_atlas_sub_name_1}]";
            Assert.That(atlasLink.RuntimeKeyString, Is.EqualTo(expectedKey));

            // Cleanup
            atlasLink.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [UnityTest]
        public IEnumerator MultipleSprites_ShouldTrackSeparately() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Load different sprite from same atlas
            var atlasedSpriteLink1 = new AssetLinkAtlasedSprite(_sprite_atlas_asset_name);
            atlasedSpriteLink1.SetAssetName(atlasedSpriteLink1.AssetName, _sprite_atlas_sub_name_1);

            var atlasedSpriteLink2 = new AssetLinkAtlasedSprite(_sprite_atlas_asset_name);
            atlasedSpriteLink2.SetAssetName(atlasedSpriteLink1.AssetName, _sprite_atlas_sub_name_2);

            // Act
            var sprite1 = await atlasedSpriteLink1.LoadAssetAsync().ToUniTask();
            var sprite2 = await atlasedSpriteLink2.LoadAssetAsync().ToUniTask();

            // Wait for Addressables internal async completion
            await UniTask.DelayFrame(10);

            // Assert
            Assert.That(sprite1, Is.Not.Null);
            Assert.That(sprite1.name.Replace("(Clone)", ""), Is.EqualTo(_sprite_atlas_sub_name_1));

            Assert.That(sprite2, Is.Not.Null);
            Assert.That(sprite2.name.Replace("(Clone)", ""), Is.EqualTo(_sprite_atlas_sub_name_2));

            Assert.That(sprite1, Is.Not.EqualTo(sprite2));

            Assert.That(atlasedSpriteLink1.GetReferenceCount(), Is.EqualTo(1));
            Assert.That(atlasedSpriteLink2.GetReferenceCount(), Is.EqualTo(1));

            // Cleanup
            atlasedSpriteLink1.ReleaseAsset();

            await UniTask.DelayFrame(1);

            Assert.That(atlasedSpriteLink2.GetReferenceCount(), Is.EqualTo(1));

            atlasedSpriteLink2.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });
#endif
    }
}
