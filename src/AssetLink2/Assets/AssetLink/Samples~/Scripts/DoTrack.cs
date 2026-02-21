using System;

using UnityEngine;

#if ASSETLINK_UNITASK_INTEGRATION
using Cysharp.Threading.Tasks;
#endif

using xpTURN.AssetLink;

public class DoTrack : MonoBehaviour
{
    void ReleaseUnreferenced()
    {
        var declaringLinks = GameObject.FindFirstObjectByType<DeclaringLinks>();
        if (declaringLinks != null)
        {
            Destroy(declaringLinks.gameObject);
            declaringLinks = null;
        }
    }

#if ASSETLINK_UNITASK_INTEGRATION
    async void DetectAndReportLeaks()
    {
        // Run GC
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Unload unused assets
        await Resources.UnloadUnusedAssets().ToUniTask();
        await UniTask.DelayFrame(2);

        // Run GC
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        await UniTask.WaitForSeconds(0.1f);

        // Log Link/Ref that leaked due to missing Release
        // Use error log to fix code (add ReleaseAsset() calls)
        AddressablesTracker.ReportUnreferencedHandles();

        // Log Link/Ref that leaked & force ReleaseAsset (fallback)
        // But not release the handles that are assets of scene links. (Too many dangerous)
        AddressablesTracker.ReleaseUnreferencedHandles(true);
    }

    void DoLeakScenario()
    {
        var leakScenario = GameObject.FindFirstObjectByType<LeakScenario>();
        if (leakScenario != null)
        {
            Destroy(leakScenario.gameObject);
            leakScenario = null;
        }

        DetectAndReportLeaks();
    }
#endif

    void DoStart()
    {
        var declaringLinks = GameObject.FindFirstObjectByType<DeclaringLinks>();
        if (declaringLinks != null)
        {
            declaringLinks.DoStart();
        }
    }

    void DoAutoRelease()
    {
        var arrFound = GameObject.FindObjectsByType<Empty>(FindObjectsSortMode.None);
        if (arrFound != null && arrFound.Length > 0)
        {
            for (int i = 0; i < arrFound.Length; ++i)
                Destroy(arrFound[i].gameObject);
        }
    }

    void DoUnloadSceneLink()
    {
        var declaringLinks = GameObject.FindFirstObjectByType<DeclaringLinks>();
        if (declaringLinks != null)
        {
            declaringLinks.UnloadSceneByLink();
        }
    }

    void DoUnloadSceneRef()
    {
        var declaringLinks = GameObject.FindFirstObjectByType<DeclaringLinks>();
        if (declaringLinks != null)
        {
            declaringLinks.UnloadSceneByRef();
        }
    }
}
