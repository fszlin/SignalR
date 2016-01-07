namespace System.Text
{
    public static class StringBuilderShim
    {
        public static StringBuilder Clear(this StringBuilder builder)
        {
            builder.Length = 0;
            return builder;
        }
    }
}
