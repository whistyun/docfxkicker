using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NuGetHelper
{
    public class UPackageInfoCollection : IEnumerable<UPackageInfo>
    {
        private readonly HashSet<UPackageInfo> _dependsOnSet;
        private readonly List<UPackageInfo> _dependsOn;

        public UPackageInfo Primary { get; }
        public IReadOnlyCollection<UPackageInfo> DependsOn => _dependsOn.AsReadOnly();

        public UPackageInfoCollection(UPackageInfo primary)
        {
            Primary = primary;
            _dependsOnSet = new();
            _dependsOn = new();
        }

        internal void Add(UPackageInfo dep)
        {
            if (_dependsOnSet.Add(dep))
                _dependsOn.Add(dep);

        }

        internal void Add(UPackageInfoCollection depList)
        {
            Add(depList.Primary);

            foreach (var dep in depList.DependsOn)
                Add(dep);
        }

        internal void AddRange(IEnumerable<UPackageInfo> depList)
        {
            foreach (var dep in depList)
                Add(dep);
        }

        internal void AddRange(IEnumerable<UPackageInfoCollection> depListList)
        {
            foreach (var depList in depListList)
                Add(depList);
        }

        public IEnumerator<UPackageInfo> GetEnumerator() => Enumerable.Concat(new[] { Primary }, _dependsOn).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
