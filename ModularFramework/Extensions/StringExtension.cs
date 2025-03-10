public static class StringExtension {
    public static bool IsEmpty(this string str) => str == null || str.Length == 0;

    public static bool NonEmpty(this string str) => str != null && str.Length > 0;
}