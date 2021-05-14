namespace OpcPlc.Tests
{
    using System;
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

        private PlcSimulatorFixture _simulator;
        private Session _session;

        [OneTimeSetUp]
        public void Setup()
        {
            _simulator = new PlcSimulatorFixture();
            _session = _simulator.CreateSessionAsync(nameof(PlcSimulatorTests)).GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _session.CloseSession(null, true);
            _simulator.Dispose();
        }

        [Test]
        [TestCase("FastUInt1", typeof(uint), 1)]
        [TestCase("SlowUInt1", typeof(uint), 10)]
        [TestCase("BadFastUInt1", typeof(uint), 1)]
        [TestCase("BadSlowUInt1", typeof(uint), 10)]
        [TestCase("RandomSignedInt32", typeof(int), 0.1)]
        [TestCase("RandomUnsignedInt32", typeof(uint), 0.1)]
        public void Telemetry_ChangesWithPeriod(string identifier, Type type, double periodInSeconds)
        {
            var nodeId = NodeId.Create(
                identifier,
                OpcPlcNamespaceUri,
                _session.NamespaceUris);

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
    }
}