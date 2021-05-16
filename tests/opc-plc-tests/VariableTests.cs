namespace OpcPlc.Tests
{
    using System;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class VariableTests : SimulatorTestsBase
    {
        [Test]
        public void WriteValue_UpdatesValue()
        {
            var nodeId = FindNode(ObjectsFolder, OpcPlc.Namespaces.OpcPlcReferenceTest, "ReferenceTest", "Scalar", "Scalar_Static", "Scalar_Static_Double");
            var newValue = new Random().NextDouble();

            var valuesToWrite = new WriteValueCollection
            {
                new WriteValue
                {
                    NodeId = nodeId,
                    AttributeId = Attributes.Value,
                    Value =
                    {
                        Value = newValue
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

            currentValue.Value.Should().Be(newValue);
        }
    }
}