using System;
using System.Collections.Generic;

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

        // New properties for attachments and streaming
        public bool IsStreaming { get; set; }
        public bool IsComplete { get; set; }
        public string MessageId { get; set; } // To track message parts in streaming
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }

    // New class for attachments
    public class Attachment
    {
        public string AttachmentId { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}