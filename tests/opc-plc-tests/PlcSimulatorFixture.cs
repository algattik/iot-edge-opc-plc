namespace OpcPlc.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Configuration;
    using OpcPlc;
    using Serilog;

    [SetUpFixture]
    public class PlcSimulatorFixture
    {
        private TextWriter _log;

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Program.Logger = new LoggerConfiguration()
                .WriteTo.NUnitOutput()
                .CreateLogger();
            _log = TestContext.Progress;
            ServerTask = Task.Run(() => Program.MainAsync(new[] { "--autoaccept" }).GetAwaiter().GetResult());
            var endpointUrl = WaitForServerUp();
            _log.Write($"Found server at {endpointUrl}");
            Config = GetConfiguration().GetAwaiter().GetResult();
            var endpoint = CoreClientUtils.SelectEndpoint(endpointUrl, false, 15000);
            ServerEndpoint = GetServerEndpoint(endpoint, Config);
            Instance = this;
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            // TODO shutdown simulator
            ServerTask = null;
            Config = null;
            ServerEndpoint = null;
        }

        private string WaitForServerUp()
        {
            while (true)
            {
                if (ServerTask.IsFaulted)
                {
                    throw ServerTask.Exception!;
                }

                if (ServerTask.IsCompleted)
                {
                    throw new Exception("Server failed to start");
                }

                if (!Program.Ready)
                {
                    _log.WriteLine("Waiting for server to start...");
                    Thread.Sleep(1000);
                    continue;
                }

                return Program.PlcServer.GetEndpoints().First().EndpointUrl;
            }
        }

        public static PlcSimulatorFixture Instance { get; private set; }

        private Task ServerTask { get; set; }

        private ApplicationConfiguration Config { get; set; }

        private ConfiguredEndpoint ServerEndpoint { get; set; }

        public async Task<ApplicationConfiguration> GetConfiguration()
        {
            _log.WriteLine("Create an Application Configuration.");

            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationName = nameof(PlcSimulatorFixture),
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = nameof(PlcSimulatorFixture) // Defines name of *.Config.xml file read
            };

            // load the application configuration.
            ApplicationConfiguration config = await application.LoadApplicationConfiguration(false).ConfigureAwait(false);

            // check the application certificate.
            bool haveAppCertificate = await application.CheckApplicationInstanceCertificate(false, 0).ConfigureAwait(false);
            if (!haveAppCertificate)
            {
                throw new Exception("Application instance certificate invalid!");
            }

            // Note for future OpcUa update: Utils is renamed X509Utils in later versions
            config.ApplicationUri = Utils.GetApplicationUriFromCertificate(config.SecurityConfiguration.ApplicationCertificate.Certificate);

            // Auto-accept server certificate
            config.CertificateValidator.CertificateValidation += CertificateValidator_AutoAccept;

            return config;
        }

        public ConfiguredEndpoint GetServerEndpoint(EndpointDescription endpoint, ApplicationConfiguration config)
        {
            var endpointConfiguration = EndpointConfiguration.Create(config);
            return new ConfiguredEndpoint(null, endpoint, endpointConfiguration);
        }

        public Task<Session> CreateSessionAsync(string sessionName)
        {
            _log.WriteLine("Create a session with OPC UA server.");
            var userIdentity = new UserIdentity(new AnonymousIdentityToken());
            return Session.Create(Config, ServerEndpoint, false, sessionName, 60000, userIdentity, null);
        }

        private void CertificateValidator_AutoAccept(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                e.Accept = true;
            }
        }
    }
}