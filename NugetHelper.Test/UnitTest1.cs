using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NuGetHelper.test
{
    [TestClass]
    public class UnitTest1
    {
#if NETCOREAPP
        private const string Framework = "netcoreapp3.1";
#elif NETFRAMEWORK
        private const string Framework = "net461";
#endif

        [TestMethod]
        public void FindLatestIdentity()
        {
            var repo = new URepository();
            var idTask = repo.FindLatestIdentityAsync("Markdown.Avalonia");

            idTask.Wait();
            Assert.AreEqual(TaskStatus.RanToCompletion, idTask.Status);
        }

        [TestMethod]
        public void FindAsms()
        {
            var repo = new URepository();
            var idTask = repo.FindLatestIdentityAsync("Markdown.Avalonia");

            idTask.Wait();

            var asmTask = repo.FindAssemblyAsync(idTask.Result, Framework);

            asmTask.Wait();
            Assert.AreEqual(TaskStatus.RanToCompletion, asmTask.Status);
            Assert.AreEqual(2, asmTask.Result.Count());

            var asm = asmTask.Result
                             .Where(a => a.GetName().Name == "Markdown.Avalonia")
                             .First();

            var types = asm.DefinedTypes;

            var type = asm.GetType("Markdown.Avalonia.MarkdownScrollViewer");
            Assert.IsNotNull(type);
        }

        [TestMethod]
        public void FindAsms2()
        {
            var repo = new URepository();
            var idTask = repo.FindLatestIdentityAsync("Markdown.Xaml");
            idTask.Wait();

#if NETCOREAPP
            try
            {
                var asmTask = repo.DownloadWithDependenciesAsync(idTask.Result, Framework);
                asmTask.Wait();
                Assert.Fail();
            }
            catch (AggregateException e)
            {
                Assert.AreEqual("not supported package", e.InnerException.Message);
            }

#elif NETFRAMEWORK
            var asmTask = repo.DownloadWithDependenciesAsync(idTask.Result, Framework);

            asmTask.Wait();
            Assert.AreEqual(TaskStatus.RanToCompletion, asmTask.Status);
            Assert.AreEqual(1, asmTask.Result.Count());
#endif
        }
    }
}
