namespace OpcPlc.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class SimulatorTestsBase
    {
        private const string OpcPlcNamespaceUri = "http://microsoft.com/Opc/OpcPlc/";
        
        protected Session Session;

        [OneTimeSetUp]
        public async Task Setup()
        {
            Session = await PlcSimulatorFixture.Instance.CreateSessionAsync(GetType().Name);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Session.Close();
        }

        protected NodeId GetOpcPlcNodeId(string identifier)
        {
            var nodeId = NodeId.Create(
                identifier,
                OpcPlcNamespaceUri,
                Session.NamespaceUris);
            return nodeId;
        }
    }
}