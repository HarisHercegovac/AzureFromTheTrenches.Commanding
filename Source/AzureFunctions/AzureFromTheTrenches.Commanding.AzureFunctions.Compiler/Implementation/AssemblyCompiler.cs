﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AzureFromTheTrenches.Commanding.AzureFunctions.Model;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureFromTheTrenches.Commanding.AzureFunctions.Compiler.Implementation
{
    internal class AssemblyCompiler : IAssemblyCompiler
    {
        private readonly ITemplateProvider _templateProvider;

        public AssemblyCompiler(ITemplateProvider templateProvider = null)
        {
            _templateProvider = templateProvider ?? new TemplateProvider();
        }

        public async Task Compile(IReadOnlyCollection<AbstractFunctionDefinition> functionDefinitions,
            IReadOnlyCollection<Assembly> externalAssemblies,
            string outputBinaryFolder,
            string assemblyName)
        {
            IReadOnlyCollection<SyntaxTree> syntaxTrees = CompileSource(functionDefinitions);

            CompileAssembly(syntaxTrees, externalAssemblies, outputBinaryFolder, assemblyName);
        }

        private List<SyntaxTree> CompileSource(IReadOnlyCollection<AbstractFunctionDefinition> functionDefinitions)
        {
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();

            foreach (AbstractFunctionDefinition functionDefinition in functionDefinitions)
            {
                string templateSource = _templateProvider.GetCSharpTemplate(functionDefinition);
                Func<object, string> template = Handlebars.Compile(templateSource);

                string outputCode = template(functionDefinition);
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(outputCode);
                syntaxTrees.Add(syntaxTree);
            }

            return syntaxTrees;
        }

        private void CompileAssembly(IReadOnlyCollection<SyntaxTree> syntaxTrees,
            IReadOnlyCollection<Assembly> externalAssemblies,
            string outputBinaryFolder,
            string outputAssemblyName)
        {
            
            HashSet<string> locations = new HashSet<string>
            {
                typeof(Runtime).GetTypeInfo().Assembly.Location,
                typeof(Abstractions.ICommand).GetTypeInfo().Assembly.Location,
                typeof(System.Net.Http.HttpMethod).GetTypeInfo().Assembly.Location,
                typeof(System.Net.HttpStatusCode).GetTypeInfo().Assembly.Location,
                typeof(HttpRequest).Assembly.Location,
                typeof(object).GetTypeInfo().Assembly.Location,
                typeof(Hashtable).GetTypeInfo().Assembly.Location,
                typeof(JsonConvert).GetTypeInfo().Assembly.Location,
                typeof(OkObjectResult).GetTypeInfo().Assembly.Location,
                typeof(IActionResult).GetTypeInfo().Assembly.Location,
                typeof(FunctionNameAttribute).GetTypeInfo().Assembly.Location,
                typeof(ILogger).GetTypeInfo().Assembly.Location,
                Assembly.GetExecutingAssembly().Location,
                Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll"),
                Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"),
            };
            foreach (Assembly externalAssembly in externalAssemblies)
            {
                locations.Add(externalAssembly.Location);
            }

            PortableExecutableReference[] references = locations.Select(x => MetadataReference.CreateFromFile(x)).ToArray();
            var compilation = CSharpCompilation.Create(outputAssemblyName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (Stream stream = new FileStream(Path.Combine(outputBinaryFolder, outputAssemblyName), FileMode.Create))
            {
                EmitResult result = compilation.Emit(stream);
                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);
                    StringBuilder messageBuilder = new StringBuilder();

                    foreach (Diagnostic diagnostic in failures)
                    {
                        messageBuilder.AppendFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }

                    throw new ConfigurationException(messageBuilder.ToString());
                }
            }
        }
    }
}
