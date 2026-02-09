#if UNITY_EDITOR
using System.Reflection;
#endif

using UnityEngine;
using UnityEngine.AddressableAssets;

namespace xpTURN.AssetLink
{
#if UNITY_EDITOR
    public static class AddressablesEx
    {
        private static readonly PropertyInfo s_ReinitializeAddressables;

        static AddressablesEx()
        {
            s_ReinitializeAddressables = typeof(Addressables).GetProperty("reinitializeAddressables", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }

        public static bool ReinitializeAddressables
        {
            get
            {
                if (s_ReinitializeAddressables != null)
                {
                    return (bool)s_ReinitializeAddressables.GetValue(null);
                }

                return false;
            }
        }
    }
#endif
}