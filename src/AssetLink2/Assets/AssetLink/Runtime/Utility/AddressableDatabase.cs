using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace xpTURN.AssetLink.Utility
{
    /// <summary>
    /// Utility class for AddressableDatabase.
    /// </summary>
    public static class AddressableDatabase
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

        public static string AssetPathToGUID(string path)
        {
            return AssetDatabase.AssetPathToGUID(path);
        }

        public static string AssetPathToAddressName(string assetPath)
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

        public static string GUIDToAddressName(string assetGuid)
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
        public static string GUIDToAssetPath(string assetGuid)
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

        public static string AddressNameToAssetPath(string addressableKey)
        {
            var handle = Addressables.LoadResourceLocationsAsync(string.IsNullOrEmpty(addressableKey) ? "null" : addressableKey);
            handle.WaitForCompletion();

            try
            {
                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result.Count == 0)
                    return string.Empty;
                return handle.Result[0].InternalId;
            }
            finally
            {
                if (handle.IsValid())
                    handle.Release();
            }
        }
#endif
    }
}