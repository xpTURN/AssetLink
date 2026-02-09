using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

using xpTURN.AssetLink.Utility;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace xpTURN.AssetLink
{
    /// <summary>
    /// Reference to an addressable asset. Can be used in script to provide fields that can be easily set in the editor and loaded dynamically at runtime.
    /// Use RuntimeKeyIsValid() to check if the reference is set.
    /// </summary>
    [Serializable]
    public class AssetLink : IKeyEvaluator, IAssetOwner, ISerializationCallbackReceiver
    {
        /// <summary>
        /// The name of an asset.
        /// </summary>
        [FormerlySerializedAs("m_assetName")]
        [SerializeField]
        protected internal string m_AssetName = "";

        [SerializeField]
        string m_SubObjectName;

        [SerializeField]
        string m_SubObjectType = null;

        long m_OwnerId = 0;
        AsyncOperationHandle m_Operation;
#if UNITY_EDITOR
        virtual protected internal Type DerivedClassType { get; }
        virtual protected internal Type MainClassType { get; } // Set only when SubObjectType is present
#endif
        /// <summary>
        /// The AsyncOperationHandle currently in use by this AssetLink.
        /// For example, when you call AssetLink.LoadAssetAsync, this property returns the handle for that operation.
        /// </summary>
        public AsyncOperationHandle OperationHandle
        {
            get { return m_Operation; }
            internal set
            {
                m_Operation = value;
            }
        }

        /// <summary>
        /// The actual key used to request the asset at runtime. Use RuntimeKeyIsValid() to check if this reference is set.
        /// </summary>
        public virtual object RuntimeKey
        {
            get
            {
                if (m_AssetName == null)
                    m_AssetName = string.Empty;
                if (!string.IsNullOrEmpty(m_SubObjectName))
                    return string.Format("{0}[{1}]", m_AssetName, m_SubObjectName);
                return m_AssetName;
            }
        }

        /// <summary>
        /// The unique instance ID of the AssetLink.
        /// </summary>
        public virtual long OwnerId => m_OwnerId;

        /// <summary>
        /// Whether the AssetLink is a spawner. Spawner is a special AssetLink that can spawn assets.
        /// </summary>
        public virtual bool IsSpawner => false;

        /// <summary>
        /// The string representation of the RuntimeKey.
        /// </summary>
        public virtual string RuntimeKeyString => RuntimeKey as string;

        /// <summary>
        /// Stores the name of the asset.
        /// </summary>
        public virtual string AssetName => m_AssetName;

        /// <summary>
        /// Stores the name of the sub-object. Some assets such as models or sprite atlases contain multiple objects, which can be loaded by specifying name and type.
        /// </summary>
        public virtual string SubObjectName => m_SubObjectName;

        internal virtual Type SubObjectType
        {
            get
            {
                if (!string.IsNullOrEmpty(m_SubObjectName) && m_SubObjectType != null)
                    return Type.GetType(m_SubObjectType);
                return null;
            }
        }

        /// <summary>
        /// Returns the state of the internal operation. Returns false if load has not started or the handle has been released.
        /// Use this to check whether this AssetLink has started loading.
        /// </summary>
        /// <returns>True if the operation is valid. A valid operation is created when loading starts and invalidated when released.</returns>
        public bool IsValid()
        {
            return m_Operation.IsValid();
        }

        /// <summary>
        /// Gets the loading state of the internal operation.
        /// </summary>
        /// <value>True when the operation is done. Returns false if the operation is invalid.</value>
        public bool IsDone => m_Operation.IsDone;

        /// <summary>
        /// Creates a new AssetLink instance.
        /// </summary>
        public AssetLink()
        {
            m_OwnerId = GenerateInstanceId.Next();
        }

        /// <summary>
        /// Creates a new AssetLink instance.
        /// </summary>
        /// <param name="name">The name of the asset.</param>
        public AssetLink(string name)
        {
            m_OwnerId = GenerateInstanceId.Next();
            m_AssetName = name;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_OwnerId == 0)
                m_OwnerId = GenerateInstanceId.Next();
        }

        /// <summary>
        /// The loaded asset. This is set only after the AsyncOperationHandle returned from LoadAssetAsync has completed.
        /// Not set if only InstantiateAsync was called. Set to null when Release is called.
        /// </summary>
        public virtual Object Asset
        {
            get
            {
                if (!m_Operation.IsValid())
                    return null;

                return m_Operation.Result as Object;
            }
        }

#if UNITY_EDITOR
        Object m_CachedAsset;
        string m_CachedName = "";

        /// <summary>
        /// Cached editor asset.
        /// </summary>
        protected internal Object CachedAsset
        {
            get
            {
                if (m_CachedName != m_AssetName)
                {
                    m_CachedAsset = null;
                    m_CachedName = "";
                }

                return m_CachedAsset;
            }
            set
            {
                m_CachedAsset = value;
                m_CachedName = m_AssetName;
            }
        }
#endif
        /// <summary>
        /// String representation of the asset reference.
        /// </summary>
        /// <returns>The asset name as a string.</returns>
        public override string ToString()
        {
#if UNITY_EDITOR
            return "[" + m_AssetName + "]" + CachedAsset;
#else
            return "[" + m_AssetName + "]";
#endif
        }

        static AsyncOperationHandle<T> CreateFailedOperation<T>()
        {
            // Must be set so ResourceManager.ExceptionHandler is wired to AddressablesImpl.LogException.
            Addressables.InitializeAsync();
            return Addressables.ResourceManager.CreateCompletedOperation(default(T), new Exception("Attempted to load an asset reference with no assigned asset.").Message);
        }

        public void Reset()
        {
            if (m_Operation.IsValid())
            {
                AddressablesTracker.Remove(RuntimeKeyString, OwnerId);

                m_Operation.Release();
                m_Operation = default(AsyncOperationHandle);
            }

            m_AssetName = string.Empty;
            m_SubObjectName = null;
            m_SubObjectType = null;

#if UNITY_EDITOR
            m_EditorAssetChanged = false;
            m_CachedAsset = null;
            m_CachedName = "";
#endif
        }

#if UNITY_INCLUDE_TESTS
        public int GetReferenceCount()
        {
            if (OperationHandle.IsValid())
            {
                return OperationHandle.GetReferenceCount();
            }

            return 0;
        }
#endif

        private void OnInstantiate(AsyncOperationHandle operationHandle)
        {
            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
            {
#if UNITY_INCLUDE_TESTS
                Debug.Log($"Call OnInstantiate for {RuntimeKeyString}");
#endif
                DoAutoRelease.Setup(operationHandle, RuntimeKeyString);
            }

            operationHandle.Completed -= OnInstantiate;
        }

        public void SetAssetName(string assetName, string subObjectName = null)
        {
            if (string.Compare(m_AssetName, assetName) == 0 && string.Compare(m_SubObjectName, subObjectName) == 0)
            {
                return;
            }

            if (m_Operation.IsValid())
            {
                AddressablesTracker.Remove(RuntimeKeyString, OwnerId);

                m_Operation.Release();
                m_Operation = default(AsyncOperationHandle);
            }

            m_AssetName = assetName;
            m_SubObjectName = subObjectName;
        }

        /// <summary>
        /// Loads the referenced asset as type TObject.
        /// </summary>
        /// <typeparam name="TObject">The object type.</typeparam>
        /// <returns>Returns the load operation if no valid cached operation exists; otherwise returns the default operation.</returns>
        /// <remarks>
        /// A second load cannot be used until the first one is released.
        /// </remarks>
        public virtual AsyncOperationHandle<TObject> LoadAssetAsync<TObject>()
        {
            AsyncOperationHandle<TObject> result = default(AsyncOperationHandle<TObject>);
            if (m_Operation.IsValid())
                Debug.LogError("Attempting to load AssetLink that has already been loaded. Handle is exposed through getter OperationHandle");
            else
            {
                result = Addressables.LoadAssetAsync<TObject>(RuntimeKey);
                OperationHandle = result;
                AddressablesTracker.Add(this, AddressablesTracker.HANDLE_TYPE.ASSET_OWNER, result);
            }

            return result;
        }

        /// <summary>
        /// Loads the reference as a scene.
        /// </summary>
        /// <param name="loadMode">Scene load mode.</param>
        /// <param name="activateOnLoad">If false, the scene is loaded but not activated (for background loading). The returned SceneInstance has an Activate() method to activate later.</param>
        /// <param name="priority">Async operation priority for scene loading.</param>
        /// <returns>Returns the operation handle for the request if no valid cached operation exists; otherwise returns the default operation.</returns>
        /// <remarks>
        /// A second load cannot be used until the first one is unloaded.
        /// </remarks>
        public virtual AsyncOperationHandle<SceneInstance> LoadSceneAsync(LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            AsyncOperationHandle<SceneInstance> result = default(AsyncOperationHandle<SceneInstance>);
            if (m_Operation.IsValid())
                Debug.LogError("Attempting to load AssetLink Scene that has already been loaded. Handle is exposed through getter OperationHandle");
            else
            {
                result = Addressables.LoadSceneAsync(RuntimeKey, loadMode, activateOnLoad, priority);
                OperationHandle = result;
                var trackedHandle = AddressablesTracker.Add(this, AddressablesTracker.HANDLE_TYPE.ASSET_OWNER_SCENE, result);
                OperationHandle.Destroyed += trackedHandle.OnDestroyedScene;
            }

            return result;
        }

        /// <summary>
        /// Unloads the reference from the scene.
        /// </summary>
        /// <returns>Operation handle for the scene load.</returns>
        public virtual AsyncOperationHandle<SceneInstance> UnLoadScene()
        {
            return Addressables.UnloadSceneAsync(m_Operation, true);
        }

        /// <summary>
        /// Asynchronously instantiates the referenced asset as type TObject.
        /// </summary>
        /// <param name="position">Position of the instantiated object.</param>
        /// <param name="rotation">Rotation of the instantiated object.</param>
        /// <param name="parent">Parent of the instantiated object.</param>
        /// <returns>Handle for the operation.</returns>
        /// <remarks>
        /// A second load cannot be used until the first one is released. To call load multiple times from AssetLink,
        /// use Addressables.InstantiateAsync() and pass the AssetLink as the key.
        /// See the Addressables asset loading documentation for details.
        /// </remarks>
        public virtual AsyncOperationHandle<GameObject> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            AsyncOperationHandle<GameObject> result = default(AsyncOperationHandle<GameObject>);
            result = Addressables.InstantiateAsync(RuntimeKey, position, rotation, parent, true);
            result.CompletedTypeless += OnInstantiate;
            return result;
        }

        /// <summary>
        /// Asynchronously instantiates the referenced asset as type TObject.
        /// </summary>
        /// <param name="parent">Parent of the instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Option to maintain world space when instantiating with a parent.</param>
        /// <returns>Handle for the operation.</returns>
        /// <remarks>
        /// A second load cannot be used until the first one is released. To call load multiple times from AssetLink,
        /// use Addressables.InstantiateAsync() and pass the AssetLink as the key.
        /// See the Addressables asset loading documentation for details.
        /// </remarks>
        public virtual AsyncOperationHandle<GameObject> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            AsyncOperationHandle<GameObject> result = default(AsyncOperationHandle<GameObject>);
            result = Addressables.InstantiateAsync(RuntimeKey, parent, instantiateInWorldSpace, true);
            result.CompletedTypeless += OnInstantiate;
            return result;
        }

        /// <inheritdoc/>
        public virtual bool RuntimeKeyIsValid()
        {
            return !string.IsNullOrEmpty(AssetName);
        }

        /// <summary>
        /// Releases the internal operation handle.
        /// </summary>
        public virtual void ReleaseAsset()
        {
            if (!m_Operation.IsValid())
            {
                Debug.LogWarning("Cannot release a null or unloaded asset.");
                return;
            }

            AddressablesTracker.Remove(RuntimeKeyString, OwnerId);
            m_Operation.Release();
            m_Operation = default(AsyncOperationHandle);
        }

        /// <summary>
        /// Validates whether the given object is allowed for this asset reference.
        /// </summary>
        /// <param name="obj">The object to validate.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public virtual bool ValidateAsset(Object obj)
        {
            return true;
        }

        /// <summary>
        /// Validates whether the given path is allowed for this asset reference.
        /// </summary>
        /// <param name="path">The path of the asset.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public virtual bool ValidateAsset(string path)
        {
            return true;
        }

#if UNITY_EDITOR

        [SerializeField]
#pragma warning disable CS0414
        bool m_EditorAssetChanged;

#pragma warning restore CS0414

        /// <summary>
        /// Represents the main asset referenced in the editor.
        /// </summary>
        public virtual Object editorAsset
        {
            get { return GetEditorAssetInternal(); }
        }

        /// <summary>
        /// Helper that can be overridden to customize the base class editorAsset accessor.
        /// </summary>
        /// <returns>The referenced main asset used in the editor.</returns>
        internal virtual Object GetEditorAssetInternal()
        {
            if (CachedAsset != null || string.IsNullOrEmpty(m_AssetName))
                return CachedAsset;

            var asset = FetchEditorAsset();

            if (DerivedClassType == null)
                return CachedAsset = asset;

            if (asset == null)
                Debug.LogWarning("Assigned editorAsset does not match type " + DerivedClassType + ". EditorAsset will be null.");
            return CachedAsset = asset;
        }

        internal Object FetchEditorAsset()
        {
            var assetPath = AddressableDatabase.AddressNameToAssetPath(m_AssetName);
            var asset = AssetDatabase.LoadAssetAtPath(assetPath, DerivedClassType ?? AssetDatabase.GetMainAssetTypeAtPath(assetPath));
            return asset;
        }

        /// <summary>
        /// Sets the main asset on this AssetLink. Valid in editor only; sets both the editorAsset property and
        /// the internal asset name that drives RuntimeKey. When the reference uses a sub-object,
        /// the editor loads the editor asset in edit mode and the sub-object at runtime. For example,
        /// when AssetLink is set to a sprite inside a sprite atlas, editorAsset is the atlas (loaded in edit mode)
        /// and the sub-object is the sprite (loaded at runtime). When called from AssetLinkT, if the object is of type T,
        /// editorAsset is set to the given object; otherwise it is set to null.
        /// </summary>
        /// <param name="value">The object to reference.</param>
        public virtual bool SetEditorAsset(Object value)
        {
            return SetEditorAssetInternal(value);
        }

        internal virtual bool SetEditorAssetInternal(Object value)
        {
            return OnSetEditorAsset(value, DerivedClassType);
        }

        internal bool OnSetEditorAsset(Object value, Type derivedType)
        {
            if (value == null)
            {
                CachedAsset = null;
                m_AssetName = string.Empty;
                m_SubObjectName = null;
                m_EditorAssetChanged = true;
                return true;
            }

            if (CachedAsset != value)
            {
                m_SubObjectName = null;
                var path = AssetDatabase.GetAssetOrScenePath(value);
                if (string.IsNullOrEmpty(path))
                {
                    Addressables.LogWarningFormat("Invalid object for AssetLink {0}.", value);
                    return false;
                }

                if (!ValidateAsset(path))
                {
                    Addressables.LogWarningFormat("Invalid asset for AssetLink path = '{0}'.", path);
                    return false;
                }
                else
                {
                    m_AssetName = AddressableDatabase.AssetPathToAddressName(path);
                    Object mainAsset;
                    if (derivedType != null)
                        mainAsset = LocateEditorAssetForTypedAssetLink(value, path, derivedType);
                    else
                    {
                        mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                        if (value != mainAsset)
                            SetEditorSubObject(value);
                    }

                    CachedAsset = mainAsset;
                }
            }

            m_EditorAssetChanged = true;
            return true;
        }

        internal Object LocateEditorAssetForTypedAssetLink(Object value, string path, Type type)
        {
            Object mainAsset;
            if (value.GetType() != type)
            {
                mainAsset = null;
            }
            else
            {
                // Check if the value is a sub-asset (e.g., sub-sprite in a sprite sheet)
                bool isSubAsset = AssetDatabase.IsSubAsset(value);

                if (isSubAsset)
                {
                    // For sub-assets, set the sub-object name and return the value as mainAsset
                    m_SubObjectName = value.name;
                    m_SubObjectType = value.GetType().AssemblyQualifiedName;
                    mainAsset = value;
                }
                else
                {
                    mainAsset = AssetDatabase.LoadAssetAtPath(path, type);
                    if (mainAsset != value)
                    {
                        // Try to find the value among sub-assets
                        mainAsset = null;
                        var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                        foreach (var asset in subAssets)
                        {
                            if (asset.GetType() == type && value == asset)
                            {
                                mainAsset = asset;
                                m_SubObjectName = value.name;
                                m_SubObjectType = value.GetType().AssemblyQualifiedName;
                                break;
                            }
                        }
                    }
                }
            }

            if (mainAsset == null)
                Debug.LogWarning("Assigned editorAsset does not match type " + type + ". EditorAsset will be null.");

            return mainAsset;
        }


        /// <summary>
        /// Sets the sub-object for this asset reference.
        /// </summary>
        /// <param name="value">The sub-object.</param>
        /// <returns>True if set successfully.</returns>
        public virtual bool SetEditorSubObject(Object value)
        {
            if (value == null)
            {
                m_SubObjectName = null;
                m_SubObjectType = null;
                m_EditorAssetChanged = true;
                return true;
            }

            if (editorAsset == null)
                return false;
            if (editorAsset.GetType() == typeof(SpriteAtlas))
            {
                var spriteName = AssetRefUtilities.FormatName(value.name);
                var atlas = editorAsset as SpriteAtlas;

                var foundMatch = false;
                var subObjects = AssetRefUtilities.GetAtlasSpritesAndPackables(ref atlas);
                foreach ((Object sprite, string _) in subObjects)
                {
                    var namesMatch = AssetRefUtilities.FormatName(sprite.name) == spriteName;
                    if (namesMatch)
                    {
                        foundMatch = true;
                    }
                }
                if (!foundMatch)
                {
                    Debug.LogWarningFormat("Unable to find sprite {0} in atlas {1}.", spriteName, editorAsset.name);
                    return false;
                }
                m_SubObjectName = spriteName;
                m_SubObjectType = typeof(Sprite).AssemblyQualifiedName;
                m_EditorAssetChanged = true;
                return true;
            }

            var assetPath = AddressableDatabase.AddressNameToAssetPath(m_AssetName);
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (var s in subAssets)
            {
                if (s.name == value.name && s.GetType() == value.GetType())
                {
                    m_SubObjectName = s.name;
                    m_SubObjectType = s.GetType().AssemblyQualifiedName;
                    m_EditorAssetChanged = true;
                    return true;
                }
            }

            return false;
        }
#endif
    }
}