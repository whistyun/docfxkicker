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

        private readonly ILogger _logger;
        private readonly LocalPackage _local;
        private readonly SourceCacheContext _cache;
        private readonly SourceRepository _repository;
        private readonly CancellationToken _cancel;


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
            _local = new LocalPackage(logger);
            _logger = logger;
            _cache = new SourceCacheContext();
            _repository = Repository.Factory.GetCoreV3(repositorySource);
            _cancel = new CancellationToken();
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
            Check.NotNull(nameof(packageId), packageId);

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
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(range), range);

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
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(range), range);

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
            Check.NotNull(nameof(packageId), packageId);

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
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(range), range);

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
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(range), range);

            return PrivateSearchLatestIdentityAsync(packageId, range, token, includePrerelease);
        }

        private async Task<PackageIdentity> PrivateSearchLatestIdentityAsync(string packageId, VersionRange range, CancellationToken token, bool includePrerelease = false)
        {
            var latestVersion = await SearchLatestVersionAsync(packageId, range, token, includePrerelease);
            return new PackageIdentity(packageId, latestVersion);
        }

        #endregion

        #region DownloadPackageAsync

        public Task<UDownloadResult> DownloadPackageAsync(string packageId)
        {
            Check.NotNull(nameof(packageId), packageId);

            return PrivateDownloadPackageAsync(packageId, VersionRange.All);
        }

        public Task<UDownloadResult> DownloadPackageAsync(string packageId, VersionRange range)
        {
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(range), range);

            return PrivateDownloadPackageAsync(packageId, range);
        }

        public Task<UDownloadResult> DownloadPackageAsync(string packageId, string version)
        {
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, new NuGetVersion(version));
            return PrivateDownloadPackageAsync(identity, _cancel);
        }

        public Task<UDownloadResult> DownloadPackageAsync(string packageId, NuGetVersion version)
        {
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, version);
            return PrivateDownloadPackageAsync(identity, _cancel);
        }

        public Task<UDownloadResult> DownloadPackageAsync(PackageIdentity identity)
        {
            Check.NotNull(nameof(identity), identity);

            return PrivateDownloadPackageAsync(identity, _cancel);
        }

        public Task<UDownloadResult> DownloadPackageAsync(PackageIdentity identity, CancellationToken token)
        {
            Check.NotNull(nameof(identity), identity);

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
                _local.GlobalPackagesFolder,
                _logger,
                token);

            if (result.Status != DownloadResourceResultStatus.Available)
            {
                return new UDownloadResult(identity, result.Status);
            }

            var localPack = _local.FindLocalPackage(identity);

            if (localPack is null)
            {
                await GlobalPackagesFolderUtility.AddPackageAsync(
                    result.PackageSource,
                    identity,
                    result.PackageStream,
                    _local.GlobalPackagesFolder,
                    context.ParentId,
                    context.ClientPolicyContext,
                    _logger,
                    token);

                localPack = _local.FindLocalPackage(identity);
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
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, version);
            return PrivateFindPackageAsync(identity, _cancel);
        }

        public Task<LocalPackageInfo?> FindPackageAsync(PackageIdentity identity)
        {
            Check.NotNull(nameof(identity), identity);

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

            var info = _local.FindLocalPackage(identity);
            if (info is not null)
                return info;

            return (await DownloadPackageAsync(identity, token)).Info;
        }

        #endregion
    }
}
