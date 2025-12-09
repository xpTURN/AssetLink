#if UNITY_INCLUDE_TESTS
namespace xpTURN.Link
{
    /// <summary>
    /// AddressableManager is a class that manages objects loaded asynchronously through Addressables.
    /// </summary>
    internal static partial class AddressableManager
    {
        #region Public Properties
        public static int PoolMaxCount => _handlerPool.MaxCount;
        public static int PoolUsedCount => _handlerPool.UsedCount;
        public static int PoolRemaining => _handlerPool.Remaining;
        #endregion

        #region Public Methods
        public static void ResetPool()
        {
            _handlerPool = new (() => new AsyncHandler(), handler => handler.Reset(), k_poolCapacity);
        }
        #endregion
    }
}
#endif