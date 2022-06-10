using docfxkicker.plugin;
using System;
using System.IO;
using System.Linq;

namespace docfxkicker
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine($"No subcommand input!");
                PrintHelpDoc();
                return -1;
            }

            var options = args.Skip(1).ToArray();

            switch (args[0])
            {
                case "init":
                    return Init(options);

                case "build":
                    return Build(options);

                default:
                    Console.Error.WriteLine($"Unknown subcommand!");
                    PrintHelpDoc();
                    return -1;
            }
        }

        static void PrintHelpDoc()
        {
            Console.WriteLine(LoadResource("helpdoc.txt"));
        }

        static int Init(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Paremeter count do not match!");
                PrintHelpDoc();
                return -1;
            }

            var projectDir = args[0];
            var projectName = Directory.GetFiles(projectDir, "*.??proj")
                                       .Select(path => Path.GetFileNameWithoutExtension(path))
                                       .FirstOrDefault()
                              ?? Path.GetDirectoryName(Path.Combine(projectDir, "dummy"));


            var configContent = LoadResource("docfxkicker.json").Replace("%projectName%", projectName);

            File.WriteAllText(Path.Combine(projectDir, "docfxkicker.json"), configContent);

            return 0;
        }

        static int Build(string[] args)
        {
            if (args.Length != 4)
            {
                Console.Error.WriteLine("Paremeter count do not match!");
                PrintHelpDoc();
                return -1;
            }

            var docfxConsole = PluginLoader.SetupDocfxConsole();
            if (docfxConsole is null)
            {
                throw new Exception("Failed to load docfx.console");
            }
            var docfxExe = Path.Combine(Path.GetDirectoryName(docfxConsole.Path), "tools", "docfx.exe");


            var option = new KickerOption(
                toolPrefix: args[0],
                docfxExe: docfxExe,
                configFilePath: args[1],
                logFilePath: args[2],
                templateDirectory: args[3]);

            var logger = new Logger(option.LogFilePath);
            var process = new KickerProcess(option);
            process.Execute(logger);

            return 0;
        }


        static string LoadResource(string resourceName)
        {
            var assembly = typeof(Program).Assembly;
            var stream = assembly.GetManifestResourceStream($"docfxkicker.{resourceName}");
            var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
    }
}
