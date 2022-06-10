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
    internal class KickerTask
    {
        private KickerOption _option;
        private PluginLoader _loader;
        private ConfigNode[] _nodes;
        private Dictionary<string, ISubCommand> _commandMap;

        public KickerTask(
                KickerOption opt,
                PluginLoader ld,
                ConfigNode[] nodes,
                Dictionary<string, ISubCommand> map)
        {
            _option = opt;
            _loader = ld;
            _nodes = nodes;
            _commandMap = map;

            _commandMap.OverwriteCommand(new RepositoryCommand(_loader));
            _commandMap.OverwriteCommand(new PluginCommand(_loader, _commandMap));
            _commandMap.OverwriteCommand(new ChangeDirectoryCommand(_option));
            _commandMap.OverwriteCommand(new InitCommand(_option));
        }

        public void Execute(Logger logger, bool ignoreFirstLoop)
        {
            for (int nodeIdx = 0; nodeIdx < _nodes.Length; ++nodeIdx)
            {
                ConfigNode node = _nodes[nodeIdx];

                if (node is DocFxConfigNode docfxConfig)
                {
                    using var jsonFile = new TemporaryDocFxConfigFile(_option.ProjectDirectory, ".json", docfxConfig);
                    ExecuteDocfx(jsonFile.TargetPath);

                    continue;
                }

                if (node is KickerConfigNode kickerConfig)
                {
                    if (!_commandMap.TryGetValue(kickerConfig.ConfigNode.Name, out var command))
                    {
                        logger.Log(
                            $"unknown command '{kickerConfig.ConfigNode.Name}'",
                            LogLevel.Error,
                            "DocFxKicker.KickerProcess");

                        Environment.Exit(1);
                        return;
                    }

                    var ignoreLoop = ignoreFirstLoop && nodeIdx == 0;

                    if (!ignoreLoop && command is ILoopAfter loop)
                    {
                        var info = loop.ReadLoopInfo(
                                            kickerConfig.ConfigNode.Value,
                                            _option.ProjectDirectory,
                                            logger);

                        if (info is null)
                        {
                            Environment.Exit(1);
                            return;
                        }

                        ExecuteLoop(info, _nodes.Skip(nodeIdx), logger);

                        return;
                    }
                    else
                    {
                        command.Exec(kickerConfig.ConfigNode.Value, _option.ProjectDirectory, logger);
                    }

                    continue;
                }

                throw new InvalidOperationException();
            }
        }

        private void ExecuteDocfx(string jsonConfig)
        {
            string command;
            string argument;


            if (String.IsNullOrWhiteSpace(_option.ToolPrefix))
            {
                command = _option.DocFxExe;
                argument = @$" ""{jsonConfig}"" -o ""{_option.ProjectDirectory}"" -l ""{_option.LogFilePath}"" --logLevel ""Warning""";
            }
            else
            {
                command = _option.ToolPrefix;
                argument = @$" ""{_option.DocFxExe}"" ""{jsonConfig}"" -o ""{_option.ProjectDirectory}"" -l ""{_option.LogFilePath}"" --logLevel ""Warning""";
            }

            var info = new ProcessStartInfo(command, argument)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(info).WaitForExit();
        }

        private void ExecuteLoop(LoopInfo info, IEnumerable<ConfigNode> nodes, Logger logger)
        {
            foreach (var item in info.Items)
            {
                logger.Log($"Loop #{item}", LogLevel.Info, "DocFxKicker.KickerTask.ExecuteLoop");

                var copyNodes = nodes.Select(nd => nd.DeepClone()).ToArray();
                foreach (var copyNode in copyNodes)
                    ReplaceVariable(copyNode, info.VariableName, item);

                var copyOpt = new KickerOption(_option);
                var copyLd = new PluginLoader(_loader);
                var copyMap = new Dictionary<string, ISubCommand>(_commandMap);
                var task = new KickerTask(copyOpt, copyLd, copyNodes, copyMap);

                task.Execute(logger, true);
            }
        }

        private void ReplaceVariable(ConfigNode node, string variableName, string value)
        {
            if (node is DocFxConfigNode docfxConfig)
            {
                var jsonTxt = ReplaceVariable(AsString(docfxConfig.ConfigObject), variableName, value);
                docfxConfig.ConfigObject = (JObject)JToken.Parse(jsonTxt);

                return;
            }
            if (node is KickerConfigNode kickerConfig)
            {
                JProperty obj = kickerConfig.ConfigNode;
                var jsonTxt = ReplaceVariable(AsString(obj.Value), variableName, value);
                obj.Value = JToken.Parse(jsonTxt);

                return;
            }

            throw new InvalidOperationException();
        }

        private string ReplaceVariable(string text, string variableName, string value)
        {
            var keyord = $@"\${variableName}(?=[^a-zA-Z0-9])|\${variableName}$|\$\{{{variableName}\}}";
            return Regex.Replace(text, keyord, value);
        }

        private static string AsString(JToken token)
        {
            var writer = new StringWriter();
            using (var jwriter = new JsonTextWriter(writer))
            {
                token.WriteTo(jwriter);
            }
            return writer.ToString();
        }

        class InitCommand : ISubCommand
        {
            public string CommandName => "@init";

            private KickerOption _option;

            public InitCommand(KickerOption option)
            {
                _option = option;
            }

            public void Exec(JToken configNode, string baseDirectory, Logger logger)
            {
                if (!(configNode is JObject obj))
                    throw new FormatException("");

                if (!obj.TryGetValue("dest", out var destTkn))
                    throw new FormatException("dest");

                if (!obj.TryGetValue("trigger", out var triggerTkn))
                    throw new FormatException("trigger");

                var destDir = Path.Combine(baseDirectory, destTkn.ToString());
                var triggerFile = Path.Combine(destDir, triggerTkn.ToString());

                if (File.Exists(triggerFile))
                    return;

                logger.Log("output template file", LogLevel.Info, "DocFxKicker.KickerTask.InitCommand");
                Copy(_option.TemplateDirectory, destDir);
            }

            private void Copy(string from, string to)
            {
                var dirs = Directory.GetDirectories(from);
                var files = Directory.GetFiles(from);

                foreach (var dir in dirs)
                {
                    var dirNm = Path.GetFileName(dir);

                    var toChild = Path.Combine(to, dirNm);
                    Directory.CreateDirectory(toChild);

                    Copy(dir, toChild);
                }

                foreach (var file in files)
                {
                    var fileNm = Path.GetFileName(file);
                    var toChild = Path.Combine(to, fileNm);

                    File.Copy(file, toChild);
                }
            }
        }

        class RepositoryCommand : ISubCommand
        {
            public string CommandName => "@repository";

            private PluginLoader _loader;

            public RepositoryCommand(PluginLoader loader)
            {
                _loader = loader;
            }


            public void Exec(JToken token, string baseDirectory, Logger logger)
            {
                IEnumerable<string> urls = token switch
                {
                    JArray array
                        => array.Select(p => p.ToString()),

                    JValue val when val.Type == JTokenType.String && val.Value is not null
                        => new[] { (string)val.Value },

                    _ => throw new FormatException(""),
                };

                foreach (var url in urls)
                {
                    _loader.AddRepoitory(url);
                }
            }
        }

        class PluginCommand : ISubCommand
        {
            public string CommandName => "@plugin";

            private PluginLoader _loader;
            private Dictionary<string, ISubCommand> _commandMap;

            public PluginCommand(PluginLoader loader, Dictionary<string, ISubCommand> map)
            {
                _loader = loader;
                _commandMap = map;
            }

            public void Exec(JToken token, string baseDirectory, Logger logger)
            {
                IEnumerable<ISubCommand> commands = token switch
                {
                    JObject jobj
                        => jobj.Properties().SelectMany(p => _loader.Load(p.Name, p.Value.ToString())),

                    JArray array
                        => array.SelectMany(p => _loader.Load(p.ToString())),

                    JValue val when val.Type == JTokenType.String && val.Value is not null
                        => _loader.Load((string)val.Value),

                    _ => throw new FormatException(""),
                };

                foreach (var command in commands)
                {
                    var commandName = command.CommandName;

                    if (!commandName.StartsWith("@"))
                        commandName = "@" + commandName;


                    if (_commandMap.ContainsKey(commandName))
                    {
                        logger.Log(
                            $"'{commandName}' command is dupplicated",
                            LogLevel.Warning,
                            "DocFxKicker.KickerTask.Plugin");

                        continue;
                    }

                    _commandMap[commandName] = command;
                }
            }
        }

        class ChangeDirectoryCommand : ISubCommand
        {
            public string CommandName => "@cd";

            private KickerOption _option;

            public ChangeDirectoryCommand(KickerOption option)
            {
                _option = option;
            }

            public void Exec(JToken token, string baseDirectory, Logger logger)
            {
                if (token is JValue val && val.Value is not null)
                {
                    var dir = val.Value.ToString();

                    _option.ProjectDirectory = Path.Combine(baseDirectory, dir);
                }
                else throw new FormatException();
            }
        }
    }
}
