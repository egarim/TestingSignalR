using System;

namespace TestingSignalR.StrongType
{
    // User status model
    public class UserStatus
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastActivity { get; set; }
    }
}