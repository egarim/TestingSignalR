using System;

namespace TestingSignalR.StrongType
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public ChatMessage Message { get; }

        public MessageReceivedEventArgs(ChatMessage message)
        {
            Message = message;
        }
    }
}