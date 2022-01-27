using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models.ExtractionResults
{
    public abstract class RuleDefinition
    {
        public string DocumentTypeID;
        public FailureLevel FailureLevel;

        public abstract RuleInstance GetRuleInstance(ExtractionResult extractionResult);
    }
    public abstract class RuleInstance { 
        internal RuleInstanceEvaluation Evaluation;
        internal ExtractionResult ExtractionResult;
        public FailureLevel GetEvaluatedFailureLevel()
        {
            return this.GetEvaluation().FailureLevel;
        }
        public abstract RuleDefinition GetRuleDefinition();
        public abstract string[] GetFailedFields();

        public abstract string[] GetFields();

        public abstract string ResultMessage();

        public abstract string RuleExplanation();

        internal abstract void EvaluateRule();
        internal RuleInstanceEvaluation GetEvaluation()
        {
            if (this.Evaluation is null) this.EvaluateRule();
            return this.Evaluation;
        }
    }

}