using System;
using System.Linq;

public static class TypeExtension
{
    /// <summary>
    /// Checks if a given type inherits or implements a specified base type.
    /// base must not specify generic type paramter
    /// </summary>
    /// <param name="type">The type which needs to be checked.</param>
    /// <param name="baseType">The generic type/interface which is expected to be inherited or implemented by the 'type'</param>
    /// <returns>Return true if 'type' inherits or implements 'baseType'. False otherwise</returns>
    public static bool InheritsOrImplements(this Type type, Type baseType) {
        if(baseType == type) return true;
        type = ResolveGenericType(type);
        baseType = ResolveGenericType(baseType);

        while (type != typeof(object)) {
            if(baseType == type || HasAnyInterfaces(type, baseType)) return true;

            type = ResolveGenericType(type.BaseType);
            if(type == null) return false;
        }

        return false;
    }
    static Type ResolveGenericType(Type type) {
        if(type is not { IsGenericType: true}) return type;

        var genericType = type.GetGenericTypeDefinition();
        return genericType != type? genericType : type;
    }

    static bool HasAnyInterfaces(Type type, Type interfaceType) {
        return type.GetInterfaces()
            .Any(i => i==interfaceType || ResolveGenericType(i) == interfaceType);
    }
}
