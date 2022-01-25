using Impower.DocumentUnderstanding.Extensions;
using Impower.DocumentUnderstanding.Rules;
using Impower.DocumentUnderstanding.Validation;
using Newtonsoft.Json;
using NReco.Linq;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models.ExtractionResults
{
    public abstract class RuleDefinition
    {
        public string DocumentTypeID;
        public FailureLevel FailureLevel;

        public abstract RuleInstance GetRuleInstance(ExtractionResult extractionResult);
    }
    public class VinValidationRuleDefinition : RuleDefinition
    {
        public string[] Fields;
        public override RuleInstance GetRuleInstance(ExtractionResult extractionResult)
        {
            return new VinValidationRuleInstance(extractionResult, this);
        }
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

        public FailureLevel GetEvaluatedFailureLevel()
        {
            return this.GetEvaluation().FailureLevel;
        }
    }
    public class VinValidationRuleInstance : RuleInstance
    {
        private List<string> FailedFields = new List<string>();
        internal new VinValidationRuleDefinition RuleDefinition;
        public VinValidationRuleInstance(ExtractionResult extractionResult, VinValidationRuleDefinition ruleDefinition)
        {
            this.ExtractionResult = extractionResult;
            this.RuleDefinition = ruleDefinition;
        }
        public override string ResultMessage()
        {
            if (this.Evaluation is null)
            {
                this.EvaluateRule();
            }
            return String.Format(
                "[{0}] {1} following fields ({2}) had an invalid VIN number",
                this.RuleDefinition.DocumentTypeID,
                FailedFields.Any() ? "The" : "None of the",
                String.Join(",", FailedFields.Any() ? FailedFields.ToArray() : this.RuleDefinition.Fields)
            );
        }
        public override string[] GetFailedFields()
        {
            if(this.Evaluation is null)
            {
                this.EvaluateRule();
            }
            return this.FailedFields.ToArray();
        }
        public override string[] GetFields()
        {
            return this.RuleDefinition.Fields;
        }
        public override string RuleExplanation()
        {
            return String.Format(
                "[{0}] Determines if any of these fields ({1}) have an invalid VIN number",
                this.RuleDefinition.DocumentTypeID,
                String.Join(",", this.RuleDefinition.Fields)
            );
        }
        internal override void EvaluateRule()
        {
            FixVinNumber activity = new FixVinNumber();
            foreach(string fieldID in this.RuleDefinition.Fields)
            {
                string fullFieldId = this.RuleDefinition.DocumentTypeID + "." + fieldID;
                var result = WorkflowInvoker.Invoke(activity, new Dictionary<string, object>
                {
                    ["ExtractionResult"] = this.ExtractionResult,
                    ["FieldId"] = fullFieldId
                });
                if (!(Boolean)result["Valid"])
                {
                    this.FailedFields.Add(fullFieldId);
                }
            }
            if (this.FailedFields.Any())
            {
                this.Evaluation = new RuleInstanceEvaluation(this.RuleDefinition.FailureLevel, this);
            }
            else
            {
                this.Evaluation = new RuleInstanceEvaluation(FailureLevel.None, this);
            }
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

    public class LambdaRuleInstance : RuleInstance
    {
        private Dictionary<string, object> lambdaContext;
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
                "[{0}] The following expression: \"{1}\" evaluated to {2} with inputs of {3}",
                this.RuleDefinition.DocumentTypeID,
                this.RuleDefinition.Expression,
                this.LambdaResult.Value,
                JsonConvert.SerializeObject(this.lambdaContext)
            );
        }

        internal override void EvaluateRule()
        {
            lambdaContext = new Dictionary<string, object>();
            foreach (KeyValuePair<string, string> reference in this.RuleDefinition.Fields)
            {
                ResultsDataPoint data = ExtractionResultRuleExtensions.GetDataPointByFieldId(
                    $"{this.RuleDefinition.DocumentTypeID}.{reference.Value}",
                    this.ExtractionResult
                );
                lambdaContext[reference.Key] = ExtractionResultRuleExtensions.GetDataPointValue(data);
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

    [Serializable]
    public class InvalidRuleException : Exception
    {
        public InvalidRuleException()
        {
        }

        public InvalidRuleException(string message) : base(message)
        {
        }

        public InvalidRuleException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidRuleException(SerializationInfo info, StreamingContext context) : base(info, context)
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