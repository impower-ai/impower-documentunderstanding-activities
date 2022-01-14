using Newtonsoft.Json.Linq;
using NReco.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models
{
    public enum ConfidenceType
    {
        OcrConfidence,
        Confidence
    }
    public enum FailureLevel
    {
        Exception,
        ActionCenter,
        Warning,
        None
    }
    public class ExtractionResultEvaluation
    {
        public FailureLevel FailureLevel;
        public string Message;
        public ExtractionResultEvaluation(FailureLevel failureLevel, string message)
        {
            this.FailureLevel = failureLevel;
            this.Message = message;
        }
        
    }
    public abstract class ExtractionResultRule
    {
        internal ExtractionResult result;
        internal FailureLevel failureLevel;
        internal abstract ExtractionResultEvaluation EvaluateRule();
        public ExtractionResultRule(ExtractionResult extractionResult, JObject ruleDefinition)
        {
            this.failureLevel = (FailureLevel)Enum.Parse(typeof(FailureLevel), ruleDefinition["FailureLevel"].ToString());
            this.result = extractionResult;
        }
    }
    public class ExtractionResultThreshold : ExtractionResultRule
    {
        internal decimal threshold;
        internal string targetField;
        internal ConfidenceType confidenceType;

        public ExtractionResultThreshold(ExtractionResult extractionResult, JObject ruleDefinition) : base(extractionResult,ruleDefinition)
        {
            this.threshold = ruleDefinition["Threshold"].ToObject<decimal>();
            this.confidenceType = (ConfidenceType)Enum.Parse(typeof(ConfidenceType), ruleDefinition["FailureLevel"].ToString())
            
        }
        internal override ExtractionResultEvaluation EvaluateRule()
        {
            throw new NotImplementedException();
        }
    }

    public class ExtractionResultLambda : ExtractionResultRule
    {
        internal static LambdaParser lambdaParser = new LambdaParser();
        internal string lambdaExpression;
        internal Dictionary<string, string> fields;

        public ExtractionResultLambda(ExtractionResult extractionResult, JObject ruleDefinition): base(extractionResult, ruleDefinition)
        {
            this.lambdaExpression = ruleDefinition["Expression"].ToString();
            this.fields = (ruleDefinition["Fields"] as JObject).ToObject<Dictionary<string, string>>();
        }
        internal override ExtractionResultEvaluation EvaluateRule()
        {
            var lambdaContext = new Dictionary<string, object>();
            foreach (KeyValuePair<string, string> reference in fields)
            {
                ResultsDataPoint value = this.result.ResultsDocument.Fields.Where(
                    field => field.FieldId == reference.Value
                ).Single();
                lambdaContext[reference.Key] = ExtractionResultExtensions.GetDataPointValue(value);
            }
            bool success = (bool)lambdaParser.Eval(lambdaExpression, lambdaContext);
            if(success) {
                return new ExtractionResultEvaluation(
                    FailureLevel.None,
                    "[TRUE] - " + this.lambdaExpression
                );
            } else {
                return new ExtractionResultEvaluation(
                    this.failureLevel,
                    "[FALSE] - " + this.lambdaExpression
                );
            }
        }
    }
}
