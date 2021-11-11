using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

using System;

namespace NuGetHelper
{
    public class UDownloadResult : IDisposable
    {
        public PackageIdentity Target { get; }
        public DownloadResourceResult Result { get; }

        public UDownloadResult(PackageIdentity target, DownloadResourceResult result)
        {
            Target = target;
            Result = result;
        }

        public void Dispose() => Result.Dispose();
    }
}
