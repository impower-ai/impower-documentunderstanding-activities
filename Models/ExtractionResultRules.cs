using Impower.DocumentUnderstanding.Validation;
using System;
using System.Runtime.Serialization;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models.ExtractionResults
{
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
}