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
    public class URepository
    {
        public const string NuGetOrgUrl = "https://api.nuget.org/v3/index.json";

        private ILogger _logger;
        private SourceCacheContext _cache;
        private SourceRepository _repository;
        private string _globalPackagesFolder;

        private CancellationToken _cancel;

        private FrameworkReducer _reducer;


        public URepository() :
            this(NuGetOrgUrl, NullLogger.Instance)
        {
        }

        public URepository(string repositorySource) :
            this(repositorySource, NullLogger.Instance)
        {
        }

        public URepository(string repositorySource, ILogger logger)
        {
            _logger = logger;
            _cache = new SourceCacheContext();
            _repository = Repository.Factory.GetCoreV3(repositorySource);
            _cancel = new CancellationToken();
            _reducer = new FrameworkReducer();

            _globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(root: null));
        }

        #region SearchLatestVersionAsync

        /// <summary>
        /// find latest version
        /// </summary>
        /// <param name="packageId">target package id</param>
        /// <param name="includePrerelease">Whether to include the pre-release version. true: include, false: exclude.</param>
        /// <returns>the latest version</returns>
        public Task<NuGetVersion?> SearchLatestVersionAsync(string packageId, bool includePrerelease = false)
        {
            CheckNull(nameof(packageId), packageId);

            return PrivateSearchLatestVersionAsync(packageId, VersionRange.All, _cancel, includePrerelease);
        }

        /// <summary>
        /// find latest version in specified version range.
        /// </summary>
        /// <param name="packageId">target package id</param>
        /// <param name="range">version range</param>
        /// <param name="includePrerelease">Whether to include the pre-release version. true: include, false: exclude.</param>
        /// <returns>the latest version</returns>
        public Task<NuGetVersion?> SearchLatestVersionAsync(string packageId, VersionRange range, bool includePrerelease = false)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(range), range);

            return PrivateSearchLatestVersionAsync(packageId, range, _cancel, includePrerelease);
        }

        /// <summary>
        /// find latest version in specified version range.
        /// </summary>
        /// <param name="packageId">target package id</param>
        /// <param name="range">version range</param>
        /// <param name="includePrerelease">Whether to include the pre-release version. true: include, false: exclude.</param>
        /// <returns>the latest version</returns>
        public Task<NuGetVersion?> SearchLatestVersionAsync(string packageId, VersionRange range, CancellationToken token, bool includePrerelease = false)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(range), range);

            return PrivateSearchLatestVersionAsync(packageId, range, token, includePrerelease);
        }

        private async Task<NuGetVersion?> PrivateSearchLatestVersionAsync(string packageId, VersionRange range, CancellationToken token, bool includePrerelease = false)
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

            return null;
        }

        #endregion

        #region FindLatestIdentityAsync

        /// <summary>
        /// find package identity with latest version.
        /// </summary>
        /// <param name="packageId">target package id</param>
        /// <param name="includePrerelease">Whether to include the pre-release version. true: include, false: exclude.</param>
        public Task<PackageIdentity> SearchLatestIdentityAsync(string packageId, bool includePrerelease = false)
        {
            CheckNull(nameof(packageId), packageId);

            return PrivateSearchLatestIdentityAsync(packageId, VersionRange.All, _cancel, includePrerelease);
        }

        /// <summary>
        /// find package identity with latest version in specified version range.
        /// </summary>
        /// <param name="packageId">target package id</param>
        /// <param name="range">version range</param>
        /// <param name="includePrerelease">Whether to include the pre-release version. true: include, false: exclude.</param>
        public Task<PackageIdentity> SearchLatestIdentityAsync(string packageId, VersionRange range, bool includePrerelease = false)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(range), range);

            return PrivateSearchLatestIdentityAsync(packageId, range, _cancel, includePrerelease);
        }

        /// <summary>
        /// find package identity with latest version in specified version range.
        /// </summary>
        /// <param name="packageId">target package id</param>
        /// <param name="range">version range</param>
        /// <param name="includePrerelease">Whether to include the pre-release version. true: include, false: exclude.</param>
        public Task<PackageIdentity> SearchLatestIdentityAsync(string packageId, VersionRange range, CancellationToken token, bool includePrerelease = false)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(range), range);

            return PrivateSearchLatestIdentityAsync(packageId, range, token, includePrerelease);
        }

        private async Task<PackageIdentity> PrivateSearchLatestIdentityAsync(string packageId, VersionRange range, CancellationToken token, bool includePrerelease = false)
        {
            var latestVersion = await SearchLatestVersionAsync(packageId, range, token, includePrerelease);
            return new PackageIdentity(packageId, latestVersion);
        }

        #endregion

        #region FindLocalPackage

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(string packageId, bool includePrerelease = false)
            => FindLocalPackage(packageId, VersionRange.All, includePrerelease);

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(string packageId, VersionRange range, bool includePrerelease = false)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(range), range);

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

            return null;
        }

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(string packageId, string version)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, new NuGetVersion(version));
            return LocalFolderUtility.GetPackageV3(_globalPackagesFolder, identity, _logger);
        }

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(string packageId, NuGetVersion version)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, version);
            return LocalFolderUtility.GetPackageV3(_globalPackagesFolder, identity, _logger);
        }

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(PackageIdentity identity)
        {
            CheckNull(nameof(identity), identity);

            return LocalFolderUtility.GetPackageV3(_globalPackagesFolder, identity, _logger);
        }

        #endregion

        #region DownloadPackageAsync

        public Task<UDownloadResult> DownloadPackageAsync(string packageId)
        {
            CheckNull(nameof(packageId), packageId);

            return PrivateDownloadPackageAsync(packageId, VersionRange.All);
        }

        public Task<UDownloadResult> DownloadPackageAsync(string packageId, VersionRange range)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(range), range);

            return PrivateDownloadPackageAsync(packageId, range);
        }

        public Task<UDownloadResult> DownloadPackageAsync(string packageId, string version)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, new NuGetVersion(version));
            return PrivateDownloadPackageAsync(identity, _cancel);
        }

        public Task<UDownloadResult> DownloadPackageAsync(string packageId, NuGetVersion version)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, version);
            return PrivateDownloadPackageAsync(identity, _cancel);
        }

        public Task<UDownloadResult> DownloadPackageAsync(PackageIdentity identity)
        {
            CheckNull(nameof(identity), identity);

            return PrivateDownloadPackageAsync(identity, _cancel);
        }

        public Task<UDownloadResult> DownloadPackageAsync(PackageIdentity identity, CancellationToken token)
        {
            CheckNull(nameof(identity), identity);

            return PrivateDownloadPackageAsync(identity, token);
        }

        private async Task<UDownloadResult> PrivateDownloadPackageAsync(string packageId, VersionRange range)
        {
            var ver = await SearchLatestVersionAsync(packageId, range);
            if (ver is null) return UDownloadResult.Failed(packageId);

            return await DownloadPackageAsync(packageId, ver);
        }

        private async Task<UDownloadResult> PrivateDownloadPackageAsync(PackageIdentity identity, CancellationToken token)
        {
            if (!identity.HasVersion)
            {
                var identity2 = await SearchLatestIdentityAsync(identity.Id, VersionRange.All, token);
                if (identity2 is null)
                    return UDownloadResult.Failed(identity.Id);

                identity = identity2;
            }

            var context = new PackageDownloadContext(_cache);

            var downloader = await _repository.GetResourceAsync<DownloadResource>();
            var result = await downloader.GetDownloadResourceResultAsync(
                identity,
                context,
                _globalPackagesFolder,
                _logger,
                token);

            if (result.Status != DownloadResourceResultStatus.Available)
            {
                return new UDownloadResult(identity, result.Status);
            }

            var localPack = FindLocalPackage(identity);

            if (localPack is null)
            {
                await GlobalPackagesFolderUtility.AddPackageAsync(
                    result.PackageSource,
                    identity,
                    result.PackageStream,
                    _globalPackagesFolder,
                    context.ParentId,
                    context.ClientPolicyContext,
                    _logger,
                    token);

                localPack = FindLocalPackage(identity);
            }

            if (localPack is null)
            {
                return new UDownloadResult(identity, DownloadResourceResultStatus.AvailableWithoutStream);
            }
            else
            {
                return new UDownloadResult(identity, localPack);
            }
        }

        #endregion


        #region FindPackageAsync

        public Task<LocalPackageInfo?> FindPackageAsync(string packageId, NuGetVersion version)
        {
            CheckNull(nameof(packageId), packageId);
            CheckNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, version);
            return PrivateFindPackageAsync(identity, _cancel);
        }

        public Task<LocalPackageInfo?> FindPackageAsync(PackageIdentity identity)
        {
            CheckNull(nameof(identity), identity);

            return PrivateFindPackageAsync(identity, _cancel);
        }


        private async Task<LocalPackageInfo?> PrivateFindPackageAsync(PackageIdentity identity, CancellationToken token)
        {
            if (!identity.HasVersion)
            {
                var identity2 = await SearchLatestIdentityAsync(identity.Id, VersionRange.All, token);
                if (identity2 is null)
                    return null;

                identity = identity2;
            }

            var info = FindLocalPackage(identity);
            if (info is not null)
                return info;

            return (await DownloadPackageAsync(identity, token)).Info;
        }

        #endregion



        #region DownloadWithDependenciesAsync

        public Task<UPackageInfo[]?> FindPackageWithDependenciesAsync(PackageIdentity identity, string requestedFramework)
        {
            CheckNull(nameof(identity), identity);
            CheckNull(nameof(requestedFramework), requestedFramework);

            return PrivateDownloadWithDependenciesAsync(identity, NuGetFramework.Parse(requestedFramework), _cancel);
        }

        public Task<UPackageInfo[]?> FindPackageWithDependenciesAsync(PackageIdentity identity, NuGetFramework requestedFramework)
        {
            CheckNull(nameof(identity), identity);
            CheckNull(nameof(requestedFramework), requestedFramework);

            return PrivateDownloadWithDependenciesAsync(identity, requestedFramework, _cancel);
        }

        public Task<UPackageInfo[]?> FindPackageWithDependenciesAsync(PackageIdentity identity, NuGetFramework requestedFramework, CancellationToken token)
        {
            CheckNull(nameof(identity), identity);
            CheckNull(nameof(requestedFramework), requestedFramework);

            return PrivateDownloadWithDependenciesAsync(identity, requestedFramework, token);
        }

        private async Task<UPackageInfo[]?> PrivateDownloadWithDependenciesAsync(PackageIdentity identity, NuGetFramework requestedFramework, CancellationToken token)
        {
            var package = await FindPackageAsync(identity);
            if (package is null)
                return null;

            var asmpaths = ParseAssemblyPaths(package, _reducer, requestedFramework);
            if (asmpaths is null)
            {
                _logger.LogWarning($"'{identity}' dose not support {requestedFramework}");
                return null;
            }

            var result = new List<UPackageInfo>
            {
                new UPackageInfo(package,  requestedFramework, asmpaths)
            };

            var depGroups = package.Nuspec.GetDependencyGroups().ToArray();
            if (depGroups.Length == 0)
                return result.ToArray();

            var match = _reducer.GetNearest(requestedFramework, depGroups.Select(grp => grp.TargetFramework));
            if (match is null)
            {
                _logger.LogWarning("not supported package");
                return null;
            }

            var depTasks = depGroups
                       .Where(grp => Object.Equals(grp.TargetFramework, match))
                       .First()
                       .Packages
                       .Select(async (pkg) =>
                       {
                           var version = await SearchLatestVersionAsync(pkg.Id, pkg.VersionRange, token);
                           if (version is null) return null;

                           var identity = new PackageIdentity(pkg.Id, version);

                           var result = await FindPackageWithDependenciesAsync(identity, requestedFramework, token);
                           if (result is null) return null;

                           return result;
                       })
                       .ToArray();

            foreach (var depTask in depTasks)
            {
                var dep = await depTask;
                if (dep is null) return null;

                result.AddRange(dep);
            }

            return result.Distinct().ToArray();
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

        #endregion

        #region FindAssemblyAsync

        public Task<Assembly[]?> FindAssemblyAsync(PackageIdentity identity, string requestedFramework)
        {
            CheckNull(nameof(identity), identity);
            CheckNull(nameof(requestedFramework), requestedFramework);

            return PrivateFindAssemblyAsync(identity, NuGetFramework.Parse(requestedFramework));
        }

        public Task<Assembly[]?> FindAssemblyAsync(PackageIdentity identity, NuGetFramework framework)
        {
            CheckNull(nameof(identity), identity);
            CheckNull(nameof(framework), framework);

            return PrivateFindAssemblyAsync(identity, framework);
        }

        private async Task<Assembly[]?> PrivateFindAssemblyAsync(PackageIdentity identity, NuGetFramework framework)
        {
            var infos = await FindPackageWithDependenciesAsync(identity, framework);
            if (infos is null) return null;

            var dlls = infos.SelectMany(inf => inf.AssemblyPaths).ToArray();

            var targetInfo = infos.Where(info => Object.Equals(info.Identity, identity)).First();

            var loader = new FilePathListAssemblyLoaderContext(dlls);

            return targetInfo.AssemblyPaths
                             .Select(path => loader.LoadFromAssemblyName(AssemblyName.GetAssemblyName(path)))
                             .ToArray();
        }

        #endregion


        private void CheckNull(string fieldNm, object value)
        {
            if (value is null) throw new ArgumentNullException(fieldNm);
        }
    }
}
