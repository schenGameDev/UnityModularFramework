public static class StringExtension {
    public static bool IsEmpty(this string str) => str == null || str.Length == 0;

    public static bool NonEmpty(this string str) => str != null && str.Length > 0;

    public static string SubstringBetween(this string str, int start, int endExclusive) {
        int len = endExclusive - start;
        if (len <= 0) return str;
        return str.Substring(start, len);
    }
}