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
    public class AssetLinkSpriteTests
    {
        private string _sprite_asset_name = "Atlas/piece_2_3.jpg";
        private string _sprite_sub_name_1 = "piece_2_3";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("AssetLinkSpriteTests OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("AssetLinkSpriteTests SetUp");

#if ASSETLINK_UNITASK_INTEGRATION
            // Configure UniTask to propagate exceptions as-is
            UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Exception;
            UniTaskScheduler.PropagateOperationCanceledException = true;
#endif
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("AssetLinkSpriteTests TearDown");
        }

#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator LoadAssetAsync_ShouldLoadSprite() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Load individual sprite image
            var spriteLink = new AssetLinkSprite(_sprite_asset_name);
            spriteLink.SetAssetName(spriteLink.AssetName, _sprite_sub_name_1);

            // Act
            var sprite = await spriteLink.LoadAssetAsync().ToUniTask();

            // Assert
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite, Is.TypeOf<Sprite>());
            Assert.That(sprite.name, Is.EqualTo(_sprite_sub_name_1));
            Assert.That(spriteLink.SubObjectName, Is.EqualTo(_sprite_sub_name_1));

            // RuntimeKey must include SubObjectName
            var expectedKey = $"{_sprite_asset_name}[{_sprite_sub_name_1}]";
            Assert.That(spriteLink.RuntimeKeyString, Is.EqualTo(expectedKey));

            // Cleanup
            spriteLink.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [Test]
        public void SubObjectName_ShouldAffectRuntimeKey()
        {
            // Arrange
            var spriteLink = new AssetLinkSprite(_sprite_asset_name);

            // Assert - When SubObjectName is absent
            Assert.That(spriteLink.RuntimeKeyString, Is.EqualTo(_sprite_asset_name));

            // Act - Set SubObjectName
            spriteLink.SetAssetName(spriteLink.AssetName, _sprite_sub_name_1);

            // Assert - RuntimeKey including SubObjectName
            var expectedKey = $"{_sprite_asset_name}[{_sprite_sub_name_1}]";
            Assert.That(spriteLink.RuntimeKeyString, Is.EqualTo(expectedKey));

            // Act - Remove SubObjectName
            spriteLink.SetAssetName(spriteLink.AssetName, null);

            // Assert - Restore to original RuntimeKey
            Assert.That(spriteLink.RuntimeKeyString, Is.EqualTo(_sprite_asset_name));
        }

        [UnityTest]
        public IEnumerator Reset_ShouldClearSubObjectName() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var spriteLink = new AssetLinkSprite(_sprite_asset_name);
            spriteLink.SetAssetName(spriteLink.AssetName, _sprite_sub_name_1);
            await spriteLink.LoadAssetAsync().ToUniTask();

            // Assert - State after load
            Assert.That(spriteLink.SubObjectName, Is.EqualTo(_sprite_sub_name_1));
            Assert.That(spriteLink.IsValid(), Is.True);

            // Act
            spriteLink.Reset();

            // Assert - State after Reset
            Assert.That(spriteLink.SubObjectName, Is.Null);
            Assert.That(spriteLink.AssetName, Is.EqualTo(string.Empty));
            Assert.That(spriteLink.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });
#endif
    }
}
