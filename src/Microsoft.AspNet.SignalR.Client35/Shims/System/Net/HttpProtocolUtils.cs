using System.Globalization;

namespace System.Net
{ //
  // HttpProtocolUtils - A collection of utility functions for HTTP usage.
  //

    internal class HttpProtocolUtils
    {

        private HttpProtocolUtils()
        {
        }

        //
        // extra buffers for build/parsing, recv/send HTTP data,
        //  at some point we should consolidate
        //


        // parse String to DateTime format.
        internal static DateTime string2date(String S)
        {
            DateTime dtOut;
            if (HttpDateParse.ParseHttpDate(S, out dtOut))
            {
                return dtOut;
            }
            else {
                throw new ProtocolViolationException(SR.GetString(SR.net_baddate));
            }

        }

        // convert Date to String using RFC 1123 pattern
        internal static string date2string(DateTime D)
        {
            DateTimeFormatInfo dateFormat = new DateTimeFormatInfo();
            return D.ToUniversalTime().ToString("R", dateFormat);
        }
    }

}
