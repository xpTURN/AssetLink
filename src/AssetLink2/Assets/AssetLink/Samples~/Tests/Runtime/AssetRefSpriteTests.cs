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
    public class AssetRefSpriteTests
    {
        // piece_2_3.jpg GUID
        private string _sprite_asset_guid = "b112d1c539ce74c1b9272d82362f616d";
        private string _sprite_sub_name_1 = "piece_2_3";
        private string _sprite_sub_name_2 = "piece_2_3";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("AssetRefSpriteTests OneTimeSetUp");

            // Initialize Addressables
            Addressables.InitializeAsync(true);
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("AssetRefSpriteTests SetUp");

#if ASSETLINK_UNITASK_INTEGRATION
            // Configure UniTask to propagate exceptions as-is
            UniTaskScheduler.UnobservedExceptionWriteLogType = LogType.Exception;
            UniTaskScheduler.PropagateOperationCanceledException = true;
#endif
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("AssetRefSpriteTests TearDown");
        }

#if ASSETLINK_UNITASK_INTEGRATION
        [UnityTest]
        public IEnumerator LoadAssetAsync_ShouldLoadSprite() => UniTask.ToCoroutine(async () =>
        {
            // Arrange - Load individual sprite image
            var spriteRef = new AssetRefSprite(_sprite_asset_guid);
            spriteRef.SetAssetGUID(spriteRef.AssetGUID, _sprite_sub_name_1);

            // Act
            var sprite = await spriteRef.LoadAssetAsync().ToUniTask();

            // Assert
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite, Is.TypeOf<Sprite>());
            Assert.That(sprite.name, Is.EqualTo(_sprite_sub_name_1));
            Assert.That(spriteRef.SubObjectName, Is.EqualTo(_sprite_sub_name_1));

            // RuntimeKey must include SubObjectName
            var expectedKey = $"{_sprite_asset_guid}[{_sprite_sub_name_1}]";
            Assert.That(spriteRef.RuntimeKeyString, Is.EqualTo(expectedKey));

            // Cleanup
            spriteRef.ReleaseAsset();

            await UniTask.DelayFrame(1);
        });

        [Test]
        public void SubObjectName_ShouldAffectRuntimeKey()
        {
            // Arrange
            var spriteRef = new AssetRefSprite(_sprite_asset_guid);

            // Assert - When SubObjectName is absent
            Assert.That(spriteRef.RuntimeKeyString, Is.EqualTo(_sprite_asset_guid));

            // Act - Set SubObjectName
            spriteRef.SetAssetGUID(spriteRef.AssetGUID, _sprite_sub_name_1);

            // Assert - RuntimeKey including SubObjectName
            var expectedKey = $"{_sprite_asset_guid}[{_sprite_sub_name_1}]";
            Assert.That(spriteRef.RuntimeKeyString, Is.EqualTo(expectedKey));

            // Act - Remove SubObjectName
            spriteRef.SetAssetGUID(spriteRef.AssetGUID, null);

            // Assert - Restore to original RuntimeKey
            Assert.That(spriteRef.RuntimeKeyString, Is.EqualTo(_sprite_asset_guid));
        }

        [UnityTest]
        public IEnumerator Reset_ShouldClearSubObjectName() => UniTask.ToCoroutine(async () =>
        {
            // Arrange
            var spriteRef = new AssetRefSprite(_sprite_asset_guid);
            spriteRef.SetAssetGUID(spriteRef.AssetGUID, _sprite_sub_name_1);
            await spriteRef.LoadAssetAsync().ToUniTask();

            // Assert - State after load
            Assert.That(spriteRef.SubObjectName, Is.EqualTo(_sprite_sub_name_1));
            Assert.That(spriteRef.IsValid(), Is.True);

            // Act
            spriteRef.Reset();

            // Assert - State after Reset
            Assert.That(spriteRef.SubObjectName, Is.Null);
            Assert.That(spriteRef.AssetGUID, Is.EqualTo(string.Empty));
            Assert.That(spriteRef.IsValid(), Is.False);

            await UniTask.DelayFrame(1);
        });
#endif
    }
}
