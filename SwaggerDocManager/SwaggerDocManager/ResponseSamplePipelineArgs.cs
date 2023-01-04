using System.Collections.Generic;
using System.Reflection;

namespace SwaggerDocManager
{
    internal class ResponseSamplePipelineArgs : ArgsBase
    {
        public Dictionary<string, List<PropertyInfo>> ModelProperties { get; set; } = new();
    }
}