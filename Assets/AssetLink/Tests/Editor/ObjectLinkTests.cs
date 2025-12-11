using System;
using System.Collections;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AddressableAssets;

using UnityEditor;

using xpTURN.Link;

namespace xpTURN.Link.Editor.Tests
{
    public class ObjectLinkTests
    {
        private string _player_armature = "thirdpersoncontroller/prefabs/playerarmature.prefab";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Debug.Log("OneTimeSetUp");

            // 
            Addressables.InitializeAsync();
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log("SetUp");

            // Reset AddressableManager
            AddressableManager.ResetPool();
            AddressableManager.AsyncHandler.ResetPool();
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("TearDown");
        }

        [UnityTest]
        public IEnumerator AssignAsset_ShouldSetAsset()
        {
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
    }
}
