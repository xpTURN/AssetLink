using UnityEngine;

namespace xpTURN.Link
{
    public class ReleaseOnDestory : MonoBehaviour
    {
        private AddressableManager.AsyncHandler _handler;
        private AddressableManager.AsyncHandler.Item _item;

        internal void SetHandler(AddressableManager.AsyncHandler handler, AddressableManager.AsyncHandler.Item item)
        {
            _handler = handler;
            _item = item;
        }

        private void OnDestroy()
        {
            if (_handler == null)
            {
                DebugLogger.LogError("[ReleaseOnDestory] handler is null");
                return;
            }

            if (_item == null)
            {
                DebugLogger.LogError("[ReleaseOnDestory] item is null");
                return;
            }

            AddressableManager.ReleaseInstance(_handler, _item);
        }
    }
}
