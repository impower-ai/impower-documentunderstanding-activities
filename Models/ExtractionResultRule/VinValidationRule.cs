using Impower.DocumentUnderstanding.Rules;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models.ExtractionResults
{
    public class VinValidationRuleDefinition : RuleDefinition
    {
        public string[] Fields;
        public override RuleInstance GetRuleInstance(ExtractionResult extractionResult)
        {
            return new VinValidationRuleInstance(extractionResult, this);
        }
    }
    public class VinValidationRuleInstance : RuleInstance
    {
        internal VinValidationRuleDefinition RuleDefinition;
        private readonly List<string> FailedFields = new List<string>();
        public VinValidationRuleInstance(ExtractionResult extractionResult, VinValidationRuleDefinition ruleDefinition)
        {
            this.ExtractionResult = extractionResult;
            this.RuleDefinition = ruleDefinition;
        }
        public override RuleDefinition GetRuleDefinition()
        {
            return this.RuleDefinition;
        }
        public override string[] GetFailedFields()
        {
            if (this.Evaluation is null)
            {
                this.EvaluateRule();
            }
            return this.FailedFields.ToArray();
        }

        public override string[] GetFields()
        {
            return this.RuleDefinition.Fields;
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
}