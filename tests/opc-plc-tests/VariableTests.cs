namespace OpcPlc.Tests
{
    using System.Collections;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class VariableTests : SimulatorTestsBase
    {
        private NodeId _scalarStaticNode;

        [SetUp]
        public void SetUp()
        {
            _scalarStaticNode = FindNode(ObjectsFolder, OpcPlc.Namespaces.OpcPlcReferenceTest, "ReferenceTest", "Scalar", "Scalar_Static");
        }

        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData(new[] { "Scalar_Static_Double" }, Fake.Random.Double());
                yield return new TestCaseData(new[] { "Scalar_Static_Arrays", "Scalar_Static_Arrays_String" }, Fake.Lorem.Words());
            }
        }

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void WriteValue_UpdatesValue(string[] pathParts, object newValue)
        {
            var nodeId = FindNode(_scalarStaticNode, OpcPlc.Namespaces.OpcPlcReferenceTest, pathParts);

            var valuesToWrite = new WriteValueCollection
            {
                new WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value =
                    {
                        Value = newValue,
                    }
                }
            };

            // write value.
            Session.Write(
                default,
                valuesToWrite,
                out var results,
                out var diagnosticInfos);

            results.Should().BeEquivalentTo(new[] { StatusCodes.Good });

            var currentValue = Session.ReadValue(nodeId);

            currentValue.Value.Should().BeEquivalentTo(newValue);
        }
    }
}