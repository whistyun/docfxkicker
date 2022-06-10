using docfxkicker.plugin;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker
{
    internal class LoopAfter : ILoopAfter
    {
        public LoopInfo? ReadLoopInfo(JToken configNode, string baseDirectory, Logger logger)
        {
            if (configNode is JArray array)
            {
                return new LoopInfo(
                        "item",
                        array.Children()
                             .Cast<JValue>()
                             .Select(jvl => jvl.ToString())
                             .ToArray()
                );
            }
            else if (configNode is JObject obj)
            {
                if (obj.TryGetValue("name", out var nameToken) && nameToken is JValue name
                 && obj.TryGetValue("items", out var itemToken) && itemToken is JArray items)
                {
                    return new LoopInfo(
                            name.ToString(),
                            items.Children()
                                 .Cast<JValue>()
                                 .Select(jvl => jvl.ToString())
                                 .ToArray()
                    );
                }
            }

            logger.Log("failed to parse", LogLevel.Error, "docfxkicker.LoopAfter");
            return null;
        }

        public string CommandName => "@loop";

        public void Exec(JToken configNode, string baseDirectory, Logger logger)
        {
        }
    }
}
