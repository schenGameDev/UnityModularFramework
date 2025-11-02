using System;

public static class ArrayExtension {
    public static int IndexOf<T>(this T[] array, T value)
    {
        return Array.IndexOf(array, value);
    }

    public static void BlockCopy<T>(this Span<T> source, int sourceIndex, Span<T> destination, int destinationIndex,
        int count)
    {
        source.Slice(sourceIndex, count).CopyTo(destination.Slice(destinationIndex));
    }
    public static void BlockCopy<T>(this T[] sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex,
        int count)
    {
        sourceArray.AsSpan().BlockCopy(sourceIndex, destinationArray.AsSpan(), destinationIndex, count);
    }
}