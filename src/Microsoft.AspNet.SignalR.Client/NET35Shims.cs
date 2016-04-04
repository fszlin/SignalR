using System.IO;

namespace System.Net.WebSockets
{
    public enum WebSocketMessageType
    {
        //
        // Summary:
        //     The message is clear text.
        Text = 0,
        //
        // Summary:
        //     The message is in binary format.
        Binary = 1,
        //
        // Summary:
        //     A receive has completed because a close message was received.
        Close = 2
    }
}

namespace System
{
    public static class StringShim
    {
        public static bool IsNullOrWhiteSpace(string str)
        {
            return string.IsNullOrEmpty(str) || char.IsWhiteSpace(str[0]) || char.IsWhiteSpace(str[str.Length - 1]);
        }
    }
}

namespace System.IO
{
    internal static class Extensions
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


namespace System
{
    public delegate void Action<in T1, in T2, in T3, in T4, in T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
    public delegate void Action<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);

    public interface IObservable<out T>
    {
        IDisposable Subscribe(IObserver<T> observer);
    }
    public interface IObserver<in T>
    {
        void OnNext(T value);
        void OnError(Exception error);
        void OnCompleted();
    }

    public static class ExtensionMethods
    {
        public static bool HasFlag<T>(this Enum enumerated, T value) where T : struct
        {
            if (enumerated.GetType() != typeof(T))
            {
                throw new ArgumentOutOfRangeException(@"The value " + value + " does not belong to " + enumerated.GetType().Name);
            }
            if (Enum.IsDefined(typeof(T), value) == false)
            {
                return false;
            }
            return true;
        }
    }
}