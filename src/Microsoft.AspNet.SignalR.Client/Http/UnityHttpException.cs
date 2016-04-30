using System;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class UnityHttpException : Exception
    {
        public UnityHttpException(string message) : base(message)
        {
        }
    }
}
