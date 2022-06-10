using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker.plugin
{
    public class Logger
    {
        private string _logfile;


        public Logger(string logfile)
        {
            _logfile = logfile;
        }


        public void Log(string message, LogLevel lv, string source)
        {
            using var stream = new FileStream(_logfile, FileMode.Create);
            using var writer = new StreamWriter(stream, new UTF8Encoding());

            string now = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture);

            writer.WriteLine(
                $@"{{""message"":""{message}"",""source"":""{source}"",""date_time"":""{now}"",""message_severity"":""{lv.ToString().ToLower()}"",}}"
            );
        }

        private string Escape(string text)
            => text.Replace("\\", "\\\\")
                   .Replace("\t", "\\t")
                   .Replace("\r", "\\r")
                   .Replace("\n", "\\n")
                   .Replace("\"", "\\\"");
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
    }
}
