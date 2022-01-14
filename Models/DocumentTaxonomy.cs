using System;
using System.Activities;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.DocumentProcessing.Contracts.Taxonomy;

namespace Impower.DocumentUnderstanding.Models
{
    internal static class TaxonomyExtensions
    {
        internal static DocumentTaxonomy CopyTaxonomy(DocumentTaxonomy documentTaxonomy)
        {
            return DocumentTaxonomy.Deserialize(documentTaxonomy.Serialize());
        }
    }
    [DisplayName("Filter Taxonomy By Field Ids")]
    public class FilterTaxonomyByFieldIds : CodeActivity
    {
        [Category("Input")]
        [DisplayName("Document Taxonomy")]
        [Description("Input taxonomy to be filtered.")]
        [RequiredArgument]
        public InArgument<DocumentTaxonomy> InputTaxonomy { get; set; }

        [Category("Input")]
        [DisplayName("Field IDs")]
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
            
            foreach(DocumentType documentType in documentTypes)
            {
                documentType.Fields = documentType.Fields.Where(
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
