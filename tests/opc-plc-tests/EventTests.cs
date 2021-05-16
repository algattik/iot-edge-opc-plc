namespace OpcPlc.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class EventTests : EventTestsBase
    {
        private NodeId _eventType;

        [SetUp]
        public void CreateMonitoredItem()
        {
            _eventType = ToNodeId(SimpleEvents.ObjectTypeIds.SystemCycleStartedEventType);
            
            SetUpMonitoredItem(Server, NodeClass.Object, Attributes.EventNotifier);

            // add condition fields to retrieve selected event.
            var filter = (EventFilter)MonitoredItem.Filter;
            var whereClause = filter.WhereClause;
            whereClause.Push(FilterOperator.OfType, _eventType);

            AddMonitoredItem();
        }

        [Test]
        public void EventSubscribed_FiresNotification()
        {
            // Arrange
            ReceivedEvents.Clear();

            // Act: collect events during 5 seconds
            // Event is fired every 3 seconds
            Thread.Sleep(5000);
            var events = ReceivedEvents.ToList();

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
    }
}