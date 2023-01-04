using Microsoft.CodeAnalysis.MSBuild;
using Serilog;
using System;
using System.IO;

namespace SwaggerDocManager
{
    internal class ProjectLoadProgressReporter : IProgress<ProjectLoadProgress>
    {
        private readonly ILogger _logger;

        public ProjectLoadProgressReporter(ILogger logger)
        {
            _logger = logger;
        }

        public void Report(ProjectLoadProgress loadProgress)
        {
            var projectDisplay = Path.GetFileName(loadProgress.FilePath);
            if (loadProgress.TargetFramework != null)
            {
                projectDisplay += $" ({loadProgress.TargetFramework})";
            }

            _logger.Information($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
        }
    }
}