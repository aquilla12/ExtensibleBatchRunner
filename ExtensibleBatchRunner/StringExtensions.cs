using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensibleBatchRunner
{
    static class StringExtensions
    {
        public static string ResolvePathVariables(this string str, string varName, FileInfo file)
        {
            var fullPath = file.FullName;
            return str
                .ResovleVar(varName, Path.GetFileNameWithoutExtension(fullPath))
                .ResovleVar(varName + "WithExtension", Path.GetFileName(fullPath))
                .ResovleVar(varName + "Extension", Path.GetExtension(fullPath))
                .ResovleVar(varName + "Directory", Path.GetDirectoryName(fullPath))
                .ResovleVar(varName + "FullPath", Path.GetFullPath(fullPath));
        }

        public static string ResovleVar(this string str, string varName, string value)
        {
            return str.Replace($"$({varName})", $"\"{value}\"");
        }
    }
}
