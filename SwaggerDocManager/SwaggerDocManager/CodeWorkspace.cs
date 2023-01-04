using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Reflection;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Transform;

namespace SwaggerDocManager
{
    internal class CodeWorkspace : IDisposable
    {
        #region Private Fields

        private bool disposedValue;
        private readonly MSBuildWorkspace _workspace;
        private readonly ILogger _logger;
        private readonly IObjectFinder _objectFinder;
        private readonly IProgress<ProjectLoadProgress> progressReporter;
        private readonly Project project;

        private SyntaxNode existingDescriptionsNode = null;
        private SyntaxNode existingExtModelSwaggerMiddlewareNode = null;

        #endregion Private Fields

        #region Constructor

        public CodeWorkspace(WorkspaceArgs codeGenArgs, ILogger logger, IObjectFinder objectFinder)
        {
            _logger = logger;
            _objectFinder = objectFinder;

            MSBuildLocator.RegisterDefaults();
            _workspace = MSBuildWorkspace.Create();
            string projectPath = codeGenArgs.ProjectPath;
            progressReporter = codeGenArgs.ProjectLoadProgressReporter;
            project = _workspace.OpenProjectAsync(projectPath, progressReporter).Result;
            _workspace.WorkspaceFailed += (o, e) => _logger.Error(e.Diagnostic.Message);
        }

        #endregion Constructor

        #region Public Methods

        public async Task ExecuteCreateAnnotationsPipeline(AnnotationsPipelineArgs args)
        {
            Document descriptionsDocument = GetSingleDocumentByName(args.DescriptionsDocumentName);
            Document extModelSwaggerMiddlewareDocument = GetSingleDocumentByName("AddPropertyDescriptionToExternalModels.cs");
            IEnumerable<string> interestedFileNames = args.InterestedDocuments;
            List<string> usingsToAdd = args.UsingsToAdd;
            bool dryRun = args.IsDryRun;

            Dictionary<string, Dictionary<string, string>> constantsToAddByClassName = new();
            List<string> externalModelTypes = new();

            foreach (var fullFileName in interestedFileNames)
            {
                IEnumerable<Document> documents = GetMatchingDocuments(fullFileName);
                if (!documents.Any())
                {
                    var constantsToAdd = _objectFinder.GetClassNameQualifiedPropertyNames(fullFileName);
                    if (constantsToAdd.Count > 0)
                    {
                        constantsToAddByClassName.Add(fullFileName, constantsToAdd.ToDictionary(s => s, s => s));
                        externalModelTypes.Add(fullFileName);
                    }
                    Print($"Found no match for {fullFileName}");
                    continue;
                }
                if (documents.Count() > 1)
                {
                    Print($"Found more than one match for {fullFileName}");
                    continue;
                }
                var document = documents.FirstOrDefault();
                var transformedNode = ExecuteInternalTypePipeline(document, usingsToAdd, out List<string> descriptionConstants, dryRun);
                if (descriptionConstants.Count > 0)
                {
                    constantsToAddByClassName.Add(fullFileName.Split(".")[^1], descriptionConstants.ToDictionary(s => s, s => s));
                }
            }

            existingDescriptionsNode ??= await descriptionsDocument.GetSyntaxRootAsync();
            var transformedDescriptionsNode = existingDescriptionsNode;

            foreach (var item in constantsToAddByClassName.Keys)
            {
                transformedDescriptionsNode = AppendDescriptionConstants(transformedDescriptionsNode, constantsToAddByClassName[item], item);
            }

            if (existingDescriptionsNode != transformedDescriptionsNode)
            {
                existingDescriptionsNode = transformedDescriptionsNode;
                if (dryRun)
                {
                    Print(transformedDescriptionsNode.ToFullString(), true, true);
                }
                else
                {
                    await File.WriteAllTextAsync(descriptionsDocument.FilePath, transformedDescriptionsNode.ToFullString());
                }
            }
            await AppendToListVariable(extModelSwaggerMiddlewareDocument, externalModelTypes, dryRun);
        }

        public async Task ExecuteCreateResponseSamplePipeline(ResponseSamplePipelineArgs args)
        {
            bool dryRun = args.IsDryRun;
            Dictionary<string, List<PropertyInfo>> modelProps = args.ModelProperties;

            var currentProject = project;
            ExampleGenerator exampleGenerator = new();
            ExampleBodyGenerator exampleBodyGenerator;
            foreach (var item in modelProps.Keys)
            {
                var props = modelProps[item];

                var itemParts = item.Split(".");
                var lastDotIndex = item.LastIndexOf(".");
                var modelName = item[(lastDotIndex + 1)..];
                var fileName = $"{modelName}ResponseExample.cs";
                var ns = item[..item.LastIndexOf(".")];
                var document = GetSingleDocumentByName(fileName);
                if (document != null)
                {
                    var existingNode = await document.GetSyntaxRootAsync();
                    exampleBodyGenerator = new(_logger, item, props);
                    var transformedNode = exampleBodyGenerator.Visit(existingNode);
                    if (existingNode != transformedNode)
                    {
                        if (!dryRun)
                        {
                            await File.WriteAllTextAsync(document.FilePath, transformedNode.ToFullString());
                        }
                        else
                        {
                            Print(transformedNode.ToFullString(), true, true);
                        }
                    }
                }
                else
                {
                    var source = exampleGenerator.GetExampleProviderCodeBlock(modelName, Path.GetFileNameWithoutExtension(fileName), "ResponseExamples", ns);
                    var existingNode = await SyntaxFactory.ParseSyntaxTree(source).GetRootAsync();
                    exampleBodyGenerator = new(_logger, item, modelProps[item]);
                    var transformedNode = exampleBodyGenerator.Visit(existingNode);
                    if (!dryRun)
                    {
                        document = currentProject.AddDocument(fileName, transformedNode.ToFullString(), new List<string> { "Swagger", "ResponseExamples" });
                        currentProject = document.Project;
                        _logger.Information("Generated document {fileName} for {item}", fileName, item);
                        _workspace.TryApplyChanges(currentProject.Solution);
                    }
                    else
                    {
                        Print(transformedNode.ToFullString(), true, true);
                    }
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected SyntaxNode ExecuteInternalTypePipeline(Document document, List<string> usingsToAdd, out List<string> descriptionConstants, bool dryRun)
        {
            var existingNode = document.GetSyntaxRootAsync().Result;
            var item = Path.GetFileNameWithoutExtension(document.Name);
            bool decorateMethodParameters = IsController(item);

            var transformedNode = AnnotateDescriptionAttribute(existingNode, decorateMethodParameters, out descriptionConstants);
            if (transformedNode != existingNode)
            {
                existingNode = transformedNode;
                transformedNode = AppendUsings(existingNode, usingsToAdd);
                if (existingNode != transformedNode)
                {
                    if (dryRun)
                    {
                        Print(transformedNode.ToFullString(), true, true);
                    }
                    else
                    {
                        File.WriteAllTextAsync(document.FilePath, transformedNode.ToFullString());
                    }
                }
            }

            return transformedNode;
        }

        protected SyntaxNode AnnotateDescriptionAttribute(SyntaxNode node, bool decorateMethodParameters, out List<string> descriptionConstants)
        {
            DescriptionAttributeAnnotator descAttribAnnotator = new(_logger, decorateMethodParameters);

            var transformedNode = descAttribAnnotator.Visit(node);
            descriptionConstants = descAttribAnnotator.DescriptionConstants;

            return transformedNode;
        }

        protected SyntaxNode AppendUsings(SyntaxNode node, List<string> usingsToAdd)
        {
            DescriptionAttributeAnnotator descAttribAnnotator = new(_logger);
            var transformedNode = descAttribAnnotator.AppendUsings(node, usingsToAdd);
            return transformedNode;
        }

        protected SyntaxNode AppendDescriptionConstants(SyntaxNode node, Dictionary<string, string> constantsToAddWithValue, string regionName)
        {
            StringConstantsAppender stringConstantGenerator = new(_logger, constantsToAddWithValue, regionName);

            var transformedNode = stringConstantGenerator.Visit(node);
            return transformedNode;
        }

        protected IEnumerable<Document> GetMatchingDocuments(string fullFileName)
        {
            string[] fileNameParts = fullFileName.Split(".");
            var fileName = fileNameParts[^1];
            List<string> folders = (fileNameParts.Length > 1) ? fileNameParts.Where(fname => fname != fileName).ToList() : new();

            var documents = project.Documents.Where(d => Path.GetFileNameWithoutExtension(d.Name) == fileName).ToList();
            documents = documents.Where(d=> d.Folders.SelectMany(f => f.Split(".", StringSplitOptions.RemoveEmptyEntries)).Intersect(folders).Count() == folders.Count).ToList(); ;
                
            return documents;
        }

        protected Document GetSingleDocumentByName(string documentName)
        {
            Document document = project.Documents.SingleOrDefault(d => d.Name == documentName);
            return document;
        }

        protected async Task AppendToListVariable(Document targetDocument, List<string> valuesToAdd, bool isDryRun)
        {
            bool dryRun = isDryRun;
            existingExtModelSwaggerMiddlewareNode ??= await targetDocument.GetSyntaxRootAsync();

            ExternalModelTypeAppender listVariableValAppender = new(_logger, "externalModelTypes", valuesToAdd);

            var transformedExtModelSwaggerMiddlewareNode = listVariableValAppender.Visit(existingExtModelSwaggerMiddlewareNode);

            if (transformedExtModelSwaggerMiddlewareNode != existingExtModelSwaggerMiddlewareNode)
            {
                if (dryRun)
                {
                    Print(transformedExtModelSwaggerMiddlewareNode.ToFullString(), true, true);
                }
                else
                {
                    await File.WriteAllTextAsync(targetDocument.FilePath, transformedExtModelSwaggerMiddlewareNode.ToFullString());
                }
            }
        }

        protected bool IsController(string name)
        {
            return name.ToLower().EndsWith("controller");
        }

        protected void PrintHeader() => Print(" ---------------  ******* -----------------");

        protected void PrintFooter() => Print(" ---------------  ******* -----------------");

        protected void Print(string item, bool header = false, bool footer = false)
        {
            if (header)
            {
                PrintHeader();
            }
            _logger.Information(item);
            if (footer)
            {
                PrintFooter();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _workspace.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        #endregion Protected Methods

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CodeWorkspace()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
    }
}