using System.Collections.Generic;

namespace xpTURN.Link
{
    internal static partial class AddressableManager
    {
        /// <summary>
        /// This class represents a dangling reference to an asset.
        /// </summary>
        public class DanglingRefManager : Singleton<DanglingRefManager>, IUpdateLoop
        {
            public void AddDanglingRef(AsyncHandler handler, AsyncHandler.Item item)
            {
                _danglingRefs.Add((handler, item));
            }

            public void UpdateLoop()
            {
                if (_danglingRefs.Count == 0)
                {
                    return;
                }

                foreach (var (handle, item) in _danglingRefs)
                {
                    DebugLogger.LogWarning($"[AddressableManager] DanglingRefManager: Releasing dangling reference for {handle.Key}");
                    AddressableManager.ReleaseInstance(handle, item);
                }

                _danglingRefs.Clear();
            }

            public DanglingRefManager()
            {
                UpdateLoopManager.Instance.Add(this);
            }

            private List<(AsyncHandler Handler, AsyncHandler.Item Item)> _danglingRefs = new ();
        }
    }
}