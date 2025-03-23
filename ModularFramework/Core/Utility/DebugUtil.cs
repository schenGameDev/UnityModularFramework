using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModularFramework.Utility
{
    using static EnvironmentConstants;
    public static class DebugUtil {

        public enum DebugType {LOG,
            WARNING,
            ERROR}
        public enum LogLevel {
            RUNTIME = 0,
            DEBUG = 1
        }

        public static void Print(string callerName, DebugType type, LogLevel level, string message) {
            bool suppressed = (int)level > DebugLevel;
            if(suppressed) return;
            message = callerName + "::" + message;
            switch(type) {
                case DebugType.LOG:
                    Debug.Log(message);
                    break;
                case DebugType.WARNING:
                    Debug.LogWarning(message);
                    break;
                case DebugType.ERROR:
                    Debug.LogError(message);
                    break;
            }
        }
    #region String
        private static void DebugLog(string message, string callerName) {
            Print(callerName,DebugType.LOG, LogLevel.DEBUG, message);
        }

        private static void DebugWarn(string message, string callerName) {
            Print(callerName,DebugType.WARNING, LogLevel.DEBUG, message);
        }

        private static void DebugError(string message, string callerName) {
            Print(callerName,DebugType.ERROR, LogLevel.DEBUG, message);
        }

        private static void Log(string message, string callerName) {
            Print(callerName,DebugType.LOG, LogLevel.RUNTIME, message);
        }

        private static void Warn(string message, string callerName) {
            Print(callerName,DebugType.WARNING, LogLevel.RUNTIME, message);
        }

        private static void Error( string message, string callerName) {
            Print(callerName,DebugType.ERROR, LogLevel.RUNTIME, message);
        }
    #endregion
    #region Object
        public static void DebugLog<T>(T obj, [CallerMemberName] string callerName = "") => DebugLog(obj.ToString(), callerName);
        public static void DebugWarn<T>(T obj, [CallerMemberName] string callerName = "") => DebugWarn(obj.ToString(), callerName);
        public static void DebugError<T>(T obj, [CallerMemberName] string callerName = "") => DebugError(obj.ToString(), callerName);
        public static void Log<T>(T obj, [CallerMemberName] string callerName = "") => Log(obj.ToString(), callerName);
        public static void Warn<T>(T obj, [CallerMemberName] string callerName = "") => Warn(obj.ToString(), callerName);
        public static void Error<T>(T obj, [CallerMemberName] string callerName = "") => Error(obj.ToString(), callerName);
    #endregion
    #region Enumerable
        public static void DebugLog<T>(IEnumerable<T> obj, [CallerMemberName] string callerName = "") => DebugLog(obj.Join(), callerName);
        public static void DebugWarn<T>(IEnumerable<T> obj, [CallerMemberName] string callerName = "") => DebugWarn(obj.Join(), callerName);
        public static void DebugError<T>(IEnumerable<T> obj, [CallerMemberName] string callerName = "") => DebugError(obj.Join(), callerName);
        public static void Log<T>(IEnumerable<T> obj, [CallerMemberName] string callerName = "") => Log(obj.Join(), callerName);
        public static void Warn<T>(IEnumerable<T> obj, [CallerMemberName] string callerName = "") => Warn(obj.Join(), callerName);
        public static void Error<T>(IEnumerable<T> obj, [CallerMemberName] string callerName = "") => Error(obj.Join(), callerName);

        public static void DebugLog<T>(List<T> obj, [CallerMemberName] string callerName = "") => DebugLog(obj.Join(), callerName);
        public static void DebugWarn<T>(List<T> obj, [CallerMemberName] string callerName = "") => DebugWarn(obj.Join(), callerName);
        public static void DebugError<T>(List<T> obj, [CallerMemberName] string callerName = "") => DebugError(obj.Join(), callerName);
        public static void Log<T>(List<T> obj, [CallerMemberName] string callerName = "") => Log(obj.Join(), callerName);
        public static void Warn<T>(List<T> obj, [CallerMemberName] string callerName = "") => Warn(obj.Join(), callerName);
        public static void Error<T>(List<T> obj, [CallerMemberName] string callerName = "") => Error(obj.Join(), callerName);

        public static void DebugLog<T>(HashSet<T> obj, [CallerMemberName] string callerName = "") => DebugLog(obj.Join(), callerName);
        public static void DebugWarn<T>(HashSet<T> obj, [CallerMemberName] string callerName = "") => DebugWarn(obj.Join(), callerName);
        public static void DebugError<T>(HashSet<T> obj, [CallerMemberName] string callerName = "") => DebugError(obj.Join(), callerName);
        public static void Log<T>(HashSet<T> obj, [CallerMemberName] string callerName = "") => Log(obj.Join(), callerName);
        public static void Warn<T>(HashSet<T> obj, [CallerMemberName] string callerName = "") => Warn(obj.Join(), callerName);
        public static void Error<T>(HashSet<T> obj, [CallerMemberName] string callerName = "") => Error(obj.Join(), callerName);

        public static void DebugLog<X,Y>(Dictionary<X,Y> obj, [CallerMemberName] string callerName = "") => DebugLog(obj.Join(), callerName);
        public static void DebugWarn<X,Y>(Dictionary<X,Y> obj, [CallerMemberName] string callerName = "") => DebugWarn(obj.Join(), callerName);
        public static void DebugError<X,Y>(Dictionary<X,Y> obj, [CallerMemberName] string callerName = "") => DebugError(obj.Join(), callerName);
        public static void Log<X,Y>(Dictionary<X,Y> obj, [CallerMemberName] string callerName = "") => Log(obj.Join(), callerName);
        public static void Warn<X,Y>(Dictionary<X,Y> obj, [CallerMemberName] string callerName = "") => Warn(obj.Join(), callerName);
        public static void Error<X,Y>(Dictionary<X,Y> obj, [CallerMemberName] string callerName = "") => Error(obj.Join(), callerName);
    #endregion
    }
}