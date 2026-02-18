using UnityEngine;

namespace UnityEngine
{
    public static class GameObjectEx
    {
        public static T GetOrAddComponent<T>(this GameObject goObj) where T : Component
        {
            // (Editor) Compared to GetComponent, avoids memory allocation and is slightly faster.
            if (!goObj.TryGetComponent<T>(out T co))
            {
                co = goObj.AddComponent<T>();
            }

            return co;
        }
    }
}