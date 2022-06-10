using Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docfxkicker_translationhelp
{
    internal class TranslationResultTable : IDisposable
    {
        private const string SrcLang = nameof(SrcLang);
        private const string DstLang = nameof(DstLang);

        public string Filepath { get; }

        private List<string> SrcLangOrder;
        private Dictionary<string, string> TranslationMap;

        public TranslationResultTable(string csvfilepath)
        {
            Filepath = csvfilepath;
            SrcLangOrder = new();
            TranslationMap = new();


            var readPath =
                File.Exists(Filepath + ".back") ? Filepath + ".back" :
                File.Exists(Filepath) ? Filepath :
                null;

            if (readPath is not null)
            {
                var csv = File.ReadAllText(readPath);
                var options = new CsvOptions { AllowNewLineInEnclosedFieldValues = true };
                foreach (var line in CsvReader.ReadFromText(csv, options))
                {
                    if (line.Values.Count() < 2)
                        continue;

                    var srcLang = line[SrcLang];
                    srcLang = srcLang.Trim().Replace("\r\n", "\n").Replace("\r", "\n");

                    SrcLangOrder.Add(srcLang);
                    TranslationMap[srcLang] = line[DstLang];
                }
            }
        }

        public string TryGet(string source, Func<string, string> translate)
        {
            var srcLang = source;
            srcLang = srcLang.Trim().Replace("\r\n", "\n").Replace("\r", "\n");

            if (TranslationMap.TryGetValue(srcLang, out var result))
                return result.Trim().Replace("\r\n", "\n").Replace("\r", "\n");

            SrcLangOrder.Add(srcLang);
            return TranslationMap[srcLang] = translate(srcLang).Trim().Replace("\r\n", "\n").Replace("\r", "\n");
        }



        public void Save()
        {
            if (File.Exists(Filepath))
                File.Move(Filepath, Filepath + ".back");

            using (var stream = File.OpenWrite(Filepath))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
            {
                var lines = SrcLangOrder.Select(sl => new[] { sl, TranslationMap[sl] });
                CsvWriter.Write(writer, new[] { SrcLang, DstLang }, lines);
            }

            if (File.Exists(Filepath + ".back"))
                File.Delete(Filepath + ".back");
        }

        public void Dispose() => Save();
    }
}
