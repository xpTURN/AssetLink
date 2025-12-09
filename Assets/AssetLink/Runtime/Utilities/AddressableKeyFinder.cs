using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace xpTURN.Link
{
    /// <summary>
    /// Utility class for AddressableKeyFinder.
    /// </summary>
    public static class AddressableKeyFinder
    {
#if UNITY_EDITOR
        public static bool IsAddressable(string path)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return false;

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);
            return entry != null && entry.address != null;
        }

        public static string FindAddressableKeyFromPath(string assetPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);

            if (entry == null)
            {
                Debug.LogWarning($"No Addressable Key found for {assetPath}");
                return string.Empty;
            }

            return entry.address;
        }

        public static string FindAddressableKeyFromGuid(string assetGuid)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(assetGuid);

            if (entry == null)
            {
                Debug.LogWarning($"No Addressable Key found for {assetGuid}");
                return string.Empty;
            }

            return entry.address;
        }

        // Function to find an object using the address in Addressables
        public static string FindAssetPathFromGuid(string assetGuid)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(assetGuid);

            if (entry == null)
            {
                Debug.LogWarning($"No Object found for {assetGuid}");
                return string.Empty;
            }

            return entry.AssetPath;
        }
#endif

        public static string FindAssetPathFromAddressableKey(string addressableKey)
        {
            var handle = Addressables.LoadResourceLocationsAsync(string.IsNullOrEmpty(addressableKey) ? "null" : addressableKey);
            handle.WaitForCompletion();
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result.Count == 0)
            {
                return string.Empty;
            }

            string internalId = handle.Result[0].InternalId;
            return internalId;
        }
    }
}