using System.IO;

namespace System.IO
{
    internal static class StreamShim
    {
        internal static void CopyTo(this Stream source, Stream destination)
        {
            int num;
            byte[] buffer = new byte[0x14000];
            while ((num = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, num);
            }
        }
    }
}
