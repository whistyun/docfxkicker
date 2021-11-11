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

namespace NuGetHelper
{
    public class URepository
    {
        public const string NuGetOrgUrl = "https://api.nuget.org/v3/index.json";

        private ILogger _logger;
        private ISettings _settings;
        private SourceCacheContext _cache;
        private SourceRepository _repository;
        private string _globalPackagesFolder;

        private CancellationToken _cancel;

        private FrameworkReducer _reducer;


        public URepository() : this(NuGetOrgUrl, NullLogger.Instance)
        {
        }

        public URepository(string repositorySource) : this(repositorySource, NullLogger.Instance)
        {
        }

        public URepository(string repositorySource, ILogger logger)
        {
            _logger = logger;
            _settings = Settings.LoadDefaultSettings(root: null);
            _cache = new SourceCacheContext();
            _repository = Repository.Factory.GetCoreV3(repositorySource);

            _globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(_settings);
            _cancel = new CancellationToken();

            _reducer = new FrameworkReducer();
        }


        #region FindLatestVersionAsync

        public async Task<NuGetVersion> FindLatestVersionAsync(string packageId, bool includePrerelease = false)
            => await FindLatestVersionAsync(packageId, VersionRange.All, includePrerelease);

        public async Task<NuGetVersion> FindLatestVersionAsync(string packageId, VersionRange range, bool includePrerelease = false)
            => await FindLatestVersionAsync(packageId, range, _cancel, includePrerelease);

        public async Task<NuGetVersion> FindLatestVersionAsync(string packageId, VersionRange range, CancellationToken token, bool includePrerelease = false)
        {
            var finder = await _repository.GetResourceAsync<FindPackageByIdResource>();
            IEnumerable<NuGetVersion> versions = await finder.GetAllVersionsAsync(packageId, _cache, _logger, token);

            if (!includePrerelease)
            {
                versions = versions.Where(v => !v.IsPrerelease);
            }

            foreach (var version in versions.OrderByDescending(v => v))
            {
                if (range.Satisfies(version))
                    return version;
            }

            throw new ArgumentException($"{packageId}@{range} is not found");
        }

        #endregion

        #region FindLatestIdentityAsync

        public async Task<PackageIdentity> FindLatestIdentityAsync(string packageId, bool includePrerelease = false)
            => await FindLatestIdentityAsync(packageId, VersionRange.All, includePrerelease);

        public async Task<PackageIdentity> FindLatestIdentityAsync(string packageId, VersionRange range, bool includePrerelease = false)
            => await FindLatestIdentityAsync(packageId, range, _cancel, includePrerelease);

        public async Task<PackageIdentity> FindLatestIdentityAsync(string packageId, VersionRange range, CancellationToken token, bool includePrerelease = false)
            => new PackageIdentity(packageId, await FindLatestVersionAsync(packageId, range, token, includePrerelease));

        #endregion

        #region DownloadPackageAsync

        public async Task<UDownloadResult> DownloadPackageAsync(string packageId)
            => await DownloadPackageAsync(packageId, await FindLatestVersionAsync(packageId));

        public async Task<UDownloadResult> DownloadPackageAsync(string packageId, VersionRange range)
            => await DownloadPackageAsync(packageId, await FindLatestVersionAsync(packageId, range));

        public async Task<UDownloadResult> DownloadPackageAsync(string packageId, string version)
            => await DownloadPackageAsync(new PackageIdentity(packageId, new NuGetVersion(version)));

        public async Task<UDownloadResult> DownloadPackageAsync(string packageId, NuGetVersion version)
            => await DownloadPackageAsync(new PackageIdentity(packageId, version));

        public async Task<UDownloadResult> DownloadPackageAsync(PackageIdentity identity)
            => await DownloadPackageAsync(identity, _cancel);

        public async Task<UDownloadResult> DownloadPackageAsync(PackageIdentity identity, CancellationToken token)
        {
            var downloader = await _repository.GetResourceAsync<DownloadResource>();
            var result = await downloader.GetDownloadResourceResultAsync(
                identity,
                new PackageDownloadContext(_cache),
                _globalPackagesFolder,
                _logger,
                token);

            return new UDownloadResult(identity, result);
        }

        #endregion

        #region FindLocalPackage

        public LocalPackageInfo FindLocalPackage(string packageId, bool includePrerelease = false)
            => FindLocalPackage(packageId, VersionRange.All);

        public LocalPackageInfo FindLocalPackage(string packageId, VersionRange range, bool includePrerelease = false)
        {
            IEnumerable<LocalPackageInfo> infos = LocalFolderUtility.GetPackagesV3(_globalPackagesFolder, packageId, _logger);

            if (!includePrerelease)
            {
                infos = infos.Where(p => !p.Nuspec.GetVersion().IsPrerelease);
            }

            foreach (var info in infos.OrderByDescending(p => p.Nuspec.GetVersion()))
            {
                if (range.Satisfies(info.Nuspec.GetVersion()))
                {
                    return info;
                }
            }

            throw new ArgumentException($"{packageId}@{range} is not found in local");
        }

        public LocalPackageInfo FindLocalPackage(string packageId, string version)
            => FindLocalPackage(packageId, new NuGetVersion(version));

        public LocalPackageInfo FindLocalPackage(string packageId, NuGetVersion version)
            => FindLocalPackage(new PackageIdentity(packageId, version));

        public LocalPackageInfo FindLocalPackage(PackageIdentity identity)
            => LocalFolderUtility.GetPackageV3(_globalPackagesFolder, identity, _logger);

        #endregion

        #region FindPackageAsync

        public async Task<LocalPackageInfo> FindPackageAsync(string packageId, bool includePrerelease = false)
            => await FindPackageAsync(packageId, VersionRange.All);

        public async Task<LocalPackageInfo> FindPackageAsync(string packageId, VersionRange range, bool includePrerelease = false)
            => await FindPackageAsync(packageId, range, _cancel, includePrerelease);

        public async Task<LocalPackageInfo> FindPackageAsync(string packageId, VersionRange range, CancellationToken token, bool includePrerelease = false)
        {
            var version = await FindLatestVersionAsync(packageId, range, token, includePrerelease);
            using var result = await DownloadPackageAsync(new PackageIdentity(packageId, version), token);

            switch (result.Result.Status)
            {
                case DownloadResourceResultStatus.Available:
                case DownloadResourceResultStatus.AvailableWithoutStream:
                    return FindLocalPackage(result.Target);

                default:
                    throw new ArgumentException($"Failed to download {packageId}.");
            }
        }

        public async Task<LocalPackageInfo> FindPackageAsync(string packageId, string version)
            => await FindPackageAsync(packageId, new NuGetVersion(version));

        public async Task<LocalPackageInfo> FindPackageAsync(string packageId, NuGetVersion version)
            => await FindPackageAsync(new PackageIdentity(packageId, version));

        public async Task<LocalPackageInfo> FindPackageAsync(PackageIdentity identity)
            => await FindPackageAsync(identity, _cancel);

        public async Task<LocalPackageInfo> FindPackageAsync(PackageIdentity identity, CancellationToken token)
        {
            using var result = await DownloadPackageAsync(identity);

            switch (result.Result.Status)
            {
                case DownloadResourceResultStatus.Available:
                case DownloadResourceResultStatus.AvailableWithoutStream:
                    return FindLocalPackage(result.Target);

                default:
                    throw new ArgumentException($"Failed to download {identity}.");
            }
        }

        #endregion

        #region DownloadWithDependenciesAsync

        public async Task<UPackageInfo[]> DownloadWithDependenciesAsync(PackageIdentity identity, string requestedFramework)
            => await DownloadWithDependenciesAsync(identity, NuGetFramework.Parse(requestedFramework));

        public async Task<UPackageInfo[]> DownloadWithDependenciesAsync(PackageIdentity identity, NuGetFramework requestedFramework)
            => await DownloadWithDependenciesAsync(identity, requestedFramework, _cancel);

        public async Task<UPackageInfo[]> DownloadWithDependenciesAsync(PackageIdentity identity, NuGetFramework requestedFramework, CancellationToken token)
        {
            var package = await FindPackageAsync(identity);
            var dependencyGroups = package.Nuspec.GetDependencyGroups().ToList();

            var result = new List<UPackageInfo>();
            result.Add(new UPackageInfo(package, _reducer, requestedFramework));

            if (dependencyGroups.Count > 0)
            {
                NuGetFramework depsMatchFramework = _reducer.GetNearest(requestedFramework, dependencyGroups.Select(grp => grp.TargetFramework));

                if (depsMatchFramework is null)
                {
                    throw new ArgumentException("not supported package");
                }

                Task<UPackageInfo[]>[] dependencies =
                    dependencyGroups
                           .Where(grp => Object.Equals(grp.TargetFramework, depsMatchFramework))
                           .First()
                           .Packages
                           .Select(async (pkg) => new PackageIdentity(
                                                         pkg.Id,
                                                         await FindLatestVersionAsync(pkg.Id, pkg.VersionRange, token)))
                           .Select(async (depId) => await DownloadWithDependenciesAsync(await depId, requestedFramework, token))
                           .ToArray();


                Task.WaitAll(dependencies);
                result.AddRange(dependencies.SelectMany(dep => dep.Result).Distinct());
            }

            return result.ToArray();
        }

        #endregion

        #region FindAssemblyAsync

        public async Task<Assembly[]> FindAssemblyAsync(PackageIdentity identity, string requestedFramework)
            => await FindAssemblyAsync(identity, NuGetFramework.Parse(requestedFramework));

        public async Task<Assembly[]> FindAssemblyAsync(PackageIdentity identity, NuGetFramework nuGetFramework)
        {
            var infos = await DownloadWithDependenciesAsync(identity, nuGetFramework);
            var dlls = infos.SelectMany(inf => inf.AssemblyPaths).ToArray();

            var targetInfo = infos.Where(info => Object.Equals(info.Identity, identity)).First();

            var loader = new FilePathListAssemblyLoaderContext(dlls);

            return targetInfo.AssemblyPaths
                             .Select(path => loader.LoadFromAssemblyName(AssemblyName.GetAssemblyName(path)))
                             .ToArray();
        }

        #endregion
    }
}
