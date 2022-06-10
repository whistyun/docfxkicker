using System.IO;

namespace docfxkicker
{
    public class KickerOption
    {
        public KickerOption(KickerOption copyfrom)
        {
            ToolPrefix = copyfrom.ToolPrefix;
            DocFxExe = copyfrom.DocFxExe;
            ConfigFilePath = copyfrom.ConfigFilePath;
            ProjectDirectory = copyfrom.ProjectDirectory;
            LogFilePath = copyfrom.LogFilePath;
            TemplateDirectory = copyfrom.TemplateDirectory;
        }

        public KickerOption(
            string toolPrefix,
            string docfxExe,
            string configFilePath,
            string logFilePath,
            string templateDirectory)
        {
            ToolPrefix = toolPrefix;
            DocFxExe = docfxExe;
            ConfigFilePath = configFilePath;
            ProjectDirectory = Path.GetDirectoryName(configFilePath);
            LogFilePath = Path.Combine(ProjectDirectory, logFilePath);
            TemplateDirectory = templateDirectory;
        }

        public string ToolPrefix { get; }
        public string DocFxExe { get; }
        public string ConfigFilePath { get; }
        public string ProjectDirectory { get; set; }
        public string LogFilePath { get; }
        public string TemplateDirectory { get; }
    }
}
