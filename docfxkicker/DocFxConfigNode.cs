using Newtonsoft.Json.Linq;

namespace docfxkicker
{
    class DocFxConfigNode : ConfigNode
    {
        public JObject ConfigObject { set; get; }

        private DocFxConfigNode(JObject configObject)
        {
            ConfigObject = configObject;
        }

        public DocFxConfigNode() : this(new JObject())
        {
        }

        public void Add(JProperty token) => ConfigObject.Add(token.Name, token.Value);

        public override ConfigNode DeepClone()
        {
            return new DocFxConfigNode((JObject)ConfigObject.DeepClone());
        }
    }
}
