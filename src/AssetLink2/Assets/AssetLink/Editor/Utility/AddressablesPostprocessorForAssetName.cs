using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace xpTURN.AssetLink.Editor
{
    public class AddressablesPostprocessorForAssetName : AssetPostprocessor
    {
        public const string DEFAULTS_GROUP_NAME = "Defaults";

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (BuildPipeline.isBuildingPlayer || importedAssets.Length == 0)
            {
                return;
            }

            foreach (string importedAssetPath in importedAssets)
            {
                var mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(importedAssetPath);
                if (mainAssetType == typeof(DefaultAsset)) // Folder/None asset is not addressable
                {
                    continue;
                }

                var (success, groupName, addressableName) = MatchFolderPattern(importedAssetPath);
                if (success == false) // Not match folder pattern
                {
                    continue;
                }

                ProcessAssetForAddressables(groupName, addressableName, importedAssetPath);
            }
        }

        private static (bool success, string groupName, string addressableName) MatchFolderPattern(string importedAssetPath)
        {
            string normalizedPath = importedAssetPath.Replace("\\", "/");

            var regexList = AssetLinkSettings.Instance?.AutoRegistFolderRegex;
            if (regexList == null || regexList.Count == 0)
                return (false, null, null);

            foreach (var pathRegex in regexList)
            {
                var pathMatch = pathRegex.Match(normalizedPath);
                if (pathMatch.Success)
                {
                    string addressableName = pathMatch.Groups[1].Value;
                    string groupName = addressableName.LastIndexOf("/") != -1 ?
                        addressableName.Substring(0, addressableName.LastIndexOf("/")) : DEFAULTS_GROUP_NAME;

                    groupName = groupName.Replace("/", "-");

                    return (true, groupName, addressableName);
                }
            }

            return (false, null, null);
        }

        private static void ProcessAssetForAddressables(string groupName, string addressableName, string importedAssetPath)
        {
            Debug.Log($"[AddressablesPostprocessorForAssetName] groupName: {groupName}, addressableName: {addressableName}");

            // Create settings if not exist
            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                AddressableUtils.CreateOrGetSettings();
            }

            // Create group if not exist
            if (AddressableUtils.IsGroupExist(groupName) == false)
            {
                AddressableUtils.CreateGroup(groupName,
                    includeGUIDInCatalog: AssetLinkSettings.Instance.IncludeGUIDInCatalog,
                    includeLabelsInCatalog: AssetLinkSettings.Instance.IncludeLabelsInCatalog);
            }

            // Add addressable
            AddressableUtils.CreateOrMoveAssetEntry(groupName, addressableName, importedAssetPath);
        }
    }
}