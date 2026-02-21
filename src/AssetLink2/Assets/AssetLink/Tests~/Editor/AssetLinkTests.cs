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

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

using xpTURN.AssetLink;
using xpTURN.AssetLink.Utility;

namespace xpTURN.AssetLink.Tests
{
    public class AssetLinkTests
    {
        private string _prefab_asset_name = "Prefabs/Empty.prefab";

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

            // Reset the pool for testing
            AddressablesTracker.ResetPool();
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("TearDown");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AddressablesTracker.ReleaseUnreferencedHandles(true);
        }
        
        [UnityTest]
        public IEnumerator SetEditorAsset_ShouldSetAsset()
        {
            // Find the asset path from the addressable key
            string path = AddressableDatabase.AddressNameToAssetPath(_prefab_asset_name);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            var ObjectLink_001 = new AssetLink();
            ObjectLink_001.SetEditorAsset(asset);
            Assert.That(asset, Is.EqualTo(ObjectLink_001.editorAsset));
            Assert.That(ObjectLink_001.RuntimeKeyString, Is.EqualTo(_prefab_asset_name));

            yield return null;

            ObjectLink_001.Reset();
            Assert.That(ObjectLink_001.RuntimeKeyString, Is.Empty);

            yield return null;
        }
    }
}