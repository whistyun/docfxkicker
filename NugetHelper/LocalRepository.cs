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
    public class LocalPackage
    {
        private readonly ILogger _logger;
        public string GlobalPackagesFolder { get; }

        public LocalPackage(ILogger logger)
        {
            GlobalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(Settings.LoadDefaultSettings(root: null));
            _logger = logger;
        }

        #region FindLocalPackage

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(string packageId, bool includePrerelease = false)
            => FindLocalPackage(packageId, VersionRange.All, includePrerelease);

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(string packageId, VersionRange range, bool includePrerelease = false)
        {
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(range), range);

            IEnumerable<LocalPackageInfo> infos = LocalFolderUtility.GetPackagesV3(GlobalPackagesFolder, packageId, _logger);

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
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, new NuGetVersion(version));
            return LocalFolderUtility.GetPackageV3(GlobalPackagesFolder, identity, _logger);
        }

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(string packageId, NuGetVersion version)
        {
            Check.NotNull(nameof(packageId), packageId);
            Check.NotNull(nameof(version), version);

            var identity = new PackageIdentity(packageId, version);
            return LocalFolderUtility.GetPackageV3(GlobalPackagesFolder, identity, _logger);
        }

        /// <exception cref="PackageNotFoundException">The specified package does not exist in the repository.</exception>
        public LocalPackageInfo? FindLocalPackage(PackageIdentity identity)
        {
            Check.NotNull(nameof(identity), identity);

            return LocalFolderUtility.GetPackageV3(GlobalPackagesFolder, identity, _logger);
        }

        #endregion    
    }
}
