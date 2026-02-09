#define ENABLE_TRACKING_TEST

using System;
using SerializableAttribute = System.SerializableAttribute;

using UnityEngine;
using UnityEngine.AddressableAssets;

using xpTURN.AssetLink;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEditor;

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

[Serializable]
public class AssetLinkMaterial : AssetLinkT<Material>
{
    public AssetLinkMaterial(string name) : base(name)
    {
    }
}

[Serializable]
public class AssetRefMaterial : AssetRefT<Material>
{
    public AssetRefMaterial(string guid) : base(guid)
    {
    }
}

public class DeclaringLinks : MonoBehaviour
{
    [Header("AssetReference")]
    public AssetReference assetReference;
    public AssetReferenceSprite spriteReference;
    public AssetReferenceAtlasedSprite atlasedSpriteReference;


    [Header("AssetLink")]
    public AssetLink link;

    public AssetLinkGameObject gameObjectLink;

    public AssetLinkSprite spriteLink;
    public AssetLinkAtlasedSprite atlasSpriteLink;

    public AssetLinkTexture textureLink;
    public AssetLinkTexture2D texture2DLink;
    public AssetLinkTexture3D texture3DLink;

    public AssetLinkT<AudioClip> typedLink;

    public AssetLinkMaterial materialLink;

    [Header("AssetRef")]
    public AssetRef @ref;

    public AssetRefGameObject gameObjectRef;

    public AssetRefSprite spriteRef;
    public AssetRefAtlasedSprite atlasSpriteRef;

    public AssetRefTexture textureRef;
    public AssetRefTexture2D texture2DRef;
    public AssetRefTexture3D texture3DRef;

    public AssetRefT<AudioClip> typedRef;

    public AssetRefMaterial materialRef;

    [Header("AssetLinkSpawner")]
    public AssetLinkSpawner assetLinkSpawner;

    public AssetLinkScene sceneLink;
    public AssetRefScene sceneRef;

    [Header("UILabelRestriction")]
    [AssetReferenceUILabelRestriction("animals", "characters")]
    public AssetLink linkHavLabels;

    [Header("UILabelRestriction")]
    [AssetReferenceUILabelRestriction("animals", "characters")]
    public AssetLink linkHavLabels2;

    public void DoStart()
    {
        Debug.Log($"[AssetLinkSettings] HandlePoolSize: {AssetLinkSettings.Instance.HandlePoolSize}, EnableStackTrace: {AssetLinkSettings.Instance.EnableStackTrace}");

        for (int i = 0; i < 3; ++i)
        {
            var gameObjectHandle = gameObjectLink.InstantiateAsync();
            gameObjectHandle.WaitForCompletion();
            var gameObjectAsset = gameObjectHandle.Result;
            Debug.Log($"Instantiated GameObject({gameObjectAsset.GetType().Name}): {gameObjectAsset.name}, {gameObjectLink.AssetName}");
        }

        var sprite = spriteLink.LoadAssetAsync();
        sprite.WaitForCompletion();
        var spriteAsset = sprite.Result;
        Debug.Log($"Loaded Sprite({spriteAsset.GetType().Name}): {spriteAsset.name}, {spriteLink.SubObjectName}");

        var atlasInSprite = atlasSpriteLink.LoadAssetAsync();
        atlasInSprite.WaitForCompletion();
        var atlasInSpriteAsset = atlasInSprite.Result;
        Debug.Log($"Loaded AtlasSprite({atlasInSpriteAsset.GetType().Name}): {atlasInSpriteAsset.name}, {atlasSpriteLink.SubObjectName}");

        var tex = textureLink.LoadAssetAsync();
        tex.WaitForCompletion();
        var texAsset = tex.Result;
        Debug.Log($"Loaded Texture({texAsset.GetType().Name}): {texAsset.name}, {textureLink.AssetName}");

        var tex2D = texture2DLink.LoadAssetAsync();
        tex2D.WaitForCompletion();
        var tex2DAsset = tex2D.Result;
        Debug.Log($"Loaded Texture2D({tex2DAsset.GetType().Name}): {tex2DAsset.name}, {texture2DLink.AssetName}");

        var tex3D = texture3DLink.LoadAssetAsync();
        tex3D.WaitForCompletion();
        var tex3DAsset = tex3D.Result;
        Debug.Log($"Loaded Texture3D({tex3DAsset.GetType().Name}): {tex3DAsset.name}, {texture3DLink.AssetName}");

        var typed = typedLink.LoadAssetAsync();
        typed.WaitForCompletion();
        var typedAsset = typed.Result;
        Debug.Log($"Loaded AudioClip({typedAsset.GetType().Name}): {typedAsset.name}, {typedLink.AssetName}");

        var mat = materialLink.LoadAssetAsync();
        mat.WaitForCompletion();
        var matAsset = mat.Result;
        Debug.Log($"Loaded Material({matAsset.GetType().Name}): {matAsset.name}, {materialLink.AssetName}");

        // Spawner
        for (int i = 0; i < 10; ++i)
            assetLinkSpawner.SpawnAsync().Forget();

        sceneLink.LoadSceneAsync(LoadSceneMode.Additive);
        sceneRef.LoadSceneAsync(LoadSceneMode.Additive);
    }

    void OnDestroy()
    {
#if !ENABLE_TRACKING_TEST
        if (link.IsValid())
            link.ReleaseAsset();

        if (spriteLink.IsValid())
            spriteLink.ReleaseAsset();

        if (atlasSpriteLink.IsValid())
            atlasSpriteLink.ReleaseAsset();

        if (textureLink.IsValid())
            textureLink.ReleaseAsset();

        if (texture2DLink.IsValid())
            texture2DLink.ReleaseAsset();

        if (texture3DLink.IsValid())
            texture3DLink.ReleaseAsset();

        if (typedLink.IsValid())
            typedLink.ReleaseAsset();

        if (materialLink.IsValid())
            materialLink.ReleaseAsset();
#endif
    }
}
