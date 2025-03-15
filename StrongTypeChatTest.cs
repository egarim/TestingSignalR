using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using NUnit;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace TestingSignalR
{
    public class StrongTypeChatTest
    {
        [SetUp]
        [SuppressMessage("Async/await", "CRR0033:The void async method should be in a try/catch block", Justification = "<Pending>")]
        public async void Setup()
        {
            // ARRANGE
            // Build a test server
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.UseStartup<Startup>();
                });

            var host = await hostBuilder.StartAsync();

            //Create a test server
            var server = host.GetTestServer();

            // Create SignalR connection
            var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost/chathub", options =>
                {
                    // Set up the connection to use the test server
                    options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                })
                .Build();
        }

   
    }
}
