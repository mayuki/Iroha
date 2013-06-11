using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Iroha.WebPages
{
    public static class Utility
    {
        public static String EscapeCSharpString(String s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "");
        }
    }
}