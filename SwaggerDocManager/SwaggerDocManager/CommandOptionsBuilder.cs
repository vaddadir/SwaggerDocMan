using System.Collections.Generic;
using System.CommandLine;

namespace SwaggerDocManager
{
    internal interface IBuildCommandOptions
    {
        Option<string> ProjectOption { get; }
        Option<string> AssemblyOption { get; }
        Option<bool> DryRunOption { get; }
        Option<List<string>> UsingOption { get; }
        Option<string> ControllerOption { get; }
        Option<string> TypeNameOption { get; }

        Option<string> VersionPrefixOption { get; }

        Option<string> DescriptionsDocumentOption { get; }
    }

    internal class CommandOptionsBuilder : IBuildCommandOptions
    {
        private readonly Option<string> projectOption = new(name: "--project", description: "Path to the project file. Example:") { IsRequired = true };
        private readonly Option<string> assemblyOption = new(name: "--assembly", description: "Path to the assembly. Example: ") { IsRequired = true };
        private readonly Option<bool> dryRunOption = new(name: "--dry", description: "True or false indicating whether it's a dry run") { IsRequired = true };
        private readonly Option<List<string>> usingsOption = new(name: "--using", description: "Usings to include") { AllowMultipleArgumentsPerToken = true };
        private readonly Option<string> controllerOption = new(name: "--controller", description: "Controller to work with. Example: ConnectionsController") { IsRequired = false };
        private readonly Option<string> typeNameOption = new(name: "--type", description: "Type name to use. Example: Emoney.MessageBus.Messages.Aggregation.Subscriptions.Rest.Model.Subscripton") { IsRequired = false };
        private readonly Option<string> versionPrefixOption = new(name: "--controller-version-prefix", description: "Version prefix for the controller. Example: v05") { IsRequired = false };
        private readonly Option<string> descriptionsDocumentOption = new(name: "--descriptions-document", "Example: Descriptions.Connections.cs");

        public Option<string> ProjectOption => projectOption;
        public Option<string> AssemblyOption => assemblyOption;
        public Option<bool> DryRunOption => dryRunOption;
        public Option<List<string>> UsingOption => usingsOption;
        public Option<string> ControllerOption => controllerOption;

        public Option<string> TypeNameOption => typeNameOption;
        public Option<string> VersionPrefixOption => versionPrefixOption;

        public Option<string> DescriptionsDocumentOption => descriptionsDocumentOption;
    }
}