using System;

namespace TestingSignalR.StrongType
{
    public class RoomUpdatedEventArgs : EventArgs
    {
        public string RoomId { get; }
        public int ParticipantCount { get; }
        public DateTime Timestamp { get; }

        public RoomUpdatedEventArgs(string roomId, int participantCount, DateTime timestamp)
        {
            RoomId = roomId;
            ParticipantCount = participantCount;
            Timestamp = timestamp;
        }
    }
}