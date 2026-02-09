using System;
using System.Collections;
using System.Threading.Tasks;

using UnityEngine;

using xpTURN.AssetLink;

public class DoTrack : MonoBehaviour
{
    void ReleaseUnreferencedHandlesTest()
    {
        StartCoroutine(ReleaseUnreferencedHandlesTestCoroutine());
    }

    IEnumerator ReleaseUnreferencedHandlesTestCoroutine()
    {
        var declaringLinks = GameObject.FindFirstObjectByType<DeclaringLinks>();
        if (declaringLinks != null)
        {
            Destroy(declaringLinks.gameObject);
            declaringLinks = null;
        }

        yield return null;
        yield return null;

        GC.Collect(1, GCCollectionMode.Forced, true);

        yield return new WaitForSeconds(3f);

        AddressablesTracker.ReportUnreferencedHandles();
    }

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
            declaringLinks.sceneLink.UnLoadScene();
        }
    }

    void DoUnloadSceneRef()
    {
        var declaringLinks = GameObject.FindFirstObjectByType<DeclaringLinks>();
        if (declaringLinks != null)
        {
            declaringLinks.sceneRef.UnLoadScene();
        }
    }
}
