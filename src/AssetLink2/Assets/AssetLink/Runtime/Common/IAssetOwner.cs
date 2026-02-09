using UnityEngine;

namespace xpTURN.AssetLink
{
    public interface IAssetOwner
    {
        long OwnerId { get; }
        bool IsSpawner { get; }
        string RuntimeKeyString { get; }
        void ReleaseAsset();
    }
}