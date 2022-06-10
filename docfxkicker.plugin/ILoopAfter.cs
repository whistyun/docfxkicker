using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker.plugin
{
    public interface ILoopAfter : ISubCommand
    {
        LoopInfo? ReadLoopInfo(JToken configNode, string baseDirectory, Logger logger);
    }

    public class LoopInfo
    {
        public IEnumerable<string> Items { get; }
        public string VariableName { get; }

        public LoopInfo(string variableName, string[] items)
        {
            VariableName = variableName;
            Items = items;
        }
    }
}
