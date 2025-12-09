using UnityEngine;

namespace xpTURN.Link
{
    /// <summary>
    /// Generic singleton class based on MonoBehaviour.
    /// </summary>
    public class ComponentSingleton<T> : MonoBehaviour where T : ComponentSingleton<T>
    {
        private static T _instance;
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// Access the singleton instance.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] {typeof(T)} instance has already been terminated. No new instance will be returned.");
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    if (_instance == null)
                    {
                        var singletonObject = new GameObject(typeof(T).Name);
                        _instance = singletonObject.AddComponent<T>();
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
