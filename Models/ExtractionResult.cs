using System;
using System.Activities;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models
{
    [DisplayName("Merge Extraction Results")]
    public class MergeExtractionResults : CodeActivity
    {
        [Category("Input")]
        [DisplayName("Result To Merge Into")]
        [RequiredArgument]
        public InOutArgument<ExtractionResult> ResultToCopyTo { get; set; }

        [Category("Input")]
        [DisplayName("Result To Copy From")]
        [RequiredArgument]
        public InArgument<ExtractionResult> ResultToCopyFrom { get; set; }


        protected override void Execute(CodeActivityContext context)
        {
            ExtractionResult resultToCopyTo = ResultToCopyTo.Get(context);

            var dataPointDictionary = resultToCopyTo.ResultsDocument.Fields.ToDictionary(
                field => field.FieldId,
                field => field
            );

            foreach (ResultsDataPoint field in ResultToCopyFrom.Get(context).ResultsDocument.Fields)
            {
                dataPointDictionary[field.FieldId] = field;
            }

            resultToCopyTo.ResultsDocument.Fields = dataPointDictionary.Select(kvp => kvp.Value).ToArray();
            ResultToCopyTo.Set(context,resultToCopyTo);
        }
    }
}
