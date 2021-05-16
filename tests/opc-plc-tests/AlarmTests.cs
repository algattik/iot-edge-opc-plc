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
    public class AlarmTests : EventTestsBase
    {
        private NodeId _eventType;

        [SetUp]
        public void CreateMonitoredItem()
        {
            _eventType = ToNodeId(Opc.Ua.ObjectTypes.TripAlarmType);

            var ns = Session.NamespaceUris.GetIndex(OpcPlc.Namespaces.OpcPlcAlarmsInstance);
            var areaNode = FindNode(Server, $"{ns}:Green/{ns}:East/{ns}:Blue");
            var southMotor = FindNode(areaNode, $"{ns}:SouthMotor");

            SetUpMonitoredItem(areaNode, NodeClass.Object, Attributes.EventNotifier);

            // add condition fields to retrieve selected event.
            var filter = (EventFilter)MonitoredItem.Filter;
            var whereClause = filter.WhereClause;
            var element1 = whereClause.Push(FilterOperator.OfType, _eventType);
            var element2 = whereClause.Push(FilterOperator.InList,
                new SimpleAttributeOperand
                {
                    AttributeId = Attributes.Value,
                    TypeDefinitionId = ObjectTypeIds.BaseEventType,
                    BrowsePath = new QualifiedName[] { BrowseNames.SourceNode },
                },
                new LiteralOperand
                {
                    Value = new Variant(southMotor)
                });

            whereClause.Push(FilterOperator.And, element1, element2);

            AddMonitoredItem();
        }

        [Test]
        public void AlarmEventSubscribed_FiresNotification()
        {
            // Arrange
            ReceivedEvents.Clear();

            // Act: collect events during 15 seconds
            // Event is fired every 10 seconds
            Thread.Sleep(15000);
            var events = ReceivedEvents.ToList();

            // Assert
            events.Should().HaveCountGreaterOrEqualTo(1)
                .And.HaveCountLessOrEqualTo(3);
            var values = events
                .Select(a => (EventFieldList)a.NotificationValue)
                .Select(EventFieldListToDictionary);
            foreach (var value in values)
            {
                value.Should().Contain(new Dictionary<string, object>
                {
                    ["/EventType"] = _eventType,
                    ["/SourceName"] = "SouthMotor",
                });
            }
        }
    }
}