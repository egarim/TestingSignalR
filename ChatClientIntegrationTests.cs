using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;


using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TestingSignalR;
using TestingSignalR.StrongType;

namespace TestingSignalR
{
    [TestFixture]
    public class ChatClientIntegrationTests
    {
        private IHost _host;
        private TestServer _server;
        private ChatClient _client1;
        private ChatClient _client2;
        private const string TestRoomId = "test-room";
        private readonly string _hubUrl = "http://localhost/chatHub";

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {

            // Setup test server
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {



                        services.AddControllers();
                        // Register core services
                        services.AddSignalR();
                        services.AddLogging();
                   
                        // Register ObjectSpace services
                  

                        // Register chat services
                     
                    });

                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHub<ChatHub>("/chathub");
                            endpoints.MapControllers();
                        });
                    });
                });

            var host = hostBuilder.Start();

            _server = host.GetTestServer();
            


            _host = host;
            _server = _server;
        }

        [SetUp]
        public async Task SetUp()
        {
            // Create clients with test server handler
            var handler = _server.CreateHandler();

            // Create a factory method to initialize the clients with the test server
            Func<ChatClient> clientFactory = () =>
            {
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger<ChatClient>();

                // Create a client that uses the test server handler
                return new ChatClient(_hubUrl, logger, handler);
                
            };

            // Create and initialize the clients
            _client1 = clientFactory();
            _client2 = clientFactory();

            await _client1.InitializeAsync();
            await _client2.InitializeAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            // Dispose the clients after each test
            if (_client1 != null)
            {
                await _client1.DisposeAsync();
                _client1 = null;
            }

            if (_client2 != null)
            {
                await _client2.DisposeAsync();
                _client2 = null;
            }
            
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            // Shut down the host
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _host = null;
            }
            _server?.Dispose();
        }

        [Test]
        public async Task Client_JoinRoom_ShouldRaiseOnRoomJoinedEvent()
        {
            // Arrange
            var roomJoinedTcs = new TaskCompletionSource<string>();

            _client1.OnRoomJoined += (sender, e) =>
            {
                roomJoinedTcs.TrySetResult(e.RoomId);
            };

            // Act
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");

            // Wait for the event with timeout
            var joinedRoomId = await roomJoinedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(joinedRoomId, Is.EqualTo(TestRoomId));
        }

        [Test]
        public async Task Client_SendMessage_ShouldRaiseOnMessageReceivedEvent()
        {
            // Arrange
            var messageReceivedTcs = new TaskCompletionSource<ChatMessage>();

            // Join rooms
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to message event on client2
            _client2.OnMessageReceived += (sender, e) =>
            {
                messageReceivedTcs.TrySetResult(e.Message);
            };

            // The message to send
            const string testMessage = "Hello from Client1!";

            // Act - Client1 sends a message
            await _client1.SendMessageAsync(TestRoomId, testMessage);

            // Wait for Client2 to receive the message with timeout
            var receivedMessage = await messageReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(receivedMessage, Is.Not.Null);
            Assert.That(receivedMessage.Content, Is.EqualTo(testMessage));
            Assert.That(receivedMessage.SenderName, Is.EqualTo("TestUser1"));
            Assert.That(receivedMessage.RoomId, Is.EqualTo(TestRoomId));
        }

        [Test]
        public async Task Client_SendTypingIndicator_ShouldRaiseOnTypingIndicatorChangedEvent()
        {
            // Arrange
            var typingIndicatorTcs = new TaskCompletionSource<(string userName, bool isTyping)>();

            // Join rooms
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to typing event on client2
            _client2.OnTypingIndicatorChanged += (sender, e) =>
            {
                typingIndicatorTcs.TrySetResult((e.UserName, e.IsTyping));
            };

            // Act - Client1 sends typing indicator
            await _client1.SendTypingIndicatorAsync(TestRoomId, true);

            // Wait for Client2 to receive the indicator with timeout
            var typingResult = await typingIndicatorTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(typingResult.userName, Is.EqualTo("TestUser1"));
            Assert.That(typingResult.isTyping, Is.True);
        }

        [Test]
        public async Task Client_UserJoinsLater_ShouldRaiseOnUserJoinedEvent()
        {
            // Arrange
            var userJoinedTcs = new TaskCompletionSource<(string userName, string roomId)>();

            // Client1 joins first
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");

            // Subscribe to user joined event on client1
            _client1.OnUserJoined += (sender, e) =>
            {
                userJoinedTcs.TrySetResult((e.UserName, e.RoomId));
            };

            // Act - Client2 joins later
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            Thread.Sleep(200); // Wait for the event to be raised

            // Wait for Client1 to receive the notification with timeout
            var joinResult = await userJoinedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(joinResult.userName, Is.EqualTo("TestUser2"));
            Assert.That(joinResult.roomId, Is.EqualTo(TestRoomId));
        }

        [Test]
        public async Task Client_UserLeaves_ShouldRaiseOnUserLeftEvent()
        {
            // Arrange
            var userLeftTcs = new TaskCompletionSource<(string userName, string roomId)>();

            // Both clients join
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to user left event on client1
            _client1.OnUserLeft += (sender, e) =>
            {
                userLeftTcs.TrySetResult((e.UserName, e.RoomId));
            };

            // Act - Client2 leaves
            await _client2.LeaveRoomAsync(TestRoomId);

            // Wait for Client1 to receive the notification with timeout
            var leftResult = await userLeftTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(leftResult.userName, Is.EqualTo("TestUser2"));
            Assert.That(leftResult.roomId, Is.EqualTo(TestRoomId));
        }

        [Test]
        public async Task Client_GetAvailableRooms_ShouldReturnRooms()
        {
            // Arrange - Create a room
            const string testRoomName = "Test Room for Listing";
            await _client1.CreateRoomAsync(testRoomName);

            // Act - Get available rooms
            var availableRooms = await _client1.GetAvailableRoomsAsync();

            // Assert
            Assert.That(availableRooms, Is.Not.Null);
            Assert.That(availableRooms.Count, Is.GreaterThan(0));
            Assert.That(availableRooms.Exists(r => r.RoomName == testRoomName), Is.True);
        }

        [Test]
        public async Task Client_CreateRoom_ShouldRaiseOnRoomCreatedEvent()
        {
            // Arrange
            var roomCreatedTcs = new TaskCompletionSource<(string roomName, string roomId)>();

            // Subscribe to room created event
            _client1.OnRoomCreated += (sender, e) =>
            {
                roomCreatedTcs.TrySetResult((e.RoomName, e.RoomId));
            };

            // Act - Create a room
            const string testRoomName = "New Integration Test Room";
            await _client1.CreateRoomAsync(testRoomName);

            // Wait for the event with timeout
            var roomResult = await roomCreatedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(roomResult.roomName, Is.EqualTo(testRoomName));
            Assert.That(roomResult.roomId, Is.Not.Null);
        }

        [Test]
        public async Task Client_UpdateUserStatus_ShouldRaiseOnUserStatusChangedEvent()
        {
            // Arrange
            var statusChangedTcs = new TaskCompletionSource<(string userName, bool isOnline)>();

            // Both clients join the room
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to status change event on client2
            _client2.OnUserStatusChanged += (sender, e) =>
            {
                statusChangedTcs.TrySetResult((e.UserName, e.IsOnline));
            };

            // Act - Client1 updates status
            await _client1.UpdateUserStatusAsync(false);

            // Wait for Client2 to receive the status change with timeout
            var statusResult = await statusChangedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(statusResult.userName, Is.EqualTo("TestUser1"));
            Assert.That(statusResult.isOnline, Is.False);
        }

        [Test]
        public async Task Client_UserListUpdated_ShouldGetUserList()
        {
            // Arrange
            var userListTcs = new TaskCompletionSource<List<UserStatus>>();

            // Client2 joins first
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to user list event on client1
            _client1.OnUserListUpdated += (sender, e) =>
            {
                userListTcs.TrySetResult(e.Users);
            };

            // Act - Client1 joins and should receive the user list
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");

            // Wait for Client1 to receive the user list with timeout
            var users = await userListTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count, Is.GreaterThanOrEqualTo(2)); // At least both test users

            // Verify both users are in the list
            Assert.That(users.Exists(u => u.UserName == "TestUser1"), Is.True);
            Assert.That(users.Exists(u => u.UserName == "TestUser2"), Is.True);
        }

        [Test]
        public async Task Client_Disconnect_ShouldRaiseOnUserDisconnectedEvent()
        {
            // Arrange
            var disconnectTcs = new TaskCompletionSource<string>();

            // Both clients join the room
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to disconnect event on client2
            _client2.OnUserDisconnected += (sender, e) =>
            {
                disconnectTcs.TrySetResult(e.UserName);
            };

            // Act - Dispose client1 which should trigger disconnect
            await _client1.DisposeAsync();
            _client1 = null; // Prevent double disposal in TearDown

            // Wait for Client2 to receive the disconnect notification with timeout
            var disconnectedUserName = await disconnectTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(disconnectedUserName, Is.EqualTo("TestUser1"));
        }


        [Test]
        public async Task Client_StreamingMessage_ShouldSendUpdatesAndComplete()
        {
            // Arrange
            var streamStartTcs = new TaskCompletionSource<string>();
            var streamUpdateTcs = new TaskCompletionSource<string>();
            var streamCompleteTcs = new TaskCompletionSource<bool>();
            string messageId = null;

            // Both clients join the room
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to streaming events on client2
            _client2.OnStreamingMessageReceived += (sender, e) =>
            {
                if (messageId == null)
                {
                    // First streaming message received
                    messageId = e.Message.MessageId;
                    streamStartTcs.TrySetResult(messageId);
                }
                else if (messageId == e.Message.MessageId && !string.IsNullOrEmpty(e.Message.Content) && e.Message.IsStreaming)
                {
                    // Update to streaming message
                    streamUpdateTcs.TrySetResult(e.Message.Content);
                }
                else if (messageId == e.Message.MessageId && !e.Message.IsStreaming && e.Message.IsComplete)
                {
                    // Streaming complete
                    streamCompleteTcs.TrySetResult(true);
                }
            };

            // Act - Start streaming
            await _client1.StartStreamingMessageAsync(TestRoomId);

            // Wait for the stream to start
            messageId = await streamStartTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(messageId, Is.Not.Null, "Failed to receive stream start notification");

            // Update the stream with content
            const string partialContent = "This is a partial message...";
            await _client1.UpdateStreamingMessageAsync(messageId, partialContent);

            // Wait for stream update
            var receivedContent = await streamUpdateTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(receivedContent, Is.EqualTo(partialContent), "Failed to receive streaming update");

            // Complete the stream
            await _client1.CompleteStreamingMessageAsync(messageId);

            // Wait for stream completion
            var completed = await streamCompleteTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(completed, Is.True, "Failed to receive stream completion notification");
        }

        [Test]
        public async Task Client_SendMessageWithAttachments_ShouldReceiveMessageWithAttachments()
        {
            // Arrange
            var messageReceivedTcs = new TaskCompletionSource<ChatMessage>();

            // Join rooms
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to message event on client2
            _client2.OnMessageReceived += (sender, e) =>
            {
                if (e.Message.Attachments != null && e.Message.Attachments.Count > 0)
                {
                    messageReceivedTcs.TrySetResult(e.Message);
                }
            };

            // Create test message and attachment
            const string testMessage = "Here's a file for you!";
            var attachments = new List<Attachment>
    {
        new Attachment
        {
            FileName = "test-document.pdf",
            FileUrl = "https://example.com/files/test-document.pdf",
            ContentType = "application/pdf",
            FileSize = 1024 * 1024  // 1MB
        }
    };

            // Act - Client1 sends a message with attachment
            await _client1.SendMessageWithAttachmentsAsync(TestRoomId, testMessage, attachments);

            // Wait for Client2 to receive the message with timeout
            var receivedMessage = await messageReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(receivedMessage, Is.Not.Null);
            Assert.That(receivedMessage.Content, Is.EqualTo(testMessage));
            Assert.That(receivedMessage.SenderName, Is.EqualTo("TestUser1"));
            Assert.That(receivedMessage.Attachments, Is.Not.Null);
            Assert.That(receivedMessage.Attachments.Count, Is.EqualTo(1));
            Assert.That(receivedMessage.Attachments[0].FileName, Is.EqualTo("test-document.pdf"));
            Assert.That(receivedMessage.Attachments[0].FileUrl, Is.EqualTo("https://example.com/files/test-document.pdf"));
        }

        [Test]
        public async Task Client_AddAttachmentToStreamingMessage_ShouldReceiveAttachment()
        {
            // Arrange
            var streamStartTcs = new TaskCompletionSource<string>();
            var attachmentReceivedTcs = new TaskCompletionSource<Attachment>();
            string messageId = null;

            // Both clients join the room
            await _client1.JoinRoomAsync(TestRoomId, "TestUser1");
            await _client2.JoinRoomAsync(TestRoomId, "TestUser2");

            // Subscribe to streaming events on client2
            _client2.OnStreamingMessageReceived += (sender, e) =>
            {
                if (messageId == null)
                {
                    // First streaming message received
                    messageId = e.Message.MessageId;
                    streamStartTcs.TrySetResult(messageId);
                }
            };

            // Subscribe to attachment events on client2
            _client2.OnAttachmentReceived += (sender, e) =>
            {
                attachmentReceivedTcs.TrySetResult(e.Attachment);
            };

            // Act - Start streaming
            await _client1.StartStreamingMessageAsync(TestRoomId);

            // Wait for the stream to start
            messageId = await streamStartTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(messageId, Is.Not.Null, "Failed to receive stream start notification");

            // Create an attachment
            var attachment = new Attachment
            {
                FileName = "streaming-image.jpg",
                FileUrl = "https://example.com/files/streaming-image.jpg",
                ContentType = "image/jpeg",
                FileSize = 512 * 1024  // 512KB
            };

            // Add attachment to the streaming message
            await _client1.AddAttachmentToMessageAsync(messageId, attachment);

            // Wait for attachment received notification
            var receivedAttachment = await attachmentReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.That(receivedAttachment, Is.Not.Null);
            Assert.That(receivedAttachment.FileName, Is.EqualTo("streaming-image.jpg"));
            Assert.That(receivedAttachment.FileUrl, Is.EqualTo("https://example.com/files/streaming-image.jpg"));

            // Complete the stream
            await _client1.CompleteStreamingMessageAsync(messageId);
        }

    }

  
}