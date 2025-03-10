using System.Collections.Generic;
using System.Linq;

public static class HashSetExtension {
    public static T Remove<T>(this ISet<T> collection)
    {
        var t = collection.First();
        collection.Remove(t);
        return t;
    }
}