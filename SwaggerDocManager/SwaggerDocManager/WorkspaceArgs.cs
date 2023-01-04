using Microsoft.CodeAnalysis.MSBuild;
using System;

namespace SwaggerDocManager
{
    internal class WorkspaceArgs
    {
        public string ProjectPath { get; set; } = string.Empty;
        public IProgress<ProjectLoadProgress> ProjectLoadProgressReporter { get; set; } = default;

        public bool IsDryRun { get; set; } = true;
    }
}