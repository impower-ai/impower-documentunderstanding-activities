using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UiPath.DocumentProcessing.Contracts.Results;
using UiPath.DocumentProcessing.Contracts.Taxonomy;

namespace Impower.DocumentUnderstanding.Tests
{   
    [TestFixture]
    internal class MergeExtractionResults_
    {
        private string documentTypeId;
        private string documentID = Guid.NewGuid().ToString();
        private ExtractionResult result1;
        private ExtractionResult result2;
        private MergeExtractionResults activity = new MergeExtractionResults();

        [SetUp]
        public void Setup()
        {
            Console.WriteLine("Setting Up Test");
            //TODO: Cleanup/parameterize test data creation.
            documentTypeId = "TestGroup.TestCategory.TestDocument";
            result1 = new ExtractionResult
            {
                DocumentId = documentID,
                ResultsVersion = 0,
                ResultsDocument = new ResultsDocument
                {
                    Language = "eng",
                    DataVersion = 0,
                    DocumentGroup = "TestGroup",
                    DocumentCategory = "TestCategory",
                    DocumentTypeName = "TestDocument",
                    DocumentTypeId = documentTypeId,
                    DocumentTypeField = new ResultsValue(
                        "TestDocument",
                        new ResultsContentReference(),
                        1f,
                        1f
                    ),
                    DocumentTypeSource = ResultsDataSource.Automatic,
                    DocumentTypeDataVersion = 0,
                    Bounds = new ResultsDocumentBounds(0, 0),
                    Fields = new[]
                    {
                        new ResultsDataPoint(
                            documentTypeId + ".Field1",
                            "Field 1",
                            FieldType.Number,
                            new[]{
                                new ResultsValue(
                                "1.1",
                                new ResultsContentReference(),
                                1f,
                                1f
                                )
                            }
                        ),
                        new ResultsDataPoint(
                            documentTypeId + ".Field2",
                            "Field 2",
                            FieldType.Text,
                            new[]{
                                new ResultsValue(
                                "value one",
                                new ResultsContentReference(),
                                1f,
                                1f
                                )
                            }
                        )
                    }
                }
            };
            Console.WriteLine("Created First Object");
            result2 = ExtractionResult.Deserialize(result1.Serialize());
            result2.ResultsDocument.Fields[0].Values[0].Value = "2.2";
            result2.ResultsDocument.Fields[1].Values[0].Value = "value two";
        }

        [Test]
        public void Merge_1()
        {
            WorkflowInvoker.Invoke(
                activity,
                new Dictionary<string, object>
                {
                    ["ResultToCopyTo"] = result1,
                    ["ResultToCopyFrom"] = result2,
                }
            );
            var combinedExtractionResult = result1;
            Assert.AreEqual(combinedExtractionResult.ResultsDocument.Fields[0].Values[0].Value,"2.2");
            Assert.AreEqual(combinedExtractionResult.ResultsDocument.Fields[1].Values[0].Value,"value two");
        }
        [Test]
        public void Merge_2()
        {
            WorkflowInvoker.Invoke(
                activity,
                new Dictionary<string, object>
                {
                    ["ResultToCopyTo"] = result2,
                    ["ResultToCopyFrom"] = result1,
                }
            );
            var combinedExtractionResult = result1;
            Assert.AreEqual(combinedExtractionResult.ResultsDocument.Fields[0].Values[0].Value, "1.1");
            Assert.AreEqual(combinedExtractionResult.ResultsDocument.Fields[1].Values[0].Value, "value one");
        }
        [Test]
        public void Merge_3()
        {
            result2.ResultsDocument.Fields = result2.ResultsDocument.Fields.Take(1).ToArray();
            WorkflowInvoker.Invoke(
                activity,
                new Dictionary<string, object>
                {
                    ["ResultToCopyTo"] = result1,
                    ["ResultToCopyFrom"] = result2,
                }
            );
            var combinedExtractionResult = result1;
            Assert.AreEqual(combinedExtractionResult.ResultsDocument.Fields[0].Values[0].Value, "2.2");
            Assert.AreEqual(combinedExtractionResult.ResultsDocument.Fields[1].Values[0].Value, "value one");
        }
    }
}
