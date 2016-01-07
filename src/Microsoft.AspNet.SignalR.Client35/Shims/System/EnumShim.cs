namespace System
{
    internal static class EnumShim
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
