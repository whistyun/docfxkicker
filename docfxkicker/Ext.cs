using docfxkicker.plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker
{
    internal static class Ext
    {
        public static void OverwriteCommand(this Dictionary<string, ISubCommand> map, ISubCommand cmd)
            => map[cmd.CommandName] = cmd;
    }
}
