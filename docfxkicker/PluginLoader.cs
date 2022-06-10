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
        private URepositories _repos;

        public const string Framework = "net472";

        public PluginLoader()
        {
            _repos = new URepositories();
        }

        public PluginLoader(PluginLoader copyFrom)
        {
            _repos = new URepositories(copyFrom._repos);
        }

        public void AddRepoitory(string url) => _repos.AddRepository(url);

        /// <summary>
        /// Download nuget package to get ISubCommand.
        /// </summary>
        /// <param name="packageId">nuget package id</param>
        public IEnumerable<ISubCommand> Load(string packageId) => Load(packageId, null);

        /// <summary>
        /// Download nuget package to get ISubCommand.
        /// </summary>
        public IEnumerable<ISubCommand> Load(string packageId, string? versionTxt)
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

        private async Task<TypeInfo[]> LoadAsync(string packageId, string? versionTxt)
        {
            PackageIdentity identity = new(packageId, versionTxt is not null ? new NuGetVersion(versionTxt) : null);

            var asms = await _repos.FindAssemblyAsync(identity, Framework);


            return asms is null ?
                    new TypeInfo[0] :
                    Filter(asms);
        }

        private TypeInfo[] Filter(Assembly[] asms)
        {
            return asms.SelectMany(asm => asm.DefinedTypes)
                       .Where(tinf => !(tinf.IsValueType | tinf.IsEnum | tinf.IsInterface | tinf.IsGenericType))
                       .Where(tinf => typeof(ISubCommand).IsAssignableFrom(tinf))
                       .ToArray();
        }

        /// <summary>
        /// Download docfx.console
        /// </summary>
        /// <returns>downloaded docfx.console package</returns>
        public static LocalPackageInfo? SetupDocfxConsole()
        {
            var repo = new URepositories(URepository.NuGetOrgUrl);
            var identity = new PackageIdentity("docfx.console", new NuGetVersion("2.59.1"));
            var framework = NuGetFramework.Parse(Framework);

            var result = repo.FindPackageWithDependenciesAsync(identity, framework).Result;

            return result?.Primary?.RawInfo;
        }
    }
}
