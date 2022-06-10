using Newtonsoft.Json.Linq;
using System;

namespace docfxkicker.plugin
{
    public interface ISubCommand
    {
        string CommandName { get; }
        void Exec(JToken configNode, string baseDirectory, Logger logger);
    }
}
