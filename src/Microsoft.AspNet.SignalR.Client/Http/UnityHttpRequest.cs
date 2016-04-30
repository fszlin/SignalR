using Microsoft.AspNet.SignalR.Infrastructure;
using SslStream35;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public class UnityHttpRequest
    {

        public static bool LogAllRequests = false;
        public static bool VerboseLogging = false;
        //public static string unityVersion = Application.unityVersion;
        //public static string operatingSystem = SystemInfo.operatingSystem;

        public CookieContainer cookieJar;
        public string method = "GET";
        public string protocol = "HTTP/1.1";
        public Stream byteStream;
        public Uri uri;
        public static byte[] EOL = { (byte)'\r', (byte)'\n' };
        public UnityHttpResponse response = null;
        public bool isDone = false;
        public int maximumRetryCount = 8;
        public bool acceptGzip = true;
        public bool useCache = false;
        public Exception exception = null;
        public UnityHttpRequestState state = UnityHttpRequestState.Waiting;
        public long responseTime = 0; // in milliseconds
        public bool synchronous = false;
        public int bufferSize = 4 * 1024;
        private bool aborted = false;

        public Action<UnityHttpRequest> completedCallback = null;

        Dictionary<string, List<string>> headers = new Dictionary<string, List<string>>();
        static Dictionary<string, string> etags = new Dictionary<string, string>();

        public UnityHttpRequest(string method, string uri)
        {
            this.method = method;
            this.uri = new Uri(uri);
        }

        public UnityHttpRequest(string method, string uri, bool useCache)
        {
            this.method = method;
            this.uri = new Uri(uri);
            this.useCache = useCache;
        }

        public UnityHttpRequest(string method, string uri, IDictionary<string, string> data)
        {
            this.method = method;
            this.uri = new Uri(uri);

            byte[] buffer = ProcessPostData(data);
            this.byteStream = new MemoryStream(buffer);
            this.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        }

        public UnityHttpRequest(string method, string uri, byte[] bytes)
        {
            this.method = method;
            this.uri = new Uri(uri);
            this.byteStream = new MemoryStream(bytes);
        }

        //        public Request(string method, string uri, StreamedWWWForm form)
        //        {
        //            this.method = method;
        //            this.uri = new Uri(uri);
        //            this.byteStream = form.stream;
        //            foreach (DictionaryEntry entry in form.headers)
        //            {
        //                this.AddHeader((string)entry.Key, (string)entry.Value);
        //            }
        //        }

        //        public Request(string method, string uri, WWWForm form)
        //        {
        //            this.method = method;
        //            this.uri = new Uri(uri);
        //            this.byteStream = new MemoryStream(form.data);
        //#if UNITY_5
        //            foreach ( var entry in form.headers )
        //            {
        //                this.AddHeader( entry.Key, entry.Value );
        //            }
        //#else
        //            foreach (DictionaryEntry entry in form.headers)
        //            {
        //                this.AddHeader((string)entry.Key, (string)entry.Value);
        //            }
        //#endif
        //        }

        //public Request(string method, string uri, Hashtable data)
        //{
        //    this.method = method;
        //    this.uri = new Uri(uri);
        //    this.byteStream = new MemoryStream(Encoding.UTF8.GetBytes(JSON.JsonEncode(data)));
        //    this.AddHeader("Content-Type", "application/json");
        //}

        public void Abort()
        {
            this.aborted = true;
        }

        public void AddHeader(string name, string value)
        {
            name = name.ToLower().Trim();
            value = value.Trim();
            if (!headers.ContainsKey(name))
                headers[name] = new List<string>();
            headers[name].Add(value);
        }

        public string GetHeader(string name)
        {
            name = name.ToLower().Trim();
            if (!headers.ContainsKey(name))
                return "";
            return headers[name][0];
        }

        public List<string> GetHeaders()
        {
            List<string> result = new List<string>();
            foreach (string name in headers.Keys)
            {
                foreach (string value in headers[name])
                {
                    result.Add(name + ": " + value);
                }
            }

            return result;
        }

        public List<string> GetHeaders(string name)
        {
            name = name.ToLower().Trim();
            if (!headers.ContainsKey(name))
                headers[name] = new List<string>();
            return headers[name];
        }

        public void SetHeader(string name, string value)
        {
            name = name.ToLower().Trim();
            value = value.Trim();
            if (!headers.ContainsKey(name))
                headers[name] = new List<string>();
            headers[name].Clear();
            headers[name].TrimExcess();
            headers[name].Add(value);
        }

        private void GetResponse()
        {
            System.Diagnostics.Stopwatch curcall = new System.Diagnostics.Stopwatch();
            curcall.Start();
            try
            {

                var retry = 0;
                while (++retry < maximumRetryCount)
                {
                    if (useCache)
                    {
                        string etag = "";
                        if (etags.TryGetValue(uri.AbsoluteUri, out etag))
                        {
                            SetHeader("If-None-Match", etag);
                        }
                    }

                    SetHeader("Host", uri.Host);

                    var client = new TcpClient();
                    client.Connect(uri.Host, uri.Port);
                    using (var stream = client.GetStream())
                    {
                        var ostream = stream as Stream;
                        if (uri.Scheme.ToLower() == "https")
                        {
                            var tlsStream = new TlsStream(stream, false);
                            tlsStream.Connect();
                            ostream = tlsStream;
                        }

                        if (WriteToStream(ostream))
                        {
                            response = new UnityHttpResponse();
                            response.request = this;
                            state = UnityHttpRequestState.Reading;
                            response.ReadFromStream(ostream);
                        }
                        else
                        {
                            return;
                        }
                    }
                    client.Close();

                    switch (response.status)
                    {
                        case 307:
                        case 302:
                        case 301:
                            uri = new Uri(response.GetHeader("Location"));
                            continue;
                        default:
                            retry = maximumRetryCount;
                            break;
                    }
                }
                if (useCache)
                {
                    string etag = response.GetHeader("etag");
                    if (etag.Length > 0)
                        etags[uri.AbsoluteUri] = etag;
                }

            }
            catch (Exception e)
            {
#if !UNITY_EDITOR
                Console.WriteLine("Unhandled Exception, aborting request.");
                Console.WriteLine(e);
#else
                Debug.LogError("Unhandled Exception, aborting request.");
                Debug.LogException(e);
#endif
                exception = e;
                response = null;
            }

            state = UnityHttpRequestState.Done;
            isDone = true;
            responseTime = curcall.ElapsedMilliseconds;

            if (byteStream != null)
            {
                byteStream.Close();
            }

            if (completedCallback != null)
            {
                completedCallback(this);
                //if (synchronous)
                //{
                //    completedCallback(this);
                //}
                //else
                //{
                //    // we have to use this dispatcher to avoid executing the callback inside this worker thread
                //    ResponseCallbackDispatcher.Singleton.requests.Enqueue(this);
                //}
            }

            if (LogAllRequests)
            {
#if !UNITY_EDITOR
                System.Console.WriteLine("NET: " + InfoString(VerboseLogging));
#else
                if ( response != null && response.status >= 200 && response.status < 300 )
                {
                    Debug.Log( InfoString( VerboseLogging ) );
                }
                else if ( response != null && response.status >= 400 )
                {
                    Debug.LogError( InfoString( VerboseLogging ) );
                }
                else
                {
                    Debug.LogWarning( InfoString( VerboseLogging ) );
                }
#endif
            }
        }

        public virtual void Send(Action<UnityHttpRequest> callback = null)
        {

            //if (!synchronous && callback != null && ResponseCallbackDispatcher.Singleton == null)
            //{
            //    ResponseCallbackDispatcher.Init();
            //}

            completedCallback = callback;

            isDone = false;
            state = UnityHttpRequestState.Waiting;
            if (acceptGzip)
            {
                SetHeader("Accept-Encoding", "gzip");
            }

            if (this.cookieJar != null)
            {
                var cookies = this.cookieJar.GetCookies(uri);
                string cookieString = this.GetHeader("cookie");
                for (int cookieIndex = 0; cookieIndex < cookies.Count; ++cookieIndex)
                {
                    if (cookieString.Length > 0 && cookieString[cookieString.Length - 1] != ';')
                    {
                        cookieString += ';';
                    }
                    cookieString += cookies[cookieIndex].Name + '=' + cookies[cookieIndex].Value + ';';
                }
                SetHeader("cookie", cookieString);
            }

            if (byteStream != null && byteStream.Length > 0 && GetHeader("Content-Length") == "")
            {
                SetHeader("Content-Length", byteStream.Length.ToString());
            }

            if (GetHeader("User-Agent") == "")
            {
                SetHeader("User-Agent", "UnityWeb/1.0");
                //try
                //{
                //    SetHeader("User-Agent", "UnityWeb/1.0 (Unity " + Request.unityVersion + "; " + Request.operatingSystem + ")");
                //}
                //catch (Exception)
                //{
                //    SetHeader("User-Agent", "UnityWeb/1.0");
                //}
            }

            if (GetHeader("Connection") == "")
            {
                SetHeader("Connection", "close");
            }

            // Basic Authorization
            if (!String.IsNullOrEmpty(uri.UserInfo))
            {
                SetHeader("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(uri.UserInfo)));
            }

            if (synchronous)
            {
                GetResponse();
            }
            else
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object t) {
                    GetResponse();
                }));
            }
        }

        public string Text
        {
            set { byteStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value)); }
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
#if !UNITY_EDITOR
            System.Console.WriteLine("NET: SSL Cert: " + sslPolicyErrors.ToString());
#else
            Debug.LogWarning("SSL Cert Error: " + sslPolicyErrors.ToString ());
#endif
            return true;
        }

        private bool WriteToStream(Stream outputStream)
        {
            var stream = new BinaryWriter(outputStream);
            stream.Write(ASCIIEncoding.ASCII.GetBytes(method.ToUpper() + " " + uri.PathAndQuery + " " + protocol));
            stream.Write(EOL);

            foreach (string name in headers.Keys)
            {
                foreach (string value in headers[name])
                {
                    stream.Write(ASCIIEncoding.ASCII.GetBytes(name));
                    stream.Write(':');
                    stream.Write(ASCIIEncoding.ASCII.GetBytes(value));
                    stream.Write(EOL);
                }
            }

            stream.Write(EOL);

            if (byteStream == null)
            {
                return true;
            }

            long numBytesToRead = byteStream.Length;
            byte[] buffer = new byte[bufferSize];
            while (numBytesToRead > 0)
            {
                if (this.aborted)
                {
                    return false;
                }

                int readed = byteStream.Read(buffer, 0, bufferSize);
                stream.Write(buffer, 0, readed);
                numBytesToRead -= readed;
            }

            return true;
        }

        private static string[] sizes = { "B", "KB", "MB", "GB" };
        public string InfoString(bool verbose)
        {
            string status = isDone && response != null ? response.status.ToString() : "---";
            string message = isDone && response != null ? response.message : "Unknown";
            double size = isDone && response != null && response.bytes != null ? response.bytes.Length : 0.0f;

            int order = 0;
            while (size >= 1024.0f && order + 1 < sizes.Length)
            {
                ++order;
                size /= 1024.0f;
            }

            string sizeString = String.Format("{0:0.##}{1}", size, sizes[order]);

            string result = uri.ToString() + " [ " + method.ToUpper() + " ] [ " + status + " " + message + " ] [ " + sizeString + " ] [ " + responseTime + "ms ]";

            if (verbose && response != null)
            {
                result += "\n\nRequest Headers:\n\n" + String.Join("\n", GetHeaders().ToArray());
                result += "\n\nResponse Headers:\n\n" + String.Join("\n", response.GetHeaders().ToArray());

                if (response.Text != null)
                {
                    result += "\n\nResponse Body:\n" + response.Text;
                }
            }

            return result;
        }

        public static byte[] ProcessPostData(IDictionary<string, string> postData)
        {
            if (postData == null || postData.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            foreach (var pair in postData)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }

                if (String.IsNullOrEmpty(pair.Value))
                {
                    continue;
                }

                sb.AppendFormat("{0}={1}", pair.Key, UrlEncoder.UrlEncode(pair.Value));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
