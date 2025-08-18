using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumExtension {
    public static IEnumerable<T> GetEnumValues<T>() {
		return Enum.GetValues(typeof(T)).Cast<T>();
	}

	public static T GetEnumValue<T>(this string name) {
		return (T) Enum.Parse(typeof(T), name);
	}
	
	public static string GetName(this Enum eff)
	{
		return Enum.GetName(eff.GetType(), eff);
	}
}