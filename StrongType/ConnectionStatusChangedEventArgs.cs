using System;

namespace TestingSignalR.StrongType
{
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public Exception Exception { get; }
        public bool IsReconnecting { get; }

        public ConnectionStatusChangedEventArgs(bool isConnected, Exception exception, bool isReconnecting = false)
        {
            IsConnected = isConnected;
            Exception = exception;
            IsReconnecting = isReconnecting;
        }
    }
}