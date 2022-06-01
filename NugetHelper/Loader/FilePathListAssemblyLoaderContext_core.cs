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
        private ConcurrentDictionary<string, Assembly?> _loaded;

        public FilePathListAssemblyLoaderContext(string[] dllFilepaths) : base("NuGetHelper")
        {
            _pathes = new();
            foreach (var dllpath in dllFilepaths)
            {
                var asmName = AssemblyName.GetAssemblyName(dllpath).Name;
                if (asmName is null) continue;

                _pathes[asmName] = dllpath;
            }

            _loaded = new();

            this.Resolving += FilePathListAssemblyLoaderContext_Resolving;
        }

        private Assembly? FilePathListAssemblyLoaderContext_Resolving(AssemblyLoadContext loader, AssemblyName assemblyName)
        {
            try
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }
            catch
            {
                var asmName = assemblyName.Name;
                if (asmName is null) return null;

                var result = _loaded.GetOrAdd(
                                 asmName,
                                 name => _pathes.TryGetValue(name, out var path) ?
                                              loader.LoadFromAssemblyPath(path) :
                                              null);

                return result;
            }
        }
    }
}
#endif
