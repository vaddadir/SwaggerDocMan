using System.Collections.Generic;
using System.Linq;

namespace SwaggerDocManager
{
    internal class AnnotationsPipelineArgs : ArgsBase
    {
        public IEnumerable<string> InterestedDocuments { get; set; } = Enumerable.Empty<string>();
        public List<string> UsingsToAdd { get; set; } = new();
        public string DescriptionsDocumentName { get; set; } = "Descriptions.cs";
        public string ExternalModelTypeSchemaFilter { get; set; } = "AddPropertyDescriptionToExternalModels.cs";
    }
}