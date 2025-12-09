using System.Diagnostics;
using System.Text;

namespace xpTURN.Link
{
    public class StackTraceParser
    {
        private static readonly StringBuilder _sb = new(512);
        private static readonly string[] _declaringStrings = new string[6];

        public static string ToString(StackTrace stackTrace, int skipDepth = 0, int maxDepth = 128)
        {
            _sb.Clear();
            var frames = stackTrace.GetFrames();

            if (frames == null)
                return string.Empty;

            for (var i = skipDepth + 1; i < frames.Length && i < maxDepth; i++)
            {
                if (i < 0)
                    continue;

                var frame = frames[i];
                var method = frame.GetMethod();
                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                if (method.DeclaringType is { } declaringType)
                {
                    if (declaringType.Namespace is { } declaringTypeNamespace)
                    {
                        _declaringStrings[0] = declaringTypeNamespace;
                        _declaringStrings[1] = ".";
                        _declaringStrings[2] = declaringType.Name;
                        _declaringStrings[3] = ".";
                        _declaringStrings[4] = method.Name;
                        _declaringStrings[5] = " ";

                        _sb.Append(string.Concat(_declaringStrings));
                    }
                    else
                    {
                        _sb.Append(string.Concat(declaringType.Name, ".", method.Name, " "));
                    }
                }
                else
                {
                    _sb.Append(string.Concat(method.Name, " "));
                }

                if (!string.IsNullOrEmpty(fileName) && lineNumber > 0)
                {
                    _sb.Append(string.Concat(" (at ", fileName, ":")).AppendLine(string.Concat(lineNumber.ToString(), ")"));
                }
                else
                {
                    if (method.Module is { } module)
                    {
                        if (module.FullyQualifiedName is { Length: > 0 } fullyQualifiedName)
                        {
                            _sb.AppendLine(string.Concat(" (at ", fullyQualifiedName, ")"));
                        }
                        else if (module.Name is { Length: > 0 } moduleName)
                        {
                            _sb.AppendLine(string.Concat(" (at ", moduleName, ")"));
                        }
                        else
                        {
                            _sb.AppendLine("");
                        }
                    }
                    else
                    {
                        _sb.AppendLine("");
                    }
                }
            }

            return _sb.ToString();
        }
    }
}
