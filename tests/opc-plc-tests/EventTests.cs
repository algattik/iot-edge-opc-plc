namespace OpcPlc.Tests
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class EventTests : SimulatorTestsBase
    {
        private Subscription _subscription;

        private readonly ConcurrentQueue<MonitoredItemNotificationEventArgs> _events = new ConcurrentQueue<MonitoredItemNotificationEventArgs>();
        private MonitoredItem _monitoredItem;
        private static readonly NodeId Server = Opc.Ua.ObjectIds.Server;
        private NodeId _eventType;

        [SetUp]
        public void CreateSubscription()
        {
            _subscription = new Subscription(Session.DefaultSubscription);
            Session.AddSubscription(_subscription);
            _subscription.Create();

            _eventType = ExpandedNodeId.ToNodeId(SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType, Session.NamespaceUris);
            _eventType.Should().NotBeNull();
            CreateMonitoredItem(Server, _eventType, NodeClass.Object, Attributes.EventNotifier);
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

        private void CreateMonitoredItem(NodeId startNodeId, NodeId nodeId, NodeClass nodeClass, uint attributeId)
        {
            NewMonitoredItem(startNodeId, nodeClass, attributeId);

            // add condition fields to retrieve selected event.
            var filter = (EventFilter)_monitoredItem.Filter;
            var whereClause = filter.WhereClause;
            whereClause.Push(FilterOperator.OfType, nodeId);

            AddMonitoredItem();
        }

        private void AddMonitoredItem()
        {
            _subscription.AddItem(_monitoredItem);
            _subscription.ApplyChanges();
        }

        private void NewMonitoredItem(NodeId startNodeId, NodeClass nodeClass, uint attributeId)
        {
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
        public void EventSubscribed_FiresNotification()
        {
            // Arrange
            _events.Clear();

            // Act: collect events during 5 seconds
            // Event is fired every 3 seconds
            Thread.Sleep(5000);
            var events = _events.ToList();

            // Assert
            events.Should().HaveCountGreaterOrEqualTo(2)
                .And.HaveCountLessOrEqualTo(5);
            var values = events
                .Select(a => (EventFieldList)a.NotificationValue)
                .Select(EventFieldListToDictionary);
            foreach (var value in values)
            {
                value.Should().Contain(new Dictionary<string, object>
                {
                    ["/EventType"] = _eventType,
                    ["/SourceNode"] = Server,
                    ["/SourceName"] = "System",
                });
                value.Should().ContainKey("/Message")
                    .WhichValue.Should().BeOfType<LocalizedText>()
                    .Which.Text.Should().MatchRegex("^The system cycle '\\d+' has started\\.$");
            }
        }

        private Dictionary<string, object> EventFieldListToDictionary(EventFieldList arg)
        {
            return
                ((EventFilter)_monitoredItem.Filter).SelectClauses // all retrieved fields for event
                .Zip(arg.EventFields) // values of retrieved fields
                .ToDictionary(
                    p => SimpleAttributeOperand.Format(p.First.BrowsePath), // e.g. "/EventId"
                    p => p.Second.Value);
        }
    }

}