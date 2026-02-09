using System.Collections.Generic;
using System.Text;

namespace System.Diagnostics
{
    public static class StackTraceEx
    {
        /// <summary>
        /// Enumerates stack frames as (relativePath, lineNumber, methodDisplay) for editor/diagnostics.
        /// </summary>
        public static IEnumerable<(string relativePath, int lineNumber, string methodDisplay)> GetFrameEntries(this StackTrace trace)
        {
            if (trace == null) yield break;
            var frames = trace.GetFrames();
            if (frames == null) yield break;

            foreach (var frame in frames)
            {
                var method = frame.GetMethod();
                if (method == null) continue;

                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();
                if (string.IsNullOrEmpty(fileName) || lineNumber <= 0) continue;

                var assetsIndex = fileName.IndexOf("Assets", StringComparison.Ordinal);
                var packagesIndex = fileName.IndexOf("Packages", StringComparison.Ordinal);
                string relativePath;
                if (assetsIndex >= 0)
                    relativePath = fileName.Substring(assetsIndex);
                else if (packagesIndex >= 0)
                    relativePath = fileName.Substring(packagesIndex);
                else
                    continue;

                relativePath = relativePath.Replace('\\', '/');
                var declaringType = method.DeclaringType?.Name ?? "Unknown";
                var methodName = method.Name;
                yield return (relativePath, lineNumber, $"{declaringType}:{methodName}()");
            }
        }

        /// <summary>
        /// Converts the stack trace to a clickable hyperlink format for the Unity Console.
        /// </summary>
        public static string ToStringForUnityConsole(this StackTrace trace)
        {
            if (trace == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var (relativePath, lineNumber, methodDisplay) in trace.GetFrameEntries())
                sb.AppendLine($"{methodDisplay} at <a href=\"{relativePath}\" line=\"{lineNumber}\">{relativePath}:{lineNumber}</a>");
            return sb.ToString();
        }
    }
}