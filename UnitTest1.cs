using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using NUnit;
using NUnit.Framework;

namespace TestingSignalR
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task ShouldSendAndReceiveMessage()
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

            string receivedUser = null;
            string receivedMessage = null;

            // Set up a handler for received messages
            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                receivedUser = user;
                receivedMessage = message;
            });

            // ACT
            // Start the connection
            await connection.StartAsync();

            // Send a test message through the hub
            await connection.InvokeAsync("SendMessage", "TestUser", "Hello SignalR");

            // Wait a moment for the message to be processed
            await Task.Delay(100);

            // ASSERT
            // Verify the message was received correctly
            Assert.That("TestUser"==receivedUser);
            Assert.That("Hello SignalR"== receivedMessage);

            // Clean up
            await connection.DisposeAsync();
            await host.StopAsync();
           
          
        }
    }
}
