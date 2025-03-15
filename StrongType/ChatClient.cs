using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestingSignalR.StrongType;

namespace TestingSignalR.StrongType
{
    public class ChatClient : IAsyncDisposable
    {
        private readonly HubConnection _connection;
        private bool _isConnected;
        private readonly ILogger<ChatClient> _logger;

        // Events that other classes can subscribe to
        public event EventHandler<RoomJoinedEventArgs> OnRoomJoined;
        public event EventHandler<RoomUpdatedEventArgs> OnRoomUpdated;
        public event EventHandler<RoomCreatedEventArgs> OnRoomCreated;

        public event EventHandler<UserJoinedEventArgs> OnUserJoined;
        public event EventHandler<UserLeftEventArgs> OnUserLeft;
        public event EventHandler<UserStatusChangedEventArgs> OnUserStatusChanged;
        public event EventHandler<UserDisconnectedEventArgs> OnUserDisconnected;
        public event EventHandler<UserListUpdatedEventArgs> OnUserListUpdated;

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        public event EventHandler<TypingIndicatorChangedEventArgs> OnTypingIndicatorChanged;

        public event EventHandler<ConnectionStatusChangedEventArgs> OnConnectionStatusChanged;

        public ChatClient(string hubUrl, ILogger<ChatClient> logger = null, HttpMessageHandler httpMessageHandler = null)
        {
            _logger = logger;

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    // Use the provided HttpMessageHandler if one is specified
                    if (httpMessageHandler != null)
                    {
                        options.HttpMessageHandlerFactory = _ => httpMessageHandler;
                    }
                })
                .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30) })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();

            // Set up event handlers
            RegisterEventHandlers();

            // Register connection state handlers
            _connection.Closed += OnConnectionClosed;
            _connection.Reconnecting += OnReconnecting;
            _connection.Reconnected += OnReconnected;
        }

        // Initialize and connect to the hub
        public async Task InitializeAsync()
        {
            try
            {
                await _connection.StartAsync();
                _isConnected = true;

                _logger?.LogInformation("Connected to SignalR hub.");
                OnConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(true, null));
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _logger?.LogError(ex, "Error connecting to SignalR hub.");
                OnConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, ex));
                throw;
            }
        }

        // Register all event handlers
        private void RegisterEventHandlers()
        {
            // Room events
            _connection.On<string, int, DateTime>("RoomJoined", (roomId, participantCount, timestamp) =>
            {
                _logger?.LogInformation($"Joined room {roomId} with {participantCount} participants.");
                OnRoomJoined?.Invoke(this, new RoomJoinedEventArgs(roomId, participantCount, timestamp));
            });

            _connection.On<string, int, DateTime>("RoomUpdated", (roomId, participantCount, timestamp) =>
            {
                _logger?.LogInformation($"Room {roomId} updated: {participantCount} participants.");
                OnRoomUpdated?.Invoke(this, new RoomUpdatedEventArgs(roomId, participantCount, timestamp));
            });

            _connection.On<string, string, DateTime>("RoomCreated", (roomId, roomName, timestamp) =>
            {
                _logger?.LogInformation($"New room created: {roomName} ({roomId}).");
                OnRoomCreated?.Invoke(this, new RoomCreatedEventArgs(roomId, roomName, timestamp));
            });

            // User events
            _connection.On<string, string, string, DateTime>("UserJoined", (userId, userName, roomId, timestamp) =>
            {
                _logger?.LogInformation($"User {userName} joined room {roomId}.");
                OnUserJoined?.Invoke(this, new UserJoinedEventArgs(userId, userName, roomId, timestamp));
            });

            _connection.On<string, string, string, DateTime>("UserLeft", (userId, userName, roomId, timestamp) =>
            {
                _logger?.LogInformation($"User {userName} left room {roomId}.");
                OnUserLeft?.Invoke(this, new UserLeftEventArgs(userId, userName, roomId, timestamp));
            });

            _connection.On<string, string, bool, DateTime>("UserStatusChanged", (userId, userName, isOnline, timestamp) =>
            {
                _logger?.LogInformation($"User {userName} is now {(isOnline ? "online" : "offline")}.");
                OnUserStatusChanged?.Invoke(this, new UserStatusChangedEventArgs(userId, userName, isOnline, timestamp));
            });

            _connection.On<string, string, DateTime>("UserDisconnected", (userId, userName, timestamp) =>
            {
                _logger?.LogInformation($"User {userName} disconnected.");
                OnUserDisconnected?.Invoke(this, new UserDisconnectedEventArgs(userId, userName, timestamp));
            });

            _connection.On<string, List<UserStatus>>("UserListUpdated", (roomId, users) =>
            {
                _logger?.LogInformation($"User list updated for room {roomId}. {users.Count} users.");
                OnUserListUpdated?.Invoke(this, new UserListUpdatedEventArgs(roomId, users));
            });

            // Message events
            _connection.On<ChatMessage>("ReceiveMessage", (message) =>
            {
                _logger?.LogInformation($"Message received from {message.SenderName} in room {message.RoomId}.");
                OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
            });

            _connection.On<string, string, string, bool>("TypingIndicatorChanged", (userId, userName, roomId, isTyping) =>
            {
                _logger?.LogDebug($"User {userName} is {(isTyping ? "typing" : "not typing")} in room {roomId}.");
                OnTypingIndicatorChanged?.Invoke(this, new TypingIndicatorChangedEventArgs(userId, userName, roomId, isTyping));
            });
        }

        // Handle connection state changes
        private Task OnConnectionClosed(Exception ex)
        {
            _isConnected = false;
            _logger?.LogWarning(ex, "Connection closed.");
            OnConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, ex));
            return Task.CompletedTask;
        }

        private Task OnReconnecting(Exception ex)
        {
            _isConnected = false;
            _logger?.LogWarning(ex, "Reconnecting to the hub...");
            OnConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(false, ex, true));
            return Task.CompletedTask;
        }

        private Task OnReconnected(string connectionId)
        {
            _isConnected = true;
            _logger?.LogInformation($"Reconnected to the hub with connection ID {connectionId}.");
            OnConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(true, null));
            return Task.CompletedTask;
        }

        // Client methods to call server
        public async Task JoinRoomAsync(string roomId, string userName)
        {
            EnsureConnected();
            await _connection.InvokeAsync("JoinRoom", roomId, userName);
        }

        public async Task LeaveRoomAsync(string roomId)
        {
            EnsureConnected();
            await _connection.InvokeAsync("LeaveRoom", roomId);
        }

        public async Task SendMessageAsync(string roomId, string message)
        {
            EnsureConnected();
            await _connection.InvokeAsync("SendMessage", roomId, message);
        }

        public async Task SendTypingIndicatorAsync(string roomId, bool isTyping)
        {
            EnsureConnected();
            await _connection.InvokeAsync("SendTypingIndicator", roomId, isTyping);
        }

        public async Task UpdateUserStatusAsync(bool isOnline)
        {
            EnsureConnected();
            await _connection.InvokeAsync("UpdateUserStatus", isOnline);
        }

        public async Task<List<RoomInfo>> GetAvailableRoomsAsync()
        {
            EnsureConnected();
            return await _connection.InvokeAsync<List<RoomInfo>>("GetAvailableRooms");
        }

        public async Task CreateRoomAsync(string roomName)
        {
            EnsureConnected();
            await _connection.InvokeAsync("CreateRoom", roomName);
        }

        // Helper method to check connection
        private void EnsureConnected()
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("The client is not connected to the SignalR hub. Call InitializeAsync first.");
            }
        }

        // Implement IAsyncDisposable
        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }

    // Event argument classes
    #region Event Arguments

    #endregion
}