using Impower.DocumentUnderstanding.Models.ExtractionResults;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UiPath.DocumentProcessing.Contracts.Results;
using UiPath.DocumentProcessing.Contracts.Taxonomy;

namespace Impower.DocumentUnderstanding.Extensions
{
    public static class ExtractionResultRuleExtensions
    {
        public static object GetDataPointValue(ResultsDataPoint dataPoint)
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

        public static IEnumerable<string> GetFailedFields(IEnumerable<RuleInstance> ruleInstances, FailureLevel failureLevel)
        {
            return ruleInstances.Where(
                ruleInstance => ruleInstance.GetEvaluatedFailureLevel() >= failureLevel
            ).SelectMany(
                ruleInstance => ruleInstance.GetFields().Select(
                    field => ruleInstance.RuleDefinition.DocumentTypeID + "." + field
                )
            );
        }

        public static IEnumerable<string> GetFailedFieldExplanations(IEnumerable<RuleInstance> ruleInstances, FailureLevel failureLevel)
        {
            return ruleInstances.Where(
                ruleInstance => ruleInstance.RuleDefinition.FailureLevel >= failureLevel
            ).Select(
                ruleInstance => ruleInstance.ResultMessage()
            );
        }

        public static IEnumerable<RuleDefinition> ParseRuleDefinitions(string filePath)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            if (!File.Exists(filePath)) throw new Exception($"Could not locate a file at '{filePath}'");
            string ruleDefinitionsContents = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<IEnumerable<RuleDefinition>>(ruleDefinitionsContents, settings);
        }
    }

    internal static class TaxonomyExtensions
    {
        internal static DocumentTaxonomy CopyTaxonomy(DocumentTaxonomy documentTaxonomy)
        {
            //TODO: Investigate if there is any better way to do this
            return DocumentTaxonomy.Deserialize(documentTaxonomy.Serialize());
        }
    }
}