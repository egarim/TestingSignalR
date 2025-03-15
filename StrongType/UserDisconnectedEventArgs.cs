using System;

namespace TestingSignalR.StrongType
{
    public class UserDisconnectedEventArgs : EventArgs
    {
        public string UserId { get; }
        public string UserName { get; }
        public DateTime Timestamp { get; }

        public UserDisconnectedEventArgs(string userId, string userName, DateTime timestamp)
        {
            UserId = userId;
            UserName = userName;
            Timestamp = timestamp;
        }
    }
}