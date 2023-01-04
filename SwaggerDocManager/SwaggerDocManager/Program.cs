using Serilog;
using System.CommandLine;
using System.Threading.Tasks;

namespace SwaggerDocManager
{
    internal partial class Program
    {
        private static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                            .WriteTo.Console()
                            .WriteTo.File("log-.txt", rollingInterval: RollingInterval.Day)
                            .CreateLogger();

            CommandOptionsBuilder commandOptionBuilder = new();
            CommandBuilder commandBuilder = new(commandOptionBuilder);
            CommandHandler commandHandler = new(Log.Logger, commandOptionBuilder);

            Command annotateDescription = commandBuilder.GetDescriptionAnnotatorCommand();
            annotateDescription.SetHandler(commandHandler.HandleDescriptionCommand);

            Command responseSamplesCommand = commandBuilder.GetGenerateRequestSamplesCommand();
            responseSamplesCommand.SetHandler(commandHandler.HandleResponseSamplesCommand);

            RootCommand rootCommand = new();
            rootCommand.AddCommand(annotateDescription);
            rootCommand.AddCommand(responseSamplesCommand);
            await rootCommand.InvokeAsync(args);
        }
    }
}