using System.CommandLine;

namespace SwaggerDocManager
{
    internal class CommandBuilder
    {
        private readonly IBuildCommandOptions _optionBuilder;

        public CommandBuilder(IBuildCommandOptions optionBuilder)
        {
            _optionBuilder = optionBuilder;
        }

        public Command GetDescriptionAnnotatorCommand()
        {
            Command annotateDescription = new("desc", "Annotate Description Attribute")
            {
                _optionBuilder.ProjectOption,
                _optionBuilder.AssemblyOption,
                _optionBuilder.DryRunOption,
                _optionBuilder.ControllerOption,
                _optionBuilder.UsingOption,
                _optionBuilder.TypeNameOption,
                _optionBuilder.VersionPrefixOption,
                _optionBuilder.DescriptionsDocumentOption
            };

            return annotateDescription;
        }

        public Command GetGenerateRequestSamplesCommand()
        {
            Command generateRequestSamples = new("add-response-samples", "Creates response samples")
            {
                _optionBuilder.ProjectOption,
                _optionBuilder.AssemblyOption,
                _optionBuilder.DryRunOption,
                _optionBuilder.ControllerOption,
                _optionBuilder.TypeNameOption
            };
            return generateRequestSamples;
        }
    }
}