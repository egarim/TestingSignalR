using System;

namespace TestingSignalR.StrongType
{
    public class RoomCreatedEventArgs : EventArgs
    {
        public string RoomId { get; }
        public string RoomName { get; }
        public DateTime Timestamp { get; }

        public RoomCreatedEventArgs(string roomId, string roomName, DateTime timestamp)
        {
            RoomId = roomId;
            RoomName = roomName;
            Timestamp = timestamp;
        }
    }
}