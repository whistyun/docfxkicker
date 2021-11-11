using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace docfxkicker
{
    class JsonParser : IEnumerable<ConfigNode>
    {
        List<ConfigNode> nodes = new List<ConfigNode>();

        public JsonParser(JObject json)
        {
            JObject docfxConfig = new JObject();

            foreach (JProperty token in json.Properties())
            {
                if (token.Name.StartsWith("@"))
                {
                    if (docfxConfig.Count != 0)
                    {
                        nodes.Add(new DocFxConfigNode(docfxConfig));
                        docfxConfig = new JObject();
                    }

                    nodes.Add(new KickerConfigNode(token));
                }
                else
                {
                    docfxConfig.Add(token.Name, token.Value);
                }
            }

            if (docfxConfig.Count != 0)
            {
                nodes.Add(new DocFxConfigNode(docfxConfig));
            }
        }

        public static JsonParser FromFile(string jsonFile)
        {
            var jsonText = File.ReadAllText(jsonFile);
            return new JsonParser(JObject.Parse(jsonText));
        }

        public IEnumerator<ConfigNode> GetEnumerator() => nodes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => nodes.GetEnumerator();
    }
}