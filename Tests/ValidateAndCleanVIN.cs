using Impower.DocumentUnderstanding.Validation;
using NUnit.Framework;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Impower.DocumentUnderstanding.Tests
{
    [TestFixture]
    internal class ValidateAndCleanVIN_
    {
        private ValidateAndCleanVIN activity = new ValidateAndCleanVIN();
        [TestCase("1FADP3F26DLI235O0")]
        [TestCase("4T1BF1FK6EU333627")]
        [TestCase("IC6RR6NT6ES385781")]
        public void TestValidVINs(string vin)
        {
            var output = WorkflowInvoker.Invoke(
                activity,
                new Dictionary<string, object>
                {
                    ["VIN"] = vin,
                    ["FixOCR"] = true,
                }
            );
            var properties = output["Properties"] as Dictionary<string, string>;
            Assert.IsTrue((bool)output["Valid"]);
            Assert.AreEqual(
                ValidationExtensions.VinQueryFilter.Length,
                properties.Count()
            );
            Assert.IsFalse(String.IsNullOrEmpty(properties["Make"]));
        }
        [TestCase("1ADP3F26DLI235O0")]
        [TestCase("4T1BF1FK6EU333627F")]
        [TestCase("IC6RR5NT6ES385781")]
        public void TestInvalidVINs(string vin)
        {
            var output = WorkflowInvoker.Invoke(
                activity,
                new Dictionary<string, object>
                {
                    ["VIN"] = vin,
                    ["FixOCR"] = true,
                }
            );
            Assert.IsFalse((bool)output["Valid"]);
        }
    }
}
