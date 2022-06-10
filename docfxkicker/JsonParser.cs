using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace docfxkicker
{
    class JsonParser : IEnumerable<ConfigNode>
    {
        List<ConfigNode> _nodes = new();

        public JsonParser(JObject json)
        {
            // split the configuration node into docfx and docfxkicker.

            DocFxConfigNode? lastNode = null;

            foreach (JProperty token in json.Properties())
            {
                if (token.Name.StartsWith("@"))
                {
                    _nodes.Add(new KickerConfigNode(token));
                    lastNode =null;
                }
                else
                {
                    if (lastNode is null)
                        _nodes.Add(lastNode=new());

                    lastNode.Add(token);
                }
            }
        }

        public static JsonParser FromFile(string jsonFile)
        {
            var jsonText = File.ReadAllText(jsonFile);
            return new JsonParser(JObject.Parse(jsonText));
        }

        public IEnumerator<ConfigNode> GetEnumerator() => _nodes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();
    }
}