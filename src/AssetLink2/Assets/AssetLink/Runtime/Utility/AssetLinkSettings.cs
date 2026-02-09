using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace xpTURN.AssetLink
{
    public sealed class AssetLinkSettings : ScriptableObject
    {
        public static AssetLinkSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GetPreloadedAssetsOrCreate();

                return _instance;
            }
            private set => _instance = value;
        }

        private static AssetLinkSettings _instance;

        [Header("Settings for Addressables Asset Load/Release tracking")]
        [SerializeField]
        [Tooltip("Handle pool size for Addressable Asset tracking.")]
        private int handlePoolSize = 10000;

        [SerializeField]
        [Tooltip("When enabled, outputs stack trace at load request time when emitting tracking logs.")]
        private bool enableStackTrace = true;

        [Header("Addressables Group Settings")]
        [SerializeField]
        [Tooltip("If enabled, guids are included in content catalogs. This is required if assets are to be loaded via AssetRef/AssetReference.")]
        private bool includeGUIDInCatalog = true;

        [SerializeField]
        [Tooltip("If enabled, labels are included in the content catalogs. This is required if labels are used at runtime to load assets.")]
        private bool includeLabelsInCatalog = true;

        // [/]AddressableAssets[/](.+)$
        // │     │             │  │   └─ end of string
        // │     │             │  └─ capture group: one or more characters
        // │     │             └─ path separator (Unix style)
        // │     └─ literal "AddressableAssets"
        // └─ / path separator (Unix style)
        [SerializeField]
        [Tooltip("Regex patterns for folders to auto-register as addressables.")]
        private List<string> autoRegistFolderPattern = new() { @"[/]Samples[/]AddressableAssets[/](.+)$" };

        public int HandlePoolSize { get => handlePoolSize; }
        public bool EnableStackTrace { get => enableStackTrace; }
        public IReadOnlyList<string> AutoRegistFolderPattern { get => autoRegistFolderPattern; }

        private List<Regex> _cachedAutoRegistRegex;

        /// <summary>
        /// Cached compiled Regex instances for auto-regist folder patterns. Rebuilt in OnEnable and when patterns change in the editor.
        /// </summary>
        public IReadOnlyList<Regex> AutoRegistFolderRegex
        {
            get
            {
                if (_cachedAutoRegistRegex == null)
                    BuildAutoRegistRegexCache();
                return _cachedAutoRegistRegex;
            }
        }

        private void BuildAutoRegistRegexCache()
        {
            _cachedAutoRegistRegex = new List<Regex>(autoRegistFolderPattern?.Count ?? 0);
            if (autoRegistFolderPattern == null)
                return;

            foreach (var pattern in autoRegistFolderPattern)
            {
                if (string.IsNullOrEmpty(pattern))
                    continue;
                try
                {
                    _cachedAutoRegistRegex.Add(new Regex(pattern));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AssetLinkSettings] Invalid auto-regist regex pattern \"{pattern}\": {e.Message}");
                }
            }
        }
        public bool IncludeGUIDInCatalog { get => includeGUIDInCatalog; }
        public bool IncludeLabelsInCatalog { get => includeLabelsInCatalog; }

#if UNITY_EDITOR
        /// <summary>
        /// Clears the editor cache. Instance will reload on next access after editing/saving the asset.
        /// </summary>
        public static void ClearEditorCache()
        {
            _instance = null;
        }


        /// <summary>
        /// Selects the saved AssetLinkSettings asset and activates the Inspector view.
        /// </summary>
        [UnityEditor.MenuItem("Window/AssetLink/AssetLink Settings")]
        public static void SelectAssetLinkSettings()
        {
            // Preloaded asset (the one in use) takes precedence
            var instance = UnityEditor.PlayerSettings.GetPreloadedAssets().FirstOrDefault(x => x is AssetLinkSettings) as AssetLinkSettings;
            if (instance == null)
            {
                CreateAsset();
                return;
            }

            UnityEditor.Selection.activeObject = instance;
            UnityEditor.EditorGUIUtility.PingObject(instance);

            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType != null)
            {
                var window = UnityEditor.EditorWindow.GetWindow(inspectorType);
                if (window != null)
                    window.Focus();
            }
        }

        [UnityEditor.MenuItem("Assets/Create/AssetLink/AssetLink Settings")]
        public static void CreateAsset()
        {
            var path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Save AssetLinkSettings",
                "AssetLinkSettings",
                "asset",
                string.Empty);

            if (string.IsNullOrEmpty(path))
                return;

            var newSettings = CreateInstance<AssetLinkSettings>();
            UnityEditor.AssetDatabase.CreateAsset(newSettings, path);

            var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets().ToList();
            preloadedAssets.RemoveAll(x => x is AssetLinkSettings);
            preloadedAssets.RemoveAll(x => x is null);
            preloadedAssets.Add(newSettings);
            UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());


            UnityEditor.Selection.activeObject = newSettings;
            UnityEditor.EditorGUIUtility.PingObject(newSettings);

            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            if (inspectorType != null)
            {
                var window = UnityEditor.EditorWindow.GetWindow(inspectorType);
                if (window != null)
                    window.Focus();
            }

            _instance = null; // Reload on next Instance access
        }

        /// <summary>
        /// Loads AssetLinkSettings from Preloaded Assets and invokes OnEnable. Editor-only; in builds Unity loads preloaded assets automatically.
        /// </summary>
        public static void LoadInstanceFromPreloadAssets()
        {
            var instance = UnityEditor.PlayerSettings.GetPreloadedAssets().FirstOrDefault(x => x is AssetLinkSettings) as AssetLinkSettings;
            if (instance == null)
            {
                instance = CreateInstance<AssetLinkSettings>(); // Fallback when no asset in Preloaded Assets
            }

            instance.OnDisable();
            instance.OnEnable();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitialize()
        {
            LoadInstanceFromPreloadAssets();
        }
#endif

        public static AssetLinkSettings GetPreloadedAssetsOrCreate()
        {
            AssetLinkSettings instance = null;

#if UNITY_EDITOR
            instance = UnityEditor.PlayerSettings.GetPreloadedAssets().FirstOrDefault(x => x is AssetLinkSettings) as AssetLinkSettings;
#endif

            if (instance == null)
            {
                Debug.Log($"AssetLinkSettings.GetPreloadedAssetsOrCreate: Create new instance");
                instance = CreateInstance<AssetLinkSettings>(); // Create new instance if not exist
            }

            return instance;
        }

        void OnEnable()
        {
            Debug.Log($"AssetLinkSettings.OnEnable: {this.name}");

            Instance = this;
            BuildAutoRegistRegexCache();

            //
            AddressablesTracker.SetStackTrace(Instance.enableStackTrace);
            AddressablesTracker.SetHandlePool(Instance.handlePoolSize);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            BuildAutoRegistRegexCache();
        }
#endif

        void OnDisable()
        {
            Instance = null;
        }
    }
}
