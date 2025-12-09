using System.Diagnostics;

using Debug = UnityEngine.Debug;

namespace xpTURN.Link
{
    /// <summary>
    /// Conditional debug logger to avoid GC allocation from string interpolation in release builds.
    /// </summary>
    internal static class DebugLogger
    {
        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        [Conditional("DEBUG")]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        [Conditional("DEBUG")]
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}

