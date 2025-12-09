using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
namespace xpTURN.Link
{
    /// <summary>
    /// The IAssetLink interface defines the functionality of the AssetLink class.
    /// </summary>
    public partial interface IAssetLink
    {
        void AssignAsset(Object asset);
    }

    /// <summary>
    /// The AssetLink class manages objects that are asynchronously loaded via Addressables.
    /// </summary>
    abstract public partial class AssetLink : IAssetLink
    {
        #region Private Fields
        private Object _assetForInspector = null;
        #endregion

        #region Public Properties
        public Object AssetForInspector
        {
            get
            {
                if (_assetForInspector == null)
                {
                    var path = AddressableKeyFinder.FindAssetPathFromAddressableKey(_key);
                    _assetForInspector = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(path);
                }
                return _assetForInspector;
            }

            protected set
            {
                _assetForInspector = value;
            }
        }

        public System.Type LinkableTypeForInspector { get; protected set; }
        #endregion

        #region Public Methods
        public void AssignAsset(Object asset)
        {
            if (asset == null)
            {
                Reset();
                return;
            }

            var path = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(path))
            {
                if (AddressableKeyFinder.IsAddressable(path))
                {
                    AssetForInspector = asset;

                    var key = AddressableKeyFinder.FindAddressableKeyFromPath(path);
                    AssignKey(key);
                }
                else
                {
                    Debug.LogWarning("[AssetLink] The selected asset is not addressable.");
                    Reset();
                }
            }
        }
        #endregion
    }
}
#endif