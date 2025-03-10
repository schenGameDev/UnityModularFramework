using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumExtension {
    public static IEnumerable<T> GetEnumValues<T>() {
		return System.Enum.GetValues(typeof(T)).Cast<T>();
	}

	public static T GetEnumValue<T>(string name) {
		return (T) Enum.Parse(typeof(T), name);
	}
}