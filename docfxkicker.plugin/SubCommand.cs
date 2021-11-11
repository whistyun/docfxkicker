using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace docfxkicker.plugin
{
    public abstract class SubCommand<T> : ISubCommand
    {
        public abstract string CommandName { get; }

        public void Exec(JObject configNode, string baseDirectory)
            => Exec(configNode.ToObject<T>(), baseDirectory);

        public abstract void Exec(T configNode, string baseDirectory);
    }
}
