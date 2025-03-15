using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace TestingSignalR.StrongType
{
    // Strongly-typed client interface for SignalR
    public interface IChatClient
    {
        // Room events
        Task RoomJoined(string roomId, int participantCount, DateTime timestamp);
        Task RoomUpdated(string roomId, int participantCount, DateTime timestamp);
        Task RoomCreated(string roomId, string roomName, DateTime timestamp);

        // User events
        Task UserJoined(string userId, string userName, string roomId, DateTime timestamp);
        Task UserLeft(string userId, string userName, string roomId, DateTime timestamp);
        Task UserStatusChanged(string userId, string userName, bool isOnline, DateTime timestamp);
        Task UserDisconnected(string userId, string userName, DateTime timestamp);
        Task UserListUpdated(string roomId, List<UserStatus> users);

        // Message events
        Task ReceiveMessage(ChatMessage message);
        Task TypingIndicatorChanged(string userId, string userName, string roomId, bool isTyping);

        // New methods for streaming and attachments
        Task ReceiveStreamingMessage(ChatMessage message);
        Task ReceiveAttachment(string messageId, Attachment attachment);
    }
}