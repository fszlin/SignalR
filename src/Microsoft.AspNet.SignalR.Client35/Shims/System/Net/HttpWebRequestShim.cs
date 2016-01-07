namespace System.Net
{
    internal static class HttpWebRequestShim
    {
        public static DateTime GetDate(this HttpWebRequest request)
        {
            return request.GetDateHeaderHelper(HttpKnownHeaderNames.Date);
        }
        public static void SetDate(this HttpWebRequest request, DateTime value)
        {
            request.SetDateHeaderHelper(HttpKnownHeaderNames.Date, value);
        }

        private static DateTime GetDateHeaderHelper(this HttpWebRequest request, string headerName)
        {
            var text = request.Headers[headerName];
            if (text == null)
            {
                return DateTime.MinValue;
            }

            return HttpProtocolUtils.string2date(text);
        }

        private static void SetDateHeaderHelper(this HttpWebRequest request, string headerName, DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                request.SetSpecialHeaders(headerName, null); // remove header
            else
                request.SetSpecialHeaders(headerName, HttpProtocolUtils.date2string(dateTime));
        }

        private static void SetSpecialHeaders(this HttpWebRequest request, string headerName, string value)
        {
            if (value == null)
            {
                request.Headers.Remove(headerName);
            }

            value = value.Trim();
            if (value.Length == 0)
            {
                request.Headers.Remove(headerName);
            }
            else
            {
                request.Headers[headerName] = value;

            }
        }
    }
}

