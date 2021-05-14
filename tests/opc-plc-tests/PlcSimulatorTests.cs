namespace OpcPlc.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class PlcSimulatorTests : SimulatorTestsBase
    {
        private const string OpcPlcNamespaceUri = "http://microsoft.com/Opc/OpcPlc/";
        
        // FIXME: simulator does not update trended and boolean values in the first few cycles
        private const int RampUpPeriods = 6;

        [Test]
        [TestCase("FastUInt1", typeof(uint), 1, 0)]
        [TestCase("SlowUInt1", typeof(uint), 10, 0)]
        [TestCase("RandomSignedInt32", typeof(int), 0.1, 0)]
        [TestCase("RandomUnsignedInt32", typeof(uint), 0.1, 0)]
        [TestCase("AlternatingBoolean", typeof(bool), 3, 0)]
        [TestCase("NegativeTrendData", typeof(double), 5, RampUpPeriods)]
        [TestCase("PositiveTrendData", typeof(double), 5, RampUpPeriods)]
        public void Telemetry_ChangesWithPeriod(string identifier, Type type, double periodInSeconds, int rampUpPeriods)
        {
            var nodeId = GetOpcPlcNodeId(identifier);

            var period = TimeSpan.FromSeconds(periodInSeconds);
            
            Thread.Sleep(period * rampUpPeriods);

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

                var value = Session.ReadValue(nodeId).Value;
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
                        var value = Session.ReadValue(nodeId);
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

        [Test]
        [TestCase("NegativeTrendData", 5, false)]
        [TestCase("PositiveTrendData", 5, true)]
        public void TrendDataNode_HasValueWithTrend(string identifier, double periodInSeconds, bool increasing)
        {
            var nodeId = GetOpcPlcNodeId(identifier);

            var period = TimeSpan.FromSeconds(periodInSeconds);
            Thread.Sleep(period * RampUpPeriods);

            var firstValue = (double)Session.ReadValue(nodeId).Value;
            Thread.Sleep(period);
            var secondValue = (double)Session.ReadValue(nodeId).Value;
            if (increasing)
            {
                secondValue.Should().BeGreaterThan(firstValue);
            }
            else
            {
                secondValue.Should().BeLessThan(firstValue);
            }
        }

        private NodeId GetOpcPlcNodeId(string identifier)
        {
            var nodeId = NodeId.Create(
                identifier,
                OpcPlcNamespaceUri,
                Session.NamespaceUris);
            return nodeId;
        }

    }
}