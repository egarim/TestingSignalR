using System;

namespace TestingSignalR.StrongType
{
    public class UserStatusChangedEventArgs : EventArgs
    {
        public string UserId { get; }
        public string UserName { get; }
        public bool IsOnline { get; }
        public DateTime Timestamp { get; }

        public UserStatusChangedEventArgs(string userId, string userName, bool isOnline, DateTime timestamp)
        {
            UserId = userId;
            UserName = userName;
            IsOnline = isOnline;
            Timestamp = timestamp;
        }
    }
}