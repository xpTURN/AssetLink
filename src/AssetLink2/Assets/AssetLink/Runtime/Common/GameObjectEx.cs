using UnityEngine;

namespace UnityEngine
{
    public static class GameObjectEx
    {
        public static T GetOrAddComponent<T>(this GameObject goObj) where T : Component
        {
            var co = goObj.GetComponent<T>();
            if (co == null)
            {
                co = goObj.AddComponent<T>();
            }

            return co;
        }
    }
}