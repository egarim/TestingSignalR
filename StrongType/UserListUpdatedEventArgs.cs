using System;

namespace TestingSignalR.StrongType
{
    public class UserListUpdatedEventArgs : EventArgs
    {
        public string RoomId { get; }
        public List<UserStatus> Users { get; }

        public UserListUpdatedEventArgs(string roomId, List<UserStatus> users)
        {
            RoomId = roomId;
            Users = users;
        }
    }
}