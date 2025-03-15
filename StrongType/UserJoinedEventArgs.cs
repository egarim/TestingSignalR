using System;

namespace TestingSignalR.StrongType
{
    public class UserJoinedEventArgs : EventArgs
    {
        public string UserId { get; }
        public string UserName { get; }
        public string RoomId { get; }
        public DateTime Timestamp { get; }

        public UserJoinedEventArgs(string userId, string userName, string roomId, DateTime timestamp)
        {
            UserId = userId;
            UserName = userName;
            RoomId = roomId;
            Timestamp = timestamp;
        }
    }
}