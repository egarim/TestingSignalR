﻿using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestingSignalR.StrongType
{
    // Strongly-typed SignalR Hub
    public class ChatHub : Hub<IChatClient>
    {
        // Dictionary to track which rooms each user is in
        private static readonly Dictionary<string, HashSet<string>> _userRoomMap = new Dictionary<string, HashSet<string>>();

        // Dictionary to track room information
        private static readonly Dictionary<string, RoomInfo> _roomInfoMap = new Dictionary<string, RoomInfo>();

        // Dictionary to track user information
        private static readonly Dictionary<string, UserStatus> _userStatusMap = new Dictionary<string, UserStatus>();

        // Dictionary to track streaming messages
        private static readonly Dictionary<string, ChatMessage> _streamingMessages = new Dictionary<string, ChatMessage>();

        // Join a chat room
        public async Task JoinRoom(string roomId, string userName)
        {
            var connectionId = Context.ConnectionId;

            // Add user to the room
            await Groups.AddToGroupAsync(connectionId, roomId);

            // Update user-room mapping
            if (!_userRoomMap.ContainsKey(connectionId))
            {
                _userRoomMap[connectionId] = new HashSet<string>();
            }
            _userRoomMap[connectionId].Add(roomId);

            // Create or update room info
            if (!_roomInfoMap.ContainsKey(roomId))
            {
                _roomInfoMap[roomId] = new RoomInfo
                {
                    RoomId = roomId,
                    RoomName = roomId, // Default name is the ID
                    CreatedAt = DateTime.UtcNow
                };
            }
            _roomInfoMap[roomId].UserIds.Add(connectionId);

            // Update user status
            var userStatus = new UserStatus
            {
                UserId = connectionId,
                UserName = userName,
                IsOnline = true,
                LastActivity = DateTime.UtcNow
            };
            _userStatusMap[connectionId] = userStatus;

            // Notify everyone in the room that a new user joined
            await Clients.Group(roomId).UserJoined(connectionId, userName, roomId, DateTime.UtcNow);

            // Send the current room info to the user who just joined
            await Clients.Caller.RoomJoined(roomId, _roomInfoMap[roomId].UserIds.Count, DateTime.UtcNow);

            // Send the list of users in the room to the new user
            List<UserStatus> usersInRoom = new List<UserStatus>();
            foreach (var userId in _roomInfoMap[roomId].UserIds)
            {
                if (_userStatusMap.ContainsKey(userId))
                {
                    usersInRoom.Add(_userStatusMap[userId]);
                }
            }

            await Clients.Caller.UserListUpdated(roomId, usersInRoom);
        }

        // Leave a chat room
        public async Task LeaveRoom(string roomId)
        {
            var connectionId = Context.ConnectionId;

            // Remove user from the room
            await Groups.RemoveFromGroupAsync(connectionId, roomId);

            // Get user name before updating mappings
            string userName = _userStatusMap.ContainsKey(connectionId) ? _userStatusMap[connectionId].UserName : "Unknown User";

            // Update user-room mapping
            if (_userRoomMap.ContainsKey(connectionId))
            {
                _userRoomMap[connectionId].Remove(roomId);
            }

            // Update room info
            if (_roomInfoMap.ContainsKey(roomId))
            {
                _roomInfoMap[roomId].UserIds.Remove(connectionId);

                // Notify others that user left
                await Clients.Group(roomId).UserLeft(connectionId, userName, roomId, DateTime.UtcNow);

                // Update room participants count
                await Clients.Group(roomId).RoomUpdated(roomId, _roomInfoMap[roomId].UserIds.Count, DateTime.UtcNow);
            }
        }

        // Send a message to a room
        public async Task SendMessage(string roomId, string message)
        {
            var connectionId = Context.ConnectionId;

            if (!_userStatusMap.ContainsKey(connectionId))
            {
                throw new HubException("User not found. Please join the chat first.");
            }

            var userName = _userStatusMap[connectionId].UserName;
            var timestamp = DateTime.UtcNow;

            // Create the message object
            var chatMessage = new ChatMessage
            {
                SenderId = connectionId,
                SenderName = userName,
                RoomId = roomId,
                Content = message,
                Timestamp = timestamp,
                MessageId = Guid.NewGuid().ToString(),
                IsStreaming = false,
                IsComplete = true
            };

            // Send the message to all clients in the room
            await Clients.Group(roomId).ReceiveMessage(chatMessage);

            // Update user's last activity
            if (_userStatusMap.ContainsKey(connectionId))
            {
                _userStatusMap[connectionId].LastActivity = timestamp;
            }
        }

        // Start streaming a message
        public async Task StartStreamingMessage(string roomId)
        {
            var connectionId = Context.ConnectionId;

            if (!_userStatusMap.ContainsKey(connectionId))
            {
                throw new HubException("User not found. Please join the chat first.");
            }

            var userName = _userStatusMap[connectionId].UserName;
            var timestamp = DateTime.UtcNow;
            var messageId = Guid.NewGuid().ToString();

            // Create a streaming message
            var streamingMessage = new ChatMessage
            {
                MessageId = messageId,
                SenderId = connectionId,
                SenderName = userName,
                RoomId = roomId,
                Content = string.Empty,
                Timestamp = timestamp,
                IsStreaming = true,
                IsComplete = false
            };

            // Store the message for later updates
            _streamingMessages[messageId] = streamingMessage;

            // Send initial streaming message to all clients in the room
            await Clients.Group(roomId).ReceiveStreamingMessage(streamingMessage);

            // Update user's last activity
            _userStatusMap[connectionId].LastActivity = timestamp;
        }

        // Update streaming message content
        public async Task UpdateStreamingMessage(string messageId, string content)
        {
            var connectionId = Context.ConnectionId;

            if (!_streamingMessages.ContainsKey(messageId))
            {
                throw new HubException("Streaming message not found.");
            }

            var message = _streamingMessages[messageId];

            // Check if the sender is updating their own message
            if (message.SenderId != connectionId)
            {
                throw new HubException("You can only update your own messages.");
            }

            // Update the message content
            message.Content = content;

            // Send the updated streaming message to all clients in the room
            await Clients.Group(message.RoomId).ReceiveStreamingMessage(message);
        }

        // Complete a streaming message
        public async Task CompleteStreamingMessage(string messageId)
        {
            var connectionId = Context.ConnectionId;

            if (!_streamingMessages.ContainsKey(messageId))
            {
                throw new HubException("Streaming message not found.");
            }

            var message = _streamingMessages[messageId];

            // Check if the sender is completing their own message
            if (message.SenderId != connectionId)
            {
                throw new HubException("You can only complete your own messages.");
            }

            // Mark the message as complete
            message.IsComplete = true;
            message.IsStreaming = false;

            // Send the final message to all clients in the room
            await Clients.Group(message.RoomId).ReceiveStreamingMessage(message);

            // Remove the message from the streaming dictionary
            _streamingMessages.Remove(messageId);
        }

        // Send a message with attachments
        public async Task SendMessageWithAttachments(string roomId, string message, List<Attachment> attachments)
        {
            var connectionId = Context.ConnectionId;

            if (!_userStatusMap.ContainsKey(connectionId))
            {
                throw new HubException("User not found. Please join the chat first.");
            }

            var userName = _userStatusMap[connectionId].UserName;
            var timestamp = DateTime.UtcNow;
            var messageId = Guid.NewGuid().ToString();

            // Create the message object
            var chatMessage = new ChatMessage
            {
                SenderId = connectionId,
                SenderName = userName,
                RoomId = roomId,
                Content = message,
                Timestamp = timestamp,
                MessageId = messageId,
                IsStreaming = false,
                IsComplete = true,
                Attachments = attachments ?? new List<Attachment>()
            };

            // Send the message to all clients in the room
            await Clients.Group(roomId).ReceiveMessage(chatMessage);

            // Update user's last activity
            if (_userStatusMap.ContainsKey(connectionId))
            {
                _userStatusMap[connectionId].LastActivity = timestamp;
            }
        }

        // Add an attachment to an existing message
        public async Task AddAttachmentToMessage(string messageId, Attachment attachment)
        {
            var connectionId = Context.ConnectionId;

            // Check if it's a streaming message
            if (_streamingMessages.ContainsKey(messageId))
            {
                var message = _streamingMessages[messageId];

                // Check if the sender is updating their own message
                if (message.SenderId != connectionId)
                {
                    throw new HubException("You can only add attachments to your own messages.");
                }

                // Add the attachment
                message.Attachments.Add(attachment);

                // Notify about the new attachment
                await Clients.Group(message.RoomId).ReceiveAttachment(messageId, attachment);

                // Also update the streaming message content
                await Clients.Group(message.RoomId).ReceiveStreamingMessage(message);
            }
            else
            {
                throw new HubException("Message not found or not a streaming message.");
            }
        }

        // Send a typing indicator
        public async Task SendTypingIndicator(string roomId, bool isTyping)
        {
            var connectionId = Context.ConnectionId;

            if (!_userStatusMap.ContainsKey(connectionId))
            {
                return;
            }

            var userName = _userStatusMap[connectionId].UserName;

            // Send the typing indicator to all users in the room except the sender
            await Clients.GroupExcept(roomId, connectionId).TypingIndicatorChanged(connectionId, userName, roomId, isTyping);
        }

        // Update a user's status
        public async Task UpdateUserStatus(bool isOnline)
        {
            var connectionId = Context.ConnectionId;

            if (!_userStatusMap.ContainsKey(connectionId))
            {
                return;
            }

            _userStatusMap[connectionId].IsOnline = isOnline;
            _userStatusMap[connectionId].LastActivity = DateTime.UtcNow;

            var userName = _userStatusMap[connectionId].UserName;

            // Notify all rooms that the user is in about the status change
            if (_userRoomMap.ContainsKey(connectionId))
            {
                foreach (var roomId in _userRoomMap[connectionId])
                {
                    await Clients.Group(roomId).UserStatusChanged(connectionId, userName, isOnline, DateTime.UtcNow);
                }
            }
        }

        // Get a list of available rooms
        public Task<List<RoomInfo>> GetAvailableRooms()
        {
            return Task.FromResult(new List<RoomInfo>(_roomInfoMap.Values));
        }

        // Override the disconnect method to handle disconnection
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var connectionId = Context.ConnectionId;

            // Get user information
            if (_userStatusMap.TryGetValue(connectionId, out var userStatus))
            {
                // Mark the user as offline
                userStatus.IsOnline = false;
                userStatus.LastActivity = DateTime.UtcNow;

                // Handle all rooms the user was in
                if (_userRoomMap.TryGetValue(connectionId, out var rooms))
                {
                    foreach (var roomId in rooms)
                    {
                        // Notify others in the room that the user has disconnected
                        await Clients.Group(roomId).UserDisconnected(connectionId, userStatus.UserName, DateTime.UtcNow);

                        // Remove user from room
                        if (_roomInfoMap.ContainsKey(roomId))
                        {
                            _roomInfoMap[roomId].UserIds.Remove(connectionId);

                            // Update room participants count
                            await Clients.Group(roomId).RoomUpdated(roomId, _roomInfoMap[roomId].UserIds.Count, DateTime.UtcNow);
                        }
                    }

                    // Clean up the user's room memberships
                    _userRoomMap.Remove(connectionId);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Create a new room
        public async Task CreateRoom(string roomName)
        {
            var roomId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow;

            _roomInfoMap[roomId] = new RoomInfo
            {
                RoomId = roomId,
                RoomName = roomName,
                CreatedAt = timestamp
            };

            // Notify all connected clients about the new room
            await Clients.All.RoomCreated(roomId, roomName, timestamp);
        }
    }
}