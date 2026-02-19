#if ASSETLINK_UNITASK_INTEGRATION
using UnityEngine;

using Cysharp.Threading.Tasks;

using xpTURN.AssetLink;

public class ExampleSimple : MonoBehaviour
{
    [Header("AssetLink (name-based)")]
    public AssetLinkGameObject prefabLink;
    public AssetLinkSprite spriteLink;

    [Header("AssetRef (GUID-based)")]
    public AssetRefGameObject prefabRef;
    public AssetRefSprite spriteRef;

    private GameObject goCat;
    private GameObject goDog;
    private Sprite spriteCat;
    private Sprite spriteDog;

    async void LoadSprite()
    {
        // Call when dynamic mapping is needed (asset name, sprite name)
        spriteLink.SetAssetName("sprites/cats_maine_coon", "cats_maine_coon");
        spriteCat = await spriteLink.LoadAssetAsync().ToUniTask();

        // Dynamic mapping not supported
        spriteDog = await spriteRef.LoadAssetAsync().ToUniTask();
    }

    async void InstantiatePrefab()
    {
        goCat = await prefabLink.InstantiateAsync().ToUniTask();
        goDog = await prefabRef.InstantiateAsync().ToUniTask();
    }

    void OnDestroy()
    {
        // Release loaded assets
        if (spriteLink.IsValid())
            spriteLink.ReleaseAsset();

        // Release loaded assets (omission causes memory leak)
        // if (spriteRef.IsValid())
        //     spriteRef.ReleaseAsset();

        // ReleaseInstance is called automatically (by DoAutoRelease component when OnDestroy fires)
        GameObject.Destroy(goCat);
        GameObject.Destroy(goDog);

        // For instances created with AssetReference.InstantiateAsync,
        // AssetReference.ReleaseInstance() must be called (omission causes memory leak)
    }
}
#endif