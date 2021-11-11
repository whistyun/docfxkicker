#if NETCOREAPP
namespace NuGetHelper.Loader
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;

    public class FilePathListAssemblyLoaderContext : AssemblyLoadContext
    {
        private Dictionary<string, string> _pathes;
        private ConcurrentDictionary<string, Assembly> _loaded;

        public FilePathListAssemblyLoaderContext(string[] dllFilepaths) : base("NuGetHelper")
        {
            _pathes = dllFilepaths.ToDictionary(p => AssemblyName.GetAssemblyName(p).Name, p => p);
            _loaded = new ConcurrentDictionary<string, Assembly>();

            this.Resolving += FilePathListAssemblyLoaderContext_Resolving;
        }

        private Assembly FilePathListAssemblyLoaderContext_Resolving(AssemblyLoadContext loader, AssemblyName assemblyName)
        {
            try
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }
            catch
            {
                Assembly result = _loaded.GetOrAdd(assemblyName.Name, name =>
                 {
                     return _pathes.TryGetValue(name, out var path) ?
                         loader.LoadFromAssemblyPath(path) :
                         null;
                 });

                return result;
            }
        }
    }
}
#endif
