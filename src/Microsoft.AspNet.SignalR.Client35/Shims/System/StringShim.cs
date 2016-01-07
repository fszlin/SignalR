namespace System
{
    internal static class StringShim
    {
        public static bool IsNullOrWhiteSpace(string value)
        {
#if NET35_PORT
            return value == null || value.Trim().Length == 0;
#else
            return string.IsNullOrWhiteSpace(value);
#endif
        }
    }
}
