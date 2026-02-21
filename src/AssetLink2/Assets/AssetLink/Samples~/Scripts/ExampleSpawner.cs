#if ASSETLINK_UNITASK_INTEGRATION
using System.Collections.Generic;

using UnityEngine;

using Cysharp.Threading.Tasks;

using xpTURN.AssetLink;

public class ExampleSpawner : MonoBehaviour
{
    public AssetLinkSpawner prefabSpawner;

    private List<GameObject> goCats = new();

    async void Start()
    {
        for (int i = 0; i < 100; ++i)
        {
            var goCat = await prefabSpawner.SpawnAsync();
            goCat.name = $"Cat{i:d3}";

            goCats.Add(goCat);
        }
    }

    void OnDestroy()
    {
        // Instances created with InstantiateAsync are released automatically when destroyed; no need to manage prefabLink/prefabRef manually
        foreach (var goCat in goCats)
        {
            // ReleaseInstance is called internally
            // Handled by DoAutoRelease component when OnDestroy fires
            GameObject.Destroy(goCat);
        }
    }
}
#endif