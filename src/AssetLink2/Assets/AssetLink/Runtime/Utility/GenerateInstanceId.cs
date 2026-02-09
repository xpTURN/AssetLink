using System;

namespace xpTURN.AssetLink.Utility
{
    /// <summary>
    /// Thread-safe unique instance ID generator for spawned/tracked instances.
    /// </summary>
    internal static class GenerateInstanceId
    {
        private static long _counter = 0;

        /// <summary>
        /// Returns the next unique numeric instance ID (monotonically increasing).
        /// Thread-safe.
        /// </summary>
        public static long Next()
        {
            return System.Threading.Interlocked.Increment(ref _counter);
        }

        /// <summary>
        /// Returns a unique string instance ID with optional prefix (e.g. "Spawn_1", "Spawn_2").
        /// Thread-safe.
        /// </summary>
        /// <param name="prefix">Optional prefix. If null or empty, "id" is used.</param>
        public static string NextString(string prefix = null)
        {
            var p = string.IsNullOrEmpty(prefix) ? "id" : prefix;
            return $"{p}_{Next()}";
        }

#if UNITY_INCLUDE_TESTS
        /// <summary>
        /// Resets the internal counter. For testing only.
        /// </summary>
        public static void Reset()
        {
            System.Threading.Interlocked.Exchange(ref _counter, 0L);
        }
#endif
    }
}
