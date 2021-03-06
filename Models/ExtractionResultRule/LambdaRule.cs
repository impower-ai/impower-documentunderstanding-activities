using Impower.DocumentUnderstanding.Extensions;
using Newtonsoft.Json;
using NReco.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UiPath.DocumentProcessing.Contracts.Results;

namespace Impower.DocumentUnderstanding.Models.ExtractionResults
{
    public class LambdaRuleDefinition : RuleDefinition
    {
        public string Expression;
        public Dictionary<string, string> Fields;
        public override RuleInstance GetRuleInstance(ExtractionResult extractionResult)
        {
            return new LambdaRuleInstance(extractionResult, this);
        }
    }
    public class LambdaRuleInstance : RuleInstance
    {
        internal static LambdaParser Parser = new LambdaParser();
        internal Nullable<bool> LambdaResult;
        internal LambdaRuleDefinition RuleDefinition;
        private Dictionary<string, object> lambdaContext;
        private Exception exception;
        public LambdaRuleInstance(ExtractionResult extractionResult, LambdaRuleDefinition ruleDefinition)
        {
            this.RuleDefinition = ruleDefinition;
            this.ExtractionResult = extractionResult;
        }
        public override RuleDefinition GetRuleDefinition()
        {
            return this.RuleDefinition;
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

        public override string[] GetFields()
        {
            return RuleDefinition.Fields.Select(kvp => kvp.Value).Distinct().ToArray();
        }
        public override string ResultMessage()
        {
            if (!this.LambdaResult.HasValue) this.EvaluateRule();
            if (this.exception != null)
            {
                return String.Format(
                "[{0}] The following expression \"{1}\" could not be understood due to the following error - {2}",
                this.RuleDefinition.DocumentTypeID,
                this.RuleDefinition.Expression,
                this.exception.Message
                );
            }
            else
            {
                return String.Format(
                    "[{0}] The following expression: \"{1}\" evaluated to {2} with inputs of {3}",
                    this.RuleDefinition.DocumentTypeID,
                    this.RuleDefinition.Expression,
                    this.LambdaResult.Value,
                    JsonConvert.SerializeObject(this.lambdaContext)
                );
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
        internal override void EvaluateRule()
        {
            try
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
            }
            catch (Exception e)
            {
                this.exception = e;
                this.LambdaResult = false;
            }

            this.Evaluation = new RuleInstanceEvaluation(
                    this.LambdaResult.Value ? FailureLevel.None : this.RuleDefinition.FailureLevel,
                    this
                );
        }
    }
}