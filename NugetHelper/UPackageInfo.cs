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

        internal UPackageInfo(LocalPackageInfo rawInfo, FrameworkReducer reducer, NuGetFramework framework)
        {
            Identity = rawInfo.Nuspec.GetIdentity();
            TargetFramework = framework;
            RawInfo = rawInfo;

            var directory = Path.GetDirectoryName(rawInfo.Path);

            var grps = rawInfo.GetReader().GetLibItems();

            if (grps.Count() == 0)
            {
                AssemblyPaths = Array.Empty<string>();
            }
            else
            {
                var matched = reducer.GetNearest(framework, grps.Select(grp => grp.TargetFramework));
                var matchGrp = grps.Where(grp => Object.Equals(grp.TargetFramework, matched))
                                   .FirstOrDefault();

                if (matchGrp is null)
                    throw new ArgumentException("not supported package");

                AssemblyPaths = matchGrp.Items
                                    .Where(itm => Path.GetExtension(itm).ToLower().Equals(".dll"))
                                    .Select(itm => Path.Combine(directory, itm))
                                    .ToList()
                                    .AsReadOnly();
            }

        }

        public override int GetHashCode()
            => unchecked(Identity.GetHashCode() + TargetFramework.GetHashCode());

        public override bool Equals(object obj)
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
