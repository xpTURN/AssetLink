#if ASSETLINK_UNITASK_INTEGRATION
using System;
using UnityEngine;

using Cysharp.Threading.Tasks;
using xpTURN.AssetLink;

// Intentionally load without release (creates leak scenario).
// In real game code, always call Release (ReleaseAsset) for loaded assets.
public class LeakScenario : MonoBehaviour
{
    AssetLink ObjectLink_001 = new AssetLink();
    AssetLink ObjectLink_002 = new AssetLink();
    AssetLink ObjectLink_003 = new AssetLink();
    AssetLink ObjectLink_004 = new AssetLink();
    AssetLink ObjectLink_005 = new AssetLink();

    async void Start()
    {
        ObjectLink_001.SetAssetName("Prefabs/Cat1.prefab");
        await ObjectLink_001.LoadAssetAsync<GameObject>().ToUniTask();

        ObjectLink_002.SetAssetName("Prefabs/Cat2.prefab");
        await ObjectLink_002.LoadAssetAsync<GameObject>().ToUniTask();

        ObjectLink_003.SetAssetName("Prefabs/Cat3.prefab");
        await ObjectLink_003.LoadAssetAsync<GameObject>().ToUniTask();

        ObjectLink_004.SetAssetName("Prefabs/Cat4.prefab");
        await ObjectLink_004.LoadAssetAsync<GameObject>().ToUniTask();

        ObjectLink_005.SetAssetName("Prefabs/Cat5.prefab");
        await ObjectLink_005.LoadAssetAsync<GameObject>().ToUniTask();
    }

    void OnDestroy()
    {
        // Release loaded assets; ReleaseAsset intentionally omitted for testing
        // if (ObjectLink_001.IsValid())
        //     ObjectLink_001.ReleaseAsset();
        // if (ObjectLink_002.IsValid())
        //     ObjectLink_002.ReleaseAsset();
        // if (ObjectLink_003.IsValid())
        //     ObjectLink_003.ReleaseAsset();
        // if (ObjectLink_004.IsValid())
        //     ObjectLink_004.ReleaseAsset();
        // if (ObjectLink_005.IsValid())
        //     ObjectLink_005.ReleaseAsset();

        // Link objects removed without Release
        ObjectLink_001 = null;
        ObjectLink_002 = null;
        ObjectLink_003 = null;
        ObjectLink_004 = null;
        ObjectLink_005 = null;
    }
}
#endif