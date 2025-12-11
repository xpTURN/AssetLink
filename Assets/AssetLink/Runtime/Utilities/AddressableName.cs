#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace xpTURN.Link
{
    public static class AddressableName
    {

        public static void CreateSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                return;
            }

            AddressableAssetSettings.Create("AddressableAssetsData", "AddressableAssetSettings", true, true);
        }

        public static bool IsGroupExist(string groupName)
        {
            return AddressableAssetSettingsDefaultObject.Settings.FindGroup(groupName) != null;
        }

        public static bool IsAssetEntryExist(string groupName, string assetPath)
        {
            var setting = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup targetGroup = setting.FindGroup(groupName);
            if (targetGroup == null)
            {
                Debug.LogError($"[AddressableName] Group is null. GroupName: {groupName}");
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AddressableName] GUID is null. AssetPath: {assetPath}");
                return false;
            }

            var entry = targetGroup.GetAssetEntry(guid);
            return entry != null;
        }

        public static void CreateGroup(string groupName, BundledAssetGroupSchema.AssetNamingMode internalIdNamingMode = BundledAssetGroupSchema.AssetNamingMode.GUID)
        {
            var group = AddressableAssetSettingsDefaultObject.Settings.FindGroup(groupName);

            if (group != null)
            {
                Debug.Log($"[AddressableName] Already exists group {groupName}");
                return;
            }

            AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            var groupTemplate = aaSettings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
            var schemaTemplates = groupTemplate.SchemaObjects;
            var newGroup = aaSettings.CreateGroup(groupName, false, false, false, schemaTemplates, groupTemplate.GetTypes());
            if (newGroup == null)
            {
                Debug.LogError($"[AddressableName] Failed to create group {groupName}");
                return;
            }

            var schema = newGroup.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
            {
                Debug.LogError($"[AddressableName] Failed to get schema {groupName}");
                return;
            }

            //
            schema.AssetBundledCacheClearBehavior = BundledAssetGroupSchema.CacheClearBehavior.ClearWhenWhenNewVersionLoaded;
            schema.UseAssetBundleCrc = false;
            schema.IncludeAddressInCatalog = true;
            schema.IncludeGUIDInCatalog = false;
            schema.IncludeLabelsInCatalog = false;
            schema.InternalBundleIdMode = BundledAssetGroupSchema.BundleInternalIdMode.GroupGuid;
            schema.InternalIdNamingMode = internalIdNamingMode;
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;

            // Save
            aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, null, true, true);
            aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaModified, null, true, true);
        }

        public static bool CreateOrMoveAssetEntry(string groupName, string addressName, string assetPath, bool bIncludeAllSubAsset = true)
        {
            AddressableAssetSettings setting = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup targetGroup = setting.FindGroup(groupName);
            if (targetGroup == null)
            {
                Debug.LogError($"[AddressableName] Group is Null. GroupName: {groupName}, assetPath: {assetPath}");
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AddressableName] GUID is null. AssetPath: {assetPath}");
                return false;
            }

            var entry = setting.CreateOrMoveEntry(guid, targetGroup, false, false);
            entry.address = addressName;

            // Save
            setting.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true, true);

            return true;
        }

        public static bool RemoveAssetEntry(string groupName, string assetPath)
        {
            var setting = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup targetGroup = setting.FindGroup(groupName);
            if (targetGroup == null)
            {
                Debug.LogError($"[AddressableName] Group is null. GroupName: {groupName}");
                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AddressableName] GUID is null. AssetPath: {assetPath}");
                return false;
            }

            var entry = targetGroup.GetAssetEntry(guid);
            if (entry == null)
            {
                Debug.LogError($"[AddressableName] Entry is null. GroupName: {groupName}, AssetPath: {assetPath}");
                return false;
            }

            //
            targetGroup.RemoveAssetEntry(entry, false);

            // Save
            setting.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, true, true);

            return true;
        }

        public static bool RemoveAssetEntry(string assetPath)
        {
            // 
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AddressableName] GUID is null. AssetPath: {assetPath}");
                return false;
            }

            //
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                Debug.LogError($"[AddressableName] Entry is null. AssetPath: {assetPath}");
                return false;
            }

            //
            settings.RemoveAssetEntry(guid);

            // Save
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, true, true);

            return true;
        }
    }
}
#endif
