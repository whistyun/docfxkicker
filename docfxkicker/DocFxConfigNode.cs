using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace docfxkicker
{
    class DocFxConfigNode : ConfigNode
    {
        public JObject ConfigObject { get; }

        public DocFxConfigNode(JObject obj)
        {
            ConfigObject = obj;
        }
    }
}
