public static class StringExtension {
    public static bool IsEmpty(this string str) => string.IsNullOrEmpty(str);
    
    public static bool IsBlank(this string str) => string.IsNullOrWhiteSpace(str);

    public static bool NonEmpty(this string str) => !IsEmpty(str);

    public static string SubstringBetween(this string str, int start, int endExclusive) {
        int len = endExclusive - start;
        if (len <= 0) return str;
        return str.Substring(start, len);
    }
    
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => null,
            "" => input,
            _ => input[0].ToString().ToUpper() + input.Substring(1)
        };
}