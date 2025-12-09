using UnityEngine;

namespace xpTURN.Link
{
    /// <summary>
    /// The AssetLink class manages assets handled by Addressables.
    /// </summary>
    [System.Serializable]
    abstract public partial class AssetLink : IAssetLink
    {
        #region Private Fields
        [SerializeField]
        private string _key;

        private bool _isLoad = false;
        #endregion

        #region Public Properties
        public string Key { get { return _key; } }
        public bool IsLoad { get { return _isLoad; } set { _isLoad = value; } }
        #endregion

        #region Public Methods
        public void AssignKey(string key)
        {
            if (_key == key)
                return;

            Reset();
            _key = key;
        }

        public void Reset()
        {
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }

            if (IsLoad)
            {
                AddressableManager.ReleaseAsset(this);
            }

            _key = string.Empty;
            IsLoad = false;

#if UNITY_EDITOR
            AssetForInspector = null;
#endif
        }
        #endregion
    }
}
