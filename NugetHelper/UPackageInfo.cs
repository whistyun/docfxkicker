using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NuGetHelper
{
    public sealed class UPackageInfo
    {
        public PackageIdentity Identity { get; }
        public NuGetFramework TargetFramework { get; }
        public LocalPackageInfo RawInfo { get; }
        public IReadOnlyList<string> AssemblyPaths { get; }

        internal UPackageInfo(LocalPackageInfo rawInfo, NuGetFramework framework, IReadOnlyList<string> assemblyPaths)
        {
            Identity = rawInfo.Nuspec.GetIdentity();
            TargetFramework = framework;
            RawInfo = rawInfo;
            AssemblyPaths=assemblyPaths;
        }

        public override int GetHashCode()
            => unchecked(Identity.GetHashCode() + TargetFramework.GetHashCode());

        public override bool Equals(object? obj)
        {
            if (Object.ReferenceEquals(this, obj))
                return true;

            if (obj is UPackageInfo inf)
            {
                return Object.Equals(Identity, inf.Identity)
                    && Object.Equals(TargetFramework, inf.TargetFramework);
            }
            return false;
        }

        public static bool operator ==(UPackageInfo left, UPackageInfo right) => Object.Equals(left, right);
        public static bool operator !=(UPackageInfo left, UPackageInfo right) => !Object.Equals(left, right);
    }
}
