using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Configuration;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using System.Reflection;
using NuGetHelper.Loader;
using System.IO;
using NuGet.Packaging;

namespace NuGetHelper
{
    public class URepositories
    {
        private readonly ILogger _logger;
        private readonly LocalPackage _local;
        private readonly CancellationToken _cancel;
        private readonly FrameworkReducer _reducer;
        private readonly List<URepository> _repos;

        public URepositories(URepositories copyFrom) : this(copyFrom._repos[0], copyFrom._logger)
        {
            _repos.AddRange(copyFrom._repos.Skip(1));
        }

        public URepositories() :
            this(URepository.NuGetOrgUrl, NullLogger.Instance)
        { }

        public URepositories(string repositoryUrl) :
            this(repositoryUrl, NullLogger.Instance)
        { }

        public URepositories(string repositoryUrl, ILogger logger) :
            this(new URepository(repositoryUrl, logger), logger)
        { }

        public URepositories(URepository repo, ILogger logger)
        {
            _repos = new();
            _repos.Add(repo);
            _logger = logger;
            _local = new LocalPackage(logger);
            _cancel = new CancellationToken();
            _reducer = new FrameworkReducer();
        }

        public void AddRepository(string repositoryUrl)
            => AddRepository(new URepository(repositoryUrl, _logger));

        public void AddRepository(URepository repo)
            => _repos.Add(repo);

        #region DownloadWithDependenciesAsync

        public Task<UPackageInfoCollection?> FindPackageWithDependenciesAsync(PackageIdentity identity, string requestedFramework)
        {
            Check.NotNull(nameof(identity), identity);
            Check.NotNull(nameof(requestedFramework), requestedFramework);

            return PrivateDownloadWithDependenciesAsync(identity, NuGetFramework.Parse(requestedFramework), _cancel);
        }

        public Task<UPackageInfoCollection?> FindPackageWithDependenciesAsync(PackageIdentity identity, NuGetFramework requestedFramework)
        {
            Check.NotNull(nameof(identity), identity);
            Check.NotNull(nameof(requestedFramework), requestedFramework);

            return PrivateDownloadWithDependenciesAsync(identity, requestedFramework, _cancel);
        }

        public Task<UPackageInfoCollection?> FindPackageWithDependenciesAsync(PackageIdentity identity, NuGetFramework requestedFramework, CancellationToken token)
        {
            Check.NotNull(nameof(identity), identity);
            Check.NotNull(nameof(requestedFramework), requestedFramework);

            return PrivateDownloadWithDependenciesAsync(identity, requestedFramework, token);
        }

        private async Task<UPackageInfoCollection?> PrivateDownloadWithDependenciesAsync(PackageIdentity identity, NuGetFramework requestedFramework, CancellationToken token)
        {
            var package = await FindPackageAsync(identity, token);
            if (package is null)
                return null;


            var asmpaths = ParseAssemblyPaths(package, _reducer, requestedFramework);
            if (asmpaths is null)
            {
                _logger.LogWarning($"'{identity}' dose not support {requestedFramework}");
                return null;
            }

            var result = new UPackageInfoCollection(new UPackageInfo(package, requestedFramework, asmpaths));

            var depGroups = package.Nuspec.GetDependencyGroups().ToArray();
            if (depGroups.Length == 0)
                return result;

            var match = _reducer.GetNearest(requestedFramework, depGroups.Select(grp => grp.TargetFramework));
            if (match is null)
            {
                _logger.LogWarning("not supported package");
                return null;
            }


            var grp = depGroups.Where(grp => Object.Equals(grp.TargetFramework, match)).First();
            var depTsks = grp.Packages
                             .Select(async dep =>
                             {
                                 var local = await FindPackageAsync(dep.Id, dep.VersionRange, token);
                                 if (local is null)
                                     return null;

                                 var info = await PrivateDownloadWithDependenciesAsync(local.Identity, requestedFramework, token);
                                 return info;
                             })
                             .ToArray();

            foreach (var depTsk in depTsks)
            {
                var dep = await depTsk;
                if (dep is null) return null;

                result.AddRange(dep);
            }

            return result;
        }
        #endregion

        #region FindAssemblyAsync

        public Task<Assembly[]?> FindAssemblyAsync(PackageIdentity identity, string requestedFramework)
        {
            Check.NotNull(nameof(identity), identity);
            Check.NotNull(nameof(requestedFramework), requestedFramework);

            return PrivateFindAssemblyAsync(identity, NuGetFramework.Parse(requestedFramework));
        }

        public Task<Assembly[]?> FindAssemblyAsync(PackageIdentity identity, NuGetFramework framework)
        {
            Check.NotNull(nameof(identity), identity);
            Check.NotNull(nameof(framework), framework);

            return PrivateFindAssemblyAsync(identity, framework);
        }

        private async Task<Assembly[]?> PrivateFindAssemblyAsync(PackageIdentity identity, NuGetFramework framework)
        {
            var infos = await FindPackageWithDependenciesAsync(identity, framework);
            if (infos is null) return null;

            var dlls = infos.SelectMany(inf => inf.AssemblyPaths).ToArray();

            var loader = new FilePathListAssemblyLoaderContext(dlls);

            return infos.Primary
                        .AssemblyPaths
                        .Select(path => loader.LoadFromAssemblyName(AssemblyName.GetAssemblyName(path)))
                        .ToArray();
        }

        #endregion

        private async Task<LocalPackageInfo?> FindPackageAsync(string packageId, VersionRange range, CancellationToken token)
        {
            var first = _repos.Select(async r => new { Repo = r, Identity = await r.SearchLatestIdentityAsync(packageId, range, token) })
                              .ToArray()
                              .Select(set => set.Result)
                              .Where(set => set.Identity.HasVersion)
                              .OrderByDescending(set => set.Identity.Version)
                              .FirstOrDefault();

            if (first is null)
            {
                _logger.LogWarning($"'{packageId}@{range}' is not found");
                return null;
            }

            return await first.Repo.FindPackageAsync(first.Identity);
        }

        private async Task<LocalPackageInfo?> FindPackageAsync(PackageIdentity identity, CancellationToken token)
        {
            if (!identity.HasVersion)
            {
                return await FindPackageAsync(identity.Id, VersionRange.All, token);
            }

            var localpackage = _local.FindLocalPackage(identity);
            if (localpackage is not null)
                return localpackage;

            foreach (var repo in _repos)
            {
                var result = await repo.DownloadPackageAsync(identity);
                if (result.Info is not null)
                    return result.Info;
            }

            return null;
        }

        private IReadOnlyList<string>? ParseAssemblyPaths(LocalPackageInfo rawInfo, FrameworkReducer reducer, NuGetFramework framework)
        {
            var directory = Path.GetDirectoryName(rawInfo.Path);

            var grps = rawInfo.GetReader().GetLibItems();

            if (grps.Count() == 0)
            {
                return new List<string>().AsReadOnly();
            }
            else
            {
                var matched = reducer.GetNearest(framework, grps.Select(grp => grp.TargetFramework));
                var matchGrp = grps.Where(grp => Object.Equals(grp.TargetFramework, matched))
                                   .FirstOrDefault();

                if (matchGrp is null)
                    return null;

                return matchGrp.Items
                               .Where(itm => Path.GetExtension(itm).ToLower().Equals(".dll"))
                               .Select(itm => Path.Combine(directory!, itm))
                               .ToList()
                               .AsReadOnly();
            }
        }
    }
}
