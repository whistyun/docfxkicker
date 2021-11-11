using Newtonsoft.Json.Linq;
using System;

namespace docfxkicker.plugin
{
    public interface ISubCommand
    {
        string CommandName { get; }
        void Exec(JObject configNode, string baseDirectory);
    }
}
