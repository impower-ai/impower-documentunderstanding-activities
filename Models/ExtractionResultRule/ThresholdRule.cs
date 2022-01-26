using System;
using System.Collections.Generic;
using System.Linq;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models.ExtractionResults
{
    public class ThresholdRuleDefinition : RuleDefinition
    {
        public ConfidenceType ConfidenceType;
        public float DuThreshold = 0.0f;
        public string[] Fields;
        public float OcrThreshold = 0.0f;
        public override RuleInstance GetRuleInstance(ExtractionResult extractionResult)
        {
            return new ThresholdRule(extractionResult, this);
        }
    }
    public class ThresholdRule : RuleInstance
    {
        internal new ThresholdRuleDefinition RuleDefinition;
        private List<string> FailedFields = new List<string>();

        public ThresholdRule(ExtractionResult extractionResult, ThresholdRuleDefinition ruleDefinition)
        {
            this.ExtractionResult = extractionResult;
            this.RuleDefinition = ruleDefinition;
        }

        public override string[] GetFailedFields()
        {
            if (this.Evaluation is null) this.EvaluateRule();
            return this.FailedFields.ToArray();
        }

        public override string[] GetFields()
        {
            return RuleDefinition.Fields;
        }

        public override string ResultMessage()
        {
            if (this.Evaluation is null) this.EvaluateRule();
            return String.Format(
                "[{0}] {1} following fields ({2}) had an OcrConfidence below {3} or a Confidence below {4}.",
                this.RuleDefinition.DocumentTypeID,
                FailedFields.Any() ? "The" : "None of the",
                String.Join(",", FailedFields.Any() ? FailedFields.ToArray() : this.RuleDefinition.Fields),
                this.RuleDefinition.OcrThreshold.ToString("n2"),
                this.RuleDefinition.DuThreshold.ToString("n2")
            );
        }

        public override string RuleExplanation()
        {
            return String.Format(
                "[{0}] Determines if any of these fields ({1}) have and OcrConfidence below {2} or a Confidence below {3}.",
                this.RuleDefinition.DocumentTypeID,
                String.Join(",", this.RuleDefinition.Fields),
                this.RuleDefinition.OcrThreshold.ToString("n2"),
                this.RuleDefinition.DuThreshold.ToString("n2")
            );
        }

        internal override void EvaluateRule()
        {
            foreach (string field in this.RuleDefinition.Fields)
            {
                var dataPoints = this.ExtractionResult.ResultsDocument.Fields.Where(
                    resultField => resultField.FieldId == $"{this.RuleDefinition.DocumentTypeID}.{field}"
                );
                if (!dataPoints.Any())
                {
                    throw new InvalidRuleException($"Field '{field}' does not exist in the results for '{this.RuleDefinition.DocumentTypeID}'");
                }
                var invalidValues = dataPoints.SelectMany(dataPoint => dataPoint.Values).Where(
                    resultsValue => resultsValue.OcrConfidence < this.RuleDefinition.OcrThreshold || resultsValue.Confidence < this.RuleDefinition.DuThreshold
                );
                if (invalidValues.Any())
                {
                    this.FailedFields.Add(field);
                }
            }
            this.Evaluation = new RuleInstanceEvaluation(
                this.FailedFields.Any() ? this.RuleDefinition.FailureLevel : FailureLevel.None,
                this
            );
        }
    }


}