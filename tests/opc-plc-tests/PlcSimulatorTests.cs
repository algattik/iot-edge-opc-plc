namespace OpcPlc.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class PlcSimulatorTests
    {
        const string OpcPlcNamespaceUri = "http://microsoft.com/Opc/OpcPlc/";

        private Session _session;

        [OneTimeSetUp]
        public void Setup()
        {
            _session = PlcSimulatorFixture.Instance.CreateSessionAsync(nameof(PlcSimulatorTests)).GetAwaiter().GetResult();
        }

        [Test]
        [TestCase("FastUInt1", typeof(uint), 1)]
        [TestCase("SlowUInt1", typeof(uint), 10)]
        [TestCase("RandomSignedInt32", typeof(int), 0.1)]
        [TestCase("RandomUnsignedInt32", typeof(uint), 0.1)]
        public void Telemetry_ChangesWithPeriod(string identifier, Type type, double periodInSeconds)
        {
            var nodeId = GetOpcPlcNodeId(identifier);

            var period = TimeSpan.FromSeconds(periodInSeconds);

            // Measure the value 4 times, sleeping for a third of the period at which the value changes each time.
            // The number of times the value changes over the 4 measurements should be between 1 and 2.
            object lastValue = null;
            var numberOfValueChanges = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i > 0)
                {
                    Thread.Sleep(period / 3);
                }

                var value = _session.ReadValue(nodeId).Value;
                value.Should().BeOfType(type);

                if (i > 0)
                {
                    if (((IComparable)value).CompareTo(lastValue) != 0)
                    {
                        numberOfValueChanges++;
                    }
                }

                lastValue = value;
            }

            numberOfValueChanges.Should().BeInRange(1, 2);
        }

        [Test]
        [TestCase("BadFastUInt1", 1)]
        public void BadNode_HasAlternatingStatusCode(string identifier, double periodInSeconds)
        {
            var nodeId = GetOpcPlcNodeId(identifier);

            var period = TimeSpan.FromSeconds(periodInSeconds);
            var cycles = 15;
            var readsPerPeriod = 10;
            var n = cycles * readsPerPeriod;
            var values = Enumerable.Range(0, n)
                .Select(i =>
                {
                    if (i > 0)
                    {
                        Thread.Sleep(period / readsPerPeriod);
                    }

                    try
                    {
                        var value = _session.ReadValue(nodeId);
                        return (value.StatusCode, value.Value);
                    }
                    catch (ServiceResultException e)
                    {
                        return (e.StatusCode, null);
                    }
                }).ToList();

            var valuesByStatus = values.GroupBy(v => v.StatusCode).ToDictionary(g => g.Key, g => g.ToList());

            valuesByStatus
                .Keys.Should().BeEquivalentTo(new[]
                {
                    StatusCodes.Good,
                    StatusCodes.UncertainLastUsableValue,
                    StatusCodes.BadDataLost,
                    StatusCodes.BadNoCommunication,
                });

            valuesByStatus
                .Should().ContainKey(StatusCodes.Good)
                .WhichValue
                .Should().HaveCountGreaterThan(n * 5 / 10)
                .And.OnlyContain(v => v.Value != null);

            valuesByStatus
                .Should().ContainKey(StatusCodes.UncertainLastUsableValue)
                .WhichValue
                .Should().OnlyContain(v => v.Value != null);
        }

        private NodeId GetOpcPlcNodeId(string identifier)
        {
            var nodeId = NodeId.Create(
                identifier,
                OpcPlcNamespaceUri,
                _session.NamespaceUris);
            return nodeId;
        }

    }
}