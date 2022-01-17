using Impower.DocumentUnderstanding.Extensions;
using NReco.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models.ExtractionResults
{
    public abstract class RuleDefinition
    {
        public string[] RelevantFields;
        public string DocumentTypeID;
        public FailureLevel FailureLevel;

        public abstract RuleInstance GetRuleInstance(ExtractionResult extractionResult);
    }

    public class ThresholdRuleDefinition : RuleDefinition
    {
        public string[] Fields;
        public ConfidenceType ConfidenceType;
        public float OcrThreshold = 0.0f;
        public float DuThreshold = 0.0f;

        public override RuleInstance GetRuleInstance(ExtractionResult extractionResult)
        {
            return new ThresholdRuleInstance(extractionResult, this);
        }
    }

    public class LambdaRuleDefinition : RuleDefinition
    {
        public Dictionary<string, string> Fields;
        public string Expression;

        public override RuleInstance GetRuleInstance(ExtractionResult extractionResult)
        {
            return new LambdaRuleInstance(extractionResult, this);
        }
    }

    public abstract class RuleInstance
    {
        internal RuleDefinition RuleDefinition;
        internal RuleInstanceEvaluation Evaluation;
        internal ExtractionResult ExtractionResult;

        internal abstract void EvaluateRule();

        public abstract string ResultMessage();

        public abstract string RuleExplanation();

        public abstract string[] GetFields();

        public abstract string[] GetFailedFields();

        internal RuleInstanceEvaluation GetEvaluation()
        {
            if (this.Evaluation is null) this.EvaluateRule();
            return this.Evaluation;
        }

        internal FailureLevel GetEvaluatedFailureLevel()
        {
            return this.GetEvaluation().FailureLevel;
        }
    }

    public class ThresholdRuleInstance : RuleInstance
    {
        internal new ThresholdRuleDefinition RuleDefinition;
        private List<string> FailedFields = new List<string>();

        public ThresholdRuleInstance(ExtractionResult extractionResult, ThresholdRuleDefinition ruleDefinition)
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
                FailedFields.Any() ? "The" : "None of the",
                this.RuleDefinition.DocumentTypeID,
                String.Join(",", this.RuleDefinition.Fields),
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

    public class LambdaRuleInstance : RuleInstance
    {
        internal new LambdaRuleDefinition RuleDefinition;
        internal static LambdaParser Parser = new LambdaParser();
        internal Nullable<bool> LambdaResult;

        public LambdaRuleInstance(ExtractionResult extractionResult, LambdaRuleDefinition ruleDefinition)
        {
            this.RuleDefinition = ruleDefinition;
            this.ExtractionResult = extractionResult;
        }

        public override string[] GetFields()
        {
            return RuleDefinition.Fields.Select(kvp => kvp.Value).Distinct().ToArray();
        }

        public override string[] GetFailedFields()
        {
            if (!LambdaResult.HasValue) this.EvaluateRule();
            if (LambdaResult.Value)
            {
                return new string[] { };
            }
            else
            {
                return this.GetFields();
            }
        }

        public override string RuleExplanation()
        {
            return String.Format(
                "[{0}] Determines if the following expression is true or false - \"{1}\"",
                this.RuleDefinition.DocumentTypeID,
                this.RuleDefinition.Expression
            );
        }

        public override string ResultMessage()
        {
            if (!this.LambdaResult.HasValue) this.EvaluateRule();
            return String.Format(
                "[{0}] The following expression evaluated \"{1}\" to {2}",
                this.RuleDefinition.DocumentTypeID,
                this.RuleDefinition.Expression,
                this.LambdaResult.Value
            );
        }

        internal override void EvaluateRule()
        {
            var lambdaContext = new Dictionary<string, object>();
            foreach (KeyValuePair<string, string> reference in this.RuleDefinition.Fields)
            {
                ResultsDataPoint value = this.ExtractionResult.ResultsDocument.Fields.Where(
                    field => field.FieldId == reference.Value
                ).Single();
                lambdaContext[reference.Key] = ExtractionResultRuleExtensions.GetDataPointValue(value);
            }
            this.LambdaResult = (bool)Parser.Eval(this.RuleDefinition.Expression, lambdaContext);
            this.Evaluation = new RuleInstanceEvaluation(
                this.LambdaResult.Value ? FailureLevel.None : this.RuleDefinition.FailureLevel,
                this
            );
        }
    }

    public class RuleInstanceEvaluation
    {
        public FailureLevel FailureLevel;
        internal RuleInstance Rule;

        public RuleInstanceEvaluation(FailureLevel failureLevel, RuleInstance rule)
        {
            this.FailureLevel = failureLevel;
            this.Rule = rule;
        }
    }

    public class InvalidRuleException : Exception
    {
        public InvalidRuleException(string message) : base(message)
        {
        }

        public InvalidRuleException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public enum ConfidenceType
    {
        OcrConfidence,
        Confidence
    }

    public enum FailureLevel
    {
        None,
        Warning,
        ActionCenter,
        Exception
    }
}