namespace xpTURN.Link
{
    /// <summary>
    /// Generic singleton class (not inheriting from MonoBehaviour).
    /// </summary>
    public class Singleton<T> where T : class, new()
    {
        private static readonly object _lock = new object();
        private static T _instance;

        /// <summary>
        /// Access the singleton instance.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                        }
                    }
                }
                return _instance;
            }
        }

        // Protected constructor to prevent external instantiation.
        protected Singleton() { }
    }
}
