using Impower.DocumentUnderstanding.Extensions;
using Impower.DocumentUnderstanding.Models.ExtractionResults;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Impower.DocumentUnderstanding.Tests
{
    [TestFixture]
    internal class RuleSerialization
    {
        [TestCase]
        public void LambdaNamespaceTest()
        {
            var json = @"[
                          {
                            ""$type"": ""LambdaRuleDefinition"",
                            ""Fields"": {
                            ""test"": ""value""
                            },
                            ""Expression"": ""test"",
                            ""DocumentTypeID"": ""test"",
                            ""FailureLevel"": 0
                          }
                        ]
                        ";
            var rule = ExtractionResultRuleExtensions.DeserializeRuleDefinitionsFromString(json).First() as LambdaRuleDefinition;
            Assert.AreEqual(rule.Expression, "test");
        }

        [TestCase]
        public void LambdaTest()
        {
            var path = Path.GetTempFileName() + ".json";
            var rule = new LambdaRuleDefinition
            {
                DocumentTypeID = "test",
                Expression = "test",
                FailureLevel = FailureLevel.None,
                Fields = new Dictionary<string, string>
                {
                    { "test", "value" }
                }
            };
            ExtractionResultRuleExtensions.SerializeRuleDefinitions(path, new[] { rule });
            var deserializedRule = ExtractionResultRuleExtensions.DeserializeRuleDefinitions(path).First() as LambdaRuleDefinition;
            Assert.AreEqual(rule.Expression, deserializedRule.Expression);
            Assert.AreEqual(rule.FailureLevel, deserializedRule.FailureLevel);
            Assert.AreEqual(rule.DocumentTypeID, deserializedRule.DocumentTypeID);
        }
    }
}