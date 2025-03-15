using System;

namespace TestingSignalR.StrongType
{
    // Event argument classes

    public class RoomJoinedEventArgs : EventArgs
    {
        public string RoomId { get; }
        public int ParticipantCount { get; }
        public DateTime Timestamp { get; }

        public RoomJoinedEventArgs(string roomId, int participantCount, DateTime timestamp)
        {
            RoomId = roomId;
            ParticipantCount = participantCount;
            Timestamp = timestamp;
        }
    }
}