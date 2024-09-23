using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace SerializableReadonlyStruct
{
    internal class Processor : ILPostProcessor
    {
        public override ILPostProcessor GetInstance()
        {
            return this;
        }

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.References.Any(r =>
                Path.GetFileNameWithoutExtension(r) == "SerializableReadonlyStruct");
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly)) return new ILPostProcessResult(null);

            var loader = new AssemblyResolver();

            var folders = new HashSet<string>();
            foreach (var reference in compiledAssembly.References)
                folders.Add(Path.Combine(Environment.CurrentDirectory, Path.GetDirectoryName(reference)));

            var folderList = folders.OrderBy(x => x);
            foreach (var folder in folderList) loader.AddSearchDirectory(folder);

            var readerParameters = new ReaderParameters
            {
                InMemory = true,
                AssemblyResolver = loader,
                ReadSymbols = true,
                ReadingMode = ReadingMode.Deferred
            };

            readerParameters.SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData);

            var assembly = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData),
                readerParameters);

            ProcessAssembly(assembly);

            byte[] peData;
            byte[] pdbData;
            {
                var peStream = new MemoryStream();
                var pdbStream = new MemoryStream();
                var writeParameters = new WriterParameters
                {
                    SymbolWriterProvider = new PortablePdbWriterProvider(),
                    WriteSymbols = true,
                    SymbolStream = pdbStream
                };

                assembly.Write(peStream, writeParameters);
                peStream.Flush();
                pdbStream.Flush();

                peData = peStream.ToArray();
                pdbData = pdbStream.ToArray();
            }

            return new ILPostProcessResult(new InMemoryAssembly(peData, pdbData));
        }

        private void ProcessAssembly(AssemblyDefinition assembly)
        {
            foreach (var module in assembly.Modules)
            foreach (var type in module.GetTypes())
                ProcessType(type);
        }

        private void ProcessType(TypeDefinition type)
        {
            if (!type.IsValueType) return;
            if (!type.IsSerializable) return;
            var customAttributes = type.CustomAttributes;
            if (customAttributes.All(attr =>
                    attr.AttributeType.FullName != "SerializableReadonlyStruct.SerializableReadonlyAttribute")) return;

            for (var i = 0; i < customAttributes.Count; i++)
            {
                var attr = customAttributes[i];
                if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute")
                {
                    customAttributes.RemoveAt(i);
                    i--;
                }
            }

            foreach (var field in type.Fields)
                if (field.IsInitOnly)
                    field.IsInitOnly = false;
        }
    }
}