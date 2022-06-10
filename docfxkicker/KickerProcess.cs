using docfxkicker.plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGetHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace docfxkicker
{
    internal class KickerProcess
    {
        private KickerOption _option { get; }

        public KickerProcess(KickerOption opt)
        {
            _option = opt;
        }

        public void Execute(Logger logger)
        {
            var nodes = JsonParser.FromFile(_option.ConfigFilePath).ToArray();

            var cmdmap = new Dictionary<string, ISubCommand>();
            cmdmap.OverwriteCommand(new LoopAfter());

            var task = new KickerTask(_option, new PluginLoader(), nodes, cmdmap);
            task.Execute(logger, false);
        }
    }
}
