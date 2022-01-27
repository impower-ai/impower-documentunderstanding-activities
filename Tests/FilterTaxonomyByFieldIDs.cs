using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.DocumentProcessing.Contracts.Taxonomy;

namespace Impower.DocumentUnderstanding.Tests
{
    [TestFixture]
    internal class FilterTaxonomyByFieldIDs_
    {
        private string documentTypeId;
        private DocumentTaxonomy inputTaxonomy;
        private DocumentTaxonomy outputTaxonomy; 
        private readonly FilterTaxonomyByFieldIds activity = new FilterTaxonomyByFieldIds();

        [SetUp]
        public void Setup()
        {
            //TODO: Is there a simpler way to do this test data creation? store test object serialized?
            documentTypeId = "TestGroup.TestCategory.TestDocument";
            inputTaxonomy = new DocumentTaxonomy
            {
                DocumentTypes = new[] {
                    new DocumentType
                    {
                        DocumentTypeId = documentTypeId,
                        Group = "TestGroup",
                        Category = "TestCategory",
                        Name = "TestDocument",
                        TypeField = new TypeField
                        {
                            FieldId = documentTypeId + ".DocumentType",
                            FieldName = "Document Type"
                        },
                        Fields = new[] {
                            new Field
                            {
                                FieldId = documentTypeId+".Field1",
                                FieldName = "Field 1",
                                IsMultiValue = false,
                                Type = FieldType.Text
                            },
                            new Field
                            {
                                FieldId = documentTypeId+".Field2",
                                FieldName = "Field 2",
                                IsMultiValue = false,
                                Type = FieldType.Text
                            }

                        }
                    }
                }
            };
        }

        [Test]
        public void FilterToFieldOne()
        {
            IEnumerable<string> fields = new[] { documentTypeId + ".Field1" };
            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                ["InputTaxonomy"] = inputTaxonomy,
                ["Fields"] = fields
            };
            var outputArguments = WorkflowInvoker.Invoke(activity, arguments);
            Console.WriteLine(JsonConvert.SerializeObject(outputArguments));
            outputTaxonomy = outputArguments["FilteredTaxonomy"] as DocumentTaxonomy;
            var outputFields = outputTaxonomy.DocumentTypes[0].Fields;
            Assert.AreEqual(outputTaxonomy.DocumentTypes[0].Fields.Count(), 1);
            Assert.AreEqual(outputFields.Where(f => f.FieldId == documentTypeId + ".Field1").Count(), 1);
            Assert.AreEqual(outputFields.Where(f => f.FieldId == documentTypeId + ".Field2").Count(), 0);
        }
        [Test]
        public void FilterToFieldTwo()
        {
            IEnumerable<string> fields = new[] { documentTypeId + ".Field2" };
            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                ["InputTaxonomy"] = inputTaxonomy,
                ["Fields"] = fields
            };
            var outputArguments = WorkflowInvoker.Invoke(activity, arguments);
            outputTaxonomy = outputArguments["FilteredTaxonomy"] as DocumentTaxonomy;
            var outputFields = outputTaxonomy.DocumentTypes[0].Fields;
            Assert.AreEqual(outputTaxonomy.DocumentTypes[0].Fields.Count(), 1);
            Assert.AreEqual(outputFields.Where(f => f.FieldId == documentTypeId + ".Field2").Count(), 1);
            Assert.AreEqual(outputFields.Where(f => f.FieldId == documentTypeId + ".Field1").Count(), 0);
        }
    }
}