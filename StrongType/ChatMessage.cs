using System;

namespace TestingSignalR.StrongType
{
    // Message model
    public class ChatMessage
    {
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string RoomId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}