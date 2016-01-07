namespace System.Threading
{
    internal static class ManualResetEventShim
    {
        public static void Dispose(this ManualResetEvent @event)
        {
            @event.Close();
        }
    }
}
