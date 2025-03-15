using System;

namespace TestingSignalR.StrongType
{
    // Room information model
    public class RoomInfo
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public List<string> UserIds { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }
}