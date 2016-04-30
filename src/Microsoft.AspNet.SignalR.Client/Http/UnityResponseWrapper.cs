using System.IO;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class UnityResponseWrapper : IResponse
    {

        private readonly UnityHttpResponse httpResponse;

        public UnityResponseWrapper(UnityHttpResponse httpResponse)
        {
            this.httpResponse = httpResponse;
        }

        public void Dispose()
        {
        }

        public Stream GetStream()
        {
            return new MemoryStream(this.httpResponse.bytes);
        }
    }
}
