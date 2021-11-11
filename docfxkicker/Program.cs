using docfxkicker.plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Core;
using NuGetHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker
{
    class Program
    {
        private string Repository = URepository.NuGetOrgUrl;
        private static Dictionary<string, ISubCommand> _commandMap = new Dictionary<string, ISubCommand>();

        static void Main(string[] args)
        {
            var docfxConsole = PluginLoader.SetupDocfxConsole();
            var docfxExe = Path.Combine(Path.GetDirectoryName(docfxConsole.Path), "tools", "docfx.exe");

            var jsonFilepath = Path.GetFullPath(args[0]);
            var baseDirectory = Path.GetDirectoryName(jsonFilepath);
            var obj = JsonParser.FromFile(args[0]);

            int stage = 0;
            foreach (var config in obj)
            {
                if (config is KickerConfigNode kicker)
                {
                    string command = kicker.ConfigNode.Name;
                    JToken kickerConfig = kicker.ConfigNode.Value;

                    if (command == "@repository")
                    {

                    }
                    else if (command == "@plugin")
                    {
                        Pugin(kickerConfig);
                    }
                    else if (_commandMap.TryGetValue(command, out var cmd))
                    {
                        cmd.Exec(kickerConfig as JObject, baseDirectory);
                    }
                    else
                    {
                        Debug.Print($"unknown command '{command}'");
                    }
                }
                if (config is DocFxConfigNode docfx)
                {
                    var jsonConfig = Path.Combine(baseDirectory, $".docfx_{stage++}.json");

                    using (var stream = new FileStream(jsonConfig, FileMode.Create))
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    using (var jwriter = new JsonTextWriter(writer))
                    {
                        jwriter.Formatting = Formatting.Indented;
                        docfx.ConfigObject.WriteTo(jwriter);
                    }

                    Process.Start(docfxExe, jsonConfig).WaitForExit();
                }
            }
        }


        static void Pugin(JToken token)
        {
            Dictionary<string, string> ids = token switch
            {
                JObject jobj
                    => jobj.Properties().ToDictionary(p => p.Name, p => p.Value.ToString()),

                JArray array
                    => array.ToDictionary(t => t.ToString(), t => default(String)),

                JValue val when val.Type == JTokenType.String
                    => new Dictionary<string, string>() { { (string)val.Value, null } },

                _ => throw new FormatException(""),
            };

            foreach (var command in ids.SelectMany(idSet => PluginLoader.Load(idSet.Key, idSet.Value)))
            {
                var commandName = command.CommandName;

                if (!commandName.StartsWith("@"))
                    commandName = "@" + commandName;

                _commandMap[commandName] = command;
            }
        }
    }
}
