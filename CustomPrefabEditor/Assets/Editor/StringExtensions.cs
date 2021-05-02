using System.Globalization;

namespace Editor
{
    public static class StringExtensions
    {
        public static bool ContainsIgnoreCase(this string val, string match)
        {
            if (string.IsNullOrWhiteSpace(match))
            {
                return false;
            }

            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(val, match, CompareOptions.IgnoreCase) >= 0;
        }
    }
}