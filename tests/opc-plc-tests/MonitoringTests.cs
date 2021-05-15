namespace OpcPlc.Tests
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class MonitoringTests : SimulatorTestsBase
    {
        private Subscription _subscription;

        private readonly ConcurrentQueue<MonitoredItemNotificationEventArgs> _events = new ConcurrentQueue<MonitoredItemNotificationEventArgs>();
        private MonitoredItem _monitoredItem;

        [SetUp]
        public void CreateSubscription()
        {
            _subscription = Session.DefaultSubscription;
            if (Session.AddSubscription(_subscription))
                _subscription.Create();

            var nodeId = GetOpcPlcNodeId("FastUInt1");
            nodeId.Should().NotBeNull();
            CreateMonitoredItem(nodeId, NodeClass.Variable, Attributes.Value);
        }

        /// <summary>
        /// Deletes the subscription.
        /// </summary>
        [TearDown]
        public void DeleteSubscription()
        {
            if (_subscription != null)
            {
                _subscription.Delete(true);
                Session.RemoveSubscription(_subscription);
                _subscription = null;
            }
        }

        private void CreateMonitoredItem(NodeId startNodeId, NodeClass nodeClass, uint attributeId)
        {
            NewMonitoredItem(startNodeId, nodeClass, attributeId);

            AddMonitoredItem();
        }

        private void AddMonitoredItem()
        {
            _subscription.AddItem(_monitoredItem);
            _subscription.ApplyChanges();
        }

        private void NewMonitoredItem(NodeId startNodeId, NodeClass nodeClass, uint attributeId)
        {
            // add the new monitored item.
            // _monitoredItem = new MonitoredItem
            _monitoredItem = new MonitoredItem(_subscription.DefaultItem)
            {
                DisplayName = startNodeId.Identifier.ToString(),
                StartNodeId = startNodeId,
                NodeClass = nodeClass,
                SamplingInterval = 0,
                AttributeId = attributeId,
                QueueSize = 0
            };

            _monitoredItem.Notification += MonitoredItem_Notification;
        }

        private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            _events.Enqueue(e);
        }

        [Test]
        public void Monitoring_NotifiesValueUpdates()
        {
            // Arrange
            _events.Clear();

            // Act: collect events during 5 seconds
            // Value is updated every second
            Thread.Sleep(5000);
            var events = _events.ToList();

            // Assert
            events.Should().HaveCountGreaterOrEqualTo(4)
                .And.HaveCountLessOrEqualTo(6);
            var values = events.Select(a => (uint)((MonitoredItemNotification)a.NotificationValue).Value.Value).ToList();
            var differences = values.Zip(values.Skip(1), (x, y) => y - x);
            differences.Should().AllBeEquivalentTo(1, $"elements of sequence {string.Join(",", values)} should be increasing by interval 1");
        }
    }

}