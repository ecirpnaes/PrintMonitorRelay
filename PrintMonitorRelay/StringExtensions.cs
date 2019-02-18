namespace PrintMonitorRelay
{
    public static class StringExtensions
    {
        public static bool IsEmpty(object input)
        {
            return input == null || IsEmpty(input.ToString());
        }

        public static bool IsEmpty(this string input)
        {
            return string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input);
        }
    }
}