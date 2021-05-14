namespace OpcPlc.Tests
{
    using NUnit.Framework;
    using Opc.Ua.Client;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class SimulatorTestsBase
    {
        protected Session Session;

        [OneTimeSetUp]
        public void Setup()
        {
            Session = PlcSimulatorFixture.Instance.CreateSessionAsync(GetType().Name).GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Session.Close();
        }
    }
}