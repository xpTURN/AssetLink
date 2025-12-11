using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

using xpTURN.Link;

namespace xpTURN.Editor
{
    public class SampleAddressablesPostprocessor : AssetPostprocessor
    {
        public const string ADDRESSABLES_FOLDER = "sample/addressables/";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            if (importedAssets.Length == 0)
            {
                return;
            }

            foreach (string assetPath in importedAssets)
            {
                OnAssetImported(assetPath);
            }
        }

        private static void OnAssetImported(string _assetPath)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            string assetPath = _assetPath.ToLower().Replace("\\", "/");
            if (!assetPath.Contains(ADDRESSABLES_FOLDER))
            {
                return;
            }
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(_assetPath);
            if (assetType == typeof(DefaultAsset))
            {
                return;
            }

            string groupName = assetPath.Substring(assetPath.IndexOf(ADDRESSABLES_FOLDER) + ADDRESSABLES_FOLDER.Length);
            groupName = groupName.Substring(0, groupName.LastIndexOf("/"));
            groupName = groupName.Replace("/", "-");
            Debug.Log($"groupName: {groupName}");

            string assetName = assetPath.Substring(assetPath.IndexOf(ADDRESSABLES_FOLDER) + ADDRESSABLES_FOLDER.Length);
            Debug.Log($"assetName: {assetName}");

            // Create settings if not exist
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                AddressableName.CreateOrGetSettings();
            }

            // Create group if not exist
            if (AddressableName.IsGroupExist(groupName) == false)
            {
                AddressableName.CreateGroup(groupName);
            }

            // Add addressable
            AddressableName.CreateOrMoveAssetEntry(groupName, assetName, assetPath);
        }
    }
}