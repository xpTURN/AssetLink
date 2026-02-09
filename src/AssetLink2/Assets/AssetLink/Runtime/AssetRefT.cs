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
    /// Generic version of AssetRef class.  This should not be used directly as CustomPropertyDrawers do not support generic types.  Instead use the concrete derived classes such as AssetRefGameObject.
    /// </summary>
    /// <typeparam name="TObject">The type of object to use with this AssetRef</typeparam>
    [Serializable]
    public class AssetRefT<TObject> : AssetRef where TObject : Object
    {
        /// <summary>
        /// Construct a new AssetRef object.
        /// </summary>
        /// <param name="guid">The guid of the asset.</param>
        public AssetRefT(string guid)
            : base(guid)
        {
        }

#if UNITY_EDITOR
        protected override internal Type DerivedClassType => typeof(TObject);
#endif

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// This cannot be used a second time until the first load is released. If you wish to call load multiple times
        /// on an AssetRef, use <see cref="Addressables.LoadAssetAsync{TObject}(object)"/> and pass your AssetRef in as the key.
        ///
        /// See the [Loading Addressable Assets](xref:addressables-api-load-asset-async) documentation for more details.
        /// </summary>
        /// <returns>The load operation.</returns>
        public virtual AsyncOperationHandle<TObject> LoadAssetAsync()
        {
            return LoadAssetAsync<TObject>();
        }

        /// <inheritdoc/>
        public override bool ValidateAsset(Object obj)
        {
            var type = obj.GetType();
            return typeof(TObject).IsAssignableFrom(type);
        }

        /// <summary>
        /// Validates that the asset located at a path is allowable for this asset reference. An asset is allowable if
        /// it is of the correct type or if one of its sub-asset is.
        /// </summary>
        /// <param name="mainAssetPath">The path to the asset in question.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public override bool ValidateAsset(string mainAssetPath)
        {
#if UNITY_EDITOR
            Type objType = typeof(TObject);
            if (objType.IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(mainAssetPath)))
                return true;

            var types = AssetPathToTypes.GetTypesForAssetPath(mainAssetPath);
            return types.Contains(objType);
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Type-specific override of parent editorAsset.  Used by the editor to represent the main asset referenced.
        /// </summary>
        /// <returns>Editor Asset as type TObject, else null</returns>
        public new TObject editorAsset
        {
            get => base.editorAsset as TObject;
        }
#endif
    }

    /// <summary>
    /// GameObject only asset reference.
    /// </summary>
    [Serializable]
    public class AssetRefGameObject : AssetRefT<GameObject>
    {
        /// <summary>
        /// Constructs a new reference to a GameObject.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetRefGameObject(string guid) : base(guid)
        {
        }
    }

    /// <summary>
    /// Texture only asset reference.
    /// </summary>
    [Serializable]
    public class AssetRefTexture : AssetRefT<Texture>
    {
        /// <summary>
        /// Constructs a new reference to a Texture.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetRefTexture(string guid) : base(guid)
        {
        }
    }

    /// <summary>
    /// Texture2D only asset reference.
    /// </summary>
    [Serializable]
    public class AssetRefTexture2D : AssetRefT<Texture2D>
    {
        /// <summary>
        /// Constructs a new reference to a Texture2D.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetRefTexture2D(string guid) : base(guid)
        {
        }
    }

    /// <summary>
    /// Texture3D only asset reference
    /// </summary>
    [Serializable]
    public class AssetRefTexture3D : AssetRefT<Texture3D>
    {
        /// <summary>
        /// Constructs a new reference to a Texture3D.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetRefTexture3D(string guid) : base(guid)
        {
        }
    }

    /// <summary>
    /// <see cref="AssetRef"/> that only allows <see cref="Sprite"/> objects.
    /// </summary>
    [Serializable]
    public class AssetRefSprite : AssetRefT<Sprite>
    {
        /// <summary>
        /// Constructs a new reference to a AssetRefSprite.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetRefSprite(string guid) : base(guid)
        {
        }


        /// <summary>
        /// Checks whether the asset located at a path is valid for this asset reference. An asset is valid if
        /// it is of the correct type or if one of its sub-assets are.
        /// </summary>
        /// <param name="path">The file path to the asset in question.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        /// <remarks>
        /// The asset can be either a SpriteAtlas or a Sprite.
        /// </remarks>
        /// <example>
        /// <para>
        /// The example below uses ValidateAsset to check if an asset at a specific path can be set.</para>
        /// <code source="../Tests/Editor/DocExampleCode/ScriptReference/UsingAssetRefSpriteValidateAsset.cs" region="SAMPLE"/>
        /// </example>
        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SpriteAtlas))
                return true;

            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            bool isTexture = typeof(Texture2D).IsAssignableFrom(type);
            if (isTexture)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                return (importer != null) && (importer.spriteImportMode != SpriteImportMode.None);
            }
#endif
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Typeless override of parent editorAsset. Used by the editor to represent the main asset referenced.
        /// </summary>
        public new Object editorAsset
        {
            get => GetEditorAssetInternal();
        }

        internal override Object GetEditorAssetInternal()
        {
            if (CachedAsset != null || string.IsNullOrEmpty(m_AssetGUID))
                return CachedAsset;

            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            Type mainAssetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            Object asset = mainAssetType == typeof(SpriteAtlas) ? AssetDatabase.LoadAssetAtPath(assetPath, typeof(SpriteAtlas)) : AssetDatabase.LoadAssetAtPath(assetPath, DerivedClassType);

            if (DerivedClassType == null)
                return CachedAsset = asset;

            if (asset == null)
                Debug.LogWarning($"Assigned editorAsset does not match type {typeof(SpriteAtlas)} or {DerivedClassType}. EditorAsset will be null.");
            return CachedAsset = asset;
        }

        internal override bool SetEditorAssetInternal(Object value)
        {
            if (value is SpriteAtlas)
                return OnSetEditorAsset(value, typeof(SpriteAtlas));
            if (value is Texture2D)
                return OnSetEditorAsset(value, typeof(Texture2D));
            return base.SetEditorAssetInternal(value);
        }
#endif
    }

    /// <summary>
    /// <see cref="AssetRef"/> that only allows atlassed <see cref="Sprite"/> objects.  This will prevent legacy sprites from being used in this reference.
    /// If legacy sprite usage is needed, <see cref="AssetRefSprite"/> can be used instead.
    /// </summary>
    [Serializable]
    public class AssetRefAtlasedSprite : AssetRefT<Sprite>
    {
        /// <summary>
        /// Constructs a new reference to a AssetRefAtlasedSprite.
        /// </summary>
        /// <param name="guid">The object guid.</param>
        public AssetRefAtlasedSprite(string guid) : base(guid)
        {
        }

#if UNITY_EDITOR
        protected internal override Type MainClassType { get { return typeof(SpriteAtlas); } }

        internal override Object GetEditorAssetInternal()
        {
            if (CachedAsset != null || string.IsNullOrEmpty(m_AssetGUID))
                return CachedAsset;

            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(SpriteAtlas));

            if (asset == null)
                Debug.LogWarning($"Assigned editorAsset does not match type {typeof(SpriteAtlas)}. EditorAsset will be null.");
            return CachedAsset = asset;
        }

        internal override bool SetEditorAssetInternal(Object value)
        {
            return OnSetEditorAsset(value, typeof(SpriteAtlas));
        }
#endif

        /// <inheritdoc/>
        public override bool ValidateAsset(Object obj)
        {
            return obj is SpriteAtlas;
        }

        /// <inheritdoc/>
        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SpriteAtlas);
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// SpriteAtlas Type-specific override of parent editorAsset. Used by the editor to represent the main asset referenced.
        /// </summary>
        public new SpriteAtlas editorAsset
        {
            get
            {
                if (CachedAsset != null || string.IsNullOrEmpty(AssetGUID))
                    return CachedAsset as SpriteAtlas;

                var assetPath = AssetDatabase.GUIDToAssetPath(AssetGUID);
                var main = AssetDatabase.LoadMainAssetAtPath(assetPath) as SpriteAtlas;
                if (main != null)
                    CachedAsset = main;
                return main;
            }
        }
#endif
    }

    /// <summary>
    /// <see cref="AssetRef"/> that only allows <see cref="SceneAsset"/> objects.
    /// </summary>
    [Serializable]
    public class AssetRefScene : AssetRefT<Object>
    {
        public AssetRefScene(string name) : base(name)
        {
        }

        /// <inheritdoc/>
        public override bool ValidateAsset(Object obj)
        {
            return false;
        }

        /// <inheritdoc/>
        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SceneAsset);
#else
            return false;
#endif
        }
    }
}
