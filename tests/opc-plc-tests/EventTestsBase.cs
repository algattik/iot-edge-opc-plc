namespace OpcPlc.Tests
{
    using System.Collections.Concurrent;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public abstract class EventTestsBase : SimulatorTestsBase
    {
        private Subscription _subscription;

        protected readonly ConcurrentQueue<MonitoredItemNotificationEventArgs> ReceivedEvents = new ConcurrentQueue<MonitoredItemNotificationEventArgs>();
        protected MonitoredItem MonitoredItem;
        protected static readonly NodeId Server = Opc.Ua.ObjectIds.Server;

        [SetUp]
        public void CreateSubscription()
        {
            _subscription = Session.DefaultSubscription;
            Session.AddSubscription(_subscription);
                _subscription.Create();
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

        protected void AddMonitoredItem()
        {
            _subscription.AddItem(MonitoredItem);
            _subscription.ApplyChanges();
        }

        protected void NewMonitoredItem(NodeId startNodeId, NodeClass nodeClass, uint attributeId)
        {
            MonitoredItem = new MonitoredItem(_subscription.DefaultItem)
            {
                DisplayName = startNodeId.Identifier.ToString(),
                StartNodeId = startNodeId,
                NodeClass = nodeClass,
                SamplingInterval = 0,
                AttributeId = attributeId,
                QueueSize = 0
            };

            MonitoredItem.Notification += MonitoredItem_Notification;
        }

        private void MonitoredItem_Notification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            ReceivedEvents.Enqueue(e);
        }
    }

}