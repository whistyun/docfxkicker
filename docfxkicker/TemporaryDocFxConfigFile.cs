using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace docfxkicker
{
    internal class TemporaryDocFxConfigFile : IDisposable
    {
        public string TargetPath { get; }

        public TemporaryDocFxConfigFile(string directory, string extension, DocFxConfigNode content)
        {
            while (true)
            {
                FileStream stream;
                try
                {
                    var id = Guid.NewGuid();
                    var newfile = Path.Combine(directory, id + extension);
                    stream = new FileStream(newfile, FileMode.CreateNew);
                    TargetPath = newfile;
                }
                catch
                {
                    continue;
                }

                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                using (var jwriter = new JsonTextWriter(writer))
                {
                    jwriter.Formatting = Formatting.Indented;
                    content.ConfigObject.WriteTo(jwriter);
                }
                break;
            }
        }


        public void Dispose()
        {
            File.Delete(TargetPath);
        }
    }
}
