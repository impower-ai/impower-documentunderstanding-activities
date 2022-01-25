using Impower.DocumentUnderstanding.Extensions;
using Impower.DocumentUnderstanding.Models.ExtractionResults;
using Impower.DocumentUnderstanding.Validation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Rules
{
    [DisplayName("Fix And Validate VIN Number")]
    public class FixVinNumber : CodeActivity
    {
        [DisplayName("Extraction Result")]
        [RequiredArgument]
        [Category("Input")]
        public InOutArgument<ExtractionResult> ExtractionResult { get; set; }

        [DisplayName("VIN Field")]
        [RequiredArgument]
        [Category("Input")]
        public InArgument<string> FieldId { get; set; }

        [DisplayName("Valid?")]
        [Category("Output")]
        public OutArgument<bool> Valid { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            ExtractionResult result = ExtractionResult.Get(context);
            var fieldId = FieldId.Get(context);
            var dataPoint = ExtractionResultRuleExtensions.GetDataPointByFieldId(fieldId, result);
            var vin = ExtractionResultRuleExtensions.GetDataPointValue(dataPoint) as string;
            var valid = ValidationExtensions.ValidateVinWithNHTSA(ref vin, out Dictionary<string, string> properties, true);
            if (valid)
            {
                ExtractionResultRuleExtensions.UpdateDataPointValue(vin, fieldId, result);
            }
            Valid.Set(context, valid);
            ExtractionResult.Set(context, result);
        }
    }
    [DisplayName("Run Rule Set On Extraction Result")]
    public class RunRulesOnExtractionResult : CodeActivity
    {
        [RequiredArgument]
        [Category("Input")]
        [DisplayName("Rule Definitions")]
        public InArgument<IEnumerable<RuleDefinition>> RuleDefinitions { get; set; }

        [RequiredArgument]
        [Category("Input")]
        [DisplayName("Extraction Result")]
        public InOutArgument<ExtractionResult> ExtractionResult { get; set; }

        [Category("Output")]
        [DisplayName("Failed Fields")]
        public OutArgument<IEnumerable<string>> FailedFields { get; set; }

        [Category("Output")]
        [DisplayName("Rule Instances")]
        public OutArgument<IEnumerable<RuleInstance>> RuleInstances { get; set; }

        [Category("Output")]
        [DisplayName("Messages")]
        public OutArgument<IEnumerable<string>> Messages { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var ruleDefinitions = RuleDefinitions.Get(context);
            var extractionResult = ExtractionResult.Get(context);
            var ruleInstances = ruleDefinitions.Select(
                ruleDefinition => ruleDefinition.GetRuleInstance(extractionResult)
            );
            var ruleInstanceEvaluations = ruleInstances.Select(
                ruleInstance => ruleInstance.Evaluation
            );
            foreach(RuleInstance rule in ruleInstances)
            {
                rule.EvaluateRule();
            }
            Messages.Set(context, ruleInstances.Select(instance => instance.ResultMessage()));
            FailedFields.Set(context, ruleInstances.SelectMany(instance => instance.GetFailedFields()).Distinct());
            RuleInstances.Set(context, ruleInstances);
            ExtractionResult.Set(context, extractionResult);
        }
    }

    [DisplayName("Run Rule On Extraction Result")]
    public class RunRuleOnExtractionResult : CodeActivity
    {
        [RequiredArgument]
        [Category("Input")]
        [DisplayName("Rule Definition")]
        public InArgument<RuleDefinition> RuleDefinition { get; set; }

        [RequiredArgument]
        [Category("Input")]
        [DisplayName("Extraction Result")]
        public InOutArgument<ExtractionResult> ExtractionResult { get; set; }

        [Category("Output")]
        [DisplayName("Failure Level")]
        public OutArgument<FailureLevel> FailureLevel { get; set; }

        [Category("Output")]
        [DisplayName("Rule Instance")]
        public OutArgument<RuleInstance> RuleInstance { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var ruleDefinition = RuleDefinition.Get(context);
            var extractionResult = ExtractionResult.Get(context);
            var ruleInstance = ruleDefinition.GetRuleInstance(extractionResult);
            var ruleInstanceEvaluation = ruleInstance.Evaluation;
            RuleInstance.Set(context, ruleInstance);
            FailureLevel.Set(context, ruleInstanceEvaluation.FailureLevel);
            ExtractionResult.Set(context, extractionResult);
        }
    }
    [DisplayName("Load Rules From File")]
    public class LoadRulesFromFile : CodeActivity
    {
        [Category("Input")]
        [DisplayName("File Path")]
        [RequiredArgument]
        public InArgument<string> FilePath { get; set; }
        [RequiredArgument]
        [Category("Output")]
        [DisplayName("Rules")]
        public OutArgument<IEnumerable<RuleDefinition>> Rules { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var rules = ExtractionResultRuleExtensions.DeserializeRuleDefinitions(FilePath.Get(context));
            Rules.Set(context, rules);
        }
    }
}

namespace Impower.DocumentUnderstanding
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
            ExtractionResult resultToCopyFrom = ResultToCopyFrom.Get(context);
            if(resultToCopyFrom.DocumentId != resultToCopyTo.DocumentId)
            {
                throw new System.Exception("DocumentId's do not match - indicating these ExtractionResults are not from the same document");
            }

            var dataPointDictionary = resultToCopyTo.ResultsDocument.Fields.ToDictionary(
                field => field.FieldId,
                field => field
            );

            foreach (ResultsDataPoint field in ResultToCopyFrom.Get(context).ResultsDocument.Fields)
            {
                dataPointDictionary[field.FieldId] = field;
            }

            resultToCopyTo.ResultsDocument.Fields = dataPointDictionary.Select(kvp => kvp.Value).ToArray();
            ResultToCopyTo.Set(context, resultToCopyTo);
        }
    }
}