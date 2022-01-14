using System;
using System.Activities;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.DocumentProcessing.Contracts.Results;
using Newtonsoft.Json.Linq;
using NReco.Linq;
using UiPath.DocumentProcessing.Contracts.Taxonomy;

namespace Impower.DocumentUnderstanding.Models
{
    internal static class ExtractionResultExtensions
    {
        internal static object GetDataPointValue(ResultsDataPoint dataPoint)
        {
            var value = dataPoint.Values[0].Value;
            switch (dataPoint.FieldType)
            {
                case FieldType.Date:
                    return DateTime.Parse(value).Date;
                case FieldType.Number:
                    return decimal.Parse(value);
                default:
                    return value;
            }
        }
    }
    [DisplayName("Run Rule On Extraction Result")]
    public class RunRuleOnExtractionResult : CodeActivity
    {
        [Category("Input")]
        [DisplayName("Rule")]
        public InArgument<ExtractionResultRule> Rule { get; set; }
        [Category("Output")]
        [DisplayName("Valid?")]
        public InArgument<bool> Valid { get; set; }
        protected override void Execute(CodeActivityContext context)
        {

        }
    }

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
