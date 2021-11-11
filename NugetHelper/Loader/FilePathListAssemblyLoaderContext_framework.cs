#if NETFRAMEWORK
namespace NuGetHelper.Loader
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    class FilePathListAssemblyLoaderContext
    {
        private LoaderHelper _helper;

        public FilePathListAssemblyLoaderContext(string[] dllFilepaths)
        {
            var pathes = dllFilepaths.ToDictionary(p => AssemblyName.GetAssemblyName(p), p => p);
            _helper = new LoaderHelper(pathes);
            
            AppDomain.CurrentDomain.AssemblyResolve += _helper.AssemblyResolve;
        }

        public Assembly LoadFromAssemblyName(AssemblyName assemblyName)
            => AppDomain.CurrentDomain.Load(assemblyName);
    }

    [Serializable]
    class LoaderHelper
    {
        public Dictionary<AssemblyName, string> Pathes { private set; get; }

        public LoaderHelper(Dictionary<AssemblyName, string> pathes)
        {
            Pathes = pathes;
        }

        public Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var asmName = new AssemblyName(args.Name);

            var asmPath = Pathes.Keys
                            .Where(name => StringComparer.OrdinalIgnoreCase.Equals(name.Name, asmName.Name))
                            .Select(name => Pathes[name])
                            .FirstOrDefault();

            if (asmPath is null) return null;

            return Assembly.LoadFrom(asmPath);
        }
    }
}
#endif
