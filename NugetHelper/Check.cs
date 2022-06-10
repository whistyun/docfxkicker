using System;
using System.Collections.Generic;
using System.Text;

namespace NuGetHelper
{
    internal static class Check
    {
        public static void NotNull(string fieldNm, object value)
        {
            if (value is null) throw new ArgumentNullException(fieldNm);
        }
    }
}
