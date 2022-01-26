using Impower.DocumentUnderstanding.Extensions;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UiPath.DocumentProcessing.Contracts.Taxonomy;

namespace Impower.DocumentUnderstanding
{
    [DisplayName("Filter Taxonomy By Field ID's")]
    public class FilterTaxonomyByFieldIds : CodeActivity
    {
        [Category("Input")]
        [DisplayName("Document Taxonomy")]
        [Description("Input taxonomy to be filtered.")]
        [RequiredArgument]
        public InArgument<DocumentTaxonomy> InputTaxonomy { get; set; }

        [Category("Input")]
        [DisplayName("Field ID's")]
        [Description("List of fields to filter on.")]
        [RequiredArgument]
        public InArgument<IEnumerable<string>> Fields { get; set; }

        [Category("Output")]
        [DisplayName("Filtered Taxonomy")]
        [Description("Taxonomy with only the input fields remaining")]
        [RequiredArgument]
        public OutArgument<DocumentTaxonomy> FilteredTaxonomy { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var filterFields = Fields.Get(context);
            var workingTaxonomy = TaxonomyExtensions.CopyTaxonomy(InputTaxonomy.Get(context));
            var documentTypes = workingTaxonomy.DocumentTypes;
            
            //TODO: Cleanup this for-loop and the subsequent LINQ expression. one liner?
            foreach (DocumentType documentType in documentTypes)
            {
                documentType.Fields = documentType.Fields.Where(
                    field => !string.IsNullOrEmpty(field.FieldId)
                ).Where(
                    field => filterFields.Contains(field.FieldId)
                ).ToArray();
            }
            workingTaxonomy.DocumentTypes = documentTypes.Where(
                documentType => documentType.Fields.Length > 0
            ).ToArray();
            FilteredTaxonomy.Set(context, workingTaxonomy);
        }
    }
}