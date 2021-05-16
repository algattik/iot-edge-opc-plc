namespace OpcPlc.Tests
{
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class MonitoringTests : EventTestsBase
    {
        [SetUp]
        public void CreateMonitoredItem()
        {
            var nodeId = GetOpcPlcNodeId("FastUInt1");
            nodeId.Should().NotBeNull();

            SetUpMonitoredItem(nodeId, NodeClass.Variable, Attributes.Value);

            AddMonitoredItem();
        }

        [Test]
        public void Monitoring_NotifiesValueUpdates()
        {
            // Arrange
            ReceivedEvents.Clear();

            // Act: collect events during 5 seconds
            // Value is updated every second
            Thread.Sleep(5000);
            var events = ReceivedEvents.ToList();

            // Assert
            events.Should().HaveCountGreaterOrEqualTo(4)
                .And.HaveCountLessOrEqualTo(6);
            var values = events.Select(a => (uint)((MonitoredItemNotification)a.NotificationValue).Value.Value).ToList();
            var differences = values.Zip(values.Skip(1), (x, y) => y - x);
            differences.Should().AllBeEquivalentTo(1, $"elements of sequence {string.Join(",", values)} should be increasing by interval 1");
        }
    }
}