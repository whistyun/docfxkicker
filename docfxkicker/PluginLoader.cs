using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using NuGet.Frameworks;
using System.IO;
using NuGet.Packaging;
using NuGet.Configuration;
using NuGetHelper;
using NuGet.Packaging.Core;
using System.Reflection;
using docfxkicker.plugin;
using System.Diagnostics;
using System.Threading.Tasks;

namespace docfxkicker
{
    class PluginLoader
    {
        public const string Framework = "netcoreapp3.1";

        public static void Load(string packageId) => Load(packageId, null);
        public static IEnumerable<ISubCommand> Load(string packageId, string versionTxt)
        {
            Task<TypeInfo[]> typesTask = LoadAsync(packageId, versionTxt);
            typesTask.Wait();

            var results = new List<ISubCommand>();

            foreach (var type in typesTask.Result)
            {
                try
                {
                    results.Add((ISubCommand)Activator.CreateInstance(type));
                }
                catch
                {
                    Debug.Print($"Failed to create instance of '{type.FullName}'");
                }
            }

            return results;
        }

        public static LocalPackageInfo SetupDocfxConsole()
        {
            var repo = new URepository();
            var identity = new PackageIdentity("docfx.console", new NuGetVersion("2.58.8"));

            repo.DownloadPackageAsync(identity).Wait();

            return repo.FindLocalPackage(identity);
        }


        private static async Task<TypeInfo[]> LoadAsync(string packageId, string versionTxt)
        {
            var repo = new URepository();

            var identity = versionTxt is null ?
                await repo.FindLatestIdentityAsync(packageId) :
                new PackageIdentity(packageId, NuGetVersion.Parse(versionTxt));

            Assembly[] asms = await repo.FindAssemblyAsync(identity, Framework);
            TypeInfo[] types = asms.SelectMany(asm => asm.DefinedTypes)
                                   .Where(tinf => !(tinf.IsValueType | tinf.IsEnum | tinf.IsInterface | tinf.IsGenericType))
                                   .Where(tinf => typeof(ISubCommand).IsAssignableFrom(tinf))
                                   .ToArray();

            return types;
        }
    }
}
