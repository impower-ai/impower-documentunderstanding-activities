using Impower.DocumentUnderstanding.Models.ExtractionResults;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UiPath.DocumentProcessing.Contracts.Results;
using UiPath.DocumentProcessing.Contracts.Taxonomy;

namespace Impower.DocumentUnderstanding.Extensions
{
    public static class ExtractionResultRuleExtensions
    {
        public static ResultsDataPoint GetDataPointByFieldId(string fieldId, ExtractionResult result)
        {
            var values = result.ResultsDocument.Fields.Where(
                field => field.FieldId == fieldId
            );
            if(values.Count() == 1)
            {
                return values.Single();
            }
            else
            {
                throw new InvalidRuleException($"Could not find field '{fieldId}' in extraction result.");
            }
        }
        private static readonly string DecimalRegexString = @"[^0-9.]";
        public static void UpdateDataPointValue(string value, string fieldId, ExtractionResult result)
        {
            var matchingField = result.ResultsDocument.Fields.Where(field => field.FieldId == fieldId).Single();
            var dataPoint = GetDataPointByFieldId(fieldId, result);
            if (dataPoint.Values.Any())
            {
                var referenceValue = dataPoint.Values.First();
                referenceValue.Value = value;
                matchingField.Values = new[] { referenceValue };
            }
            else
            {
                //TODO: should i even be handling this?
                var resultReference = new ResultsContentReference();
                var referenceValue = new ResultsValue(value,resultReference, 1, 1);
                matchingField.Values = new[] { referenceValue };
            }
        }
        public static object GetDataPointValue(ResultsDataPoint dataPoint)
        {
            //TODO: what to do in this case?
            if (!dataPoint.Values.Any())
            {
                return "";
            }

            var value = dataPoint.Values[0].Value;
            try
            {
                switch (dataPoint.FieldType)
                {
                    case FieldType.Date:
                        return DateTime.Parse(value).Date;

                    case FieldType.Number:
                        return decimal.Parse(
                            Regex.Replace(value, DecimalRegexString, String.Empty));

                    default:
                        return value;
                }
            }catch(FormatException exception)
            {
                throw new Exception($"Could not parse '{value}' as type '{dataPoint.FieldType}'", exception);
            }
        }
        public static IEnumerable<RuleInstance> GetFailedInstances(IEnumerable<RuleInstance> ruleInstances, FailureLevel failureLevel)
        {
            return ruleInstances.Where(
                ruleInstance => ruleInstance.GetEvaluatedFailureLevel() >= failureLevel
            );
        }
        public static IEnumerable<string> GetFailedMessages(IEnumerable<RuleInstance> ruleInstances, FailureLevel failureLevel)
        {
            return GetFailedInstances(ruleInstances, failureLevel).Select(
                ruleInstance => ruleInstance.ResultMessage()
            );
        }
        public static IEnumerable<string> GetFailedFields(IEnumerable<RuleInstance> ruleInstances, FailureLevel failureLevel)
        {
            return GetFailedInstances(ruleInstances, failureLevel).SelectMany(
                ruleInstance => ruleInstance.GetFields().Select(
                    field => ruleInstance.GetRuleDefinition().DocumentTypeID + "." + field
                )
            ).ToList();
        }

        public static IEnumerable<string> GetFailedFieldExplanations(IEnumerable<RuleInstance> ruleInstances, FailureLevel failureLevel)
        {
            return ruleInstances.Where(
                ruleInstance => ruleInstance.GetRuleDefinition().FailureLevel >= failureLevel
            ).Select(
                ruleInstance => ruleInstance.ResultMessage()
            );
        }
        public static IEnumerable<RuleDefinition> DeserializeRuleDefinitionsFromString(string jsonString)
        {
            //TODO: In massive need of refactor, this implementation is just the easiest way I could think to do it under time-constraints.
            //These few lines eliminate the need to use the full name in rule definition json files.
            var types = new[] { typeof(LambdaRuleDefinition), typeof(ThresholdRuleDefinition), typeof(VinValidationRuleDefinition)};
            foreach(Type type in types)
            {
                jsonString = jsonString.Replace(
                    $"\"{type.Name}\"",
                    $"\"{type.FullName}, {type.Assembly}\""
                );
            }

            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            return JsonConvert.DeserializeObject<IEnumerable<RuleDefinition>>(jsonString, settings);
        }
        public static IEnumerable<RuleDefinition> DeserializeRuleDefinitions(string filePath)
        {
            if (!File.Exists(filePath)) throw new Exception($"Could not locate a file at '{filePath}'");
            string ruleDefinitionsContents = File.ReadAllText(filePath);
            return DeserializeRuleDefinitionsFromString(ruleDefinitionsContents);
        }
        public static void SerializeRuleDefinitions(string filePath, IEnumerable<RuleDefinition> rules)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
            File.WriteAllText(filePath,JsonConvert.SerializeObject(rules, settings));
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