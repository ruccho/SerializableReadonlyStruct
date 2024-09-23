using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace SerializableReadonlyStruct
{
    internal class AssemblyResolver : BaseAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> cache = new();

        public AssemblyResolver()
        {
            foreach (var dir in GetSearchDirectories()) RemoveSearchDirectory(dir);
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            if (cache.TryGetValue(name.FullName, out var definition)) return definition;

            var readerParameters = new ReaderParameters
            {
                InMemory = true,
                AssemblyResolver = this,
                ReadSymbols = false
            };
            readerParameters.ReadingMode = ReadingMode.Deferred;
            AssemblyDefinition assemblyDefinition;

            try
            {
                assemblyDefinition = Resolve(name, readerParameters);
            }
            catch (Exception)
            {
                if (readerParameters.ReadSymbols)
                {
                    readerParameters.ReadSymbols = false;
                    assemblyDefinition = Resolve(name, readerParameters);
                }
                else
                {
                    throw new AssemblyResolutionException(name);
                }
            }

            cache.Add(name.FullName, assemblyDefinition);
            return assemblyDefinition;
        }

        public new void AddSearchDirectory(string directory)
        {
            if (!GetSearchDirectories().Contains(directory)) base.AddSearchDirectory(directory);
        }
    }
}