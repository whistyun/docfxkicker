using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace docfxkicker
{
    class KickerConfigNode : ConfigNode
    {
        public JProperty ConfigNode { get; }

        public KickerConfigNode(JProperty prop)
        {
            ConfigNode = prop;
        }
    }
}
