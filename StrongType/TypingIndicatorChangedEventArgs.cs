using System;

namespace TestingSignalR.StrongType
{
    public class TypingIndicatorChangedEventArgs : EventArgs
    {
        public string UserId { get; }
        public string UserName { get; }
        public string RoomId { get; }
        public bool IsTyping { get; }

        public TypingIndicatorChangedEventArgs(string userId, string userName, string roomId, bool isTyping)
        {
            UserId = userId;
            UserName = userName;
            RoomId = roomId;
            IsTyping = isTyping;
        }
    }
}