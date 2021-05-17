namespace OpcPlc.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Bogus;
    using FluentAssertions;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public abstract class SimulatorTestsBase
    {
        private const string OpcPlcNamespaceUri = "http://microsoft.com/Opc/OpcPlc/";
        protected static readonly NodeId Server = Opc.Ua.ObjectIds.Server;
        protected static readonly NodeId ObjectsFolder = Opc.Ua.ObjectIds.ObjectsFolder;
        protected static readonly Faker Fake = new Faker();

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

        protected NodeId FindNode(NodeId startingNode, string namespaceUri, params string[] pathParts)
        {
            var ns = Session.NamespaceUris.GetIndex(namespaceUri);
            return FindNode(startingNode,
                string.Join('/', pathParts.Select(s => $"{ns}:{s}")));
        }

        protected NodeId FindNode(NodeId startingNode, string relativePath)
        {
            var browsePaths = new BrowsePathCollection
            {
                new BrowsePath
                {
                    StartingNode = startingNode,
                    RelativePath = Opc.Ua.RelativePath.Parse(relativePath, Session.TypeTree)
                }
            };

            Session.TranslateBrowsePathsToNodeIds(
                null,
                browsePaths,
                out var results,
                out var _);

            var nodeId = results
                .Should().ContainSingle()
                .Subject.Targets
                .Should().ContainSingle()
                .Subject.TargetId;
            return ToNodeId(nodeId);
        }

        protected NodeId ToNodeId(ExpandedNodeId nodeId)
        {
            var e = ExpandedNodeId.ToNodeId(nodeId, Session.NamespaceUris);
            e.Should().NotBeNull();
            return e;
        }

        protected NodeId ToNodeId(uint nodeId)
        {
            var e = ExpandedNodeId.ToNodeId(nodeId, Session.NamespaceUris);
            e.Should().NotBeNull();
            return e;
        }

    }
}