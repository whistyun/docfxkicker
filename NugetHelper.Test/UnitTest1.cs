using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NuGetHelper.test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SearchLatestVersion()
        {
            var repoUri = Path.GetFullPath("./repos");

            var repo = new URepository(repoUri);

            {
                var idTask = repo.SearchLatestIdentityAsync("whistyun.dummy.latestver");
                idTask.Wait();
                Assert.AreEqual(new NuGetVersion("1.3.0"), idTask.Result.Version);
            }

            {
                var idTask = repo.SearchLatestIdentityAsync("whistyun.dummy.latestver", true);
                idTask.Wait();
                Assert.AreEqual(new NuGetVersion("1.4.0-a"), idTask.Result.Version);
            }

            {
                var range = new VersionRange(
                    minVersion: new NuGetVersion("1.0.0"),
                    maxVersion: new NuGetVersion("1.1.0"));

                var idTask = repo.SearchLatestIdentityAsync("whistyun.dummy.latestver", range);
                idTask.Wait();
                Assert.AreEqual(new NuGetVersion("1.0.2"), idTask.Result.Version);
            }
        }


        [TestMethod]
        public void FailedSearchLatestVersion()
        {
            var repoUri = Path.GetFullPath("./repos");

            var repo = new URepository(repoUri);

            {
                var idTask = repo.SearchLatestIdentityAsync("whistyun.dummy.latestver.notfound");
                idTask.Wait();
                Assert.IsFalse(idTask.Result.HasVersion);
            }

            {
                var version = repo.SearchLatestVersionAsync("whistyun.dummy.latestver.notfound");
                version.Wait();
                Assert.IsNull(version.Result);
            }
        }


        [TestMethod]
        public void DownloadTest()
        {
            var repoUri = Path.GetFullPath("./repos");

            var repo = new URepository(repoUri);

#if NETCOREAPP
            var framework = NuGetFramework.Parse("netcoreapp3.1");

            var identityTask = repo.SearchLatestIdentityAsync("whistyun.dummy.frm1");
            identityTask.Wait();
            var depsTask = repo.FindPackageWithDependenciesAsync(identityTask.Result, framework);
            depsTask.Wait();

            var actual = new string[] { "whistyun.dummy.frm1", "whistyun.dummy.subfrm_core" };
            foreach (var id in depsTask.Result.Select(asm => asm.Identity.Id))
            {
                Assert.IsTrue(actual.Contains(id), id);
            }


#elif NETFRAMEWORK
            var framework = NuGetFramework.Parse("net461");

            var identityTask = repo.SearchLatestIdentityAsync("whistyun.dummy.frm1");
            identityTask.Wait();
            var depsTask = repo.FindPackageWithDependenciesAsync(identityTask.Result, framework);
            depsTask.Wait();

            var actual = new string[] { "whistyun.dummy.frm1", "whistyun.dummy.subfrm_frmwk" };
            foreach (var id in depsTask.Result.Select(asm => asm.Identity.Id))
            {
                Assert.IsTrue(actual.Contains(id), id);
            }
#endif
        }
    }
}
