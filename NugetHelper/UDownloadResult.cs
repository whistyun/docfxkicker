using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

using System;

namespace NuGetHelper
{
    public class UDownloadResult
    {
        public PackageIdentity Target { get; }
        public DownloadResourceResultStatus Status { get; }
        public LocalPackageInfo? Info { get; }

        public UDownloadResult(PackageIdentity target, DownloadResourceResultStatus status)
        {
            Target = target;
            Status = status;
            Info = null;
        }

        public UDownloadResult(PackageIdentity target, LocalPackageInfo info)
        {
            Target = target;
            Status = info is null ? DownloadResourceResultStatus.NotFound : DownloadResourceResultStatus.Available;
            Info = info;
        }

        public static UDownloadResult Failed(string packageId)
        {
            var identity = new PackageIdentity(packageId, null);
            return new UDownloadResult(identity, DownloadResourceResultStatus.NotFound);
        }
    }
}
