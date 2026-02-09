using System.Reflection;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
#if UNITY_INCLUDE_TESTS 
    public static class AsyncOperationHandleEx
    {
        private static readonly PropertyInfo s_ReferenceCountProperty;

        static AsyncOperationHandleEx()
        {
            s_ReferenceCountProperty = typeof(AsyncOperationHandle).GetProperty("ReferenceCount", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static int GetReferenceCount(this AsyncOperationHandle handle)
        {
            if (s_ReferenceCountProperty != null)
            {
                return (int)s_ReferenceCountProperty.GetValue(handle);
            }
            return 0;
        }
    }
#endif
}
