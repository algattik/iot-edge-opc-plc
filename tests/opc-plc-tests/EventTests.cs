namespace OpcPlc.Tests
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;
    using ObjectTypeIds = SimpleEvents.ObjectTypeIds;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class EventTests : SimulatorTestsBase
    {
        private Subscription _subscription;

        private readonly ConcurrentQueue<MonitoredItemNotificationEventArgs> _events = new ConcurrentQueue<MonitoredItemNotificationEventArgs>();

        [SetUp]
        public void CreateSubscription()
        {
            _subscription = new Subscription(Session.DefaultSubscription);
            Session.AddSubscription(_subscription);
                _subscription.Create();

            var nodeId = ExpandedNodeId.ToNodeId(ObjectTypeIds.SystemCycleStartedEventType, Session.NamespaceUris);
            nodeId.Should().NotBeNull();
            CreateMonitoredItem(nodeId, MonitoringMode.Reporting);
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

        private void CreateMonitoredItem(
            NodeId nodeId,
            MonitoringMode mode)
        {
            /*
            // create a monitored item based on the current filter settings.            
            var monitoredItem = new MonitoredItem
            {
                StartNodeId = Opc.Ua.ObjectIds.Server,
                AttributeId = Attributes.EventNotifier,
                MonitoringMode = MonitoringMode.Reporting,
                NodeClass = NodeClass.Unspecified,
                SamplingInterval = 0,
                QueueSize = 1000,
                DiscardOldest = true,
                Filter = null
            };

            monitoredItem.Notification += MonitoredItem_Notification;

            _subscription.AddItem(monitoredItem);
            _subscription.ApplyChanges();
            */
            // the filter to use.
            // var a = Opc.Ua.NodeId.Create(SimpleEvents.ObjectTypes.SystemCycleStatusEventType, SimpleEvents.Namespaces.SimpleEvents, Session.NamespaceUris);
            // var n = ExpandedNodeId.ToNodeId(ObjectTypeIds.SystemCycleStatusEventType, Session.NamespaceUris);
            var n = ExpandedNodeId.ToNodeId(Opc.Ua.ObjectTypes.TripAlarmType, Session.NamespaceUris);

            /*
            TypeDeclaration pubSubStatusEventType = new TypeDeclaration
            {
                NodeId = n,
                Declarations = ClientUtils.CollectInstanceDeclarationsForType(Session, n)
            };

            var m_filter = new FilterDeclaration(pubSubStatusEventType, null);

            // var m_filter = new FilterDeclaration();
 
// create a monitored item based on the current filter settings.            
            var m_monitoredItem = new MonitoredItem();
            m_monitoredItem.StartNodeId = Opc.Ua.ObjectIds.Server;
            m_monitoredItem.StartNodeId = NodeId.Create("0:East/Blue", OpcPlc.Namespaces.OpcPlcAlarmsInstance, Session.NamespaceUris);
            m_monitoredItem.AttributeId = Attributes.EventNotifier;
            // m_monitoredItem.SamplingInterval = 0;
            m_monitoredItem.QueueSize = 1000;
            m_monitoredItem.DiscardOldest = true;
            m_monitoredItem.Filter = m_filter.GetFilter();
            m_monitoredItem.NodeClass = NodeClass.Object;
 
            _subscription.AddItem(m_monitoredItem);
            _subscription.ApplyChanges();
            */
            var subscription = _subscription;
                MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem);

                monitoredItem.DisplayName = "Server";
                monitoredItem.StartNodeId = NodeId.Parse("i=2253");
            var s = Opc.Ua.ObjectIds.Server;
            var b = Session.ReadNode(monitoredItem.StartNodeId);
                monitoredItem.NodeClass = NodeClass.Object;
                monitoredItem.AttributeId      = Attributes.Value;
                monitoredItem.SamplingInterval = 0;
                monitoredItem.QueueSize        = 1;

                // add condition fields to any event filter.
                EventFilter filter = monitoredItem.Filter as EventFilter;
                    monitoredItem.AttributeId = Attributes.EventNotifier;
                    monitoredItem.QueueSize = 0;
            monitoredItem.Notification += MonitoredItem_Notification;
        
                subscription.AddItem(monitoredItem);
                subscription.ApplyChanges();

        }

        private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            _events.Enqueue(e);
        }

        [Test]
        public void Eventing_NotifiesValueUpdates()
        {
            // Arrange
            // Thread.Sleep(6000);
            _events.Clear();

            // Act: collect events during 5 seconds
            // Event is fired every 3 seconds
            Thread.Sleep(5000);
            var events = _events.ToList();

            // Assert
            events.Should().HaveCountGreaterOrEqualTo(2)
                .And.HaveCountLessOrEqualTo(3);
            var values = events.Select(a => (uint)((MonitoredItemNotification)a.NotificationValue).Value.Value).ToList();
            var differences = values.Zip(values.Skip(1), (x, y) => y - x);
            differences.Should().AllBeEquivalentTo(1, $"elements of sequence {string.Join(",", values)} should be increasing by interval 1");
        }
    }

}