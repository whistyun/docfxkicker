using docfxkicker.plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace docfxkicker_translationhelp
{
    public class TranslateCommand : ISubCommand
    {
        public string CommandName => "@docfxkicker_translationhelp";


        public void Exec(JToken configNode, string baseDirectory, Logger logger)
        {
            var option = configNode.ToObject<TranslateCommandOption>();

            if (option is null)
            {
                logger.Log(
                    "failed to parse",
                    LogLevel.Error,
                    "docfxkicker_translationhelp.TranslateCommand");

                return;
            }

            if (option.DestLang is null)
            {
                logger.Log(
                    "DestLang is empty",
                    LogLevel.Error,
                    "docfxkicker_translationhelp.TranslateCommand");
            }
            if (option.TranslationResut is null)
            {
                logger.Log(
                    "TranslationResut is empty",
                    LogLevel.Error,
                    "docfxkicker_translationhelp.TranslateCommand");
            }

            if (option.DestLang is null || option.TranslationResut is null)
            {
                return;
            }

            if (option.SrcLang == option.DestLang)
            {
                logger.Log(
                    "skip translation; SrcLang = DestLang",
                    LogLevel.Info,
                    "docfxkicker_translationhelp.TranslateCommand");

                return;
            }

            if (option.MachineTranslation != "DeepL" && option.MachineTranslation != "Google")
            {
                logger.Log(
                    $"unnsupport machine translation '{option.MachineTranslation}'",
                    LogLevel.Error,
                    "docfxkicker_translationhelp.TranslateCommand");

                return;
            }

            if (option.Files.Count != option.Dests.Count)
            {
                // count unmatch
                logger.Log(
                    "The count of input files is not equals to the count of output files.",
                    LogLevel.Error,
                    "docfxkicker_translationhelp.TranslateCommand");

                return;
            }

            using var result = new TranslationResultTable(Path.Combine(baseDirectory, option.TranslationResut));

            for (var idx = 0; idx < option.Files.Count; ++idx)
            {
                var filePath = option.Files[idx];
                var destPath = option.Dests[idx];


                if (filePath.Contains("**"))
                {
                    var filepathPattern = WildcardToRegex(filePath);
                    var rootdir = Path.Combine(baseDirectory, Regex.Split(filePath, "\\*\\*")[0]);

                    var targets = Directory.GetFiles(rootdir, "*", SearchOption.AllDirectories)
                                           .Where(fpath => filepathPattern.IsMatch(fpath));

                    foreach (var target in targets)
                    {
                        var relpath = target.Substring(rootdir.Length);
                        if (Path.IsPathRooted(relpath)) relpath = relpath.Substring(1);

                        Translate(target, Path.Combine(baseDirectory, destPath, relpath), option, result);
                    }
                }
                else if (filePath.Contains('*'))
                {
                    var filepathPattern = WildcardToRegex(filePath);
                    var rootdir = Path.Combine(baseDirectory, filePath.Split('*')[0]);

                    var targets = Directory.GetFiles(rootdir)
                                           .Where(fpath => filepathPattern.IsMatch(fpath));

                    foreach (var target in targets)
                    {
                        var relpath = target.Substring(rootdir.Length);
                        if (Path.IsPathRooted(relpath)) relpath = relpath.Substring(1);

                        Translate(target, Path.Combine(baseDirectory, destPath, relpath), option, result);
                    }
                }
                else
                {
                    var target = Path.Combine(baseDirectory, filePath);

                    Translate(target, Path.Combine(baseDirectory, destPath), option, result);
                }
            }
        }

        private Regex WildcardToRegex(string wildcard)
        {
            var buf = new StringBuilder();
            foreach (var c in wildcard)
            {
                buf.Append(c switch
                {
                    '?' => ".",
                    '*' => "*",
                    _ => Regex.Escape(c.ToString())
                });
            }

            var rgxptn = buf.ToString()
                            .Replace("**", @"([^/\\]+[/\\]?){0,}")
                            .Replace("*", ".{0,}")
                         + "$";

            return new Regex(rgxptn, RegexOptions.Compiled);
        }


        private void Translate(string inputFile, string outputFile, TranslateCommandOption option, TranslationResultTable result)
        {
            var outputDir = Path.GetDirectoryName(outputFile);
            Directory.CreateDirectory(outputDir);

            switch (Path.GetExtension(inputFile).ToLower())
            {
                case ".yml":
                    TranslateYaml(inputFile, outputFile, option, result);
                    break;

                default:
                    TranslateText(inputFile, outputFile, option, result);
                    break;
            }
        }


        private void TranslateYaml(string inputFile, string outputFile, TranslateCommandOption option, TranslationResultTable result)
        {
            var yaml = new YamlStream();

            string firstLine;

            using (var istream = File.OpenRead(inputFile))
            using (var reader = new StreamReader(istream, new UTF8Encoding()))
            {
                firstLine = reader.ReadLine();
            }

            using (var istream = File.OpenRead(inputFile))
            using (var reader = new StreamReader(istream, new UTF8Encoding()))
            {
                yaml.Load(reader);
            }

            foreach (var doc in yaml)
            {
                Translate(doc.RootNode, option, result);
            }

            if (File.Exists(outputFile)) File.Delete(outputFile);
            using (var ostream = File.OpenWrite(outputFile))
            using (var writer = new StreamWriter(ostream, new UTF8Encoding()))
            {
                // Please add `YamlMime` as the first line of file, e.g.: `### YamlMime:ManagedReference`, otherwise the file will be not treated as ManagedReference source file in near future.
                if (firstLine.StartsWith("#"))
                    writer.WriteLine(firstLine);

                yaml.Save(writer);
            }
        }
        private void TranslateText(string inputFile, string outputFile, TranslateCommandOption option, TranslationResultTable result)
        {
            var text = File.ReadAllText(inputFile, new UTF8Encoding());

            text = Translate(text, option, result);

            if (File.Exists(outputFile)) File.Delete(outputFile);
            using (var ostream = File.OpenWrite(outputFile))
            using (var writer = new StreamWriter(ostream, new UTF8Encoding()))
            {
                writer.Write(text);
            }
        }

        private void Translate(YamlNode node, TranslateCommandOption option, TranslationResultTable result)
        {
            if (node is YamlSequenceNode seq)
            {
                foreach (var child in seq)
                {
                    Translate(child, option, result);
                }
            }
            else if (node is YamlMappingNode map)
            {
                var dic = map.Children;

                foreach (var key in dic.Keys.OfType<YamlScalarNode>().ToArray())
                {
                    switch (key.Value)
                    {
                        case "summary":
                        case "remarks":
                        case "description":
                        case "title":
                        case "text":
                            if (dic[key] is YamlScalarNode val && val.Value is not null)
                            {
                                var targetText = val.Value;
                                dic[key] = Translate(targetText, option, result);
                            }
                            break;

                        default:
                            Translate(dic[key.Value!], option, result);
                            break;
                    }
                }
            }
        }
        private string Translate(string text, TranslateCommandOption option, TranslationResultTable result)
        {
            var srcLang = option.SrcLang!;
            var dstLang = option.DestLang!;

            if (string.IsNullOrWhiteSpace(text)) return text;

            return result.TryGet(text, txt =>
            {
                if (option.MachineTranslation == "DeepL")
                {
                    return DeepLTranslate.Translate(srcLang, text, dstLang).Result!;
                }
                else if (option.MachineTranslation == "Google")
                {
                    return GoogleTranslate.Translate(srcLang, text, dstLang).Result!;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });
        }
    }

    class TranslateCommandOption
    {
        [JsonProperty("files")]
        public List<string> Files { set; get; } = new();

        [JsonProperty("dests")]
        public List<string> Dests { set; get; } = new();

        [JsonProperty("srcLang")]
        public string? SrcLang { set; get; }

        [JsonProperty("destLang")]
        public string? DestLang { set; get; }

        [JsonProperty("machineTranslation")]
        public string? MachineTranslation { set; get; }

        [JsonProperty("translationResult")]
        public string? TranslationResut { set; get; }
    }
}
