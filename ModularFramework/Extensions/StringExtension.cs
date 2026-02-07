using System.Runtime.CompilerServices;

public static class StringExtension {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(this string str) => string.IsNullOrEmpty(str);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBlank(this string str) => string.IsNullOrWhiteSpace(str);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NonEmpty(this string str) => !str.IsEmpty();

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