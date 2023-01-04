using Reflection;
using Serilog;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SwaggerDocManager
{
    internal class CommandHandler
    {
        private readonly IBuildCommandOptions _optionBuilder;
        private readonly ILogger _logger;

        public CommandHandler(ILogger logger, IBuildCommandOptions optionBuilder)
        {
            _optionBuilder = optionBuilder;
            _logger = logger;
        }

        public async Task HandleDescriptionCommand(InvocationContext context)
        {
            ParseResult parseResult = context.ParseResult;
            string projectPath = parseResult.GetValueForOption(_optionBuilder.ProjectOption);
            string apiAssemblyPath = parseResult.GetValueForOption(_optionBuilder.AssemblyOption);
            bool isDryRun = parseResult.GetValueForOption(_optionBuilder.DryRunOption);
            string controllerName = parseResult.GetValueForOption(_optionBuilder.ControllerOption);
            List<string> usingsToAdd = parseResult.GetValueForOption(_optionBuilder.UsingOption);
            string typeName = parseResult.GetValueForOption(_optionBuilder.TypeNameOption);
            string versionPrefix = parseResult.GetValueForOption(_optionBuilder.VersionPrefixOption);
            string descriptionsDocument = parseResult.GetValueForOption(_optionBuilder.DescriptionsDocumentOption);

            using ObjectFinder objectFinder = new(apiAssemblyPath);
            List<string> interestedFileNames;
            if (!string.IsNullOrWhiteSpace(controllerName))
            {
                interestedFileNames = objectFinder.GetControllerModelNames(controllerName, prefixAssemblyName: false, includeControllerName: true, versionPrefix: versionPrefix);
            }
            else if (!string.IsNullOrWhiteSpace(typeName))
            {
                interestedFileNames = new() { typeName };
            }
            else
            {
                _logger.Error("One of controller name or type name is required");
                return;
            }

            interestedFileNames.ForEach(fileName => _logger.Information(fileName));

            var workspaceArgs = new WorkspaceArgs()
            {
                ProjectPath = projectPath,
                ProjectLoadProgressReporter = new ProjectLoadProgressReporter(_logger),
            };
            using CodeWorkspace workspace = new(workspaceArgs, _logger, objectFinder);
            var executeArgs = new AnnotationsPipelineArgs()
            {
                UsingsToAdd = usingsToAdd,
                IsDryRun = isDryRun,
                InterestedDocuments = interestedFileNames,
                DescriptionsDocumentName = descriptionsDocument
            };

            await workspace.ExecuteCreateAnnotationsPipeline(executeArgs);
        }

        public async Task HandleResponseSamplesCommand(InvocationContext context)
        {
            ParseResult parseResult = context.ParseResult;
            string projectPath = parseResult.GetValueForOption(_optionBuilder.ProjectOption);
            string apiAssemblyPath = parseResult.GetValueForOption(_optionBuilder.AssemblyOption);
            bool isDryRun = parseResult.GetValueForOption(_optionBuilder.DryRunOption);
            string controllerName = parseResult.GetValueForOption(_optionBuilder.ControllerOption);
            string typeName = parseResult.GetValueForOption(_optionBuilder.TypeNameOption);

            using ObjectFinder objectFinder = new(apiAssemblyPath);

            Dictionary<string, List<PropertyInfo>> modelProps = null;
            if (!string.IsNullOrWhiteSpace(controllerName))
            {
                modelProps = objectFinder.GetControllerActionResponseModelProperties(controllerName, prefixAssemblyName: true);
            }
            else if (!string.IsNullOrWhiteSpace(typeName))
            {
                modelProps = new() { { typeName, objectFinder.GetProperties(typeName) } };
            }
            else
            {
                _logger.Error("One of controller name or type name is required");
                return;
            }

            List<string> interestedFileNames = modelProps.Keys.ToList();

            interestedFileNames.ForEach(fileName => _logger.Information(fileName));

            var workspaceArgs = new WorkspaceArgs()
            {
                ProjectPath = projectPath,
                ProjectLoadProgressReporter = new ProjectLoadProgressReporter(_logger),
            };

            using CodeWorkspace workspace = new(workspaceArgs, _logger, objectFinder);
            var executeArgs = new ResponseSamplePipelineArgs()
            {
                IsDryRun = isDryRun,
                ModelProperties = modelProps,
            };
            await workspace.ExecuteCreateResponseSamplePipeline(executeArgs);
        }
    }
}