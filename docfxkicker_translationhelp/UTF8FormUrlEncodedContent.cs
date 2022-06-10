using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker_translationhelp
{
    internal class UTF8FormUrlEncodedContent : ByteArrayContent
    {
        public UTF8FormUrlEncodedContent(IEnumerable<KeyValuePair<string, string>> nameValueCollection)
              : base(GetContentByteArray(nameValueCollection))
        {
            Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        }

        private static byte[] GetContentByteArray(IEnumerable<KeyValuePair<string, string>> nameValSet)
        {
            if (nameValSet == null)
            {
                throw new ArgumentNullException(nameof(nameValSet));
            }

            var str = string.Join("&", nameValSet.Select(pair => $"{Encode(pair.Key)}={Encode(pair.Value)}"));
            return Encoding.UTF8.GetBytes(str);
        }

        private static string Encode(string data)
            => String.IsNullOrEmpty(data) ? String.Empty : Uri.EscapeDataString(data).Replace("%20", "+");
    }
}
