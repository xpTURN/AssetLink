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
    /// Generic version of AssetLink class.  This should not be used directly as CustomPropertyDrawers do not support generic types.  Instead use the concrete derived classes such as AssetLinkGameObject.
    /// </summary>
    /// <typeparam name="TObject">The type of object to use with this AssetLink</typeparam>
    [Serializable]
    public class AssetLinkT<TObject> : AssetLink where TObject : Object
    {
        /// <summary>
        /// Construct a new AssetLink object.
        /// </summary>
        /// <param name="name">The name of the asset.</param>
        public AssetLinkT(string name)
            : base(name)
        {
        }

#if UNITY_EDITOR
        protected override internal Type DerivedClassType => typeof(TObject);
#endif

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// This cannot be used a second time until the first load is released. If you wish to call load multiple times
        /// on an AssetLink, use <see cref="Addressables.LoadAssetAsync{TObject}(object)"/> and pass your AssetLink in as the key.
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
    public class AssetLinkGameObject : AssetLinkT<GameObject>
    {
        /// <summary>
        /// Constructs a new reference to a GameObject.
        /// </summary>
        /// <param name="name">The object name.</param>
        public AssetLinkGameObject(string name) : base(name)
        {
        }
    }

    /// <summary>
    /// Texture only asset reference.
    /// </summary>
    [Serializable]
    public class AssetLinkTexture : AssetLinkT<Texture>
    {
        /// <summary>
        /// Constructs a new reference to a Texture.
        /// </summary>
        /// <param name="name">The object name.</param>
        public AssetLinkTexture(string name) : base(name)
        {
        }
    }

    /// <summary>
    /// Texture2D only asset reference.
    /// </summary>
    [Serializable]
    public class AssetLinkTexture2D : AssetLinkT<Texture2D>
    {
        /// <summary>
        /// Constructs a new reference to a Texture2D.
        /// </summary>
        /// <param name="name">The object name.</param>
        public AssetLinkTexture2D(string name) : base(name)
        {
        }
    }

    /// <summary>
    /// Texture3D only asset reference
    /// </summary>
    [Serializable]
    public class AssetLinkTexture3D : AssetLinkT<Texture3D>
    {
        /// <summary>
        /// Constructs a new reference to a Texture3D.
        /// </summary>
        /// <param name="name">The object name.</param>
        public AssetLinkTexture3D(string name) : base(name)
        {
        }
    }

    /// <summary>
    /// <see cref="AssetLink"/> that only allows <see cref="Sprite"/> objects.
    /// </summary>
    [Serializable]
    public class AssetLinkSprite : AssetLinkT<Sprite>
    {
        /// <summary>
        /// Constructs a new reference to a AssetLinkSprite.
        /// </summary>
        /// <param name="name">The object name.</param>
        public AssetLinkSprite(string name) : base(name)
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
            if (CachedAsset != null || string.IsNullOrEmpty(m_AssetName))
                return CachedAsset;

            var assetPath = AddressableDatabase.AddressNameToAssetPath(m_AssetName);
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
    /// <see cref="AssetLink"/> that only allows atlassed <see cref="Sprite"/> objects.  This will prevent legacy sprites from being used in this reference.
    /// If legacy sprite usage is needed, <see cref="AssetLinkSprite"/> can be used instead.
    /// </summary>
    [Serializable]
    public class AssetLinkAtlasedSprite : AssetLinkT<Sprite>
    {
        /// <summary>
        /// Constructs a new reference to a AssetLinkAtlasedSprite.
        /// </summary>
        /// <param name="name">The object name.</param>
        public AssetLinkAtlasedSprite(string name) : base(name)
        {
        }

#if UNITY_EDITOR
        protected internal override Type MainClassType { get { return typeof(SpriteAtlas); } }

        internal override Object GetEditorAssetInternal()
        {
            if (CachedAsset != null || string.IsNullOrEmpty(m_AssetName))
                return CachedAsset;

            var assetPath = AddressableDatabase.AddressNameToAssetPath(m_AssetName);
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
                if (CachedAsset != null || string.IsNullOrEmpty(AssetName))
                    return CachedAsset as SpriteAtlas;

                var assetPath = AddressableDatabase.AddressNameToAssetPath(AssetName);
                var main = AssetDatabase.LoadMainAssetAtPath(assetPath) as SpriteAtlas;
                if (main != null)
                    CachedAsset = main;
                return main;
            }
        }
#endif
    }


    /// <summary>
    /// <see cref="AssetLink"/> that only allows <see cref="SceneAsset"/> objects.
    /// </summary>
    [Serializable]
    public class AssetLinkScene : AssetLinkT<Object>
    {
        public AssetLinkScene(string name) : base(name)
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