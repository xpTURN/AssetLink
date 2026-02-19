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
    /// Reference to an addressable asset.  This can be used in script to provide fields that can be easily set in the editor and loaded dynamically at runtime.
    /// To determine if the reference is set, use RuntimeKeyIsValid().
    /// </summary>
    [Serializable]
    public class AssetRef : IKeyEvaluator, IAssetOwner, ISerializationCallbackReceiver
    {
        /// <summary>
        /// The GUID of an asset.
        /// </summary>
        [FormerlySerializedAs("m_assetGUID")]
        [SerializeField]
        protected internal string m_AssetGUID = "";

        [SerializeField]
        string m_SubObjectName;

        [SerializeField]
        string m_SubObjectType = null;

        /// <summary>Cache for composite RuntimeKey when SubObjectName is set; invalidated when m_AssetGUID/m_SubObjectName change.</summary>
        string _cachedRuntimeKeyComposite;

        long m_OwnerId = 0;
        AsyncOperationHandle m_Operation;
#if UNITY_EDITOR
        // we store the SubObjectGUID in the Editor to track things like renames
        // and still point to the correct object in the UI
        [SerializeField]
        string m_SubObjectGUID;

        virtual protected internal Type DerivedClassType { get; }
        virtual protected internal Type MainClassType { get; } // Set only when SubObjectType is present
#endif
        /// <summary>
        /// The AsyncOperationHandle currently being used by the AssetRef.
        /// For example, if you call AssetRef.LoadAssetAsync, this property will return a handle to that operation.
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
        /// The actual key used to request the asset at runtime. RuntimeKeyIsValid() can be used to determine if this reference was set.
        /// </summary>
        public virtual object RuntimeKey
        {
            get
            {
                if (m_AssetGUID == null)
                    m_AssetGUID = string.Empty;
                if (string.IsNullOrEmpty(m_SubObjectName))
                    return m_AssetGUID;
                if (_cachedRuntimeKeyComposite == null)
                    _cachedRuntimeKeyComposite = string.Format("{0}[{1}]", m_AssetGUID, m_SubObjectName);
                return _cachedRuntimeKeyComposite;
            }
        }

        /// <summary>
        /// The unique instance ID of the AssetRef.
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
        /// Stores the guid of the asset.
        /// </summary>
        public virtual string AssetGUID => m_AssetGUID;

        /// <summary>
        /// Stores the name of the sub object.  Some assets, such as models and sprite atlases, contain multiple objects.  These objects can be loaded by specifying their name and type.
        /// </summary>
        public virtual string SubObjectName => m_SubObjectName;

#if UNITY_EDITOR
        /// <summary>
        /// Stores the guid of the sub object (if available).
        /// </summary>
        public virtual string SubObjectGUID => m_SubObjectGUID;
#endif

        internal virtual Type SubObjectType
        {
            get
            {
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(m_SubObjectGUID) && m_SubObjectType != null)
                    return Type.GetType(m_SubObjectType);
#endif
                if (!string.IsNullOrEmpty(m_SubObjectName) && m_SubObjectType != null)
                    return Type.GetType(m_SubObjectType);
                return null;
            }
        }

        /// <summary>
        /// Returns the state of the internal operation.  If the load has not been started or if it has been released, the operation will not be valid and this will return false.
        /// This can be used to determine if an AssetRef has started loading or not.
        /// </summary>
        /// <returns>True if the operation is valid.  A valid operation is created when loading begins and it is invalidated when it has been released.</returns>
        public bool IsValid()
        {
            return m_Operation.IsValid();
        }

        /// <summary>
        /// Get the loading status of the internal operation.
        /// </summary>
        /// <value>True if the operation is completed.  If the operation is not valid, it will return false as well.</value>
        public bool IsDone => m_Operation.IsDone;

        /// <summary>
        /// Construct a new AssetRef object.
        /// </summary>
        public AssetRef()
        {
            m_OwnerId = GenerateInstanceId.Next();
        }

        /// <summary>
        /// Construct a new AssetRef object.
        /// </summary>
        /// <param name="guid">The guid of the asset.</param>
        public AssetRef(string guid)
        {
            m_OwnerId = GenerateInstanceId.Next();
            m_AssetGUID = guid;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            OnAfterDeserializeImpl();
        }

        /// <summary>Invoked after deserialization. Override in derived classes and call base to run base logic (e.g. OwnerId init).</summary>
        protected virtual void OnAfterDeserializeImpl()
        {
            if (m_OwnerId == 0)
                m_OwnerId = GenerateInstanceId.Next();
        }

        /// <summary>
        /// The loaded asset.  This value is only set after the AsyncOperationHandle returned from LoadAssetAsync completes.
        /// It will not be set if only InstantiateAsync is called.  It will be set to null if release is called.
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
        string m_CachedGUID = "";

        /// <summary>
        /// Cached Editor Asset.
        /// </summary>
        protected internal Object CachedAsset
        {
            get
            {
                if (m_CachedGUID != m_AssetGUID)
                {
                    m_CachedAsset = null;
                    m_CachedGUID = "";
                }

                return m_CachedAsset;
            }
            set
            {
                m_CachedAsset = value;
                m_CachedGUID = m_AssetGUID;
            }
        }
#endif
        /// <summary>
        /// String representation of asset reference.
        /// </summary>
        /// <returns>The asset guid as a string.</returns>
        public override string ToString()
        {
#if UNITY_EDITOR
            return "[" + m_AssetGUID + "]" + CachedAsset;
#else
            return "[" + m_AssetGUID + "]";
#endif
        }

        static AsyncOperationHandle<T> CreateFailedOperation<T>()
        {
            //this needs to be set in order for ResourceManager.ExceptionHandler to get hooked up to AddressablesImpl.LogException.
            Addressables.InitializeAsync();
            return Addressables.ResourceManager.CreateCompletedOperation(default(T), new Exception("Attempting to load an asset reference that has no asset assigned to it.").Message);
        }

        public void Reset()
        {
            if (m_Operation.IsValid())
            {
                AddressablesTracker.Remove(RuntimeKeyString, OwnerId);

                m_Operation.Release();
                m_Operation = default(AsyncOperationHandle);
            }

            m_AssetGUID = string.Empty;
            m_SubObjectName = null;
            _cachedRuntimeKeyComposite = null;
#if UNITY_EDITOR
            m_SubObjectGUID = string.Empty;
#endif
            m_SubObjectType = null;

#if UNITY_EDITOR
            m_EditorAssetChanged = false;
            m_CachedAsset = null;
            m_CachedGUID = "";
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

        public void SetAssetGUID(string assetGUID, string subObjectName = null)
        {
            if (string.Compare(m_AssetGUID, assetGUID) == 0 && string.Compare(m_SubObjectName, subObjectName) == 0)
            {
                return;
            }

            if (m_Operation.IsValid())
            {
                AddressablesTracker.Remove(RuntimeKeyString, OwnerId);

                m_Operation.Release();
                m_Operation = default(AsyncOperationHandle);
            }

            m_AssetGUID = assetGUID;
            m_SubObjectName = subObjectName;
            _cachedRuntimeKeyComposite = null;
#if UNITY_EDITOR
            m_SubObjectGUID = string.Empty;
#endif
        }

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// </summary>
        /// <typeparam name="TObject">The object type.</typeparam>
        /// <returns>The load operation if there is not a valid cached operation, otherwise return default operation.</returns>
        /// <remarks>
        /// This cannot be used a second time until the first load is released. If you wish to call load multiple times
        /// on an AssetRef, use <see cref="Addressables.LoadAssetAsync{TObject}(object)"/> and pass your AssetRef in as the key.
        /// See the [Loading Addressable Assets](xref:addressables-api-load-asset-async) documentation for more details.
        /// </remarks>
        public virtual AsyncOperationHandle<TObject> LoadAssetAsync<TObject>()
        {
            AsyncOperationHandle<TObject> result = default(AsyncOperationHandle<TObject>);
            if (m_Operation.IsValid())
                Debug.LogError("Attempting to load AssetRef that has already been loaded. Handle is exposed through getter OperationHandle");
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
        /// <param name="activateOnLoad">If false, the scene will load but not activate (for background loading).  The SceneInstance returned has an Activate() method that can be called to do this at a later point.</param>
        /// <param name="priority">Async operation priority for scene loading.</param>
        /// <returns>The operation handle for the request if there is not a valid cached operation, otherwise return default operation</returns>
        /// <remarks>
        /// This cannot be used a second time until the first load is unloaded. If you wish to call load multiple times
        /// on an AssetRef, use Addressables.LoadSceneAsync() and pass your AssetRef in as the key.
        /// See the [Loading Addressable Assets](xref:addressables-api-load-asset-async) documentation for more details.
        /// </remarks>
        public virtual AsyncOperationHandle<SceneInstance> LoadSceneAsync(LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            AsyncOperationHandle<SceneInstance> result = default(AsyncOperationHandle<SceneInstance>);
            if (m_Operation.IsValid())
                Debug.LogError("Attempting to load AssetRef Scene that has already been loaded. Handle is exposed through getter OperationHandle");
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
        /// Unloads the reference as a scene.
        /// </summary>
        /// <returns>The operation handle for the scene load.</returns>
        public virtual AsyncOperationHandle<SceneInstance> UnLoadScene()
        {
            return Addressables.UnloadSceneAsync(m_Operation, true);
        }

        /// <summary>
        /// InstantiateAsync the referenced asset as type TObject.
        /// </summary>
        /// <param name="position">Position of the instantiated object.</param>
        /// <param name="rotation">Rotation of the instantiated object.</param>
        /// <param name="parent">The parent of the instantiated object.</param>
        /// <returns>The handle for the operation.</returns>
        /// <remarks>
        /// This cannot be used a second time until the first load is released. If you wish to call load multiple times
        /// on an AssetRef, use Addressables.InstantiateAsync() and pass your AssetRef in as the key.
        /// See the [Loading Addressable Assets](xref:addressables-api-load-asset-async) documentation for more details.
        /// </remarks>
        public virtual AsyncOperationHandle<GameObject> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            AsyncOperationHandle<GameObject> result = default(AsyncOperationHandle<GameObject>);
            result = Addressables.InstantiateAsync(RuntimeKey, position, rotation, parent, true);
            result.CompletedTypeless += OnInstantiate;
            return result;
        }

        /// <summary>
        /// InstantiateAsync the referenced asset as type TObject.
        /// </summary>
        /// <param name="parent">The parent of the instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Option to retain world space when instantiated with a parent.</param>
        /// <returns>The handle for the operation.</returns>
        /// <remarks>
        /// This cannot be used a second time until the first load is released. If you wish to call load multiple times
        /// on an AssetRef, use Addressables.InstantiateAsync() and pass your AssetRef in as the key.
        /// See the [Loading Addressable Assets](xref:addressables-api-load-asset-async) documentation for more details.
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
            string keyStr = RuntimeKeyString;
            if (string.IsNullOrEmpty(keyStr)) return false;
            int subObjectIndex = keyStr.IndexOf('[');
            string guidPart = subObjectIndex >= 0 ? keyStr.Substring(0, subObjectIndex) : keyStr;
            return Guid.TryParse(guidPart, out _);
        }

        /// <summary>
        /// Release the internal operation handle.
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
        /// Validates that the referenced asset allowable for this asset reference.
        /// </summary>
        /// <param name="obj">The Object to validate.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public virtual bool ValidateAsset(Object obj)
        {
            return true;
        }

        /// <summary>
        /// Validates that the referenced asset allowable for this asset reference.
        /// </summary>
        /// <param name="path">The path to the asset in question.</param>
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
        /// Used by the editor to represent the main asset referenced.
        /// </summary>
        public virtual Object editorAsset
        {
            get { return GetEditorAssetInternal(); }
        }

        /// <summary>
        /// Helper function that can be used to override the base class editorAsset accessor.
        /// </summary>
        /// <returns>Returns the main asset referenced used in the editor.</returns>
        internal virtual Object GetEditorAssetInternal()
        {
            if (CachedAsset != null || string.IsNullOrEmpty(m_AssetGUID))
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
            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            var asset = AssetDatabase.LoadAssetAtPath(assetPath, DerivedClassType ?? AssetDatabase.GetMainAssetTypeAtPath(assetPath));
            return asset;
        }

        /// <summary>
        /// Sets the main asset on the AssetRef.  Only valid in the editor, this sets both the editorAsset attribute,
        ///   and the internal asset GUID, which drives the RuntimeKey attribute. If the reference uses a sub object,
        ///   then it will load the editor asset during edit mode and load the sub object during runtime. For example, if
        ///   the AssetRef is set to a sprite within a sprite atlas, the editorAsset is the atlas (loaded during edit mode)
        ///   and the sub object is the sprite (loaded during runtime). If called by AssetRefT, will set the editorAsset
        ///   to the requested object if the object is of type T, and null otherwise.
        /// <param name="value">Object to reference</param>
        /// </summary>
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
                m_AssetGUID = string.Empty;
                m_SubObjectName = null;
                _cachedRuntimeKeyComposite = null;
                m_SubObjectGUID = string.Empty;
                m_EditorAssetChanged = true;
                return true;
            }

            if (CachedAsset != value)
            {
                m_SubObjectName = null;
                _cachedRuntimeKeyComposite = null;
                m_SubObjectGUID = string.Empty;
                var path = AssetDatabase.GetAssetOrScenePath(value);
                if (string.IsNullOrEmpty(path))
                {
                    Addressables.LogWarningFormat("Invalid object for AssetRef {0}.", value);
                    return false;
                }

                if (!ValidateAsset(path))
                {
                    Addressables.LogWarningFormat("Invalid asset for AssetRef path = '{0}'.", path);
                    return false;
                }
                else
                {
                    m_AssetGUID = AssetDatabase.AssetPathToGUID(path);
                    _cachedRuntimeKeyComposite = null;
                    Object mainAsset;
                    if (derivedType != null)
                        mainAsset = LocateEditorAssetForTypedAssetRef(value, path, derivedType);
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

        internal Object LocateEditorAssetForTypedAssetRef(Object value, string path, Type type)
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
                    _cachedRuntimeKeyComposite = null;
                    m_SubObjectGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
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
                                _cachedRuntimeKeyComposite = null;
                                m_SubObjectGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
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
        /// Sets the sub object for this asset reference.
        /// </summary>
        /// <param name="value">The sub object.</param>
        /// <returns>True if set correctly.</returns>
        public virtual bool SetEditorSubObject(Object value)
        {
            if (value == null)
            {
                m_SubObjectName = null;
                _cachedRuntimeKeyComposite = null;
                m_SubObjectGUID = string.Empty;
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
                foreach ((Object sprite, string guid) in subObjects)
                {
                    var namesMatch = AssetRefUtilities.FormatName(sprite.name) == spriteName;
                    if (namesMatch)
                    {
                        foundMatch = true;
                        m_SubObjectGUID = guid;
                    }
                }
                if (!foundMatch)
                {
                    Debug.LogWarningFormat("Unable to find sprite {0} in atlas {1}.", spriteName, editorAsset.name);
                    return false;
                }
                m_SubObjectName = spriteName;
                _cachedRuntimeKeyComposite = null;
                m_SubObjectType = typeof(Sprite).AssemblyQualifiedName;
                m_EditorAssetChanged = true;
                return true;
            }

            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GUIDToAssetPath(m_AssetGUID));
            foreach (var s in subAssets)
            {
                if (s.name == value.name && s.GetType() == value.GetType())
                {
                    m_SubObjectGUID = String.Empty;
                    m_SubObjectName = s.name;
                    _cachedRuntimeKeyComposite = null;
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
